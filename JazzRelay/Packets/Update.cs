using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class Update : IncomingPacket
    {
        public WorldPosData Pos;
        public byte Byte1;
        [CompressedArray]
        public Tile[] Tiles;
        [CompressedArray]
        public Entity[] Entities;
        [CompressedArray]
        public int[] Drops;
    }
}
