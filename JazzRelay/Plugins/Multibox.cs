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
    [PluginEnabled]
    internal class Multibox : IPlugin
    {
        Magic _magic = new();
        string[] _commands = new string[] { "main", "bot", "sync" };

        public void HookPlayerText(Client client, PlayerText packet)
        {
            if (_commands.Contains(packet.Text)) packet.Send = false;
            if (packet.Text == "main")
                _magic.SetMain(client.AccessToken, client.Position);
            else if (packet.Text == "bot")
                _magic.AddBot(client.AccessToken, client.Position);
            else if (packet.Text == "sync")
                _magic.ToggleSync();
        }

        public void LoadAccounts(List<(string, string)> logins)
        {
            //string loginArgs = string.Format("data:{platform:Deca,guid:anVzdGlubXVsdGlib3hAaG90bWFpbC5jb20=,token:bGZqSnI4NnpTNnB4Tmd0QkxvWXpueW5ReUlzWE01UlJmbFdGZnRYUHdRZCtnbmxXeklVMXZDOTRTRE5TSDgrMTMydjNwVUI5Sk5sMVZ2TmZiS2txM0R4V3dMZG9yeEFRVUhrNFJyTU5OU2MxeTNDL0dheERHTnNUREg3STVHTDNOTkp3MnFsNUFweklvOTk0RVlvY2p3WkliZ3YwQW9iOTVpVVlsQXV1SHl3Q05xV0lrYjNYSlloRU5KakhuR2lucCtha1YyWUVkVzI0c3pwZDF6Q1JtZ2RUSU1QN01Ka2xROEp0cFlwc2UvMWZ4TEk1c2xZNS8wSjk0TThZMDR1clM4ME1OQWlxbC94cDhzUVVXYnJxQlpHQnEzK29rUi9weTYvL0h1eDBEY3ZvRXZmV2I3V2hJb2x2dHIwL2dDZmU1akd1VVEyUnlJUzJmK0pqNHp1YnVBPT0=,tokenTimestamp:MTY2OTE3OTk3Mw==,tokenExpiration:ODY0MDA=,env:4}")
        }
    }
}
