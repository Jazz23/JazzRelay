#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()

using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets.DataTypes
{
    public class WorldPosData : IDataType
    {
        public float X { get; set; }
        public float Y { get; set; }

        public WorldPosData() { }

        public WorldPosData(float x, float y) { X = x; Y = y; }

        public WorldPosData(PacketReader r)
        {
            Read(r);
        }

        public virtual void Read(PacketReader r)
        {
            X = r.ReadSingle();
            Y = r.ReadSingle();
        }
        public virtual void Write(PacketWriter w)
        {
            w.Write(X);
            w.Write(Y);
        }

        public bool IsEqualTo(WorldPosData? b)
        {
            return b != null && X == b.X && Y == b.Y;
        }

        public float DistanceTo(WorldPosData pos) => (float)Math.Sqrt((X - pos.X) * (X - pos.X) + (Y - pos.Y) * (Y - pos.Y));

        public override string ToString() => $"x: {X}, y: {Y}";
    }
}
