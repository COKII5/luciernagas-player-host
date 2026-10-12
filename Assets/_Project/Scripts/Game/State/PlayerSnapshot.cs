using System.IO;

namespace Fireflies.Game.State
{
    public struct PlayerSnapshot
    {
        public byte PlayerId;
        public PlayerMotionState Motion;
        public byte CarriedNormal;
        public byte CarriedGolden;
        public ushort Score;
        public uint LastProcessedInput;

        public int CarriedCount => CarriedNormal + CarriedGolden;

        public void Write(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            Motion.Write(writer);
            writer.Write(CarriedNormal);
            writer.Write(CarriedGolden);
            writer.Write(Score);
            writer.Write(LastProcessedInput);
        }

        public static PlayerSnapshot Read(BinaryReader reader)
        {
            return new PlayerSnapshot
            {
                PlayerId = reader.ReadByte(),
                Motion = PlayerMotionState.Read(reader),
                CarriedNormal = reader.ReadByte(),
                CarriedGolden = reader.ReadByte(),
                Score = reader.ReadUInt16(),
                LastProcessedInput = reader.ReadUInt32()
            };
        }
    }
}
