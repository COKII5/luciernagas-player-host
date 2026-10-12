using Fireflies.Session;
using UnityEngine;

namespace Fireflies.App.Input
{
    // Solo para pruebas de carga con la opción -autopilot.
    public sealed class AutopilotInput : IInputSource
    {
        private const float MinWanderSeconds = 0.6f;
        private const float MaxWanderSeconds = 2f;
        private const float DashChancePerStep = 0.01f;

        private readonly System.Random _random = new();
        private Vector2 _direction;
        private float _secondsUntilTurn;

        public LocalInput ConsumeFixedStepInput()
        {
            _secondsUntilTurn -= Time.fixedDeltaTime;
            if (_secondsUntilTurn <= 0f)
            {
                float angle = (float)(_random.NextDouble() * Mathf.PI * 2.0);
                _direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _secondsUntilTurn = Mathf.Lerp(MinWanderSeconds, MaxWanderSeconds, (float)_random.NextDouble());
            }

            bool dash = _random.NextDouble() < DashChancePerStep;
            return new LocalInput(_direction, dash);
        }
    }
}
