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
        public int objectId;
        public WorldPosData position;
        [CompressedArray]
        public StatData[] stats;
        public void Read(PacketReader r)
        {
            objectId = r.ReadCompressed();
            (position = new WorldPosData()).Read(r);
            stats = r.ReadArray<StatData>(true);
        }
        public void Write(PacketWriter w)
        {
            w.WriteCompressed(objectId);
            position.Write(w);
            w.Write(stats, true);
        }
    }
}
