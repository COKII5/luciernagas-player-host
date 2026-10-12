namespace Fireflies.Session
{
    public enum SessionEndReason
    {
        LeftByUser,
        Kicked,
        Banned,
        HostClosed,
        ConnectionLost,
        ConnectionFailed,
        RejectedBanned,
        RejectedFull,
        RejectedInvalidName,
        RejectedVersion
    }
}
