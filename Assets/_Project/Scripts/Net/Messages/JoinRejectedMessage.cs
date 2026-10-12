using System.IO;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class JoinRejectedMessage : INetMessage
    {
        public JoinRejectedMessage(JoinRejectReason reason)
        {
            Reason = reason;
        }

        public PacketType Type => PacketType.JoinRejected;
        public JoinRejectReason Reason { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Reason);
        }

        public static JoinRejectedMessage Read(BinaryReader reader)
        {
            return new JoinRejectedMessage(reader.ReadDefinedEnum<JoinRejectReason>());
        }
    }
}
