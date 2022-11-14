using JazzRelay.Enums;
using JazzRelay.Extensions;
using JazzRelay.Packets;
using JazzRelay.Packets.Utils;
using JazzRelay.Plugins;
using Starksoft.Aspen.Proxy;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace JazzRelay // Note: actual namespace depends on the project name.
{
    internal interface IPlugin { }
    internal class Packet { public PacketType PacketType => (PacketType)Enum.Parse(typeof(PacketType), this.GetType().Name); public bool Send = true; }
    internal class JazzRelay
    {
        static async Task Main(string[] args) => await new JazzRelay().StartRelay();

        Dictionary<PacketType, List<(IPlugin, MethodInfo)>> _hooks = new(); //Key: Byte. Value: Plugin Instance & Function (hook)
        public List<(IPlugin, MethodInfo)> GetHooks(PacketType pt) => _hooks[pt];
        public bool HasHook(PacketType pt) => _hooks.ContainsKey(pt);
        Dictionary<PacketType, Type> _packetTypes = new();
        public Dictionary<PacketType, Type> PacketTypes => _packetTypes;
        Dictionary<Type, FieldInfo[]> _packetFields = new();
        public FieldInfo[] GetFields(Type packetType) => _packetFields[packetType]; //I want to throw an error here
        private readonly List<Proxy> _frontProxies = new List<Proxy>();
        private int _frontProxiesIndex = 0;
        public Proxy FrontProxy => _frontProxies[_frontProxiesIndex++ % _frontProxies.Count];
        public bool Listen = true;



        public async Task StartRelay()
        {
            InitPlugins();
            InitPacketTypes();
            //new Client(this, new());
            _ = Task.Run(LoadProxies);
            _ = Task.Run(TCPListen);
            await Task.Delay(-1);
        }


        void InitPacketTypes()
        {
            var types = Assembly.GetAssembly(typeof(PacketType))?.GetTypes()?.Where(x => typeof(Packet).IsAssignableFrom(x) && x != typeof(Packet));

            var dict = types?.ToDictionary(type =>
            (PacketType)Enum.Parse(typeof(PacketType), type.Name ?? ""));
            if (dict == null)
                throw new Exception("Error initializing packet types!");
            _packetTypes = dict;

            if (types == null)
                throw new Exception("Packet types are null for some reason!"); //Compiler made me put this here
            foreach (Type t in types)
            {
                FieldInfo[]? fields = t.GetFields().Where(x => x.DeclaringType != typeof(Packet)).ToArray();
                if (fields != null)
                    _packetFields.Add(t, fields);
            }
        }

        bool IsHook(MethodInfo func)
        {
            if (!func.Name.StartsWith("Hook")) return false;
            var args = func.GetParameters();
            if (!typeof(Packet).IsAssignableFrom(args[0].ParameterType)) goto BadPacket;
            //if (args[1].ParameterType.BaseType != typeof(_2NtWTAbKYSjvnGzhHCogM7SmzJH)) goto BadPacket;
            return true;

        BadPacket:
            throw new Exception($"Invalid Hook {func.Name} Detected! Make sure type is HookPacketName(Packet packet)!");
        }

        void InitPlugins()
        {
            List<IPlugin> plugins = new List<IPlugin>()
            {
                new Multibox()
            };
            foreach (var plugin in plugins)
            {
                foreach (var hook in plugin.GetType().GetMethods().Where(IsHook))
                {
                    string name = hook.Name.Remove(0, 4);
                    object? value;
                    if (!Enum.TryParse(typeof(PacketType), name, out value) || value == null) continue;
                    PacketType packet = (PacketType)value;

                    if (!_hooks.TryGetValue(packet, out var foo))
                        _hooks.Add(packet, new List<(IPlugin, MethodInfo)>());
                    _hooks[packet].Add((plugin, hook)); //We add this particular hook to the list of hooks associated with that
                                                      //packet id.
                }
            }
        }

        async Task TCPListen()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 2060);
            server.Start();

            while (Listen)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Received TCP connection from multitool");
                new Client(this, client); //TODO: Store these?
            }
        }

        async Task LoadProxies()
        {
            await Proxy.LoadWebshare("https://proxy.webshare.io/proxy/list/download/dvguknavbddgodgjawdlnfnetmuhnusmnmzkgimb/-/socks/username/direct/",
            new List<Proxy>(), _frontProxies);
        }
    }
}