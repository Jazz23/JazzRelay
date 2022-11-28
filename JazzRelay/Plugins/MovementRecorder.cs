using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Plugins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    [PluginDisabled]
    internal class MovementRecorder : IPlugin
    {
        string[] _commands = new string[] { "start", "stop", "play", "set" };
        const int _interval = 10;

        public async Task HookPlayerText(Client client, PlayerText packet)
        {
            if (_commands.Contains(packet.Text)) packet.Send = false;
            if (packet.Text == "start")
            {
                client.States["recording"] = true;
                client.States["playing"] = false;
                client.States["records"] = new List<WorldPosData>();
                _ = Task.Run(() => StartRecording(client));
            }
            else if (packet.Text == "stop")
            {
                client.States["recording"] = false;
                client.States["playing"] = false;
            }
            else if (packet.Text == "play")
            {
                client.States["playing"] = true;
                _ = Task.Run(async () => await PlayRecording(client));
            }
            else if  (packet.Text == "set")
            {
                if (!client.States.ContainsKey("exalt"))
                    client.States["exalt"] = new Exalt(client);
            }
        }

        async Task StartRecording(Client client)
        {
            try
            {
                if (!client.States.ContainsKey("recording")) return;
                if ((bool)client.States["recording"] && client.States.ContainsKey("records"))
                {
                    Exalt exalt = (Exalt)client.States["exalt"];
                    var records = (List<WorldPosData>)client.States["records"];
                    while (IsRecording(client))
                    {
                        records.Add(new WorldPosData(exalt.ReadX(), exalt.ReadY()));
                        await Task.Delay(_interval);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("{0}\n\n{1}", ex.Message, ex.StackTrace); }
        }

        //Checks to make sure client contains all necessary objects and is recording
        bool IsRecording(Client client) =>
            client.Connected && client.States.ContainsKey("recording") && client.States.ContainsKey("records") && client.States.ContainsKey("exalt") && (bool)client.States["recording"];

        async Task PlayRecording(Client client)
        {
            if (!client.States.ContainsKey("playing") || (bool)client.States["playing"] == false || !client.States.ContainsKey("records")) return;

            WorldPosData[] copy = ((List<WorldPosData>)client.States["records"]).ToArray();
            if (copy.Length == 0) return;
            Exalt exalt = (Exalt)client.States["exalt"];

            while ((bool)client.States["playing"])
            {
                for (int i = 0; i < copy.Length && (bool)client.States["playing"]; i++)
                {
                    var pos = copy[i];
                    exalt.WriteX(pos.X);
                    exalt.WriteY(pos.Y);
                    await Task.Delay(_interval);
                }
            }
        }
    }
}
