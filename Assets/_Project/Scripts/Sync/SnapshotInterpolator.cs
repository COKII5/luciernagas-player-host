using System.Collections.Generic;
using Fireflies.Game.State;
using Fireflies.Net.Messages;
using UnityEngine;

namespace Fireflies.Sync
{
    // Se dibuja un poco en el pasado (tiempo del host - delay) para tener siempre dos snapshots
    // entre los que interpolar. Si dejan de llegar paquetes, extrapola por poco tiempo.
    public sealed class SnapshotInterpolator
    {
        private const float DiscreteSwitchPoint = 0.5f;

        private readonly Dictionary<ushort, FireflySnapshot> _previousFireflies = new();

        public bool IsInterpolationEnabled { get; set; } = true;

        public void Sample(SnapshotBuffer buffer, double renderTime, float maxExtrapolation, WorldRenderState target)
        {
            target.Clear();
            if (buffer.Count == 0)
            {
                return;
            }

            if (!IsInterpolationEnabled)
            {
                SampleLatest(buffer.Latest, target);
                return;
            }

            FindBracket(buffer, renderTime, maxExtrapolation, out SnapshotMessage from, out SnapshotMessage to, out float blend);
            BlendPlayers(from, to, blend, target);
            BlendFireflies(from, to, blend, target);
        }

        public static PlayerRenderState ToRenderState(PlayerSnapshot snapshot)
        {
            return new PlayerRenderState
            {
                PlayerId = snapshot.PlayerId,
                Position = snapshot.Motion.Position,
                Facing = snapshot.Motion.Facing,
                IsDashing = snapshot.Motion.IsDashing,
                IsStunned = snapshot.Motion.IsStunned,
                CarriedNormal = snapshot.CarriedNormal,
                CarriedGolden = snapshot.CarriedGolden,
                Score = snapshot.Score,
                DashCooldownLeft = snapshot.Motion.DashCooldownLeft
            };
        }

        private static void FindBracket(SnapshotBuffer buffer, double renderTime, float maxExtrapolation,
            out SnapshotMessage from, out SnapshotMessage to, out float blend)
        {
            from = buffer[0];
            to = buffer[0];
            blend = 0f;
            if (buffer.Count == 1 || renderTime <= from.HostTime)
            {
                return;
            }

            for (int i = 1; i < buffer.Count; i++)
            {
                from = buffer[i - 1];
                to = buffer[i];
                if (renderTime <= to.HostTime)
                {
                    break;
                }
            }

            double span = to.HostTime - from.HostTime;
            if (span <= 0.0)
            {
                return;
            }

            double maxBlend = 1.0 + maxExtrapolation / span;
            blend = (float)System.Math.Min((renderTime - from.HostTime) / span, maxBlend);
        }

        private static void BlendPlayers(SnapshotMessage from, SnapshotMessage to, float blend, WorldRenderState target)
        {
            foreach (PlayerSnapshot newer in to.Players)
            {
                PlayerSnapshot older = FindPlayer(from, newer.PlayerId, newer);
                PlayerSnapshot discrete = blend >= DiscreteSwitchPoint ? newer : older;
                PlayerRenderState state = ToRenderState(discrete);
                state.Position = Vector2.LerpUnclamped(older.Motion.Position, newer.Motion.Position, blend);
                Vector2 facing = Vector2.Lerp(older.Motion.Facing, newer.Motion.Facing, Mathf.Clamp01(blend));
                state.Facing = facing.sqrMagnitude > 0f ? facing.normalized : newer.Motion.Facing;
                target.Players.Add(state);
            }
        }

        private void BlendFireflies(SnapshotMessage from, SnapshotMessage to, float blend, WorldRenderState target)
        {
            _previousFireflies.Clear();
            foreach (FireflySnapshot firefly in from.Fireflies)
            {
                _previousFireflies[firefly.FireflyId] = firefly;
            }

            foreach (FireflySnapshot newer in to.Fireflies)
            {
                Vector2 position = newer.Position;
                if (_previousFireflies.TryGetValue(newer.FireflyId, out FireflySnapshot older))
                {
                    position = Vector2.Lerp(older.Position, newer.Position, blend);
                }

                target.Fireflies.Add(new FireflyRenderState(newer.FireflyId, position, newer.IsGolden));
            }
        }

        private static void SampleLatest(SnapshotMessage latest, WorldRenderState target)
        {
            foreach (PlayerSnapshot player in latest.Players)
            {
                target.Players.Add(ToRenderState(player));
            }

            foreach (FireflySnapshot firefly in latest.Fireflies)
            {
                target.Fireflies.Add(new FireflyRenderState(firefly.FireflyId, firefly.Position, firefly.IsGolden));
            }
        }

        private static PlayerSnapshot FindPlayer(SnapshotMessage snapshot, byte playerId, PlayerSnapshot fallback)
        {
            foreach (PlayerSnapshot player in snapshot.Players)
            {
                if (player.PlayerId == playerId)
                {
                    return player;
                }
            }

            return fallback;
        }
    }
}
