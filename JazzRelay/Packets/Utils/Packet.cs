using JazzRelay.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.Utils
{
    public class Packet
    { 
        public PacketType PacketType => (PacketType)Enum.Parse(typeof(PacketType), this.GetType().Name);
        public bool Send = true;
        public virtual void Read(PacketReader r) { }
        public virtual void Write(PacketWriter w) { }
    }

    public class IncomingPacket : Packet { }
    public class OutgoingPacket : Packet { }
}
