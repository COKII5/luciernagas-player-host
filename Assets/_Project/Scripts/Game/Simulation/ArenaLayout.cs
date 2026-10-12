using Fireflies.Config;
using UnityEngine;

namespace Fireflies.Game.Simulation
{
    public sealed class ArenaLayout
    {
        public const int SlotCount = 4;

        private static readonly Vector2[] SlotDirections =
        {
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        private readonly GameplaySettings _settings;

        public ArenaLayout(GameplaySettings settings)
        {
            _settings = settings;
        }

        public float Radius => _settings.ArenaRadius;

        public Vector2 GetAltarPosition(int slot)
        {
            return SlotDirections[slot] * _settings.AltarDistanceFromCenter;
        }

        public Vector2 GetSpawnPosition(int slot)
        {
            float spawnDistance = _settings.AltarDistanceFromCenter - _settings.SpawnOffsetTowardCenter;
            return SlotDirections[slot] * spawnDistance;
        }

        public Vector2 GetSpawnFacing(int slot)
        {
            return -SlotDirections[slot];
        }

        public bool IsNearAnyAltar(Vector2 position, float clearance)
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (Vector2.Distance(position, GetAltarPosition(slot)) < clearance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
