using System;

namespace Fireflies.Session
{
    public static class MatchSessionQueries
    {
        public static PlayerInfo FindPlayer(this IMatchSession session, byte playerId)
        {
            return FindFirst(session, player => player.PlayerId == playerId);
        }

        public static PlayerInfo FindPlayerInSlot(this IMatchSession session, int slot)
        {
            return FindFirst(session, player => player.Slot == slot);
        }

        public static PlayerInfo FindHost(this IMatchSession session)
        {
            return FindFirst(session, player => player.IsHost);
        }

        private static PlayerInfo FindFirst(IMatchSession session, Predicate<PlayerInfo> match)
        {
            if (session == null)
            {
                return null;
            }

            foreach (PlayerInfo player in session.Players)
            {
                if (match(player))
                {
                    return player;
                }
            }

            return null;
        }
    }
}
