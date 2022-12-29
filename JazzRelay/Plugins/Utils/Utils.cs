using JazzRelay.DataTypes;
using JazzRelay.Enums;
using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins.Utils
{
    internal static class Utils
    {
        public static async Task SendNotification(this Client client, string text)
        {
            await client.SendToClient(new Notification()
            {
                Message = text,
                ObjectId = client.ObjectId,
                Byte1 = 6,
                Color = 65535
            });
        }

        public static string? Name(this ObjectStatusData stats) => stats.Stats.FirstOrDefault(x => x.statType == (byte)StatDataType.Name)?.stringValue;

        public static int? FindPlayer(this Client client, string name) => client.Entities.Values.FirstOrDefault(x => x.Stats.Name() == name)?.Stats.ObjectId;

        public static bool IsNexus(this ConnectInfo info) => JazzRelay.FindServerByHost(info.Reconnect.host) != null;

        public static Reconnect CloneReconnect(this Reconnect packet)
        {
            return new Reconnect()
            {
                host = packet.host,
                Port = packet.Port,
                GameId = packet.GameId,
                MapName = packet.MapName,
                Key = packet.Key,
                KeyTime = packet.KeyTime
            };
        }

        public static WorldPosData Copy(this WorldPosData pos) => new WorldPosData(pos.X, pos.Y);
    }
}
