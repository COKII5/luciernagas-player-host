using System.IO;
using Fireflies.Game.Rules;
using Fireflies.Net.Protocol;

namespace Fireflies.Net.Messages
{
    public sealed class RulesMessage : INetMessage
    {
        public RulesMessage(MatchRules rules)
        {
            Rules = rules;
        }

        public PacketType Type => PacketType.Rules;
        public MatchRules Rules { get; }

        public void Write(BinaryWriter writer)
        {
            Rules.Write(writer);
        }

        public static RulesMessage Read(BinaryReader reader)
        {
            return new RulesMessage(MatchRules.Read(reader));
        }
    }
}
