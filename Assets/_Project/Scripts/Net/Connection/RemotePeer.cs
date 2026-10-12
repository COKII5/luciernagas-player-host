using System;
using System.Collections.Generic;
using System.Net;
using Fireflies.Net.Protocol;
using Fireflies.Net.Reliability;
using Fireflies.Net.Transport;

namespace Fireflies.Net.Connection
{
    // El host tiene uno por cliente y el cliente uno para el host, así ambos lados comparten el ACK/reenvío.
    public sealed class RemotePeer
    {
        private const float RoundTripSmoothing = 0.25f;

        private readonly UdpTransport _transport;
        private readonly ReliableChannel _reliableChannel = new();
        private readonly List<byte[]> _resendBuffer = new();

        public RemotePeer(UdpTransport transport, IPEndPoint endPoint, double now)
        {
            _transport = transport;
            EndPoint = endPoint;
            LastHeardTime = now;
        }

        public IPEndPoint EndPoint { get; }
        public double LastHeardTime { get; private set; }
        public int RoundTripMs { get; private set; }

        public void SendReliable(INetMessage message, double now)
        {
            _transport.Send(_reliableChannel.PrepareOutgoing(message, now), EndPoint);
        }

        public void SendUnreliable(INetMessage message)
        {
            _transport.Send(PacketCodec.EncodeUnreliable(message), EndPoint);
        }

        public void ResendUnacknowledged(double now, double resendInterval)
        {
            _resendBuffer.Clear();
            _reliableChannel.CollectResends(now, resendInterval, _resendBuffer);
            foreach (byte[] datagram in _resendBuffer)
            {
                _transport.Send(datagram, EndPoint);
            }
        }

        public void HandleIncoming(DecodedPacket packet, double now, List<DecodedPacket> deliverable)
        {
            LastHeardTime = now;
            switch (packet.Channel)
            {
                case DeliveryChannel.Ack:
                    _reliableChannel.Acknowledge(packet.Sequence);
                    break;
                case DeliveryChannel.Reliable:
                    if (_reliableChannel.AcceptIncoming(packet, deliverable))
                    {
                        _transport.Send(PacketCodec.EncodeAck(packet.Sequence), EndPoint);
                    }

                    break;
                default:
                    deliverable.Add(packet);
                    break;
            }
        }

        public void RecordRoundTrip(double sentTime, double now)
        {
            int sample = ClampRoundTrip((now - sentTime) * 1000.0);
            RoundTripMs = RoundTripMs == 0 ? sample : (int)(RoundTripMs + (sample - RoundTripMs) * RoundTripSmoothing);
        }

        public void SetRoundTrip(int roundTripMs)
        {
            RoundTripMs = ClampRoundTrip(roundTripMs);
        }

        private static int ClampRoundTrip(double milliseconds)
        {
            return (int)Math.Max(0.0, Math.Min(ushort.MaxValue, milliseconds));
        }
    }
}
