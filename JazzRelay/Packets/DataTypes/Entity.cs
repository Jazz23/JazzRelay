using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    public class Entity : IDataType
    {
        public ushort ObjectType;
        public ObjectStatusData Stats;


        public void Read(PacketReader r)
        {
            ObjectType = r.ReadUInt16();
            (Stats = new ObjectStatusData()).Read(r);
        }

        public void Write(PacketWriter w)
        {
            w.Write(ObjectType);
            Stats.Write(w);
        }
    }
}
