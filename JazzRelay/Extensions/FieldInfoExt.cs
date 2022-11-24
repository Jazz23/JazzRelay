using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using System;
using System.Collections;
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

        private static Dictionary<Type, Func<PacketReader, FieldInfo, object>> _readTypes = new Dictionary<Type, Func<PacketReader, FieldInfo, object>>
        {
            { typeof(int), (pr, f) => pr.ReadInt32() },
            { typeof(string), (pr, f) => pr.ReadUTF() },
            { typeof(byte[]), (pr, f) => pr.ReadByteArray() },
            { typeof(short), (pr, f) => pr.ReadInt16() },
            { typeof(ushort), (pr, f) => pr.ReadUInt16() },
            { typeof(bool), (pr, f) => pr.ReadBoolean() },
            { typeof(ObjectStatusData[]), (pr, f) => pr.ReadArray<ObjectStatusData>(f) },
            { typeof(uint), (pr, f) => pr.ReadUInt32() },
            { typeof(sbyte), (pr, f) => pr.ReadSByte() },
            { typeof(WorldPosData), (pr, f) => new WorldPosData(pr) },
            { typeof(float), (pr, f) => pr.ReadSingle() },
            { typeof(byte), (pr, f) => pr.ReadByte() },
            { typeof(StatData), (pr, f) => new StatData(pr) },
            { typeof(MoveRecord[]), (pr, f) => pr.ReadArray<MoveRecord>(f) }
        };

        private static Dictionary<Type, Action<PacketWriter, object, FieldInfo>> _writeTypes = new Dictionary<Type, Action<PacketWriter, object, FieldInfo>>
        {
            { typeof(int), (pw, v, f) => pw.Write((int)v)},
            { typeof(string), (pw, v, f) => pw.Write((string)v) },
            { typeof(byte[]), (pw, v, f) => pw.WriteByteArray((byte[])v) },
            { typeof(short), (pw, v, f) => pw.Write((short)v) },
            { typeof(ushort), (pw, v, f) => pw.Write((ushort)v) },
            { typeof(bool), (pw, v, f) => pw.Write((bool)v) },
            { typeof(ObjectStatusData[]), (pw, v, f) => pw.Write((ObjectStatusData[])v, f) },
            { typeof(uint), (pw, v, f) => pw.Write((uint)v) },
            { typeof(sbyte), (pw, v, f) => pw.Write((sbyte)v) },
            { typeof(WorldPosData), (pw, v, f) => ((WorldPosData)v).Write(pw) },
            { typeof(float), (pw, v, f) => pw.Write((float)v) },
            { typeof(byte), (pw, v, f) => pw.Write((byte)v) },
            { typeof(StatData), (pw, v, f) => ((StatData)v).Write(pw) },
            { typeof(MoveRecord[]), (pw, v, f) => pw.Write((MoveRecord[])v, f) }
        };

        public static void SetFromReader(this FieldInfo field, object obj, PacketReader reader)
        {
            Func<PacketReader, FieldInfo, object>? deserializer;
            if (!_readTypes.TryGetValue(field.FieldType, out deserializer))
            {
                throw new NotSupportedException(string.Format(
                    "Type of property '{0}' isn't supported ({1}).", field.Name, field.FieldType));
            }

            var deserialized = deserializer(reader, field);
            field.SetValue(obj, deserialized);
        }

        public static void WriteToWriter(this FieldInfo field, object obj, PacketWriter writer)
        {
            Action<PacketWriter, object, FieldInfo>? deserializer;
            if (!_writeTypes.TryGetValue(field.FieldType, out deserializer))
            {
                throw new NotSupportedException(string.Format(
                    "Type of property '{0}' isn't supported ({1}).", field.Name, field.FieldType));
            }

            object value = field.GetValue(obj) ?? throw new Exception($"Null value found in packet {obj.GetType().Name}!");
            deserializer.Invoke(writer, value, field);
        }

        //public static bool IsEnumerable(this Type type) => typeof(IEnumerable).IsAssignableFrom(type);
        //public static Type? GetEnumerableUnderlyingType(this Type type)
        //{
        //    if (!type.IsEnumerable()) return null;
        //    return type.IsArray ? type.GetElementType() : type.GetGenericArguments().SingleOrDefault(); //Default is null
        //}
    }
}
