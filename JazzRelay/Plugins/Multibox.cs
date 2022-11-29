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
        string[] _commands = new string[] { "main", "bot", "sync" };
        List<Exalt> _exalts = new();
        Exalt? _main = null;
        bool _syncing = false;

        public void ToggleSync()
        {
            _syncing = !_syncing;
            if (_syncing)
                Task.Run(SyncClients);
        }

        //I tried to do as little operations as possible in this function. Maybe inlining WriteX/WriteY would be faster?
        public void SyncClients()
        {
            try
            {
                while (_syncing && _main != null && _exalts.Count > 0 && _main.Client.Connected)
                {
                    _main.UpdatePosition();
                    for (int i = 0; i < _exalts.Count; i++)
                    {
                        var bot = _exalts[i];
                        if (bot != _main && bot.Active/* && bot.Client.Connected*/)
                        {
                            bot.WriteX(_main.X);
                            bot.WriteY(_main.Y1, _main.Y2);
                        }
                    }
                }
                _syncing = false;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //Returns false if accoutn already existed
        public bool AddAccount(Client client, out Exalt exalt)
        {
            Exalt? existing = _exalts.FirstOrDefault(x => x.Id == client.AccessToken);
            bool result = false;
            if (existing == null)
            {
                result = true;
                existing = new Exalt(client, JazzRelay.Form.GrabNewPanel());
                _exalts.Add(existing);
            }

            existing.Client = client; //If we're recovering previous client
            exalt = existing;
            return result;
        }

        public void SetMain(Exalt exalt)
        {
            if (_main != exalt)
            {
                _main = exalt;
                JazzRelay.Form.SwapPanels(exalt.Panel, JazzRelay.Form.MainPanel);
            }
        }

        public void HookNewTick(Client client, NewTick packet)
        {
            if (packet.tickId != 1) return;
            UpdateClient(client);
        }

        void UpdateClient(Client client)
        {
            foreach (var Exalt in _exalts)
            {
                if (Exalt.Id == client.AccessToken)
                {
                    Exalt.Client = client;
                    Exalt.SetAddresses();
                    return;
                }
            }
        }

        public void HookPlayerText(Client client, PlayerText packet)
        {
            if (_commands.Contains(packet.Text)) packet.Send = false;
            if (packet.Text == "main")
            {
                Exalt exalt;
                AddAccount(client, out exalt);
                SetMain(exalt);
            }
            else if (packet.Text == "bot")
            {
                Exalt exalt;
                if (!AddAccount(client, out exalt))
                    exalt.Active = !exalt.Active;
            }
            else if (packet.Text == "sync")
                ToggleSync();
        }

        public void LoadAccounts(List<(string, string)> logins)
        {
            //string loginArgs = string.Format("data:{platform:Deca,guid:anVzdGlubXVsdGlib3hAaG90bWFpbC5jb20=,token:bGZqSnI4NnpTNnB4Tmd0QkxvWXpueW5ReUlzWE01UlJmbFdGZnRYUHdRZCtnbmxXeklVMXZDOTRTRE5TSDgrMTMydjNwVUI5Sk5sMVZ2TmZiS2txM0R4V3dMZG9yeEFRVUhrNFJyTU5OU2MxeTNDL0dheERHTnNUREg3STVHTDNOTkp3MnFsNUFweklvOTk0RVlvY2p3WkliZ3YwQW9iOTVpVVlsQXV1SHl3Q05xV0lrYjNYSlloRU5KakhuR2lucCtha1YyWUVkVzI0c3pwZDF6Q1JtZ2RUSU1QN01Ka2xROEp0cFlwc2UvMWZ4TEk1c2xZNS8wSjk0TThZMDR1clM4ME1OQWlxbC94cDhzUVVXYnJxQlpHQnEzK29rUi9weTYvL0h1eDBEY3ZvRXZmV2I3V2hJb2x2dHIwL2dDZmU1akd1VVEyUnlJUzJmK0pqNHp1YnVBPT0=,tokenTimestamp:MTY2OTE3OTk3Mw==,tokenExpiration:ODY0MDA=,env:4}")
        }
    }
}
