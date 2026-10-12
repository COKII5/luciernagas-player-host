using System.Collections.Generic;
using System.IO;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public readonly struct PlayerValue
    {
        public PlayerValue(byte playerId, ushort value)
        {
            PlayerId = playerId;
            Value = value;
        }

        public byte PlayerId { get; }
        public ushort Value { get; }
    }

    // Base de los mensajes que son "un número por jugador" (ping, resultados).
    public abstract class PlayerValueListMessage : INetMessage
    {
        protected PlayerValueListMessage(IReadOnlyList<PlayerValue> entries)
        {
            Entries = entries;
        }

        public abstract PacketType Type { get; }
        public IReadOnlyList<PlayerValue> Entries { get; }

        public void Write(BinaryWriter writer)
        {
            writer.WriteList(Entries, (output, entry) =>
            {
                output.Write(entry.PlayerId);
                output.Write(entry.Value);
            });
        }

        protected static List<PlayerValue> ReadEntries(BinaryReader reader)
        {
            return reader.ReadList(ProtocolConstants.MaxPlayersInMessage, input => new PlayerValue(input.ReadByte(), input.ReadUInt16()));
        }
    }
}
