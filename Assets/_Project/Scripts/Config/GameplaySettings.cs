using Fireflies.Game.Rules;
using UnityEngine;

namespace Fireflies.Config
{
    [CreateAssetMenu(fileName = "GameplaySettings", menuName = "Fireflies/Gameplay Settings")]
    public sealed class GameplaySettings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0.5f)] private float _moveSpeed = 6f;
        [SerializeField, Min(1f)] private float _dashSpeed = 17f;
        [SerializeField, Range(0.05f, 1f)] private float _dashDuration = 0.18f;
        [SerializeField, Range(0.1f, 3f)] private float _stunDuration = 0.6f;
        [SerializeField, Range(0f, 5f)] private float _knockbackDistance = 1.2f;
        [SerializeField, Range(0.2f, 2f)] private float _playerRadius = 0.5f;

        [Header("Arena")]
        [SerializeField, Min(5f)] private float _arenaRadius = 15f;
        [SerializeField, Min(2f)] private float _altarDistanceFromCenter = 11f;
        [SerializeField, Min(0f)] private float _spawnOffsetTowardCenter = 2.2f;

        [Header("Fireflies")]
        [SerializeField, Range(0.2f, 3f)] private float _pickupRadius = 0.9f;
        [SerializeField, Range(0.5f, 5f)] private float _depositRadius = 2f;
        [SerializeField, Range(1, 10)] private int _goldenValue = 3;
        [SerializeField, Min(0f)] private float _altarSpawnClearance = 3f;
        [SerializeField, Min(0.5f)] private float _scatterRadius = 2.5f;
        [SerializeField, Range(0.1f, 2f)] private float _scatterDuration = 0.45f;
        [SerializeField, Range(1, 50)] private int _spawnAttempts = 12;

        [Header("Match flow")]
        [SerializeField, Range(1, 10)] private int _countdownSeconds = 3;

        [Header("Default rules (editable by the host at runtime)")]
        [SerializeField] private MatchRules _defaultRules = new();

        public float MoveSpeed => _moveSpeed;
        public float DashSpeed => _dashSpeed;
        public float DashDuration => _dashDuration;
        public float StunDuration => _stunDuration;
        public float KnockbackDistance => _knockbackDistance;
        public float PlayerRadius => _playerRadius;
        public float ArenaRadius => _arenaRadius;
        public float PlayableRadius => _arenaRadius - _playerRadius;
        public float AltarDistanceFromCenter => _altarDistanceFromCenter;
        public float SpawnOffsetTowardCenter => _spawnOffsetTowardCenter;
        public float PickupRadius => _pickupRadius;
        public float DepositRadius => _depositRadius;
        public int GoldenValue => _goldenValue;
        public float AltarSpawnClearance => _altarSpawnClearance;
        public float ScatterRadius => _scatterRadius;
        public float ScatterDuration => _scatterDuration;
        public int SpawnAttempts => _spawnAttempts;
        public int CountdownSeconds => _countdownSeconds;

        public MatchRules CreateDefaultRules()
        {
            return _defaultRules.Clone();
        }
    }
}
