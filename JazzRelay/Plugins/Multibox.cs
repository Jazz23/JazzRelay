using JazzRelay.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    internal class Multibox : IPlugin
    {
        //public void HookHello(Client client, Hello packet)
        //{

        //}

        public async Task HookPlayerText(Client client, PlayerText packet)
        {
            if (packet.Text == "dez")
            {
                packet.Send = false;
                await client.SendToServer(new PlayerText() { Text = "balls" });
            }
        }

        public void HookFailure(Client client, Failure packet)
        {

        }
    }
}
