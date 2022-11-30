#pragma warning disable 8618

using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    public class ObjectStatusData : IDataType
    {
        public int ObjectId;
        public WorldPosData Position;
        [CompressedArray]
        public StatData[] Stats;
        public void Read(PacketReader r)
        {
            ObjectId = r.ReadCompressed();
            (Position = new WorldPosData()).Read(r);
            Stats = r.ReadArray<StatData>(true);
        }
        public void Write(PacketWriter w)
        {
            w.WriteCompressed(ObjectId);
            Position.Write(w);
            w.Write(Stats, true);
        }
    }
}
