using System.IO;
using Fireflies.Net.Protocol;

namespace Fireflies.Net.Messages
{
    public sealed class JoinAcceptedMessage : INetMessage
    {
        public JoinAcceptedMessage(byte playerId, byte slot)
        {
            PlayerId = playerId;
            Slot = slot;
        }

        public PacketType Type => PacketType.JoinAccepted;
        public byte PlayerId { get; }
        public byte Slot { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(Slot);
        }

        public static JoinAcceptedMessage Read(BinaryReader reader)
        {
            return new JoinAcceptedMessage(reader.ReadByte(), reader.ReadByte());
        }
    }
}
