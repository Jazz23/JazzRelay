using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class ServerPlayerShoot : IncomingPacket
    {
        public short bulletId;
        public int ownerId;
        public int containerType;
        public WorldPosData position;
        public float angle;
        public short damage;
    }
}
