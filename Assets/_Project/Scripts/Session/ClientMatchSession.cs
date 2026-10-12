using System;
using System.Collections.Generic;
using System.Net;
using Fireflies.Config;
using Fireflies.Game.Rules;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using Fireflies.Net.Connection;
using Fireflies.Net.Messages;
using Fireflies.Net.Protocol;
using Fireflies.Sync;
using UnityEngine;

namespace Fireflies.Session
{
    public sealed class ClientMatchSession : IMatchSession, ISyncDiagnostics
    {
        private readonly GameplaySettings _gameplaySettings;
        private readonly ClientNetwork _network;
        private readonly ClientRoster _roster = new();
        private readonly ClientWorldView _world;
        private double _now;
        private bool _hasReceivedRoster;
        private bool _hasEnded;

        public ClientMatchSession(NetworkSettings networkSettings, GameplaySettings gameplaySettings, float fixedDeltaTime)
        {
            _gameplaySettings = gameplaySettings;
            _world = new ClientWorldView(networkSettings, fixedDeltaTime);
            Rules = gameplaySettings.CreateDefaultRules();

            _network = new ClientNetwork(networkSettings);
            _network.Connected += accepted => _world.LocalPlayerId = accepted.PlayerId;
            _network.Rejected += reason => End(MapRejection(reason));
            _network.ConnectionAttemptTimedOut += () => End(SessionEndReason.ConnectionFailed);
            _network.Disconnected += reason => End(MapDisconnect(reason));
            _network.PacketReceived += OnPacketReceived;
        }

        public event Action<PlayerInfo> PlayerJoined;
        public event Action<PlayerInfo> PlayerLeft;
        public event Action RosterChanged;
        public event Action PingsUpdated;
        public event Action<MatchPhase> PhaseChanged;
        public event Action RulesChanged;
        public event Action<GameEvent> GameEventRaised;
        public event Action<IReadOnlyList<PlayerValue>> MatchFinished;
        public event Action<SessionEndReason> Ended;

        public bool IsHost => false;
        public bool IsReady => _network.State == ClientConnectionState.Connected && _hasReceivedRoster;
        public byte LocalPlayerId => _world.LocalPlayerId;
        public MatchPhase Phase { get; private set; }
        public float PhaseSecondsRemaining { get; private set; }
        public MatchRules Rules { get; private set; }
        public IReadOnlyList<PlayerInfo> Players => _roster.Players;

        public bool InterpolationEnabled
        {
            get => _world.InterpolationEnabled;
            set => _world.InterpolationEnabled = value;
        }

        public int RoundTripMs => _network.RoundTripMs;
        public int BufferedSnapshots => _world.BufferedSnapshots;

        private MovementTuning Tuning => new MovementTuning(_gameplaySettings, Rules);

        public void Connect(IPEndPoint hostEndPoint, string playerName, byte preferredSlot, double now)
        {
            _now = now;
            _network.Connect(hostEndPoint, playerName, preferredSlot, now);
        }

        public void Poll(double now, float frameDeltaTime)
        {
            _now = now;
            _network.Poll(now);
            _world.DecayCorrection(frameDeltaTime);
            if (Phase == MatchPhase.Countdown || Phase == MatchPhase.Playing)
            {
                PhaseSecondsRemaining = Mathf.Max(0f, PhaseSecondsRemaining - frameDeltaTime);
            }
        }

        public void FixedTick(double now, float deltaTime, LocalInput input)
        {
            if (IsReady)
            {
                _network.SendUnreliable(_world.PredictInput(input, Tuning, MatchPhaseRules.AllowsMovement(Phase)));
            }
        }

        public void FillRenderState(WorldRenderState target, double now, float fixedStepAlpha)
        {
            _world.Fill(target, _network.Clock.HostTimeAt(now), fixedStepAlpha);
        }

        public void Dispose()
        {
            _network.Dispose();
            End(SessionEndReason.LeftByUser);
        }

        private void OnPacketReceived(DecodedPacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.Roster when PacketCodec.TryReadPayload(packet, RosterMessage.Read, out RosterMessage roster):
                    ApplyRoster(roster);
                    break;
                case PacketType.PingTable when PacketCodec.TryReadPayload(packet, PingTableMessage.Read, out PingTableMessage pings):
                    _roster.ApplyPings(pings);
                    PingsUpdated?.Invoke();
                    break;
                case PacketType.Snapshot when PacketCodec.TryReadPayload(packet, SnapshotMessage.Read, out SnapshotMessage snapshot):
                    ApplySnapshot(snapshot);
                    break;
                case PacketType.MatchPhase when PacketCodec.TryReadPayload(packet, MatchPhaseMessage.Read, out MatchPhaseMessage phase):
                    Phase = phase.Phase;
                    PhaseSecondsRemaining = phase.SecondsRemaining;
                    PhaseChanged?.Invoke(Phase);
                    break;
                case PacketType.Rules when PacketCodec.TryReadPayload(packet, RulesMessage.Read, out RulesMessage rules):
                    Rules = rules.Rules;
                    RulesChanged?.Invoke();
                    break;
                case PacketType.GameEvent when PacketCodec.TryReadPayload(packet, GameEventMessage.Read, out GameEventMessage gameEvent):
                    GameEventRaised?.Invoke(gameEvent.Event);
                    break;
                case PacketType.MatchResults when PacketCodec.TryReadPayload(packet, MatchResultsMessage.Read, out MatchResultsMessage results):
                    MatchFinished?.Invoke(results.Entries);
                    break;
            }
        }

        private void ApplyRoster(RosterMessage roster)
        {
            _roster.Apply(roster);
            if (_hasReceivedRoster)
            {
                foreach (PlayerInfo joined in _roster.JoinedInLastUpdate)
                {
                    PlayerJoined?.Invoke(joined);
                }

                foreach (PlayerInfo left in _roster.LeftInLastUpdate)
                {
                    PlayerLeft?.Invoke(left);
                }
            }

            _hasReceivedRoster = true;
            RosterChanged?.Invoke();
        }

        private void ApplySnapshot(SnapshotMessage snapshot)
        {
            if (_world.TryApplySnapshot(snapshot, Tuning))
            {
                _network.Clock.ObserveSnapshot(snapshot.HostTime, _now);
                PhaseSecondsRemaining = snapshot.PhaseSecondsRemaining;
            }
        }

        private void End(SessionEndReason reason)
        {
            if (_hasEnded)
            {
                return;
            }

            _hasEnded = true;
            Ended?.Invoke(reason);
        }

        private static SessionEndReason MapRejection(JoinRejectReason reason)
        {
            return reason switch
            {
                JoinRejectReason.Banned => SessionEndReason.RejectedBanned,
                JoinRejectReason.SessionFull => SessionEndReason.RejectedFull,
                JoinRejectReason.InvalidName => SessionEndReason.RejectedInvalidName,
                _ => SessionEndReason.RejectedVersion
            };
        }

        private static SessionEndReason MapDisconnect(DisconnectReason reason)
        {
            return reason switch
            {
                DisconnectReason.Kicked => SessionEndReason.Kicked,
                DisconnectReason.Banned => SessionEndReason.Banned,
                DisconnectReason.HostClosed => SessionEndReason.HostClosed,
                _ => SessionEndReason.ConnectionLost
            };
        }
    }
}
