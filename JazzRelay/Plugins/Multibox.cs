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
        public static Exalt? Main = null;
        List<Exalt> _exalts = new();
        bool _syncing = false;
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
                while (_syncing && Main != null && _exalts.Count > 0 && Main.Client.Connected)
                {
                    Main.UpdatePosition();
                    for (int i = 0; i < _exalts.Count; i++)
                    {
                        var bot = _exalts[i];
                        if (bot != Main && bot.Active && IsTogether(Main.Client, bot.Client)/* && bot.Client.Connected*/)
                        {
                            bot.WriteX(Main.X);
                            bot.WriteY(Main.Y1, Main.Y2);
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
            if (Main != exalt)
            {
                Main = exalt;
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

        public void HookCreateSuccess(Client client, CreateSuccess packet) => client.Command += this.OnCommand;

        void OnCommand(Client client, string command, string[] args)
        {
            if (command == "main")
            {
                Exalt exalt;
                AddAccount(client, out exalt);
                SetMain(exalt);
            }
            else if (command == "bot")
            {
                Exalt exalt;
                if (!AddAccount(client, out exalt))
                    exalt.Active = !exalt.Active;
            }
            else if (command == "sync")
                ToggleSync();
            else if (command == "test")
            {

            }
        }

        public async Task HookUsePortal(Client client, UsePortal packet)
        {
            if (Main?.Client != client || client.ConnectionInfo.IsNexus()) return;

            await SendBotsThroughPortal(packet.ObjectId, !_syncing); //If we're synced, no need to teleport
        }

        public async Task SendBotsThroughPortal(int objectId, bool teleport)
        {
            if (Main == null) return;

            var exalts = _exalts.Where(x => x.Client != Main.Client && IsTogether(x.Client, Main.Client)).ToList();
            if (exalts.Count == 0) return;

            foreach (var exalt in exalts)
            {
                exalt.Client.States["targetPortal"] = Main.Client.Entities[objectId].ObjectType;
                if (teleport)
                {
                    var player = exalt.Client.FindPlayer(Main.Client.Name);
                    if (player == null) continue;
                    _ = Task.Run(async () => await exalt.Client.SendToServer(new Teleport() { ObjectId = (int)player, Name = Main.Client.Name }));
                }
            }

            var time = Environment.TickCount + 1000; //Safety so we don't get stuck
            while (exalts.Exists(x => x.Client.Connected) && Environment.TickCount < time)
                await Task.Delay(50);
        }

        public async Task HookReconnect(Client client, Reconnect packet)
        {
            //If we're going to a realm, we're main, and we're in nexus
            if (packet.GameId != 0 || Main?.Client != client || !client.OriginalConnInfo.IsNexus()) return;
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
            if (Main?.Client == client && _syncing)
            {
                foreach (var exalt in _exalts)
                    if (exalt.Client != client) exalt.Client.Escape();
            }
        }
    }
}
