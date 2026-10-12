using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Fireflies.Shared
{
    // Todo lo que llega de la red pasa por aquí, así que cada lectura valida rangos y valores finitos.
    public static class BinaryStreamExtensions
    {
        public const int MaxShortStringBytes = 64;

        public static void WriteVector2(this BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            float x = reader.ReadFiniteSingle();
            float y = reader.ReadFiniteSingle();
            return new Vector2(x, y);
        }

        public static float ReadFiniteSingle(this BinaryReader reader)
        {
            float value = reader.ReadSingle();
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidDataException("Non-finite float in payload.");
            }

            return value;
        }

        public static double ReadFiniteDouble(this BinaryReader reader)
        {
            double value = reader.ReadDouble();
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidDataException("Non-finite double in payload.");
            }

            return value;
        }

        public static void WriteShortString(this BinaryWriter writer, string value)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(value ?? string.Empty);
            if (encoded.Length > MaxShortStringBytes)
            {
                throw new InvalidDataException("String too long for the protocol.");
            }

            writer.Write((byte)encoded.Length);
            writer.Write(encoded);
        }

        public static string ReadShortString(this BinaryReader reader)
        {
            int length = reader.ReadByte();
            if (length > MaxShortStringBytes)
            {
                throw new InvalidDataException("String length exceeds protocol limit.");
            }

            byte[] encoded = reader.ReadBytes(length);
            if (encoded.Length != length)
            {
                throw new EndOfStreamException();
            }

            return Encoding.UTF8.GetString(encoded);
        }

        public static TEnum ReadDefinedEnum<TEnum>(this BinaryReader reader) where TEnum : struct, Enum
        {
            byte rawValue = reader.ReadByte();
            if (!Enum.IsDefined(typeof(TEnum), rawValue))
            {
                throw new InvalidDataException($"Unknown {typeof(TEnum).Name} value {rawValue}.");
            }

            return (TEnum)Enum.ToObject(typeof(TEnum), rawValue);
        }

        public static void WriteList<T>(this BinaryWriter writer, IReadOnlyList<T> items, Action<BinaryWriter, T> writeItem)
        {
            if (items.Count > byte.MaxValue)
            {
                throw new InvalidDataException("Too many items for a byte-prefixed list.");
            }

            writer.Write((byte)items.Count);
            foreach (T item in items)
            {
                writeItem(writer, item);
            }
        }

        public static List<T> ReadList<T>(this BinaryReader reader, int maxCount, Func<BinaryReader, T> readItem)
        {
            int count = reader.ReadByte();
            if (count > maxCount)
            {
                throw new InvalidDataException($"List count {count} exceeds limit {maxCount}.");
            }

            List<T> items = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                items.Add(readItem(reader));
            }

            return items;
        }
    }
}
