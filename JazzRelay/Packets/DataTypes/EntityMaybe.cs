using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    internal class Entity : IDataType
    {
        public short X;
        public short Y;
        public ushort Type;
        public void Read(PacketReader pr)
        {
            X = pr.ReadInt16();
            Y = pr.ReadInt16();
            Type = pr.ReadUInt16();
        }

        public void Write(PacketWriter pw)
        {
            pw.Write(X);
            pw.Write(Y);
            pw.Write(Type);
        }
    }
}
