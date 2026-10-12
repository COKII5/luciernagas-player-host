using Fireflies.Config;
using Fireflies.Game.Rules;

namespace Fireflies.Game.Simulation
{
    // El host y la predicción del cliente tienen que usar exactamente los mismos valores.
    public readonly struct MovementTuning
    {
        public MovementTuning(GameplaySettings settings, MatchRules rules)
        {
            MoveSpeed = settings.MoveSpeed;
            DashSpeed = settings.DashSpeed;
            DashDuration = settings.DashDuration;
            ArenaRadius = settings.PlayableRadius;
            DashCooldown = rules.DashCooldown;
            DashEnabled = rules.DashEnabled;
        }

        public float MoveSpeed { get; }
        public float DashSpeed { get; }
        public float DashDuration { get; }
        public float ArenaRadius { get; }
        public float DashCooldown { get; }
        public bool DashEnabled { get; }
    }
}
