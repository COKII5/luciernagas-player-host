using Fireflies.Game.State;
using UnityEngine;

namespace Fireflies.Game.Simulation
{
    // Lo usan el host y la predicción del cliente, por eso tiene que ser determinista.
    public static class PlayerMotor
    {
        private const float MinInputForFacing = 0.01f;

        public static void Step(ref PlayerMotionState state, InputCommand command, float deltaTime, in MovementTuning tuning, bool canMove)
        {
            state.DashCooldownLeft = Mathf.Max(0f, state.DashCooldownLeft - deltaTime);
            state.StunTimeLeft = Mathf.Max(0f, state.StunTimeLeft - deltaTime);

            if (!canMove || state.IsStunned)
            {
                state.DashTimeLeft = 0f;
                return;
            }

            Vector2 move = command.Move;
            if (move.sqrMagnitude > MinInputForFacing)
            {
                state.Facing = move.normalized;
            }

            TryStartDash(ref state, command, tuning);

            Vector2 velocity = state.IsDashing ? state.DashDirection * tuning.DashSpeed : move * tuning.MoveSpeed;
            state.DashTimeLeft = Mathf.Max(0f, state.DashTimeLeft - deltaTime);
            state.Position = ClampToArena(state.Position + velocity * deltaTime, tuning.ArenaRadius);
        }

        public static Vector2 ClampToArena(Vector2 position, float arenaRadius)
        {
            return Vector2.ClampMagnitude(position, arenaRadius);
        }

        private static void TryStartDash(ref PlayerMotionState state, InputCommand command, in MovementTuning tuning)
        {
            bool canDash = command.DashPressed && tuning.DashEnabled && !state.IsDashing && state.DashCooldownLeft <= 0f;
            if (!canDash)
            {
                return;
            }

            state.DashDirection = state.Facing.sqrMagnitude > 0f ? state.Facing : Vector2.up;
            state.DashTimeLeft = tuning.DashDuration;
            state.DashCooldownLeft = tuning.DashCooldown;
        }
    }
}
