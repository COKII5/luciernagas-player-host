using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.State;
using UnityEngine;

namespace Fireflies.Game.Simulation
{
    public static class DashCollisionResolver
    {
        public static void Resolve(IReadOnlyList<SimPlayer> players, GameplaySettings settings, FireflyField field, List<GameEvent> events)
        {
            float hitDistance = settings.PlayerRadius * 2f;
            foreach (SimPlayer attacker in players)
            {
                if (!attacker.Motion.IsDashing)
                {
                    continue;
                }

                SimPlayer victim = FindVictim(players, attacker, hitDistance);
                if (victim != null)
                {
                    ApplyHit(attacker, victim, settings, field, events);
                }
            }
        }

        private static SimPlayer FindVictim(IReadOnlyList<SimPlayer> players, SimPlayer attacker, float hitDistance)
        {
            foreach (SimPlayer candidate in players)
            {
                bool isTarget = candidate != attacker && !candidate.Motion.IsStunned;
                if (isTarget && Vector2.Distance(candidate.Motion.Position, attacker.Motion.Position) <= hitDistance)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void ApplyHit(SimPlayer attacker, SimPlayer victim, GameplaySettings settings, FireflyField field, List<GameEvent> events)
        {
            int dropped = victim.CarriedCount;
            field.Scatter(victim.Motion.Position, victim.CarriedNormal, victim.CarriedGolden);
            victim.ClearCarried();

            Vector2 knockback = attacker.Motion.DashDirection * settings.KnockbackDistance;
            victim.ApplyHit(settings.StunDuration, knockback, settings.PlayableRadius);
            attacker.EndDash();

            events.Add(new GameEvent(GameEventType.DashHit, attacker.PlayerId, victim.PlayerId, (byte)dropped));
        }
    }
}
