using UnityEngine;

namespace Fireflies.Sync
{
    public struct FireflyRenderState
    {
        public ushort FireflyId;
        public Vector2 Position;
        public bool IsGolden;

        public FireflyRenderState(ushort fireflyId, Vector2 position, bool isGolden)
        {
            FireflyId = fireflyId;
            Position = position;
            IsGolden = isGolden;
        }
    }
}
