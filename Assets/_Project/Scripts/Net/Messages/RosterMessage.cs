using System.Collections.Generic;
using System.IO;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public readonly struct RosterEntry
    {
        public RosterEntry(byte playerId, byte slot, string playerName, bool isHost)
        {
            PlayerId = playerId;
            Slot = slot;
            PlayerName = playerName;
            IsHost = isHost;
        }

        public byte PlayerId { get; }
        public byte Slot { get; }
        public string PlayerName { get; }
        public bool IsHost { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(Slot);
            writer.WriteShortString(PlayerName);
            writer.Write(IsHost);
        }

        public static RosterEntry Read(BinaryReader reader)
        {
            byte playerId = reader.ReadByte();
            byte slot = reader.ReadByte();
            string playerName = reader.ReadShortString();
            bool isHost = reader.ReadBoolean();
            return new RosterEntry(playerId, slot, playerName, isHost);
        }
    }

    public sealed class RosterMessage : INetMessage
    {
        public RosterMessage(IReadOnlyList<RosterEntry> entries)
        {
            Entries = entries;
        }

        public PacketType Type => PacketType.Roster;
        public IReadOnlyList<RosterEntry> Entries { get; }

        public void Write(BinaryWriter writer)
        {
            writer.WriteList(Entries, (output, entry) => entry.Write(output));
        }

        public static RosterMessage Read(BinaryReader reader)
        {
            return new RosterMessage(reader.ReadList(ProtocolConstants.MaxPlayersInMessage, RosterEntry.Read));
        }
    }
}
