namespace Fireflies.Net.Messages
{
    public enum DisconnectReason : byte
    {
        ClientLeft = 0,
        Kicked = 1,
        Banned = 2,
        HostClosed = 3,
        TimedOut = 4
    }
}
