using System.Collections.Generic;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using UnityEngine;

namespace Fireflies.Sync
{
    // Predicción + reconciliación: el jugador local se mueve al instante; cuando llega el estado
    // del host se reaplican los comandos aún no confirmados y la diferencia se corrige de a poco.
    public sealed class ClientPrediction
    {
        private const int MaxPendingCommands = 150;

        private readonly List<PredictedCommand> _pending = new();
        private readonly float _stepDuration;
        private readonly float _correctionRate;
        private readonly float _snapDistance;
        private PlayerMotionState _predicted;
        private Vector2 _previousPosition;
        private Vector2 _correctionOffset;
        private uint _nextSequence = 1;

        public ClientPrediction(float stepDuration, float correctionRate, float snapDistance)
        {
            _stepDuration = stepDuration;
            _correctionRate = correctionRate;
            _snapDistance = snapDistance;
        }

        public bool HasAuthoritativeState { get; private set; }
        public PlayerMotionState Predicted => _predicted;

        public InputCommand PredictNext(Vector2 move, bool dashPressed, in MovementTuning tuning, bool canMove)
        {
            InputCommand command = new InputCommand(_nextSequence++, move, dashPressed);
            _pending.Add(new PredictedCommand(command, canMove));
            if (_pending.Count > MaxPendingCommands)
            {
                _pending.RemoveAt(0);
            }

            _previousPosition = _predicted.Position;
            if (HasAuthoritativeState)
            {
                PlayerMotor.Step(ref _predicted, command, _stepDuration, tuning, canMove);
            }

            return command;
        }

        public void CollectRecentCommands(int count, List<InputCommand> target)
        {
            target.Clear();
            int start = Mathf.Max(0, _pending.Count - count);
            for (int i = start; i < _pending.Count; i++)
            {
                target.Add(_pending[i].Command);
            }
        }

        public void Reconcile(PlayerSnapshot authoritative, in MovementTuning tuning)
        {
            Vector2 positionBefore = _predicted.Position;
            _pending.RemoveAll(entry => entry.Command.Sequence <= authoritative.LastProcessedInput);

            _predicted = authoritative.Motion;
            foreach (PredictedCommand entry in _pending)
            {
                PlayerMotor.Step(ref _predicted, entry.Command, _stepDuration, tuning, entry.CanMove);
            }

            if (!HasAuthoritativeState)
            {
                HasAuthoritativeState = true;
                _previousPosition = _predicted.Position;
                return;
            }

            Vector2 shift = _predicted.Position - positionBefore;
            _previousPosition += shift;
            _correctionOffset = shift.magnitude > _snapDistance ? Vector2.zero : _correctionOffset - shift;
        }

        public void DecayCorrection(float frameDeltaTime)
        {
            _correctionOffset *= Mathf.Exp(-_correctionRate * frameDeltaTime);
        }

        public Vector2 GetRenderPosition(float fixedStepAlpha)
        {
            return Vector2.Lerp(_previousPosition, _predicted.Position, fixedStepAlpha) + _correctionOffset;
        }

        private readonly struct PredictedCommand
        {
            public PredictedCommand(InputCommand command, bool canMove)
            {
                Command = command;
                CanMove = canMove;
            }

            public InputCommand Command { get; }
            public bool CanMove { get; }
        }
    }
}
