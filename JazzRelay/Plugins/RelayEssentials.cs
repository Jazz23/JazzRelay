using JazzRelay.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    internal class RelayEssentials : IPlugin
    {
        public void HookReconnect(Client client, Reconnect packet)
        {
            if (packet.host == "")
            {
                packet.host = client.ConnectionInfo.Item1;
                packet._XJmNMkJzG2oBMCp91qCtsWWPJUc = (ushort)client.ConnectionInfo.Item2;
            }
            //_ = Task.Run(async () => { await Task.Delay(50); client.Dispose(); });
        }

        public void HookHello(Client client, Hello packet) => client.SetPersistantObjects(packet.AccessToken);
    }
}
