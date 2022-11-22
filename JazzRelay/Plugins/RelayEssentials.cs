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
        }

        //I'm choosing no persistance between Exalt restarts/accesstokens. I don't want to wait until update.
        public void HookHello(Client client, Hello packet)
        {
            client.SetPersistantObjects(packet.AccessToken);
            client.AccessToken = packet.AccessToken;
        }

        public void HookCreateSuccess(Client client, CreateSuccess packet) => client.ObjectId = packet.objectId;

        public void HookNewTick(Client client, NewTick packet)
        {
            foreach (var status in packet.statuses)
            {
                if (status.objectId == client.ObjectId)
                {
                    client.Position = status.position;
                    return;
                }
            }
        }

    }
}
