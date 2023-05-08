using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    public class UseItem : OutgoingPacket
    {
        public int Time;
        public SlotObjectData SlotObjectData;
        public WorldPosData ItemUsePosition;
        public byte useType;
    }
}
