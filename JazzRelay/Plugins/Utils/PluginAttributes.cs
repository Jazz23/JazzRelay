using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins.Utils
{
    internal class PluginEnabled : Attribute
    {
    }
    internal class PluginDisabled : Attribute //Doesn't actually do anything but makes it clear
    {
    }
}
