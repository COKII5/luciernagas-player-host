using System.IO;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class PingMessage : INetMessage
    {
        public PingMessage(double hostTime, ushort lastRoundTripMs)
        {
            HostTime = hostTime;
            LastRoundTripMs = lastRoundTripMs;
        }

        public PacketType Type => PacketType.Ping;
        public double HostTime { get; }
        public ushort LastRoundTripMs { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(HostTime);
            writer.Write(LastRoundTripMs);
        }

        public static PingMessage Read(BinaryReader reader)
        {
            double hostTime = reader.ReadFiniteDouble();
            ushort lastRoundTripMs = reader.ReadUInt16();
            return new PingMessage(hostTime, lastRoundTripMs);
        }
    }
}
