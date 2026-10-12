using System;
using System.Collections.Generic;
using Fireflies.Game.State;
using UnityEngine;

namespace Fireflies.Game.Simulation
{
    public sealed class SimPlayer
    {
        private readonly Queue<InputCommand> _pendingCommands = new();
        private uint _lastReceivedSequence;
        private InputCommand _lastAppliedCommand;

        public SimPlayer(byte playerId, int slot, PlayerMotionState spawnState)
        {
            PlayerId = playerId;
            Slot = slot;
            Motion = spawnState;
            PreviousPosition = spawnState.Position;
        }

        public byte PlayerId { get; }
        public int Slot { get; }
        public PlayerMotionState Motion { get; private set; }
        public Vector2 PreviousPosition { get; private set; }
        public int CarriedNormal { get; private set; }
        public int CarriedGolden { get; private set; }
        public int Score { get; private set; }
        public uint LastProcessedInput { get; private set; }

        public int CarriedCount => CarriedNormal + CarriedGolden;

        public void EnqueueCommands(IReadOnlyList<InputCommand> commands)
        {
            foreach (InputCommand command in commands)
            {
                EnqueueCommand(command);
            }
        }

        public void EnqueueCommand(InputCommand command)
        {
            if (command.Sequence > _lastReceivedSequence)
            {
                _pendingCommands.Enqueue(command);
                _lastReceivedSequence = command.Sequence;
            }
        }

        // Un comando por tick, igual que la predicción. Si llegan en ráfaga se consumen varios
        // para que el jugador no quede atrasado para siempre.
        public void Step(float deltaTime, in MovementTuning tuning, bool canMove, int maxBacklog)
        {
            PreviousPosition = Motion.Position;
            int steps = Math.Max(1, _pendingCommands.Count - maxBacklog + 1);
            for (int i = 0; i < steps; i++)
            {
                PlayerMotionState motion = Motion;
                PlayerMotor.Step(ref motion, TakeNextCommand(), deltaTime, tuning, canMove);
                Motion = motion;
            }
        }

        public void ResetForMatch(PlayerMotionState spawnState)
        {
            Motion = spawnState;
            PreviousPosition = spawnState.Position;
            CarriedNormal = 0;
            CarriedGolden = 0;
            Score = 0;
        }

        public void AddCarried(bool isGolden)
        {
            if (isGolden)
            {
                CarriedGolden++;
            }
            else
            {
                CarriedNormal++;
            }
        }

        public void ClearCarried()
        {
            CarriedNormal = 0;
            CarriedGolden = 0;
        }

        public int DepositCarried(int goldenValue)
        {
            int points = CarriedNormal + CarriedGolden * goldenValue;
            Score += points;
            ClearCarried();
            return points;
        }

        public void ApplyHit(float stunDuration, Vector2 knockback, float arenaRadius)
        {
            PlayerMotionState motion = Motion;
            motion.StunTimeLeft = stunDuration;
            motion.DashTimeLeft = 0f;
            motion.Position = PlayerMotor.ClampToArena(motion.Position + knockback, arenaRadius);
            Motion = motion;
        }

        public void EndDash()
        {
            PlayerMotionState motion = Motion;
            motion.DashTimeLeft = 0f;
            Motion = motion;
        }

        public PlayerSnapshot ToSnapshot()
        {
            return new PlayerSnapshot
            {
                PlayerId = PlayerId,
                Motion = Motion,
                CarriedNormal = (byte)CarriedNormal,
                CarriedGolden = (byte)CarriedGolden,
                Score = (ushort)Score,
                LastProcessedInput = LastProcessedInput
            };
        }

        private InputCommand TakeNextCommand()
        {
            if (_pendingCommands.Count == 0)
            {
                return _lastAppliedCommand.WithoutDash();
            }

            _lastAppliedCommand = _pendingCommands.Dequeue();
            LastProcessedInput = _lastAppliedCommand.Sequence;
            return _lastAppliedCommand;
        }
    }
}
