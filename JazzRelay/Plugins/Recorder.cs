using JazzRelay.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    internal partial class Recorder : Multibox
    {
        bool _recording = false;
        bool _playing = false;
        List<(byte[], byte[], byte[])> _path = new();

        public new void HookCreateSuccess(Client client, CreateSuccess packet) => client.Command += OnCommand;

        void OnCommand(Client client, string command, string[] args)
        {
            if (command == "start")
            {
                if (Main?.Client != client) return;
                Task.Run(StartRecording);
            }
            else if (command == "stop")
            {
                StopRecording();
                StopPlaying();
            }
            else if (command == "play")
            {
                if (_path.Count > 0)
                    Task.Run(async () => await Play(client));
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


        }

        async Task StartRecording()
        {
            _playing = false;
            if (Main == null) return;
            _recording = false;
            await Task.Delay(100); //Let existing recording stop
            _path = new();
            _recording = true;

            while (_recording && Main.Client.Connected)
            {
                Main.UpdatePosition();
                _path.Add((Main.X.ToArray(), Main.Y1.ToArray(), Main.Y2.ToArray()));
                await Task.Delay(Constants.RecordingDelay);
            }
        }

        void StopRecording() => _recording = false;
    }
}
