using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class OtherHit : OutgoingPacket
    {
        public int Time;
        public ushort BulletId;
        public int OwnerId;
        public int TargetId;
    }
}
