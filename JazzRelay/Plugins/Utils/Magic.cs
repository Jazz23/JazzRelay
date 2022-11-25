using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JazzRelay.Packets.DataTypes;
using System.Collections.Concurrent;

namespace JazzRelay.Plugins.Utils
{
    internal class Magic //TODO: Migrate to Multibox.cs
    {
        List<Exalt> _bots = new();
        Exalt? _main = null;
        bool _syncing = false;

        public void ToggleSync()
        {
            if (_main == null || _bots.Count == 0) return; //We're not setup, sync should do nothing
            _syncing = !_syncing;
            if (_syncing) Task.Run(SyncClients);
        }

        //I tried to do as little operations as possible in this function. Maybe inlining WriteX/WriteY would be faster?
        public void SyncClients()
        {
            try
            {
                while (_syncing && _main != null && _bots.Count > 0)
                {
                    float x = _main.ReadX();
                    float y = _main.ReadY();
                    byte[] bytesX = BitConverter.GetBytes(x);
                    byte[] bytesY1 = BitConverter.GetBytes(y);
                    byte[] bytesY2 = BitConverter.GetBytes(y * -1);
                    for (int i = 0; i < _bots.Count; i++)
                    {
                        var bot = _bots[i];
                        if (bot.Active)
                        {
                            bot.WriteX(bytesX);
                            bot.WriteY(bytesY1, bytesY2);
                        }
                    }
//hehe              //await Task.Delay(1); //Don't wanna destroy cpu
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        public void AddBot(Client client)
        {
            var bot = _bots.FirstOrDefault(x => x.id == client.AccessToken);
            if (bot == null) //We're not already a bot
            {
                if (_main?.id == client.AccessToken) //We're actually main, lets stop syncing but become a bot
                {
                    _syncing = false;
                    _bots.Add(_main);
                    _main = null;
                }
                else //We're not main and we're not a bot. We need a new instance
                {
                    KeyValuePair<Panel, MultiPanel> panel = JazzRelay.Form.Panels.First();
                    foreach (var element in JazzRelay.Form.Panels)
                    {
                        if (element.Value.HasExalt == false)
                        {
                            panel = element;
                            break;
                        }
                    }
                    _bots.Add(new Exalt(client, panel.Key));
                }
            }
            else //We're already a bot, toggle us off
                bot.ToggleActive();
        }

        public void SetMain(Client client)
        {
            if (_main == null || _main.id != client.AccessToken) //Main is either not assigned or we aren't main, so main must be reassigned
            {
                int index = _bots.FindIndex(x => x.id == client.AccessToken);
                if (index != -1) //We're already a bot, no need to make any new instances
                {
                    if (_main != null) //Main is already defined, lets swap em
                    {
                        var temp = _bots[index];
                        _bots[index] = _main; //I'm pretty sure this is atomic
                        _main = temp;
                    }
                    else //Main is not set, lets set main
                    {
                        _main = _bots[index];
                        _bots.RemoveAt(index); //Not race condition, if _main is null we can't be syncing.
                    }
                }
                else //We're not a bot and we're not main, we need a fresh instance of Exalt
                {
                    _main = new Exalt(client, JazzRelay.Form.Panels.Last().Key);
                }
            }
            else //We are main and main is not null, turn off sync
            {
                _syncing = false;
                _main = null;
            }
        }
    }
}