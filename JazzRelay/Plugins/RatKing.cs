using JazzRelay.Enums;
using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Plugins.Utils;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace JazzRelay.Plugins
{
    [PluginDisabled]
    internal class RatKing : IPlugin
    {
        public Dictionary<string, Client> _clients = new();
        public void HookNewTick(Client client, NewTick packet)
        {
            if (packet.tickId != 1) return;
            _clients[client.Name] = client;
        }
        public async Task HookPlayerHit(Client client, PlayerHit packet)
        {
            packet.Send = false;
            var otherName = _clients.Values.First(x => x != client).Name;
            var otherClient = client.Entities.First(x => x.Value.Stats.Name() == otherName).Value;
            await client.SendToServer(new OtherHit
            {
                BulletId = packet.bulletId,
                OwnerId = packet.objectId,
                TargetId = otherClient.Stats.ObjectId,
                Time = client.Time
            });
        }
    }
}
