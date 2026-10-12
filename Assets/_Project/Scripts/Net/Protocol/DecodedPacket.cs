using System.IO;

namespace Fireflies.Net.Protocol
{
    public readonly struct DecodedPacket
    {
        private readonly byte[] _datagram;

        public DecodedPacket(DeliveryChannel channel, ushort sequence, PacketType type, byte[] datagram)
        {
            Channel = channel;
            Sequence = sequence;
            Type = type;
            _datagram = datagram;
        }

        public DeliveryChannel Channel { get; }
        public ushort Sequence { get; }
        public PacketType Type { get; }

        public BinaryReader CreatePayloadReader()
        {
            int payloadLength = _datagram.Length - ProtocolConstants.HeaderSize;
            return new BinaryReader(new MemoryStream(_datagram, ProtocolConstants.HeaderSize, payloadLength, false));
        }
    }
}
