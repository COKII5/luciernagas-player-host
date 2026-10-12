using System.Collections.Generic;
using System.IO;
using Fireflies.Game.State;
using Fireflies.Net.Protocol;
using Fireflies.Shared;

namespace Fireflies.Net.Messages
{
    // Se reenvían los últimos comandos en cada paquete: si uno se pierde, el siguiente lo cubre.
    public sealed class InputMessage : INetMessage
    {
        private const int MaxCommands = 16;

        public InputMessage(IReadOnlyList<InputCommand> commands)
        {
            Commands = commands;
        }

        public PacketType Type => PacketType.Input;
        public IReadOnlyList<InputCommand> Commands { get; }

        public void Write(BinaryWriter writer)
        {
            writer.WriteList(Commands, (output, command) => command.Write(output));
        }

        public static InputMessage Read(BinaryReader reader)
        {
            return new InputMessage(reader.ReadList(MaxCommands, InputCommand.Read));
        }
    }
}
