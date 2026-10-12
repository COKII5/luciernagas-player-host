using UnityEngine;

namespace Fireflies.Session
{
    public readonly struct LocalInput
    {
        public LocalInput(Vector2 move, bool dashPressed)
        {
            Move = move;
            DashPressed = dashPressed;
        }

        public Vector2 Move { get; }
        public bool DashPressed { get; }
    }
}
