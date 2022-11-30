using JazzRelay.Enums;
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
        int _speed = -1;

        public void ToggleSync()
        {
            _syncing = !_syncing;
            if (_syncing)
                Task.Run(SyncClients);
        }

        void SetSpeed(int speed)
        {
            _speed = speed;
            foreach (var exalt in _exalts)
                exalt.Client.States["setSpeed"] = true;
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
                client.States["setSpeed"] = true;
                if (_speed == -1 || client.Speed < _speed)
                {
                    SetSpeed(client.Speed);
                }
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

        public async Task HookNewTick(Client client, NewTick packet)
        {
            await CheckForPortal(client);

            if (packet.tickId == 1)
                UpdateClient(client);

            if (client.States.ContainsKey("setSpeed"))
            {
                foreach (var status in packet.statuses)
                {
                    if (status.ObjectId == client.ObjectId)
                    {
                        var newStats = status.Stats.Where(x => x.statValue != (byte)StatDataType.Speed).ToList();
                        newStats.Add(new StatData() { magicNumber = 65, statType = (byte)StatDataType.Speed, statValue = _speed });
                        status.Stats = newStats.ToArray();
                        break;
                    }
                }
            }
        }

        async Task CheckForPortal(Client client)
        {
            if (client.States.ContainsKey("targetPortal"))
            {
                ushort type = (ushort)client.States["targetPortal"];
                var portal = client.Entities.Values.FirstOrDefault(x => x.ObjectType == type);
                if (portal != default)
                {
                    client.States.Remove("targetPortal");
                    await client.SendToServer(new UsePortal() { ObjectId = portal.Stats.ObjectId });
                }
            }
        }

        void UpdateClient(Client client)
        {
            foreach (var Exalt in _exalts)
            {
                if (Exalt.Id == client.AccessToken)
                {
                    Exalt.Client = client;
                    Exalt.SetAddresses();
                    if (_speed == -1 || client.Speed < _speed)
                    {
                        SetSpeed(client.Speed);
                    }
                    else if (client.Speed > _speed)
                        client.States["setSpeed"] = true;
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
                    Task.Run(async () => await Play(client));
            }
            else if (packet.Text == "test")
            {

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
            _syncing = false;
            await Task.Delay(100); //Let other play stop
            _playing = true;
            try
            {
                List<Exalt> bots = _exalts.Where(x => IsTogether(x.Client, client)).ToList();
                while (_playing && bots.Count > 0)
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
                        await Task.Delay(Constants.RecordingDelay + 1); //To help dc just in case we're ahead or something
                    }
                }
                _playing = false;
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
            if (_main?.Client != client || client.ConnectionInfo.IsNexus()) return;

            await SendBotsThroughPortal(packet.ObjectId, !_syncing); //If we're synced, no need to teleport
        }

        public async Task SendBotsThroughPortal(int objectId, bool teleport)
        {
            if (_main == null) return;

            var exalts = _exalts.Where(x => x.Client != _main.Client && IsTogether(x.Client, _main.Client)).ToList();
            if (exalts.Count == 0) return;

            foreach (var exalt in exalts)
            {
                exalt.Client.States["targetPortal"] = _main.Client.Entities[objectId].ObjectType;
                if (teleport)
                {
                    var player = exalt.Client.FindPlayer(_main.Client.Name);
                    if (player == null) continue;
                    _ = Task.Run(async () => await exalt.Client.SendToServer(new Teleport() { ObjectId = (int)player, Name = _main.Client.Name }));
                }
            }

            var time = Environment.TickCount + 1000; //Safety so we don't get stuck
            while (exalts.Exists(x => x.Client.Connected) && Environment.TickCount < time)
                await Task.Delay(50);
        }

        public async Task HookReconnect(Client client, Reconnect packet)
        {
            //If we're going to a realm, we're main, and we're in nexus
            if (packet.GameId != 0 || _main?.Client != client || !client.OriginalConnInfo.IsNexus()) return;
            foreach (var exalt in _exalts)
            {
                if (exalt.Client != client)
                {
                    await exalt.Client.ConnectTo(packet.host, 2050, -2, "Realm", new byte[0], -1);
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
    }
}
