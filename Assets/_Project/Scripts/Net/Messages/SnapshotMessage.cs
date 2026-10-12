using System.Collections.Generic;
using System.IO;
using Fireflies.Game.State;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    public sealed class SnapshotMessage : INetMessage
    {
        private const int MaxFireflies = 120;

        public SnapshotMessage(uint tick, double hostTime, float phaseSecondsRemaining,
            IReadOnlyList<PlayerSnapshot> players, IReadOnlyList<FireflySnapshot> fireflies)
        {
            Tick = tick;
            HostTime = hostTime;
            PhaseSecondsRemaining = phaseSecondsRemaining;
            Players = players;
            Fireflies = fireflies;
        }

        public PacketType Type => PacketType.Snapshot;
        public uint Tick { get; }
        public double HostTime { get; }
        public float PhaseSecondsRemaining { get; }
        public IReadOnlyList<PlayerSnapshot> Players { get; }
        public IReadOnlyList<FireflySnapshot> Fireflies { get; }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Tick);
            writer.Write(HostTime);
            writer.Write(PhaseSecondsRemaining);
            writer.WriteList(Players, (output, player) => player.Write(output));
            writer.WriteList(Fireflies, (output, firefly) => firefly.Write(output));
        }

        public static SnapshotMessage Read(BinaryReader reader)
        {
            uint tick = reader.ReadUInt32();
            double hostTime = reader.ReadFiniteDouble();
            float secondsRemaining = reader.ReadFiniteSingle();
            List<PlayerSnapshot> players = reader.ReadList(ProtocolConstants.MaxPlayersInMessage, PlayerSnapshot.Read);
            List<FireflySnapshot> fireflies = reader.ReadList(MaxFireflies, FireflySnapshot.Read);
            return new SnapshotMessage(tick, hostTime, secondsRemaining, players, fireflies);
        }
    }
}
