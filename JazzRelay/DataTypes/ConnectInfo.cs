using JazzRelay.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.DataTypes
{
    public class ConnectInfo
    {
        public Reconnect Reconnect { get; set; }
        public Proxy Proxy { get; set; }
        public ConnectInfo(Proxy proxy, Reconnect recon)
        {
            Proxy = proxy;
            Reconnect = recon;
        }
    }
}
