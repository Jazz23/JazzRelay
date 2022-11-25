using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class Notification : IncomingPacket
    {
        public byte Byte1;
        public byte Byte2 = 0; //I guess?
        public string Message;
        public int ObjectId;
        public int Color;

        public override void Read(PacketReader r)
        {
            Byte1 = r.ReadByte();
            Byte2 = r.ReadByte();
            if (Byte1 == 6)
            {
                Message = r.ReadString();
                ObjectId = r.ReadInt32();
                Color = r.ReadInt32();
            }
        }

        public override void Write(PacketWriter w)
        {
            w.Write(Byte1);
            w.Write(Byte2);
            if (Byte1 == 6)
            {
                w.Write(Message);
                w.Write(ObjectId);
                w.Write(Color);
            }
        }
    }
}
