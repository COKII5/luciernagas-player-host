using UnityEngine;

namespace Fireflies.Config
{
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "Fireflies/Network Settings")]
    public sealed class NetworkSettings : ScriptableObject
    {
        [Header("Connection")]
        [SerializeField, Range(1024, 65535)] private int _defaultPort = 7777;
        [SerializeField, Range(2, 4)] private int _maxPlayers = 4;
        [SerializeField, Min(0.1f)] private float _joinRetryInterval = 0.5f;
        [SerializeField, Min(1f)] private float _joinTimeout = 5f;
        [SerializeField, Min(0.1f)] private float _heartbeatInterval = 1f;
        [SerializeField, Min(1f)] private float _connectionTimeout = 8f;
        [SerializeField, Min(0.05f)] private float _reliableResendInterval = 0.2f;
        [SerializeField, Range(1, 10)] private int _disconnectNoticeRepeats = 3;
        [SerializeField] private string _banListFileName = "banned_addresses.json";

        [Header("Synchronization")]
        [SerializeField, Range(1, 10)] private int _ticksPerSnapshot = 2;
        [SerializeField, Range(0.02f, 0.5f)] private float _interpolationDelay = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float _maxExtrapolation = 0.25f;
        [SerializeField, Range(1, 10)] private int _redundantInputCommands = 4;
        [SerializeField, Range(1, 8)] private int _maxInputBacklog = 4;
        [SerializeField, Min(0.1f)] private float _correctionSmoothingRate = 12f;
        [SerializeField, Min(0.1f)] private float _snapCorrectionDistance = 3f;
        [SerializeField, Range(0.1f, 2f)] private float _clockResyncThreshold = 0.35f;

        [Header("Network condition simulation (testing only)")]
        [SerializeField, Range(0, 500)] private int _simulatedLatencyMs;
        [SerializeField, Range(0, 50)] private int _simulatedPacketLossPercent;

        public int DefaultPort => _defaultPort;
        public int MaxPlayers => _maxPlayers;
        public float JoinRetryInterval => _joinRetryInterval;
        public float JoinTimeout => _joinTimeout;
        public float HeartbeatInterval => _heartbeatInterval;
        public float ConnectionTimeout => _connectionTimeout;
        public float ReliableResendInterval => _reliableResendInterval;
        public int DisconnectNoticeRepeats => _disconnectNoticeRepeats;
        public string BanListFileName => _banListFileName;
        public int TicksPerSnapshot => _ticksPerSnapshot;
        public float InterpolationDelay => _interpolationDelay;
        public float MaxExtrapolation => _maxExtrapolation;
        public int RedundantInputCommands => _redundantInputCommands;
        public int MaxInputBacklog => _maxInputBacklog;
        public float CorrectionSmoothingRate => _correctionSmoothingRate;
        public float SnapCorrectionDistance => _snapCorrectionDistance;
        public float ClockResyncThreshold => _clockResyncThreshold;
        public int SimulatedLatencyMs => _simulatedLatencyMs;
        public int SimulatedPacketLossPercent => _simulatedPacketLossPercent;
    }
}
