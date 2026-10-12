using System.IO;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class PongMessage : INetMessage
    {
        public PongMessage(double echoedHostTime)
        {
            EchoedHostTime = echoedHostTime;
        }

        public PacketType Type => PacketType.Pong;
        public double EchoedHostTime { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(EchoedHostTime);
        }

        public static PongMessage Read(BinaryReader reader)
        {
            return new PongMessage(reader.ReadFiniteDouble());
        }
    }
}
