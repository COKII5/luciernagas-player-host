using Fireflies.Game.State;
using UnityEngine;

namespace Fireflies.Game.Simulation
{
    public sealed class SimFirefly
    {
        private Vector2 _glideStart;
        private Vector2 _glideTarget;
        private float _glideElapsed;
        private float _glideDuration;

        public SimFirefly(ushort fireflyId, Vector2 position, bool isGolden)
        {
            FireflyId = fireflyId;
            Position = position;
            PreviousPosition = position;
            IsGolden = isGolden;
        }

        public ushort FireflyId { get; }
        public bool IsGolden { get; }
        public Vector2 Position { get; private set; }
        public Vector2 PreviousPosition { get; private set; }

        public bool IsCollectible => _glideElapsed >= _glideDuration;

        public void GlideTo(Vector2 target, float duration)
        {
            _glideStart = Position;
            _glideTarget = target;
            _glideElapsed = 0f;
            _glideDuration = duration;
        }

        public void Advance(float deltaTime)
        {
            PreviousPosition = Position;
            if (IsCollectible)
            {
                return;
            }

            _glideElapsed += deltaTime;
            float progress = Mathf.Clamp01(_glideElapsed / _glideDuration);
            float eased = 1f - (1f - progress) * (1f - progress);
            Position = Vector2.Lerp(_glideStart, _glideTarget, eased);
        }

        public FireflySnapshot ToSnapshot()
        {
            return new FireflySnapshot { FireflyId = FireflyId, Position = Position, IsGolden = IsGolden };
        }
    }
}
