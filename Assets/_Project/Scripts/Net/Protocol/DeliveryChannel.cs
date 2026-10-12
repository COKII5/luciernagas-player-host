namespace Fireflies.Net.Protocol
{
    public enum DeliveryChannel : byte
    {
        Unreliable = 0,
        Reliable = 1,
        Ack = 2
    }
}
