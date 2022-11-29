using JazzRelay.Packets;
using JazzRelay.Plugins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    [PluginEnabled]
    internal class Location : IPlugin
    {
        public void HookPlayerText(Client client, PlayerText packet)
        {
            if (packet.Text == "loc")
            {
                packet.Send = false;
                Console.WriteLine(client.Position);
            }
        }
    }
}
