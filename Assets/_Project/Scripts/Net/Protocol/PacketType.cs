namespace Fireflies.Net.Protocol
{
    public enum PacketType : byte
    {
        None = 0,
        JoinRequest = 1,
        JoinAccepted = 2,
        JoinRejected = 3,
        Disconnect = 4,
        Ping = 5,
        Pong = 6,
        Roster = 7,
        PingTable = 8,
        Input = 9,
        Snapshot = 10,
        MatchPhase = 11,
        Rules = 12,
        GameEvent = 13,
        MatchResults = 14
    }
}
