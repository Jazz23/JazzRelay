using JazzRelay.Packets;
using System;
using System.Collections.Generic;
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

        public async Task HookUseItem(Client client, UseItem packet)
        {
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
            }
            await Task.WhenAll(tasks);
            await Task.Delay(10000);

            packet.Send = false;
            return;
            Task.Run(async () =>
            {
                var tasks = new List<Task>();
                for (int i = 0; i < 1000; i++)
                {
                    var useItem = new UseItem
                    {
                        ItemUsePosition = packet.ItemUsePosition,
                        SlotObjectData = packet.SlotObjectData,
                        Time = packet.Time + i * 500,
                        useType = packet.useType
                    };
                    // useItem.SlotObjectData.objectType = 2564;
                    useItem.SlotObjectData.slotId = 1;
                    tasks.Add(Task.Run(async () => {
                        //await Task.Delay(i);
                        await client.SendToServer(useItem);
                        //await client.SendToServer(lastLoad);
                    }));
                }
                await Task.WhenAll(tasks);
            });
        }
    }
}
