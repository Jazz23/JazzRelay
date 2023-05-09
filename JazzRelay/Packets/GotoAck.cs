using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class GotoAck : OutgoingPacket
    {
        public int Time;
        public bool IsValid;
    }
}
