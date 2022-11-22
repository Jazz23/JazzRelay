#pragma warning disable 8618

using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    internal class StatData : IDataType
    {
        public byte magicNumber;
        public byte statType;
        public int statValue;
        public string stringValue;

        public StatData() { }
        public StatData(PacketReader r) => Read(r);

        public void Read(PacketReader r)
        {
            statType = r.ReadByte();
            if (!IsStringStat())
                statValue = r.ReadCompressed();
            else
                stringValue = r.ReadUTF();

            magicNumber = r.ReadByte();
        }

        public void Write(PacketWriter w)
        {
            w.Write(statType);
            if (!IsStringStat())
                w.WriteCompressed(statValue);
            else
                w.WriteUTF(stringValue);

            w.Write(magicNumber);
        }

        public bool IsStringStat()
        {
            return statType switch
            {
                (byte)StatDataType.Name => true,
                (byte)StatDataType.AccountId => true,
                (byte)StatDataType.Experience => true,
                (byte)StatDataType.GuildName => true,
                (byte)StatDataType.PetName => true,
                (byte)StatDataType.GraveAccountId => true,
                (byte)StatDataType.Skin => true,
                (byte)StatDataType.Unknown121 => true,
                //(byte) StatDataType.Unknown122 => true,
                (byte)StatDataType.Unknown123 => true,
                _ => false
            };
        }
    }
}
