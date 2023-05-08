using JazzRelay.Packets;
using JazzRelay.Plugins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    [PluginDisabled]
    internal class AutoNexus : IPlugin
    {
        public async void HookPlayerHit(Client client, PlayerHit packet)
        {
            if (client.Dead)
            {
                Console.WriteLine("Autonexused");
                client.KillServerConnection();
                client.Nexus();
            }
            //await Task.Delay(500);
            //Check(client);
            //packet.Send = client.Connected;
            //await Task.Delay(500);
            //Check(client);
            //packet.Send = client.Connected;
        }

        public void HookDamage(Client client, Damage packet)
        {
            if (packet.targetId == client.ObjectId)
            {
                Console.WriteLine("We got hit!");
            }
        }

        public async void HookProjectileAck(Client client, ProjectileAck packet)
        {
            await Task.Delay(500);
            if (packet.Time > 0)
                packet.Time += 500;
            //Task.Run(async () =>
            //{
            //    await Task.Delay(500);
            //    Console.WriteLine($"MEME {client.PrevHealth}");
            //    if (packet.Time > 0) packet.Time += 500;
            //    await client.SendToServer(packet);
            //});
        }
    }
}
