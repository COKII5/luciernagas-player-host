using System.IO;
using UnityEngine;

namespace Fireflies.Game.State
{
    // Se cuantiza antes de simular para que cliente y host apliquen exactamente el mismo valor.
    public readonly struct InputCommand
    {
        private const float QuantizationScale = 127f;

        private readonly sbyte _moveX;
        private readonly sbyte _moveY;

        public InputCommand(uint sequence, Vector2 move, bool dashPressed)
        {
            Vector2 clamped = Vector2.ClampMagnitude(move, 1f);
            Sequence = sequence;
            _moveX = (sbyte)Mathf.RoundToInt(clamped.x * QuantizationScale);
            _moveY = (sbyte)Mathf.RoundToInt(clamped.y * QuantizationScale);
            DashPressed = dashPressed;
        }

        private InputCommand(uint sequence, sbyte moveX, sbyte moveY, bool dashPressed)
        {
            Sequence = sequence;
            _moveX = moveX;
            _moveY = moveY;
            DashPressed = dashPressed;
        }

        public uint Sequence { get; }
        public bool DashPressed { get; }
        public Vector2 Move => Vector2.ClampMagnitude(new Vector2(_moveX, _moveY) / QuantizationScale, 1f);

        public InputCommand WithoutDash()
        {
            return new InputCommand(Sequence, _moveX, _moveY, false);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Sequence);
            writer.Write(_moveX);
            writer.Write(_moveY);
            writer.Write(DashPressed);
        }

        public static InputCommand Read(BinaryReader reader)
        {
            uint sequence = reader.ReadUInt32();
            sbyte moveX = reader.ReadSByte();
            sbyte moveY = reader.ReadSByte();
            bool dash = reader.ReadBoolean();
            return new InputCommand(sequence, moveX, moveY, dash);
        }
    }
}
