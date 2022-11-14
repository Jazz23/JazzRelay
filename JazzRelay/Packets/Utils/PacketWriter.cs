using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    }
}
