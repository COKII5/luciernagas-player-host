using System.IO;
using Fireflies.Net.Protocol;
using UnityEngine;
using Fireflies.Shared;

namespace Fireflies.Game.State
{
    public struct FireflySnapshot
    {
        public ushort FireflyId;
        public Vector2 Position;
        public bool IsGolden;

        public void Write(BinaryWriter writer)
        {
            writer.Write(FireflyId);
            writer.WriteVector2(Position);
            writer.Write(IsGolden);
        }

        public static FireflySnapshot Read(BinaryReader reader)
        {
            return new FireflySnapshot
            {
                FireflyId = reader.ReadUInt16(),
                Position = reader.ReadVector2(),
                IsGolden = reader.ReadBoolean()
            };
        }
    }
}
