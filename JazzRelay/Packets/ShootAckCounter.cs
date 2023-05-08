using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class ShootAckCounter : OutgoingPacket
    {
        public int Time;
        public short Amount = 1;
    }
}
