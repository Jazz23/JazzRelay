using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class FeedPet : OutgoingPacket
    {
        public byte petType;
        public int pidOne;
        public int pidTwo;
        public int objectId;
        public byte paymentType;
        public SlotObjectData[] slots;
    }
}
