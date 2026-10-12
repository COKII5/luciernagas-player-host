using System.IO;
using Fireflies.Game.State;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class GameEventMessage : INetMessage
    {
        public GameEventMessage(GameEvent gameEvent)
        {
            Event = gameEvent;
        }

        public PacketType Type => PacketType.GameEvent;
        public GameEvent Event { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Event.Type);
            writer.Write(Event.PlayerId);
            writer.Write(Event.OtherPlayerId);
            writer.Write(Event.Amount);
        }

        public static GameEventMessage Read(BinaryReader reader)
        {
            GameEventType type = reader.ReadDefinedEnum<GameEventType>();
            byte playerId = reader.ReadByte();
            byte otherPlayerId = reader.ReadByte();
            byte amount = reader.ReadByte();
            return new GameEventMessage(new GameEvent(type, playerId, otherPlayerId, amount));
        }
    }
}
