using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    internal class Damage : IncomingPacket
    {
        public int targetId;
        public byte[] effects;
        public ushort damageAmount;
        public byte flags;
        public bool _8IpInt7MqhLBDklNtyy4AAx09jx;
        public bool _fZ49KOSMgbnLzrBeBl8rEAbIqqB;
        public bool armorPierce;
        public ushort bulletId;
        public int objectId;

        public override void Read(PacketReader r)
        {
            targetId = r.ReadInt32();
            effects = new byte[r.ReadByte()];
            for (var i = 0; i < effects.Length; i++)
            {
                effects[i] = r.ReadByte();
            }

            damageAmount = r.ReadUInt16();
            flags = r.ReadByte();
            _8IpInt7MqhLBDklNtyy4AAx09jx = (flags & 1) > 0;
            _fZ49KOSMgbnLzrBeBl8rEAbIqqB = (flags & 2) > 0;
            armorPierce = (flags & 4) > 0;
            bulletId = r.ReadUInt16();
            objectId = r.ReadInt32();
        }
    }
}
