using System.Collections.Generic;
using Fireflies.Game.Simulation;
using Fireflies.Net.Messages;

namespace Fireflies.Session
{
    public sealed class HostRoster
    {
        private readonly List<PlayerInfo> _players = new();

        public IReadOnlyList<PlayerInfo> Players => _players;

        public bool TryAssignSlot(byte preferredSlot, out byte slot)
        {
            if (preferredSlot < ArenaLayout.SlotCount && IsSlotFree(preferredSlot))
            {
                slot = preferredSlot;
                return true;
            }

            for (byte candidate = 0; candidate < ArenaLayout.SlotCount; candidate++)
            {
                if (IsSlotFree(candidate))
                {
                    slot = candidate;
                    return true;
                }
            }

            slot = 0;
            return false;
        }

        public PlayerInfo Add(byte playerId, byte slot, string playerName, bool isHost)
        {
            PlayerInfo info = new PlayerInfo(playerId, slot, playerName, isHost);
            _players.Add(info);
            _players.Sort((left, right) => left.Slot.CompareTo(right.Slot));
            return info;
        }

        public PlayerInfo Remove(byte playerId)
        {
            PlayerInfo info = Find(playerId);
            if (info != null)
            {
                _players.Remove(info);
            }

            return info;
        }

        public PlayerInfo Find(byte playerId)
        {
            return _players.Find(player => player.PlayerId == playerId);
        }

        public RosterMessage ToRosterMessage()
        {
            List<RosterEntry> entries = new List<RosterEntry>(_players.Count);
            foreach (PlayerInfo player in _players)
            {
                entries.Add(new RosterEntry(player.PlayerId, player.Slot, player.PlayerName, player.IsHost));
            }

            return new RosterMessage(entries);
        }

        public PingTableMessage ToPingTableMessage()
        {
            List<PlayerValue> entries = new List<PlayerValue>(_players.Count);
            foreach (PlayerInfo player in _players)
            {
                entries.Add(new PlayerValue(player.PlayerId, (ushort)player.RoundTripMs));
            }

            return new PingTableMessage(entries);
        }

        private bool IsSlotFree(byte slot)
        {
            return !_players.Exists(player => player.Slot == slot);
        }
    }
}
