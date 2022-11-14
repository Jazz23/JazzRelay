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
        public void HookHello(Hello packet)
        {

        }

        public void HookPlayerText(PlayerText packet)
        {
            packet.Text = "dez";
        }

        public void HookFailure(Failure packet)
        {

        }
    }
}
