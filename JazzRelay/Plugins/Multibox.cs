using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JazzRelay.Plugins
{
    internal class Multibox : IPlugin
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern ushort GetAsyncKeyState(VirtualKeys virtualKey);
        List<IntPtr> _handles = new();

        static bool Looping = false;
        int _objId = -1;
        WorldPosData _myPos = new();
        Magic _magic = new();

        public void HookHello(Client client, Hello packet)
        {
            //UpdateHandles();
            //if (!Looping)
            //{
            //    Looping = true;
            //    //Task.Run(GetKeys);
            //}
            //Task.Run(async () =>
            //{
            //    await Task.Delay(5000);
            //    //PressKey(VirtualKeys.W, true);
            //});
        }

        public void HookCreateSuccess(Client client, CreateSuccess packet) => _objId = packet.objectId;

        public void HookNewTick(Client clinent, NewTick packet)
        {
            foreach (var status in packet.statuses)
            {
                if (status.objectId == _objId && !(_myPos?.IsEqualTo(status.position) ?? false))
                {
                    _myPos = status.position;
                    Console.WriteLine(_myPos);
                    return;
                }
            }
        }//Was going to test spamming the heck out of goto packets

        public void HookPlayerText(Client client, PlayerText packet)
        {
            if (packet.Text == "main")
                _magic.SetMain(client.Position);
            else if (packet.Text == "bot")
                _magic.AddBot(client.Position);
            else if (packet.Text == "sync")
                _magic.Sync();
            packet.Send = false;
        }

        async Task GetKeys()
        {
            while (true)
            {
                bool w = GetAsyncKeyState(VirtualKeys.W) > 0;
                bool a = GetAsyncKeyState(VirtualKeys.A) > 0;
                bool s = GetAsyncKeyState(VirtualKeys.S) > 0;
                bool d = GetAsyncKeyState(VirtualKeys.D) > 0;

                if (w && s)
                {
                    PressKey(VirtualKeys.W, false);
                    PressKey(VirtualKeys.S, false);
                }
                else if (w)
                {
                    PressKey(VirtualKeys.W, true);
                    PressKey(VirtualKeys.S, false);
                }
                else if (s)
                {
                    PressKey(VirtualKeys.S, true);
                    PressKey(VirtualKeys.W, false);
                }

                if (a && d)
                {
                    PressKey(VirtualKeys.A, false);
                    PressKey(VirtualKeys.D, false);
                }
                else if (a)
                {
                    PressKey(VirtualKeys.A, true);
                    PressKey(VirtualKeys.D, false);
                }
                else if (d)
                {
                    PressKey(VirtualKeys.D, true);
                    PressKey(VirtualKeys.A, false);
                }

                await Task.Delay(500);
            }
        }

        void PressKey(VirtualKeys key, bool down)
        {
            foreach (var handle in _handles)
            {
                if (down)
                {
                    PostMessage(handle, 0x104, (IntPtr)(int)key, new IntPtr(0x1f0001));
                    PostMessage(handle, 0x102, (IntPtr)(int)key + 32, new IntPtr(0x1f0001));
                }
                else
                    PostMessage(handle, 0x101, (IntPtr)(int)key, new IntPtr(0xc01f0001));
            }
        }

        void UpdateHandles() => _handles = 
            Process.GetProcesses().Where(x => x.ProcessName.ToLower().Contains("exalt")).Select(x => x.MainWindowHandle).ToList();
    }
}
