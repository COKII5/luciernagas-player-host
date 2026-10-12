using System;
using System.Collections.Generic;
using System.Net;
using Fireflies.Config;
using Fireflies.Game.Rules;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using Fireflies.Net.Messages;
using Fireflies.Net.Protocol;
using Fireflies.Net.Connection;
using Fireflies.Sync;
using UnityEngine;

namespace Fireflies.Session
{
    public sealed class ClientMatchSession : IMatchSession, ISyncDiagnostics
    {
        private readonly NetworkSettings _networkSettings;
        private readonly GameplaySettings _gameplaySettings;
        private readonly ClientNetwork _network;
        private readonly ClientRoster _roster = new();
        private readonly SnapshotBuffer _snapshots = new();
        private readonly SnapshotInterpolator _interpolator = new();
        private readonly ClientPrediction _prediction;
        private readonly List<InputCommand> _outgoingCommands = new();
        private double _now;
        private bool _hasReceivedRoster;
        private bool _hasEnded;

        public ClientMatchSession(NetworkSettings networkSettings, GameplaySettings gameplaySettings, float fixedDeltaTime)
        {
            _networkSettings = networkSettings;
            _gameplaySettings = gameplaySettings;
            _prediction = new ClientPrediction(fixedDeltaTime, networkSettings.CorrectionSmoothingRate, networkSettings.SnapCorrectionDistance);
            Rules = gameplaySettings.CreateDefaultRules();

            _network = new ClientNetwork(networkSettings);
            _network.Connected += accepted => LocalPlayerId = accepted.PlayerId;
            _network.Rejected += reason => End(MapRejection(reason));
            _network.ConnectionAttemptTimedOut += () => End(SessionEndReason.ConnectionFailed);
            _network.Disconnected += reason => End(MapDisconnect(reason));
            _network.PacketReceived += OnPacketReceived;
        }

        public event Action<PlayerInfo> PlayerJoined;
        public event Action<PlayerInfo> PlayerLeft;
        public event Action RosterChanged;
        public event Action<MatchPhase> PhaseChanged;
        public event Action RulesChanged;
        public event Action<GameEvent> GameEventRaised;
        public event Action<IReadOnlyList<PlayerValue>> MatchFinished;
        public event Action<SessionEndReason> Ended;

        public bool IsHost => false;
        public bool IsReady => _network.State == ClientConnectionState.Connected && _hasReceivedRoster;
        public byte LocalPlayerId { get; private set; }
        public MatchPhase Phase { get; private set; }
        public float PhaseSecondsRemaining { get; private set; }
        public MatchRules Rules { get; private set; }
        public IReadOnlyList<PlayerInfo> Players => _roster.Players;

        public bool InterpolationEnabled
        {
            get => _interpolator.IsInterpolationEnabled;
            set => _interpolator.IsInterpolationEnabled = value;
        }

        public int RoundTripMs => _network.RoundTripMs;
        public int BufferedSnapshots => _snapshots.Count;

        private bool CanMove => Phase == MatchPhase.Lobby || Phase == MatchPhase.Playing;

        public void Connect(IPEndPoint hostEndPoint, string playerName, byte preferredSlot, double now)
        {
            _now = now;
            _network.Connect(hostEndPoint, playerName, preferredSlot, now);
        }

        public void Poll(double now, float frameDeltaTime)
        {
            _now = now;
            _network.Poll(now);
            _prediction.DecayCorrection(frameDeltaTime);
            if (Phase == MatchPhase.Countdown || Phase == MatchPhase.Playing)
            {
                PhaseSecondsRemaining = Mathf.Max(0f, PhaseSecondsRemaining - frameDeltaTime);
            }
        }

        public void FixedTick(double now, float deltaTime, LocalInput input)
        {
            if (!IsReady)
            {
                return;
            }

            MovementTuning tuning = new MovementTuning(_gameplaySettings, Rules);
            _prediction.PredictNext(input.Move, input.DashPressed, tuning, CanMove);
            _prediction.CollectRecentCommands(_networkSettings.RedundantInputCommands, _outgoingCommands);
            _network.SendUnreliable(new InputMessage(_outgoingCommands));
        }

        public void FillRenderState(WorldRenderState target, double now, float fixedStepAlpha)
        {
            double renderTime = _network.Clock.HostTimeAt(now) - _networkSettings.InterpolationDelay;
            _interpolator.Sample(_snapshots, renderTime, _networkSettings.MaxExtrapolation, target);
            ApplyPredictedLocalPlayer(target, fixedStepAlpha);
        }

        public void Dispose()
        {
            _network.Dispose();
            End(SessionEndReason.LeftByUser);
        }

        private void ApplyPredictedLocalPlayer(WorldRenderState target, float fixedStepAlpha)
        {
            int index = target.Players.FindIndex(player => player.PlayerId == LocalPlayerId);
            if (index < 0 || !_prediction.HasAuthoritativeState || !TryGetLatestLocalSnapshot(out PlayerSnapshot latest))
            {
                return;
            }

            PlayerMotionState predicted = _prediction.Predicted;
            PlayerRenderState state = SnapshotInterpolator.ToRenderState(latest);
            state.Position = _prediction.GetRenderPosition(fixedStepAlpha);
            state.Facing = predicted.Facing;
            state.IsDashing = predicted.IsDashing;
            state.IsStunned = predicted.IsStunned;
            state.DashCooldownLeft = predicted.DashCooldownLeft;
            target.Players[index] = state;
        }

        private bool TryGetLatestLocalSnapshot(out PlayerSnapshot snapshot)
        {
            snapshot = default;
            SnapshotMessage latest = _snapshots.Latest;
            if (latest == null)
            {
                return false;
            }

            foreach (PlayerSnapshot player in latest.Players)
            {
                if (player.PlayerId == LocalPlayerId)
                {
                    snapshot = player;
                    return true;
                }
            }

            return false;
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
                    RosterChanged?.Invoke();
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
            if (!_snapshots.TryAdd(snapshot))
            {
                return;
            }

            _network.Clock.SynchronizeRoughly(snapshot.HostTime, _now);
            PhaseSecondsRemaining = snapshot.PhaseSecondsRemaining;
            if (TryGetLatestLocalSnapshot(out PlayerSnapshot local))
            {
                _prediction.Reconcile(local, new MovementTuning(_gameplaySettings, Rules));
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
