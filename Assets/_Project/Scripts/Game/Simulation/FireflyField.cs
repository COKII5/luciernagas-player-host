using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.Rules;
using UnityEngine;

namespace Fireflies.Game.Simulation
{
    public sealed class FireflyField
    {
        private const float MinScatterDistanceFraction = 0.5f;

        private readonly List<SimFirefly> _fireflies = new();
        private readonly System.Random _random = new();
        private readonly GameplaySettings _settings;
        private readonly ArenaLayout _layout;
        private ushort _nextFireflyId;
        private float _spawnTimer;

        public FireflyField(GameplaySettings settings, ArenaLayout layout)
        {
            _settings = settings;
            _layout = layout;
        }

        public IReadOnlyList<SimFirefly> Fireflies => _fireflies;

        public void Clear()
        {
            _fireflies.Clear();
            _spawnTimer = 0f;
        }

        public void Advance(float deltaTime)
        {
            foreach (SimFirefly firefly in _fireflies)
            {
                firefly.Advance(deltaTime);
            }
        }

        public bool TrySpawn(float deltaTime, MatchRules rules, out bool spawnedGolden)
        {
            spawnedGolden = false;
            _spawnTimer += deltaTime;
            if (_spawnTimer < rules.SpawnInterval || _fireflies.Count >= rules.FireflyLimit)
            {
                return false;
            }

            _spawnTimer = 0f;
            spawnedGolden = !HasGoldenOnField() && _random.NextDouble() < rules.GoldenChance;
            _fireflies.Add(new SimFirefly(_nextFireflyId++, FindSpawnPoint(), spawnedGolden));
            return true;
        }

        public bool TryCollect(Vector2 collectorPosition, float radius, out bool isGolden)
        {
            for (int i = 0; i < _fireflies.Count; i++)
            {
                SimFirefly firefly = _fireflies[i];
                if (firefly.IsCollectible && Vector2.Distance(firefly.Position, collectorPosition) <= radius)
                {
                    isGolden = firefly.IsGolden;
                    _fireflies.RemoveAt(i);
                    return true;
                }
            }

            isGolden = false;
            return false;
        }

        public void Scatter(Vector2 origin, int normalCount, int goldenCount)
        {
            int total = normalCount + goldenCount;
            for (int i = 0; i < total; i++)
            {
                bool isGolden = i < goldenCount;
                SimFirefly firefly = new SimFirefly(_nextFireflyId++, origin, isGolden);
                firefly.GlideTo(FindScatterTarget(origin), _settings.ScatterDuration);
                _fireflies.Add(firefly);
            }
        }

        private bool HasGoldenOnField()
        {
            foreach (SimFirefly firefly in _fireflies)
            {
                if (firefly.IsGolden)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2 FindSpawnPoint()
        {
            Vector2 candidate = Vector2.zero;
            for (int attempt = 0; attempt < _settings.SpawnAttempts; attempt++)
            {
                candidate = RandomPointInDisc(_layout.Radius - _settings.PickupRadius);
                if (!_layout.IsNearAnyAltar(candidate, _settings.AltarSpawnClearance))
                {
                    return candidate;
                }
            }

            return candidate;
        }

        private Vector2 FindScatterTarget(Vector2 origin)
        {
            float angle = (float)(_random.NextDouble() * Mathf.PI * 2.0);
            float distance = Mathf.Lerp(_settings.ScatterRadius * MinScatterDistanceFraction, _settings.ScatterRadius, (float)_random.NextDouble());
            Vector2 target = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            return Vector2.ClampMagnitude(target, _layout.Radius - _settings.PickupRadius);
        }

        private Vector2 RandomPointInDisc(float radius)
        {
            float angle = (float)(_random.NextDouble() * Mathf.PI * 2.0);
            float distance = radius * Mathf.Sqrt((float)_random.NextDouble());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }
    }
}
