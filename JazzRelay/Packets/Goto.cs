using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class Goto : IncomingPacket
    {
        public int ObjectId;
        public WorldPosData Position;
        public int Unknown;
    }
}
