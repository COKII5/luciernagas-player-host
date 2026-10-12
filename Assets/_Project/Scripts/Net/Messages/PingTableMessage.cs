using System.Collections.Generic;
using System.IO;
using Fireflies.Net.Protocol;

namespace Fireflies.Net.Messages
{
    public sealed class PingTableMessage : PlayerValueListMessage
    {
        public PingTableMessage(IReadOnlyList<PlayerValue> roundTripsMs) : base(roundTripsMs)
        {
        }

        public override PacketType Type => PacketType.PingTable;

        public static PingTableMessage Read(BinaryReader reader)
        {
            return new PingTableMessage(ReadEntries(reader));
        }
    }
}
