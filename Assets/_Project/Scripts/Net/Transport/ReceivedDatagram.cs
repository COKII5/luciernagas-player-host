using System.Net;

namespace Fireflies.Net.Transport
{
    public readonly struct ReceivedDatagram
    {
        public ReceivedDatagram(IPEndPoint sender, byte[] bytes)
        {
            Sender = sender;
            Bytes = bytes;
        }

        public IPEndPoint Sender { get; }
        public byte[] Bytes { get; }
    }
}
