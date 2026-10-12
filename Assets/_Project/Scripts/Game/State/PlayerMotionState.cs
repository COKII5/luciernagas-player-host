using System.IO;
using Fireflies.Net.Protocol;
using UnityEngine;
using Fireflies.Shared;

namespace Fireflies.Game.State
{
    public struct PlayerMotionState
    {
        public Vector2 Position;
        public Vector2 Facing;
        public Vector2 DashDirection;
        public float DashTimeLeft;
        public float DashCooldownLeft;
        public float StunTimeLeft;

        public bool IsDashing => DashTimeLeft > 0f;
        public bool IsStunned => StunTimeLeft > 0f;

        public static PlayerMotionState At(Vector2 position, Vector2 facing)
        {
            return new PlayerMotionState { Position = position, Facing = facing.normalized };
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteVector2(Position);
            writer.WriteVector2(Facing);
            writer.WriteVector2(DashDirection);
            writer.Write(DashTimeLeft);
            writer.Write(DashCooldownLeft);
            writer.Write(StunTimeLeft);
        }

        public static PlayerMotionState Read(BinaryReader reader)
        {
            return new PlayerMotionState
            {
                Position = reader.ReadVector2(),
                Facing = reader.ReadVector2(),
                DashDirection = reader.ReadVector2(),
                DashTimeLeft = reader.ReadFiniteSingle(),
                DashCooldownLeft = reader.ReadFiniteSingle(),
                StunTimeLeft = reader.ReadFiniteSingle()
            };
        }
    }
}
