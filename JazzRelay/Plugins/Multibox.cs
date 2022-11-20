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
        public void HookHello(Client client, Hello packet)
        {

        }

        //public void HookLoad(Client client, Load packet) { packet._awb0tbsCnBkmxlCKfoiCWtLYpjO = true; }

        public async Task HookPlayerText(Client client, PlayerText packet)
        {
            client.States["test"] = 4;
            if (packet.Text == "dez")
            {
                packet.Send = false;
                await client.SendToServer(new PlayerText() { Text = "balls" });
            }
        }

        public void HookFailure(Client client, Failure packet) { }
    }
}
