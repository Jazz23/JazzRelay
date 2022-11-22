#pragma warning disable 8618

using JazzRelay.Packets.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class NewTick : IncomingPacket
    {
        public int tickId;
        public int tickTime;
        public uint serverRealTimeMs;
        public ushort lastServerRealTimeMs;
        public ObjectStatusData[] statuses;
    }
}
