using System.IO;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class DisconnectMessage : INetMessage
    {
        public DisconnectMessage(DisconnectReason reason)
        {
            Reason = reason;
        }

        public PacketType Type => PacketType.Disconnect;
        public DisconnectReason Reason { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Reason);
        }

        public static DisconnectMessage Read(BinaryReader reader)
        {
            return new DisconnectMessage(reader.ReadDefinedEnum<DisconnectReason>());
        }
    }
}
