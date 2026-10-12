using System.Collections.Generic;

namespace Fireflies.Sync
{
    public sealed class WorldRenderState
    {
        public List<PlayerRenderState> Players { get; } = new();
        public List<FireflyRenderState> Fireflies { get; } = new();

        public void Clear()
        {
            Players.Clear();
            Fireflies.Clear();
        }

        public bool TryGetPlayer(byte playerId, out PlayerRenderState player)
        {
            foreach (PlayerRenderState candidate in Players)
            {
                if (candidate.PlayerId == playerId)
                {
                    player = candidate;
                    return true;
                }
            }

            player = default;
            return false;
        }
    }
}
