using JazzRelay.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace JazzRelay.Plugins
{
    internal class PacketSpam : IPlugin
    {
        Move lastMove;
        Load lastLoad;
        public void HookLoad(Client client, Load packet)
        {
            lastLoad = packet;
        }

        public void HookHello(Client client, Hello packet)
        {
            packet.GameId = -1;
        }
        int time = 0;
        long ourTime = 0;
        Stopwatch sw = Stopwatch.StartNew();
        //public void HookServerPlayerShoot(Client client, ServerPlayerShoot packet)
        //{
        //    packet.bulletId = -1;
        //    //packet.Send = false;
        //    //var shootAck2 = new ShootAckCounter
        //    //{
        //    //    Amount = 1,
        //    //    Time = time + (int)(sw.ElapsedMilliseconds - ourTime)
        //    //};
        //    //await client.SendToServer(shootAck2);
        //}

        public async Task HookUseItem(Client client, UseItem packet)
        {
            // client.PacketBlackList.Add(Enums.PacketType.ShowEffect);
            var tasks = new List<Task>();
            for (int i = 0; i < 350000; i++)
            {
                var useItem = new UseItem
                {
                    ItemUsePosition = packet.ItemUsePosition,
                    SlotObjectData = packet.SlotObjectData,
                    Time = packet.Time + i * 500,
                    useType = packet.useType
                };
                tasks.Add(client.SendToServer(useItem));
                // await client.SendToServer(useItem);
            }
            await Task.WhenAll(tasks);
            await Task.Delay(15000);

            packet.Send = false;
        }
    }
}
