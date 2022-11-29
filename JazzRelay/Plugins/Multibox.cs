using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Plugins.Utils;
using JazzRelay.Properties;
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
        string[] _commands = new string[] { "main", "bot", "sync", "start", "stop", "play" };
        List<Exalt> _exalts = new();
        Exalt? _main = null;
        bool _syncing = false;
        List<(byte[], byte[], byte[])> _path = new();
        bool _recording = false;
        bool _playing = false;

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
                        if (bot != _main && bot.Active && IsTogether(_main.Client, bot.Client)/* && bot.Client.Connected*/)
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

        bool IsTogether(Client client1, Client client2) =>
            client1.ConnectionInfo.Reconnect.host == client2.ConnectionInfo.Reconnect.host && client1.ConnectionInfo.Reconnect.GameId
                == client2.ConnectionInfo.Reconnect.GameId;

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
            else if (packet.Text == "start")
            {
                if (_main?.Client != client) return;
                Task.Run(StartRecording);
            }
            else if (packet.Text == "stop")
            {
                StopRecording();
                StopPlaying();
            }
            else if (packet.Text == "play")
            {
                if (_path.Count > 0)
                    Task.Run(async() => await Play(client));
            }
        }

        void StopPlaying()
        {
            _playing = false;
        }

        async Task Play(Client client)
        {
            _playing = false;
            _recording = false;
            await Task.Delay(100); //Let other play stop
            _playing = true;
            try
            {
                List<Exalt> bots = _exalts.Where(x => IsTogether(x.Client, client)).ToList();
                while (_playing)
                {
                    for (int i = 0; i < _path.Count && _playing; i++)
                    {
                        (byte[], byte[], byte[]) pos = _path[i];
                        foreach (var bot in bots.ToArray())
                        {
                            if (!bot.Client.Connected) //We dc'd, don't write pos
                                bots.Remove(bot);
                            else if (bot.Active) //We're in, and we're active
                            {
                                bot.WriteX(pos.Item1);
                                bot.WriteY(pos.Item2, pos.Item3);
                            }
                        }
                        await Task.Delay(Constants.RecordingDelay);
                    }
                }
                _syncing = false;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        async Task StartRecording()
        {
            _playing = false;
            if (_main == null) return;
            _recording = false;
            await Task.Delay(100); //Let existing recording stop
            _path = new();
            _recording = true;

            while (_recording && _main.Client.Connected)
            {
                _main.UpdatePosition();
                _path.Add((_main.X.ToArray(), _main.Y1.ToArray(), _main.Y2.ToArray()));
                await Task.Delay(Constants.RecordingDelay);
            }
        }

        void StopRecording() => _recording = false;

        public void HookUsePortal(Client client, UsePortal packet)
        {
            if (_main?.Client == client && _syncing)
                JazzRelay.Form.PressGlobalKey(Settings.Default.InteractHotkey);
        }

        public async Task HookReconnect(Client client, Reconnect packet)
        {
            if (packet.GameId == 0 && _main?.Client == client)
            {
                foreach (var exalt in _exalts)
                {
                    if (exalt.Client != client)
                    {
                        await exalt.Client.ConnectTo(packet.host, 2050, -2, "Realm", new byte[0], -1);
                    }
                }

            }
        }

        public void HookEscape(Client client, Escape packet)
        {
            if (_main?.Client == client && _syncing)
            {
                foreach (var exalt in _exalts)
                    if (exalt.Client != client) exalt.Client.Escape();
            }
        }

        public void LoadAccounts(List<(string, string)> logins)
        {
            //string loginArgs = string.Format("data:{platform:Deca,guid:anVzdGlubXVsdGlib3hAaG90bWFpbC5jb20=,token:bGZqSnI4NnpTNnB4Tmd0QkxvWXpueW5ReUlzWE01UlJmbFdGZnRYUHdRZCtnbmxXeklVMXZDOTRTRE5TSDgrMTMydjNwVUI5Sk5sMVZ2TmZiS2txM0R4V3dMZG9yeEFRVUhrNFJyTU5OU2MxeTNDL0dheERHTnNUREg3STVHTDNOTkp3MnFsNUFweklvOTk0RVlvY2p3WkliZ3YwQW9iOTVpVVlsQXV1SHl3Q05xV0lrYjNYSlloRU5KakhuR2lucCtha1YyWUVkVzI0c3pwZDF6Q1JtZ2RUSU1QN01Ka2xROEp0cFlwc2UvMWZ4TEk1c2xZNS8wSjk0TThZMDR1clM4ME1OQWlxbC94cDhzUVVXYnJxQlpHQnEzK29rUi9weTYvL0h1eDBEY3ZvRXZmV2I3V2hJb2x2dHIwL2dDZmU1akd1VVEyUnlJUzJmK0pqNHp1YnVBPT0=,tokenTimestamp:MTY2OTE3OTk3Mw==,tokenExpiration:ODY0MDA=,env:4}")
        }
    }
}
