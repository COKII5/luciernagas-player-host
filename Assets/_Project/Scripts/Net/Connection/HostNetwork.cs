using System;
using System.Collections.Generic;
using System.Net;
using Fireflies.Config;
using Fireflies.Net.Messages;
using Fireflies.Net.Protocol;
using Fireflies.Net.Transport;

namespace Fireflies.Net.Connection
{
    public sealed class HostNetwork : IDisposable
    {
        private readonly NetworkSettings _settings;
        private readonly UdpTransport _transport;
        private readonly PlayerConnectionRegistry _registry = new();
        private readonly JoinRequestValidator _joinValidator;
        private readonly List<DecodedPacket> _deliverable = new();
        private readonly List<ConnectedClient> _clientsSnapshot = new();
        private double _nextHeartbeatTime;

        public HostNetwork(NetworkSettings settings, BanList banList)
        {
            _settings = settings;
            BanList = banList;
            _joinValidator = new JoinRequestValidator(banList, settings.MaxPlayers);
            NetworkConditionSimulator simulator = new NetworkConditionSimulator(settings.SimulatedLatencyMs, settings.SimulatedPacketLossPercent);
            _transport = new UdpTransport(simulator);
        }

        public event Action<ConnectedClient> ClientJoined;
        public event Action<ConnectedClient, DisconnectReason> ClientLeft;
        public event Action<ConnectedClient, DecodedPacket> GameplayPacketReceived;

        public BanList BanList { get; }
        public int LocalPort => _transport.LocalPort;

        public void Start(int port)
        {
            _transport.Open(port);
        }

        public void Poll(double now)
        {
            ReceiveAll(now);
            ResendReliable(now);
            SendHeartbeats(now);
            DropTimedOutClients(now);
        }

        public bool TryGetClient(byte playerId, out ConnectedClient client)
        {
            return _registry.TryGetByPlayerId(playerId, out client);
        }

        public void SendReliable(ConnectedClient client, INetMessage message, double now)
        {
            client.Peer.SendReliable(message, now);
        }

        public void BroadcastReliable(INetMessage message, double now)
        {
            foreach (ConnectedClient client in _registry.Clients)
            {
                client.Peer.SendReliable(message, now);
            }
        }

        public void BroadcastUnreliable(INetMessage message)
        {
            byte[] datagram = PacketCodec.EncodeUnreliable(message);
            foreach (ConnectedClient client in _registry.Clients)
            {
                _transport.Send(datagram, client.Peer.EndPoint);
            }
        }

        public void Kick(byte playerId, DisconnectReason reason)
        {
            if (!_registry.TryGetByPlayerId(playerId, out ConnectedClient client))
            {
                return;
            }

            SendRepeatedNotice(client.Peer.EndPoint, new DisconnectMessage(reason));
            RemoveClient(client, reason);
        }

        public void Ban(byte playerId)
        {
            if (!_registry.TryGetByPlayerId(playerId, out ConnectedClient client))
            {
                return;
            }

            BanList.Add(client.Peer.EndPoint.Address);
            Kick(playerId, DisconnectReason.Banned);
        }

        public void Dispose()
        {
            DisconnectMessage notice = new DisconnectMessage(DisconnectReason.HostClosed);
            foreach (ConnectedClient client in _registry.Clients)
            {
                SendRepeatedNotice(client.Peer.EndPoint, notice);
            }

            _transport.Dispose();
        }

        private void ReceiveAll(double now)
        {
            while (_transport.TryReceive(now, out ReceivedDatagram datagram))
            {
                if (!PacketCodec.TryDecode(datagram.Bytes, out DecodedPacket packet))
                {
                    continue;
                }

                if (_registry.TryGetByEndPoint(datagram.Sender, out ConnectedClient client))
                {
                    HandleClientPacket(client, packet, now);
                }
                else if (packet.Type == PacketType.JoinRequest)
                {
                    HandleJoinRequest(datagram.Sender, packet, now);
                }
            }
        }

        private void HandleClientPacket(ConnectedClient client, DecodedPacket packet, double now)
        {
            _deliverable.Clear();
            client.Peer.HandleIncoming(packet, now, _deliverable);
            foreach (DecodedPacket deliverable in _deliverable)
            {
                if (!client.IsConnected)
                {
                    return;
                }

                RouteClientPacket(client, deliverable, now);
            }
        }

        // Un cliente solo puede mandar estos tipos; cualquier otro (p. ej. un Snapshot falso) se ignora.
        private void RouteClientPacket(ConnectedClient client, DecodedPacket packet, double now)
        {
            switch (packet.Type)
            {
                case PacketType.Pong when PacketCodec.TryReadPayload(packet, PongMessage.Read, out PongMessage pong):
                    client.Peer.RecordRoundTrip(pong.EchoedHostTime, now);
                    break;
                case PacketType.Disconnect:
                    RemoveClient(client, DisconnectReason.ClientLeft);
                    break;
                case PacketType.Input:
                    GameplayPacketReceived?.Invoke(client, packet);
                    break;
            }
        }

        private void HandleJoinRequest(IPEndPoint sender, DecodedPacket packet, double now)
        {
            if (!PacketCodec.TryReadPayload(packet, JoinRequestMessage.Read, out JoinRequestMessage request))
            {
                return;
            }

            string playerName = PlayerNameRules.Normalize(request.PlayerName);
            JoinRejectReason? rejection = _joinValidator.FindRejection(sender, request, playerName, _registry.Count);
            if (rejection.HasValue)
            {
                _transport.Send(PacketCodec.EncodeUnreliable(new JoinRejectedMessage(rejection.Value)), sender);
                return;
            }

            RemotePeer peer = new RemotePeer(_transport, sender, now);
            ConnectedClient client = _registry.Register(peer, playerName, request.PreferredSlot);
            ClientJoined?.Invoke(client);
        }

        private void ResendReliable(double now)
        {
            foreach (ConnectedClient client in _registry.Clients)
            {
                client.Peer.ResendUnacknowledged(now, _settings.ReliableResendInterval);
            }
        }

        private void SendHeartbeats(double now)
        {
            if (now < _nextHeartbeatTime)
            {
                return;
            }

            _nextHeartbeatTime = now + _settings.HeartbeatInterval;
            foreach (ConnectedClient client in _registry.Clients)
            {
                client.Peer.SendUnreliable(new PingMessage(now, (ushort)client.Peer.RoundTripMs));
            }
        }

        private void DropTimedOutClients(double now)
        {
            _clientsSnapshot.Clear();
            _clientsSnapshot.AddRange(_registry.Clients);
            foreach (ConnectedClient client in _clientsSnapshot)
            {
                if (now - client.Peer.LastHeardTime > _settings.ConnectionTimeout)
                {
                    RemoveClient(client, DisconnectReason.TimedOut);
                }
            }
        }

        private void RemoveClient(ConnectedClient client, DisconnectReason reason)
        {
            _registry.Remove(client);
            ClientLeft?.Invoke(client, reason);
        }

        private void SendRepeatedNotice(IPEndPoint target, INetMessage notice)
        {
            byte[] datagram = PacketCodec.EncodeUnreliable(notice);
            for (int i = 0; i < _settings.DisconnectNoticeRepeats; i++)
            {
                _transport.Send(datagram, target);
            }
        }
    }
}
