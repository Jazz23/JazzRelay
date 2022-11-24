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
    [PluginEnabled]
    internal class MovementRecorder : IPlugin
    {
        string[] _commands = new string[] { "start", "stop", "play" };

        public void HookMove(Client client, Move packet)
        {
            if (!client.States.ContainsKey("recording")) return;
            if ((bool)client.States["recording"] && client.States.ContainsKey("records"))
                ((List<MoveRecord>)client.States["records"]).AddRange(packet.MoveRecs);
        }

        public void HookPlayerText(Client client, PlayerText packet)
        {
            if (_commands.Contains(packet.Text)) packet.Send = false;
            if (packet.Text == "start")
            {
                if (!client.States.ContainsKey("exalt"))
                    client.States["exalt"] = new Exalt(client.AccessToken, client.Position);
                client.States["recording"] = true;
                client.States["playing"] = false;
                client.States["records"] = new List<MoveRecord>();
            }
            else if (packet.Text == "stop")
            {
                client.States["recording"] = false;
                client.States["playing"] = false;
            }
            else if (packet.Text == "play")
            {
                object? records;
                if (!client.States.TryGetValue("records", out records)) return;
                client.States["playing"] = true;
                Task.Run(async () => await PlayRecording(client, (List<MoveRecord>)records));
            }
        }

        async Task PlayRecording(Client client, List<MoveRecord> records)
        {
            if (!client.States.ContainsKey("playing") || (bool)client.States["playing"] == false) return;

            MoveRecord[] copy = records.ToArray();
            if (copy.Length == 0) return;

            SetPos(client, copy[0]);
            while ((bool)client.States["playing"])
            {
                for (int i = 1; i < copy.Length && (bool)client.States["playing"]; i++)
                {
                    await Task.Delay(copy[i].Time - copy[i - 1].Time); //Wait the time between moverecords
                    SetPos(client, copy[i]);
                }
            }
        }

        void SetPos(Client client, WorldPosData pos)
        {
            if (!client.States.ContainsKey("exalt")) return;
            Exalt exalt = (Exalt)client.States["exalt"];
            exalt.WriteX(pos.X);
            exalt.WriteY(pos.Y);
        }
    }
}
