using System;
using System.IO;
using Fireflies.Shared;
using UnityEngine;

namespace Fireflies.Game.Rules
{
    [Serializable]
    public sealed class MatchRules
    {
        public const int MinTargetScore = 5;
        public const int MaxTargetScore = 100;
        public const int MinDurationSeconds = 60;
        public const int MaxDurationSeconds = 600;
        public const float MinSpawnInterval = 0.4f;
        public const float MaxSpawnInterval = 5f;
        public const int MinFireflyLimit = 4;
        public const int MaxFireflyLimit = 40;
        public const int MinCarryCapacity = 1;
        public const int MaxCarryCapacity = 10;
        public const float MinDashCooldown = 0.5f;
        public const float MaxDashCooldown = 6f;
        public const float MaxGoldenChance = 0.5f;

        [SerializeField, Range(MinTargetScore, MaxTargetScore)] private int _targetScore = 20;
        [SerializeField, Range(MinDurationSeconds, MaxDurationSeconds)] private int _durationSeconds = 180;
        [SerializeField, Range(MinSpawnInterval, MaxSpawnInterval)] private float _spawnInterval = 1.2f;
        [SerializeField, Range(MinFireflyLimit, MaxFireflyLimit)] private int _fireflyLimit = 14;
        [SerializeField, Range(MinCarryCapacity, MaxCarryCapacity)] private int _carryCapacity = 5;
        [SerializeField, Range(MinDashCooldown, MaxDashCooldown)] private float _dashCooldown = 2f;
        [SerializeField] private bool _dashEnabled = true;
        [SerializeField, Range(0f, MaxGoldenChance)] private float _goldenChance = 0.08f;

        public int TargetScore
        {
            get => _targetScore;
            set => _targetScore = Mathf.Clamp(value, MinTargetScore, MaxTargetScore);
        }

        public int DurationSeconds
        {
            get => _durationSeconds;
            set => _durationSeconds = Mathf.Clamp(value, MinDurationSeconds, MaxDurationSeconds);
        }

        public float SpawnInterval
        {
            get => _spawnInterval;
            set => _spawnInterval = Mathf.Clamp(value, MinSpawnInterval, MaxSpawnInterval);
        }

        public int FireflyLimit
        {
            get => _fireflyLimit;
            set => _fireflyLimit = Mathf.Clamp(value, MinFireflyLimit, MaxFireflyLimit);
        }

        public int CarryCapacity
        {
            get => _carryCapacity;
            set => _carryCapacity = Mathf.Clamp(value, MinCarryCapacity, MaxCarryCapacity);
        }

        public float DashCooldown
        {
            get => _dashCooldown;
            set => _dashCooldown = Mathf.Clamp(value, MinDashCooldown, MaxDashCooldown);
        }

        public bool DashEnabled
        {
            get => _dashEnabled;
            set => _dashEnabled = value;
        }

        public float GoldenChance
        {
            get => _goldenChance;
            set => _goldenChance = Mathf.Clamp(value, 0f, MaxGoldenChance);
        }

        public MatchRules Clone()
        {
            return (MatchRules)MemberwiseClone();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)_targetScore);
            writer.Write((ushort)_durationSeconds);
            writer.Write(_spawnInterval);
            writer.Write((byte)_fireflyLimit);
            writer.Write((byte)_carryCapacity);
            writer.Write(_dashCooldown);
            writer.Write(_dashEnabled);
            writer.Write(_goldenChance);
        }

        // Se asigna por los setters para que un paquete manipulado no meta reglas fuera de rango.
        public static MatchRules Read(BinaryReader reader)
        {
            return new MatchRules
            {
                TargetScore = reader.ReadByte(),
                DurationSeconds = reader.ReadUInt16(),
                SpawnInterval = reader.ReadFiniteSingle(),
                FireflyLimit = reader.ReadByte(),
                CarryCapacity = reader.ReadByte(),
                DashCooldown = reader.ReadFiniteSingle(),
                DashEnabled = reader.ReadBoolean(),
                GoldenChance = reader.ReadFiniteSingle()
            };
        }
    }
}
