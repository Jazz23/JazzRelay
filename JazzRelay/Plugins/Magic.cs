using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JazzRelay.Packets.DataTypes;
using System.Collections.Concurrent;

namespace JazzRelay.Plugins
{
    class Exalt
    {

        #region Definitions
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        // REQUIRED METHODS
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION64
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public int AllocationProtect;
            public int __alignment1;
            public ulong RegionSize;
            public int State;
            public int Protect;
            public int Type;
            public int __alignment2;
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }
        #endregion
        public string id;
        public IntPtr Handle;
        public IntPtr x1;
        public IntPtr x2;
        public IntPtr y1;
        public IntPtr y2;
        IntPtr filler;
        public bool Active { get; set; }

        public Exalt(string accessToken, WorldPosData pos)
        {
            id = accessToken;
            Active = true;
            uint pid;
            GetWindowThreadProcessId(GetForegroundWindow(), out pid);
            Handle = OpenProcess((uint)(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryWrite),
            false, pid);

            SetFloats(pos);
        }

        public void ToggleActive() => Active = !Active; //TECHINCALLY this will sync even if all bots are inactive. Bite me

        public float ReadX()
        {
            float x;
            byte[] temp = new byte[4];
            ReadProcessMemory(Handle, x1, temp, 4, out filler);
            x = BitConverter.ToSingle(temp, 0);
            if (x == 0) throw new Exception("Error reading memory!");
            return x;
        }
        public float ReadY()
        {
            float y;
            byte[] temp = new byte[4];
            ReadProcessMemory(Handle, y1, temp, 4, out filler);
            y = BitConverter.ToSingle(temp, 0);
            if (y == 0) throw new Exception("Error reading memory!");
            return y;
        }

        public void WriteX(byte[] bytes)
        {
            WriteProcessMemory(Handle, x1, bytes, 4, out filler);
            WriteProcessMemory(Handle, x2, bytes, 4, out filler);
        }
        public void WriteY(byte[] bytes1, byte[] bytes2)
        {
            WriteProcessMemory(Handle, y1, bytes1, 4, out filler);
            WriteProcessMemory(Handle, y2, bytes2, 4, out filler);
        }

        void SetFloats(WorldPosData loc)
        {
            try
            {
                SYSTEM_INFO sys_info = new SYSTEM_INFO();
                GetSystemInfo(out sys_info);
                long proc_min_address_l = (long)sys_info.minimumApplicationAddress;
                long proc_max_address_l = (long)sys_info.maximumApplicationAddress;

                List<MEMORY_BASIC_INFORMATION64> regions = new List<MEMORY_BASIC_INFORMATION64>();
                while (proc_min_address_l < proc_max_address_l)
                {
                    MEMORY_BASIC_INFORMATION64 mem_basic_info = new MEMORY_BASIC_INFORMATION64();
                    VirtualQueryEx(Handle, new IntPtr(proc_min_address_l), out mem_basic_info, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64)));

                    if (mem_basic_info.State == 4096 && mem_basic_info.AllocationProtect == 4 && mem_basic_info.Type == 131072 && mem_basic_info.RegionSize == 16777216)
                        regions.Add(mem_basic_info);

                    proc_min_address_l += (long)mem_basic_info.RegionSize;
                }

                IntPtr bytesRead = (IntPtr)0;
                foreach (var region in regions)
                {
                    var baseAddress = region.BaseAddress;
                    ulong buffSize = region.RegionSize;
                    byte[] buffer = new byte[buffSize];
                    ReadProcessMemory(Handle, (IntPtr)baseAddress, buffer, (int)buffSize, out bytesRead);

                    for (ulong i = 0; i < (ulong)bytesRead - 4 - 0x2c; i += 4)
                    {
                        float meme = BitConverter.ToSingle(buffer, (int)i);
                        float meme2 = BitConverter.ToSingle(buffer, (int)i + 0x2C);
                        ulong address = baseAddress + i;

                        if (meme == loc.X && meme2 == meme)
                        {
                            x1 = (IntPtr)address;
                            x2 = (IntPtr)(address + 0x2C);
                            y1 = (IntPtr)address + 4;
                            y2 = (IntPtr)(address + 4 + 0x2C);
                            Console.WriteLine("Pattern found!");
                            return;
                        }
                    }
                }

                throw new Exception("Pattern not matched! Contact Jazz.");
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
    internal class Magic
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

        public void AddBot(string token, WorldPosData pos)
        {
            var bot = _bots.FirstOrDefault(x => x.id == token);
            if (bot == null) //We're not already a bot
            {
                if (_main?.id == token) //We're actually main, lets stop syncing but become a bot
                {
                    _syncing = false;
                    _bots.Add(_main);
                    _main = null;
                }
                else //We're not main and we're not a bot. We need a new instance
                {
                    _bots.Add(new Exalt(token, pos));
                }
            }
            else //We're already a bot, toggle us off
                bot.ToggleActive();
        }

        public void SetMain(string token, WorldPosData pos)
        {
            if (_main == null || _main.id != token) //Main is either not assigned or we aren't main, so main must be reassigned
            {
                int index = _bots.FindIndex(x => x.id == token);
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
                    _main = new Exalt(token, pos);
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