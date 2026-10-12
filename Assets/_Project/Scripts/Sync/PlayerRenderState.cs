using UnityEngine;

namespace Fireflies.Sync
{
    public struct PlayerRenderState
    {
        public byte PlayerId;
        public Vector2 Position;
        public Vector2 Facing;
        public bool IsDashing;
        public bool IsStunned;
        public int CarriedNormal;
        public int CarriedGolden;
        public int Score;
        public float DashCooldownLeft;
    }
}
