using JazzRelay.Packets.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.Utils
{
    public class PacketWriter : BinaryWriter
    {
        public PacketWriter(Stream s) : base(s)
        {
        }

        public override void Write(short value)
        {
            base.Write(IPAddress.NetworkToHostOrder(value));
        }

        public override void Write(ushort value)
        {
            base.Write((ushort)IPAddress.HostToNetworkOrder((short)value));
        }

        public override void Write(int value)
        {
            base.Write(IPAddress.NetworkToHostOrder(value));
        }

        public override void Write(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            base.Write(bytes);
        }

        public override void Write(string value)
        {
            WriteUTF(value);
        }

        public void WriteUTF(string s)
        {
            Write((short)s.Length);
            Write(Encoding.UTF8.GetBytes(s));
        }

        public void WriteUTF32(string s)
        {
            Write(s.Length);
            Write(Encoding.UTF8.GetBytes(s));
        }

        public void WriteByteArray(byte[] value)
        {
            Write((ushort)value.Length);
            Write(value);
        }

        public void WriteCompressed(int value)
        {
            bool flag = value < 0;
            uint num = (uint)(flag ? (-(uint)value) : value);
            byte b = (byte)(num & 63u);
            if (flag)
            {
                b |= 64;
            }
            num >>= 6;
            bool flag2;
            if (flag2 = (num > 0u))
            {
                b |= 128;
            }
            Write(b);
            while (flag2)
            {
                b = (byte)(num & 127u);
                num >>= 7;
                if (flag2 = (num > 0u))
                {
                    b |= 128;
                }
                Write(b);
            }
        }

        public void Write(IDataType[] array, FieldInfo field) => Write(array, Attribute.IsDefined(field, typeof(CompressedArray)));

        public void Write(IDataType[] array, bool compressed = false)
        {
            if (compressed) WriteCompressed(array.Length);
            else Write((short)array.Length);
            foreach (IDataType item in array) item.Write(this);
        }
    }
}
