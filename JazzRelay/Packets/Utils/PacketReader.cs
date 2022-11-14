using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.Utils
{
    public class PacketReader : BinaryReader
    {
        public PacketReader(Stream input) : base(input)
        {
        }

        public override short ReadInt16()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt16());
        }

        public override ushort ReadUInt16()
        {
            return (ushort)IPAddress.NetworkToHostOrder((short)base.ReadUInt16());
        }

        public override int ReadInt32()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt32());
        }

        public override float ReadSingle()
        {
            var array = base.ReadBytes(4);
            Array.Reverse(array);
            return BitConverter.ToSingle(array, 0);
        }

        public int ReadCompressed()
        {
            var b = ReadByte();
            var flag = (b & 64) > 0;
            var num = 6;
            var num2 = b & 63;
            while ((b & 128) > 0)
            {
                b = ReadByte();
                num2 |= (b & 127) << num;
                num += 7;
            }

            return flag ? -num2 : num2;
        }

        public string ReadUTF()
        {
            return Encoding.UTF8.GetString(ReadBytes(ReadInt16()));
        }

        public string ReadUTF32()
        {
            return Encoding.UTF8.GetString(ReadBytes(ReadInt32()));
        }

        public byte[] ReadByteArray() => ReadBytes(ReadInt16());

        public int Remaining()
        {
            return (int)(BaseStream.Length - BaseStream.Position);
        }
    }
}
