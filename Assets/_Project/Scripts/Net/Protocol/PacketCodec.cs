using System;
using System.IO;
using UnityEngine;

namespace Fireflies.Net.Protocol
{
    // Cabecera: protocolo (2) | canal (1) | secuencia (2) | tipo (1) | payload
    public static class PacketCodec
    {
        private const int ProtocolIdOffset = 0;
        private const int ChannelOffset = 2;
        private const int SequenceOffset = 3;
        private const int TypeOffset = 5;

        public static byte[] Encode(DeliveryChannel channel, ushort sequence, INetMessage message)
        {
            using MemoryStream stream = new MemoryStream(64);
            using BinaryWriter writer = new BinaryWriter(stream);
            WriteHeader(writer, channel, sequence, message.Type);
            message.Write(writer);
            return stream.ToArray();
        }

        public static byte[] EncodeUnreliable(INetMessage message)
        {
            return Encode(DeliveryChannel.Unreliable, 0, message);
        }

        public static byte[] EncodeAck(ushort acknowledgedSequence)
        {
            using MemoryStream stream = new MemoryStream(ProtocolConstants.HeaderSize);
            using BinaryWriter writer = new BinaryWriter(stream);
            WriteHeader(writer, DeliveryChannel.Ack, acknowledgedSequence, PacketType.None);
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] datagram, out DecodedPacket packet)
        {
            packet = default;
            if (datagram == null || datagram.Length < ProtocolConstants.HeaderSize)
            {
                return false;
            }

            ushort protocolId = BitConverter.ToUInt16(datagram, ProtocolIdOffset);
            byte channel = datagram[ChannelOffset];
            ushort sequence = BitConverter.ToUInt16(datagram, SequenceOffset);
            byte type = datagram[TypeOffset];

            bool isKnownChannel = Enum.IsDefined(typeof(DeliveryChannel), channel);
            bool isKnownType = Enum.IsDefined(typeof(PacketType), type);
            if (protocolId != ProtocolConstants.ProtocolId || !isKnownChannel || !isKnownType)
            {
                return false;
            }

            packet = new DecodedPacket((DeliveryChannel)channel, sequence, (PacketType)type, datagram);
            return true;
        }

        public static bool TryReadPayload<T>(DecodedPacket packet, Func<BinaryReader, T> read, out T message)
        {
            try
            {
                using BinaryReader reader = packet.CreatePayloadReader();
                message = read(reader);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is ArgumentException)
            {
                Debug.LogWarning($"[PacketCodec] Malformed {packet.Type} packet discarded: {exception.Message}");
                message = default;
                return false;
            }
        }

        private static void WriteHeader(BinaryWriter writer, DeliveryChannel channel, ushort sequence, PacketType type)
        {
            writer.Write(ProtocolConstants.ProtocolId);
            writer.Write((byte)channel);
            writer.Write(sequence);
            writer.Write((byte)type);
        }
    }
}
