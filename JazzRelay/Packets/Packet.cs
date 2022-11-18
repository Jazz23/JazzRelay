using JazzRelay.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class Packet
    { 
        public PacketType PacketType => (PacketType)Enum.Parse(typeof(PacketType), this.GetType().Name);
        public bool Send = true; 
    }

    internal class IncomingPacket : Packet { }
    internal class OutgoingPacket : Packet { }
}
