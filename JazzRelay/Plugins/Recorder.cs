using JazzRelay.Enums;
using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Plugins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins
{
    [PluginDisabled]
    internal class Recorder : IPlugin
    {
        #region WinAPI

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern ushort GetAsyncKeyState(VirtualKeys virtualKey);

        #endregion

        bool _recording = false;
        bool _playing = false;

        //The triple of bytes is the byte representation of the position
        List<WorldPosData> _path = new();

        public void HookLoad(Client client, Load packet) => client.Command += OnnCommand;

        void OnnCommand(Client client, string command, string[] args)
        {
            if (command == "start")
            {
                if (Multibox.Main?.Client != client) return;
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
                    Task.Run(Play);
            }
        }


        async Task Play()
        {
            try
            {
                _playing = false;
                _recording = false;
                if (Multibox.Main?.Client == null || _path.Count == 0) return;
                await Task.Delay(100); //Let other play stop

                _playing = true;
                WorldPosData myPos = new();
                WorldPosData pos = _path.First();
                Client client = Multibox.Main.Client;
                IntPtr handle = GetForegroundWindow();
                bool playing() => _playing && client.Connected && GetAsyncKeyState(VirtualKeys.W) < 2;

                _ = Task.Run(() => WalkTowards(pos, myPos, handle));
                while (_playing && client.Connected && GetAsyncKeyState(VirtualKeys.W) < 2)
                {
                    for (int i = 0; i < _path.Count && playing(); i++)
                    {
                        myPos.X = Multibox.Main.ReadX();
                        myPos.Y = Multibox.Main.ReadY();

                        if (myPos.DistanceTo(pos) < 0.5f)
                        {
                            pos.X = _path[i].X;
                            pos.Y = _path[i].Y;
                        }

                        Console.WriteLine(i);
                        await Task.Delay(Constants.RecordingDelay);
                    }
                }

                Console.WriteLine("Stopping");
                StopPlaying();
                ReleaseKeys(handle);
            }
            catch (Exception ex)
            {

            }
        }

        private void ReleaseKeys(IntPtr handle)
        {
            PressKey(VirtualKeys.A, false, handle);
            PressKey(VirtualKeys.D, false, handle);
            PressKey(VirtualKeys.W, false, handle);
            PressKey(VirtualKeys.S, false, handle);
        }

        private async Task WalkTowards(WorldPosData pos, WorldPosData myPos, IntPtr handle)
        {
            try
            {
                while (_playing && (Multibox.Main?.Client?.Connected ?? false))
                {
                    float deltaX = pos.X - myPos.X;
                    float deltaY = pos.Y - myPos.Y;

                    //if (myPos.DistanceTo(pos) < 0.5f)
                    //{
                    //    //if (Multibox.Main == null) return;
                    //    //Multibox.Main.WriteX(pos.X);
                    //    //Multibox.Main.WriteY(pos.Y);
                    //    PressKey(VirtualKeys.A, false, handle);
                    //    PressKey(VirtualKeys.D, false, handle);
                    //    PressKey(VirtualKeys.W, false, handle);
                    //    PressKey(VirtualKeys.S, false, handle);
                    //    await Task.Delay(10);
                    //    continue;
                    //}

                    if (Math.Abs(deltaX) > 0.5f)
                    {
                        if (deltaX > 0) //We are to the left, we must walk right
                        {
                            PressKey(VirtualKeys.A, false, handle);
                            PressKey(VirtualKeys.D, true, handle);
                        }
                        else if (deltaX < 0)
                        {
                            PressKey(VirtualKeys.D, false, handle);
                            PressKey(VirtualKeys.A, true, handle);
                        }
                    }
                    else
                    {
                        PressKey(VirtualKeys.A, false, handle);
                        PressKey(VirtualKeys.D, false, handle);
                    }

                    if (Math.Abs(deltaY) > 0.5f)
                    {
                        if (deltaY < 0) //We are below, we must walk upwards
                        {
                            PressKey(VirtualKeys.S, false, handle);
                            PressKey(VirtualKeys.W, true, handle);
                        }
                        else if (deltaY > 0)
                        {
                            PressKey(VirtualKeys.W, false, handle);
                            PressKey(VirtualKeys.S, true, handle);
                        }
                    }
                    else
                    {
                        PressKey(VirtualKeys.W, false, handle);
                        PressKey(VirtualKeys.S, false, handle);
                    }

                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {

            }
        }

        async Task StartRecording()
        {
            _playing = false;
            if (Multibox.Main == null) return;
            _recording = false;
            await Task.Delay(100); //Let existing recording stop
            _path = new();
            _recording = true;
            int time = Environment.TickCount;

            while (_recording && Multibox.Main.Client.Connected)
            {
                Multibox.Main.UpdatePosition();
                _path.Add(Multibox.Main.Pos.Copy());
                await Task.Delay(Constants.RecordingDelay);
            }
        }

        void StopPlaying() => _playing = false;

        void StopRecording() => _recording = false;

        void PressKey(VirtualKeys key, bool down, IntPtr handle)
        {
            if (down)
            {
                SendMessage(handle, 0x100, (IntPtr)(int)key, new IntPtr(0x1f0001)); //spy++ said this was lparam
                SendMessage(handle, 0x102, (IntPtr)(int)key + 32, new IntPtr(0x1f0001));
            }
            else
            {
                SendMessage(handle, 0x101, (IntPtr)(int)key, new IntPtr(0xc01f0001));
            }
        }
    }
}
