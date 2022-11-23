using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Plugins.Utils;
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
        Magic _magic = new();

        public void HookPlayerText(Client client, PlayerText packet)
        {
            _magic.Test();
            return;
            if (packet.Text == "main")
                _magic.SetMain(client.AccessToken, client.Position);
            else if (packet.Text == "bot")
                _magic.AddBot(client.AccessToken, client.Position);
            else if (packet.Text == "sync")
                _magic.ToggleSync();
            packet.Send = false;
        }
    }
}
