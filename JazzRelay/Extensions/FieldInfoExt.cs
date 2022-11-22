using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Extensions
{
    internal static class FieldInfoExt
    {

        private static Dictionary<Type, Func<PacketReader, object>> _readTypes = new Dictionary<Type, Func<PacketReader, object>>
        {
            { typeof(int), pr => pr.ReadInt32() },
            { typeof(string), pr => pr.ReadUTF() },
            { typeof(byte[]), pr => pr.ReadByteArray() },
            { typeof(short), pr => pr.ReadInt16() },
            { typeof(ushort), pr => pr.ReadUInt16() },
            { typeof(bool), pr => pr.ReadBoolean() },
            { typeof(ObjectStatusData[]), pr => pr.ReadArray<ObjectStatusData>() },
            { typeof(uint), pr => pr.ReadUInt32() }
        };

        private static Dictionary<Type, Action<PacketWriter, object>> _writeTypes = new Dictionary<Type, Action<PacketWriter, object>>
        {
            { typeof(int), (pw, v) => pw.Write((int)v)},
            { typeof(string), (pw, v) => pw.Write((string)v) },
            { typeof(byte[]), (pw, v) => pw.WriteByteArray((byte[])v) },
            { typeof(short), (pw, v) => pw.Write((short)v) },
            { typeof(ushort), (pw, v) => pw.Write((ushort)v) },
            { typeof(bool), (pw, v) => pw.Write((bool)v) },
            { typeof(ObjectStatusData[]), (pw, v) => pw.Write((ObjectStatusData[])v) },
            { typeof(uint), (pw, v) => pw.Write((uint)v) }
        };
        public static void SetFromReader(this FieldInfo field, object obj, PacketReader reader)
        {
            Func<PacketReader, object>? deserializer;
            if (!_readTypes.TryGetValue(field.FieldType, out deserializer))
            {
                throw new NotSupportedException(string.Format(
                    "Type of property '{0}' isn't supported ({1}).", field.Name, field.FieldType));
            }

            var deserialized = deserializer(reader);
            field.SetValue(obj, deserialized);
        }

        public static void WriteToWriter(this FieldInfo field, object obj, PacketWriter writer)
        {
            Action<PacketWriter, object>? deserializer;
            if (!_writeTypes.TryGetValue(field.FieldType, out deserializer))
            {
                throw new NotSupportedException(string.Format(
                    "Type of property '{0}' isn't supported ({1}).", field.Name, field.FieldType));
            }

            object? value = field.GetValue(obj);
            if (value == null)
                throw new Exception($"Null value found in packet {obj.GetType().Name}!");
            deserializer.Invoke(writer, value);
        }
    }
}
