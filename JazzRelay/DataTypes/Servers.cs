using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JazzRelay.DataTypes
{
    public class Servers
    {
        [XmlElement(ElementName = "Server")]
        public Server[] ServerList;
    }

    public class Server
    {
        public string Name;
        public string DNS;
        public float Usage;
    }
}
