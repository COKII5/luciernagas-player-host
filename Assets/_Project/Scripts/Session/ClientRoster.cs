using System.Collections.Generic;
using Fireflies.Net.Messages;

namespace Fireflies.Session
{
    // Comparando rosters consecutivos se sabe quién entró o salió sin mensajes extra.
    public sealed class ClientRoster
    {
        private readonly List<PlayerInfo> _players = new();
        private readonly List<PlayerInfo> _joined = new();
        private readonly List<PlayerInfo> _left = new();

        public IReadOnlyList<PlayerInfo> Players => _players;
        public IReadOnlyList<PlayerInfo> JoinedInLastUpdate => _joined;
        public IReadOnlyList<PlayerInfo> LeftInLastUpdate => _left;

        public void Apply(RosterMessage roster)
        {
            _joined.Clear();
            _left.Clear();
            List<PlayerInfo> previous = new List<PlayerInfo>(_players);
            _players.Clear();

            foreach (RosterEntry entry in roster.Entries)
            {
                PlayerInfo existing = previous.Find(player => player.PlayerId == entry.PlayerId);
                PlayerInfo info = new PlayerInfo(entry.PlayerId, entry.Slot, entry.PlayerName, entry.IsHost);
                if (existing != null)
                {
                    info.RoundTripMs = existing.RoundTripMs;
                    previous.Remove(existing);
                }
                else
                {
                    _joined.Add(info);
                }

                _players.Add(info);
            }

            _left.AddRange(previous);
        }

        public void ApplyPings(PingTableMessage pingTable)
        {
            foreach (PlayerValue entry in pingTable.Entries)
            {
                PlayerInfo player = _players.Find(candidate => candidate.PlayerId == entry.PlayerId);
                if (player != null)
                {
                    player.RoundTripMs = entry.Value;
                }
            }
        }
    }
}
