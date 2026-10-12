namespace Fireflies.Net.Messages
{
    public enum JoinRejectReason : byte
    {
        Banned = 0,
        SessionFull = 1,
        InvalidName = 2,
        VersionMismatch = 3
    }
}
