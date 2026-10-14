using System;
using System.Collections.Generic;
using System.Net;
using Fireflies.Config;
using Fireflies.Net.Messages;
using Fireflies.Net.Protocol;
using Fireflies.Net.Transport;
using Fireflies.Sync;

namespace Fireflies.Net.Connection
{
    public sealed class ClientNetwork : IDisposable
    {
        private readonly NetworkSettings _settings;
        private readonly UdpTransport _transport;
        private readonly List<DecodedPacket> _deliverable = new();
        private RemotePeer _hostPeer;
        private JoinRequestMessage _joinRequest;
        private double _connectStartedTime;
        private double _nextJoinAttemptTime;

        public ClientNetwork(NetworkSettings settings)
        {
            _settings = settings;
            _transport = UdpTransport.FromSettings(settings);
            Clock = new NetClock(settings.ClockResyncThreshold);
        }

        public event Action<JoinAcceptedMessage> Connected;
        public event Action<JoinRejectReason> Rejected;
        public event Action ConnectionAttemptTimedOut;
        public event Action<DisconnectReason> Disconnected;
        public event Action<DecodedPacket> PacketReceived;

        public ClientConnectionState State { get; private set; }
        public NetClock Clock { get; }
        public int RoundTripMs => _hostPeer?.RoundTripMs ?? 0;

        public void Connect(IPEndPoint hostEndPoint, string playerName, byte preferredSlot, double now)
        {
            if (State != ClientConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Already connected or connecting.");
            }

            if (!PlayerNameRules.IsValid(playerName))
            {
                throw new ArgumentException("Invalid player name.", nameof(playerName));
            }

            _transport.Open(0);
            _hostPeer = new RemotePeer(_transport, hostEndPoint, now);
            _joinRequest = new JoinRequestMessage(ProtocolConstants.ProtocolVersion, playerName, preferredSlot);
            _connectStartedTime = now;
            _nextJoinAttemptTime = now;
            State = ClientConnectionState.Connecting;
        }

        public void Poll(double now)
        {
            if (State == ClientConnectionState.Disconnected)
            {
                return;
            }

            ReceiveAll(now);
            if (State == ClientConnectionState.Connecting)
            {
                RetryJoin(now);
            }
            else if (State == ClientConnectionState.Connected)
            {
                _hostPeer.ResendUnacknowledged(now, _settings.ReliableResendInterval);
                DetectHostLoss(now);
            }
        }

        public void SendUnreliable(INetMessage message)
        {
            if (State == ClientConnectionState.Connected)
            {
                _hostPeer.SendUnreliable(message);
            }
        }

        public void Dispose()
        {
            // También al cancelar mientras conecta: el host pudo haber registrado ya al jugador.
            if (State != ClientConnectionState.Disconnected)
            {
                byte[] goodbye = PacketCodec.EncodeUnreliable(new DisconnectMessage(DisconnectReason.ClientLeft));
                _transport.SendRepeated(goodbye, _hostPeer.EndPoint, _settings.DisconnectNoticeRepeats);
            }

            Close();
        }

        private void ReceiveAll(double now)
        {
            while (State != ClientConnectionState.Disconnected && _transport.TryReceive(now, out ReceivedDatagram datagram))
            {
                bool isFromHost = datagram.Sender.Equals(_hostPeer.EndPoint);
                if (!isFromHost || !PacketCodec.TryDecode(datagram.Bytes, out DecodedPacket packet))
                {
                    continue;
                }

                _deliverable.Clear();
                _hostPeer.HandleIncoming(packet, now, _deliverable);
                foreach (DecodedPacket deliverable in _deliverable)
                {
                    RouteHostPacket(deliverable, now);
                }
            }
        }

        private void RouteHostPacket(DecodedPacket packet, double now)
        {
            if (State == ClientConnectionState.Disconnected)
            {
                return;
            }

            switch (packet.Type)
            {
                case PacketType.JoinAccepted:
                    HandleJoinAccepted(packet);
                    break;
                case PacketType.JoinRejected:
                    HandleJoinRejected(packet);
                    break;
                case PacketType.Ping:
                    HandlePing(packet, now);
                    break;
                case PacketType.Disconnect:
                    HandleDisconnect(packet);
                    break;
                default:
                    if (State == ClientConnectionState.Connected)
                    {
                        PacketReceived?.Invoke(packet);
                    }

                    break;
            }
        }

        private void HandleJoinAccepted(DecodedPacket packet)
        {
            if (State != ClientConnectionState.Connecting)
            {
                return;
            }

            if (!PacketCodec.TryReadPayload(packet, JoinAcceptedMessage.Read, out JoinAcceptedMessage accepted))
            {
                return;
            }

            State = ClientConnectionState.Connected;
            Connected?.Invoke(accepted);
        }

        private void HandleJoinRejected(DecodedPacket packet)
        {
            if (State != ClientConnectionState.Connecting)
            {
                return;
            }

            if (!PacketCodec.TryReadPayload(packet, JoinRejectedMessage.Read, out JoinRejectedMessage rejected))
            {
                return;
            }

            Close();
            Rejected?.Invoke(rejected.Reason);
        }

        private void HandlePing(DecodedPacket packet, double now)
        {
            if (!PacketCodec.TryReadPayload(packet, PingMessage.Read, out PingMessage ping))
            {
                return;
            }

            _hostPeer.SendUnreliable(new PongMessage(ping.HostTime));
            _hostPeer.SetRoundTrip(ping.LastRoundTripMs);
            Clock.AddSample(ping.HostTime, ping.LastRoundTripMs, now);
        }

        private void HandleDisconnect(DecodedPacket packet)
        {
            if (!PacketCodec.TryReadPayload(packet, DisconnectMessage.Read, out DisconnectMessage notice))
            {
                return;
            }

            Close();
            Disconnected?.Invoke(notice.Reason);
        }

        private void RetryJoin(double now)
        {
            if (now - _connectStartedTime > _settings.JoinTimeout)
            {
                Close();
                ConnectionAttemptTimedOut?.Invoke();
                return;
            }

            if (now >= _nextJoinAttemptTime)
            {
                _nextJoinAttemptTime = now + _settings.JoinRetryInterval;
                _transport.Send(PacketCodec.EncodeUnreliable(_joinRequest), _hostPeer.EndPoint);
            }
        }

        private void DetectHostLoss(double now)
        {
            if (now - _hostPeer.LastHeardTime > _settings.ConnectionTimeout)
            {
                Close();
                Disconnected?.Invoke(DisconnectReason.TimedOut);
            }
        }

        private void Close()
        {
            State = ClientConnectionState.Disconnected;
            _transport.Dispose();
        }
    }
}
