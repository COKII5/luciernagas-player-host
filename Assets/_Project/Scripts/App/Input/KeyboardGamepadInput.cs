using Fireflies.Session;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fireflies.App.Input
{
    // La embestida se guarda en Update hasta el siguiente FixedUpdate para no perder pulsaciones cortas.
    public sealed class KeyboardGamepadInput : MonoBehaviour, IInputSource
    {
        private bool _dashLatched;

        private void Update()
        {
            if (WasDashPressedThisFrame())
            {
                _dashLatched = true;
            }
        }

        public LocalInput ConsumeFixedStepInput()
        {
            LocalInput input = new LocalInput(ReadMove(), _dashLatched);
            _dashLatched = false;
            return input;
        }

        private static Vector2 ReadMove()
        {
            Vector2 move = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                move.x = AxisFromKeys(
                    keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed,
                    keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
                move.y = AxisFromKeys(
                    keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed,
                    keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && move == Vector2.zero)
            {
                move = gamepad.leftStick.ReadValue();
            }

            return Vector2.ClampMagnitude(move, 1f);
        }

        private static float AxisFromKeys(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private static bool WasDashPressedThisFrame()
        {
            bool keyboardDash = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool gamepadDash = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return keyboardDash || gamepadDash;
        }
    }
}
