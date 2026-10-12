namespace Fireflies.Game.State
{
    public readonly struct GameEvent
    {
        public GameEvent(GameEventType type, byte playerId, byte otherPlayerId, byte amount)
        {
            Type = type;
            PlayerId = playerId;
            OtherPlayerId = otherPlayerId;
            Amount = amount;
        }

        public GameEventType Type { get; }
        public byte PlayerId { get; }
        public byte OtherPlayerId { get; }
        public byte Amount { get; }
    }
}
