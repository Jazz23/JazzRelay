using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    public interface IDataType
    {
        public void Read(PacketReader r);
        public void Write(PacketWriter w);
    }
}
