using System.IO;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class JoinRequestMessage : INetMessage
    {
        public JoinRequestMessage(ushort protocolVersion, string playerName, byte preferredSlot)
        {
            ProtocolVersion = protocolVersion;
            PlayerName = playerName;
            PreferredSlot = preferredSlot;
        }

        public PacketType Type => PacketType.JoinRequest;
        public ushort ProtocolVersion { get; }
        public string PlayerName { get; }
        public byte PreferredSlot { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(ProtocolVersion);
            writer.WriteShortString(PlayerName);
            writer.Write(PreferredSlot);
        }

        public static JoinRequestMessage Read(BinaryReader reader)
        {
            return new JoinRequestMessage(reader.ReadUInt16(), reader.ReadShortString(), reader.ReadByte());
        }
    }
}
