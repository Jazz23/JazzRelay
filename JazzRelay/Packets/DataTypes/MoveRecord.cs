using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    internal class MoveRecord : WorldPosData
    {
        public int Time;
        public override void Read(PacketReader r)
        {
            Time = r.ReadInt32();
            base.Read(r);
        }

        public override void Write(PacketWriter w)
        {
            w.Write(Time);
            base.Write(w);
        }
    }
}
