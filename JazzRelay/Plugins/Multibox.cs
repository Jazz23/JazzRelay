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
            if (!packet.Text.StartsWith("!")) return;
            packet.Send = false;
            packet.Text = packet.Text.Remove(0, 1);

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
            else if (packet.Text.StartsWith("sethotkey "))
            {
                string key = packet.Text.Split(' ')[1];
                if (key.Length == 1)
                {
                    Settings.Default.InteractHotkey = key;
                    Settings.Default.Save();
                }
            }
            else if (packet.Text == "test")
            {
                Task.Run(() => HookUsePortal(client, new UsePortal()));
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

        public async Task HookUsePortal(Client client, UsePortal packet)
        {
            if (_main?.Client == client)
            {
                if (_syncing)
                    JazzRelay.Form.PressGlobalKey(Settings.Default.InteractHotkey);
                else if (client.ConnectionInfo.Reconnect.GameId == 0)
                {
                    for (int i = 0; i < _exalts.Count; i++)
                    {
                        var exalt = _exalts[i];
                        if (exalt.Client != client && IsTogether(exalt.Client, client))
                        {
                            var player = exalt.Client.FindPlayer(_main.Client.Name);
                            if (player == null) continue;
                            _ = Task.Run(async() => 
                            { 
                                await exalt.Client.SendToServer(new Teleport() { ObjectId = (int)player, Name = _main.Client.Name });
                                await Task.Delay(1000);
                                JazzRelay.Form.PressKey(exalt.Panel, Settings.Default.InteractHotkey);
                                JazzRelay.Form.FocusPanel(_main.Panel);
                            });
                        }
                    }
                    await Task.Delay(1000);
                }

            }
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

        public void HookUpdate(Client client, Update packet)
        {

        }
    }
}
