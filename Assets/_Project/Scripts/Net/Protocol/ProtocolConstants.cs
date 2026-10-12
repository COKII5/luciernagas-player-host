namespace Fireflies.Net.Protocol
{
    public static class ProtocolConstants
    {
        public const ushort ProtocolId = 0x4C46;
        public const ushort ProtocolVersion = 1;
        public const int HeaderSize = 6;
        public const int MaxPlayerNameLength = 16;
        public const int MaxPlayersInMessage = 8;
        public const byte HostPlayerId = 0;
    }
}
