using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JazzRelay.Plugins
{
    internal class Multibox : IPlugin
    {
        int _objId = -1;
        WorldPosData _myPos = new();
        Magic _magic = new();

        public void HookPlayerShoot(Client client, PlayerShoot packet)
        {

        }

        //public void HookCreateSuccess(Client client, CreateSuccess packet) => _objId = packet.objectId;

        public void HookNewTick(Client clinent, NewTick packet)
        {

        }

        public void HookPlayerText(Client client, PlayerText packet)
        {
            if (packet.Text == "main")
                _magic.SetMain(client.Position);
            else if (packet.Text == "bot")
                _magic.AddBot(client.Position);
            else if (packet.Text == "sync")
                _magic.Sync();
            packet.Send = false;
        }
    }
}
