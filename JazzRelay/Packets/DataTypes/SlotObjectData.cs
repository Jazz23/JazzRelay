using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    internal class SlotObjectData : IDataType
    {
        public int objectId;
        public int objectType;
        public int slotId;
        public byte Unknown = 0;

        public void Read(PacketReader r)
        {
            objectId = r.ReadInt32();
            objectType = r.ReadInt32();
            slotId = r.ReadInt32();
            Unknown = r.ReadByte();
        }

        public void Write(PacketWriter w)
        {
            w.Write(objectId);
            w.Write(slotId);
            w.Write(objectType);
            w.Write(Unknown);
        }
    }
}
