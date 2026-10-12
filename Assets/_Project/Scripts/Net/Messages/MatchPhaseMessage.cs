using System.IO;
using Fireflies.Game.State;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class MatchPhaseMessage : INetMessage
    {
        public MatchPhaseMessage(MatchPhase phase, float secondsRemaining)
        {
            Phase = phase;
            SecondsRemaining = secondsRemaining;
        }

        public PacketType Type => PacketType.MatchPhase;
        public MatchPhase Phase { get; }
        public float SecondsRemaining { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Phase);
            writer.Write(SecondsRemaining);
        }

        public static MatchPhaseMessage Read(BinaryReader reader)
        {
            MatchPhase phase = reader.ReadDefinedEnum<MatchPhase>();
            float secondsRemaining = reader.ReadFiniteSingle();
            return new MatchPhaseMessage(phase, secondsRemaining);
        }
    }
}
