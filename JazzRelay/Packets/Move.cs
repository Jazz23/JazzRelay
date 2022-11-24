#pragma warning disable 0649

using JazzRelay.Packets.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class Move : OutgoingPacket
    {
        public int Id;
        public int Time; //Different from moverec time idk why
        public MoveRecord[] MoveRecs;
    }
}
