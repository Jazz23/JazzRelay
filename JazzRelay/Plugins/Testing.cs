using JazzRelay.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    internal class Testing : IPlugin
    {
        public void HookNewTick(Client client, NewTick packet)
        {
            var lad = client.Entities.Values.FirstOrDefault(x => x.ObjectType == 1860);
            if (lad is not null)
            {
                Console.WriteLine(lad.Stats.Position);
            }
        }
    }
}
