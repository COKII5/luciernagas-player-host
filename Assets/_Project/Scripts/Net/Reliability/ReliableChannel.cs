using System.Collections.Generic;
using Fireflies.Net.Protocol;

namespace Fireflies.Net.Reliability
{
    public sealed class ReliableChannel
    {
        private const int MaxBufferedOutOfOrder = 256;

        private readonly Dictionary<ushort, PendingPacket> _unacknowledged = new();
        private readonly Dictionary<ushort, DecodedPacket> _outOfOrder = new();
        private ushort _nextOutgoingSequence;
        private ushort _nextExpectedSequence;

        public byte[] PrepareOutgoing(INetMessage message, double now)
        {
            ushort sequence = _nextOutgoingSequence++;
            byte[] datagram = PacketCodec.Encode(DeliveryChannel.Reliable, sequence, message);
            _unacknowledged[sequence] = new PendingPacket(datagram, now);
            return datagram;
        }

        public void Acknowledge(ushort sequence)
        {
            _unacknowledged.Remove(sequence);
        }

        public void CollectResends(double now, double resendInterval, List<byte[]> resends)
        {
            foreach (PendingPacket pending in _unacknowledged.Values)
            {
                if (now - pending.LastSentTime >= resendInterval)
                {
                    pending.LastSentTime = now;
                    resends.Add(pending.Datagram);
                }
            }
        }

        // Devuelve false solo si el paquete se descartó sin guardarlo; en ese caso no hay que
        // confirmarlo, así el emisor lo reenvía más tarde.
        public bool AcceptIncoming(DecodedPacket packet, List<DecodedPacket> deliverable)
        {
            ushort sequence = packet.Sequence;
            bool isExpected = sequence == _nextExpectedSequence;
            bool isAlreadyDelivered = !isExpected && !SequenceMath.IsNewer(sequence, _nextExpectedSequence);
            if (isAlreadyDelivered)
            {
                return true;
            }

            if (!isExpected && _outOfOrder.Count >= MaxBufferedOutOfOrder)
            {
                return false;
            }

            _outOfOrder[sequence] = packet;
            while (_outOfOrder.TryGetValue(_nextExpectedSequence, out DecodedPacket next))
            {
                _outOfOrder.Remove(_nextExpectedSequence);
                deliverable.Add(next);
                _nextExpectedSequence++;
            }

            return true;
        }

        private sealed class PendingPacket
        {
            public PendingPacket(byte[] datagram, double sentTime)
            {
                Datagram = datagram;
                LastSentTime = sentTime;
            }

            public byte[] Datagram { get; }
            public double LastSentTime { get; set; }
        }
    }
}
