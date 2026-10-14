using UnityEngine;

namespace Fireflies.Shared
{
    public static class RandomExtensions
    {
        public static Vector2 NextUnitDirection(this System.Random random)
        {
            float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
