using System.Collections.Generic;
using System.IO;
using Fireflies.Net.Protocol;

namespace Fireflies.Net.Messages
{
    public sealed class MatchResultsMessage : PlayerValueListMessage
    {
        public MatchResultsMessage(IReadOnlyList<PlayerValue> scoresByRank) : base(scoresByRank)
        {
        }

        public override PacketType Type => PacketType.MatchResults;

        public static MatchResultsMessage Read(BinaryReader reader)
        {
            return new MatchResultsMessage(ReadEntries(reader));
        }
    }
}
