namespace Fireflies.Session
{
    public sealed class PlayerInfo
    {
        public PlayerInfo(byte playerId, byte slot, string playerName, bool isHost)
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
        public int RoundTripMs { get; set; }
    }
}
