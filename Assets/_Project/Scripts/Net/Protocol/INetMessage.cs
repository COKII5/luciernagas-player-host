using System.IO;

namespace Fireflies.Net.Protocol
{
    public interface INetMessage
    {
        PacketType Type { get; }

        void Write(BinaryWriter writer);
    }
}
