using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.Rules;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using Fireflies.Net.Connection;
using Fireflies.Net.Messages;

namespace Fireflies.Session
{
    public sealed class HostReplicator
    {
        private readonly HostNetwork _network;
        private readonly NetworkSettings _settings;
        private readonly HostWorldReader _worldReader = new();
        private double _nextPingTableTime;

        public HostReplicator(HostNetwork network, NetworkSettings settings)
        {
            _network = network;
            _settings = settings;
        }

        // El orden importa: si entra durante Results, los resultados tienen que llegar antes que la fase.
        public void Welcome(ConnectedClient client, byte slot, MatchRules rules, MatchPhase phase, float secondsRemaining,
            IReadOnlyList<PlayerValue> lastStandings, double now)
        {
            _network.SendReliable(client, new JoinAcceptedMessage(client.PlayerId, slot), now);
            _network.SendReliable(client, new RulesMessage(rules), now);
            if (phase == MatchPhase.Results && lastStandings.Count > 0)
            {
                _network.SendReliable(client, new MatchResultsMessage(lastStandings), now);
            }

            _network.SendReliable(client, new MatchPhaseMessage(phase, secondsRemaining), now);
        }

        public void BroadcastSnapshotIfDue(MatchSimulation simulation, uint tick, double now, float secondsRemaining)
        {
            if (tick % _settings.TicksPerSnapshot == 0)
            {
                _network.BroadcastUnreliable(_worldReader.ComposeSnapshot(simulation, tick, now, secondsRemaining));
            }
        }

        public bool TryBroadcastPings(HostRoster roster, double now)
        {
            if (now < _nextPingTableTime)
            {
                return false;
            }

            _nextPingTableTime = now + _settings.HeartbeatInterval;
            foreach (PlayerInfo player in roster.Players)
            {
                if (!player.IsHost && _network.TryGetClient(player.PlayerId, out ConnectedClient client))
                {
                    player.RoundTripMs = client.Peer.RoundTripMs;
                }
            }

            _network.BroadcastUnreliable(roster.ToPingTableMessage());
            return true;
        }
    }
}
