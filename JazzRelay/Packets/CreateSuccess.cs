using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class CreateSuccess : IncomingPacket
    {
        public int objectId;
        public int characterId;
        public string unknown;
    }
}
