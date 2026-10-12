using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using Fireflies.Net.Messages;
using Fireflies.Sync;
using UnityEngine;

namespace Fireflies.Session
{
    // El jugador local sale de la predicción y no de los snapshots, para que responda sin esperar al host.
    public sealed class ClientWorldView
    {
        private readonly NetworkSettings _settings;
        private readonly SnapshotBuffer _snapshots = new();
        private readonly SnapshotInterpolator _interpolator = new();
        private readonly ClientPrediction _prediction;
        private readonly List<InputCommand> _outgoingCommands = new();

        public ClientWorldView(NetworkSettings settings, float fixedDeltaTime)
        {
            _settings = settings;
            _prediction = new ClientPrediction(fixedDeltaTime, settings.CorrectionSmoothingRate, settings.SnapCorrectionDistance);
        }

        public byte LocalPlayerId { get; set; }
        public int BufferedSnapshots => _snapshots.Count;

        public bool InterpolationEnabled
        {
            get => _interpolator.IsInterpolationEnabled;
            set => _interpolator.IsInterpolationEnabled = value;
        }

        public InputMessage PredictInput(LocalInput input, in MovementTuning tuning, bool canMove)
        {
            _prediction.PredictNext(input.Move, input.DashPressed, tuning, canMove);
            _prediction.CollectRecentCommands(_settings.RedundantInputCommands, _outgoingCommands);
            return new InputMessage(_outgoingCommands);
        }

        public bool TryApplySnapshot(SnapshotMessage snapshot, in MovementTuning tuning)
        {
            if (!_snapshots.TryAdd(snapshot))
            {
                return false;
            }

            if (TryGetLatestLocalSnapshot(out PlayerSnapshot local))
            {
                _prediction.Reconcile(local, tuning);
            }

            return true;
        }

        public void DecayCorrection(float frameDeltaTime)
        {
            _prediction.DecayCorrection(frameDeltaTime);
        }

        public void Fill(WorldRenderState target, double hostNow, float fixedStepAlpha)
        {
            double renderTime = hostNow - _settings.InterpolationDelay;
            _interpolator.Sample(_snapshots, renderTime, _settings.MaxExtrapolation, target);
            ApplyPredictedLocalPlayer(target, fixedStepAlpha);
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
    }
}
