﻿/* JazzRelay is a proxy designed to be between 059's exalt multitool and rotmg. I use relfection
 * to read/write packets to avoid having to write all those god damn read writes for every single
 * packet type. Plugin hooks are also interpreted via reflection instead of manually added them in the constructor.
 * The compiler hates this so I suppress warning 8618, but it's worth it. 
 * I use starksoft.aspen to use socks5 proxies (loaded from webshare.com) with my connection to rotmg to allow for multiple
 * login. Plugin hooks can be async Tasks. Client instances get collected after exalt disconnects or
 * socket is closed. As of this comment this proxy is bare bones and contains 0 utilities besides
 * persistant client states.
 */

using JazzRelay.DataTypes;
using JazzRelay.Enums;
using JazzRelay.Extensions;
using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using JazzRelay.Plugins;
using JazzRelay.Plugins.Utils;
using JazzRelay.Properties;
using Starksoft.Aspen.Proxy;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using static JazzRelay.Client;
using ObjectList = System.Collections.Generic.Dictionary<string, object>;

namespace JazzRelay
{
    public interface IPlugin { }
    public class JazzRelay
    {
        static async Task Main(string[] args) => await new JazzRelay().StartRelay();

        [DllImport("kernel32.dll")]
        internal static extern Boolean AllocConsole();

        public List<(IPlugin, MethodInfo)> GetHooks(PacketType pt) => _hooks[pt];
        public bool HasHook(PacketType pt) => _hooks.ContainsKey(pt);
        public Server DefaultServer { get; set; } = new Server() { DNS = "54.235.235.140", Name = "USWest4" };
        public ObjectList GetPersistantObjects(string accessToken)
        {
            ObjectList? temp;
            if (!_persistantObjects.TryGetValue(accessToken, out temp))
                _persistantObjects.Add(accessToken, temp = new()
                {
                    { "ConnectionInfo", new ConnectInfo(FrontProxy, new Reconnect() { host = DefaultServer.DNS, Port = 2050, GameId = -2, Key = new byte[0], KeyTime = -1, MapName = "Nexus" }) },
                    { "defaultServer", DefaultServer}
                });
            return temp;
        }
        public Dictionary<PacketType, Type> PacketTypes => _packetTypes;
        public FieldInfo[] GetFields(Type packetType) => _packetFields[packetType]; //I want to throw an error here
        readonly List<Proxy> _frontProxies = new List<Proxy>();
        int _frontProxiesIndex = 0;
        public Proxy FrontProxy => /*_frontProxies.First(x => x.Ip == "45.12.140.22");*/ _frontProxies[_frontProxiesIndex++ % _frontProxies.Count];
        public bool Listen = true;
        public List<Client> Clients { get; set; } = new();
        public static List<Server> Servers = new();


        Dictionary<PacketType, Type> _packetTypes = new();
        Dictionary<Type, FieldInfo[]> _packetFields = new();
        Dictionary<string, ObjectList> _persistantObjects = new();
        Dictionary<PacketType, List<(IPlugin, MethodInfo)>> _hooks = new(); //Key: Byte. Value: Plugin Instance & Function (hook)

        public async Task StartRelay()
        {
#if ! DEBUG
            if (!HWIDLock.IsMike())
                return;
#endif
            //try
            //{
                AllocConsole();
                InitServerList();
                InitPlugins();
                InitPacketTypes();
                _ = Task.Run(LoadProxies);
                _ = Task.Run(TCPListen);
                await Task.Delay(-1);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("{0}\n{1}", ex.Message, ex.StackTrace);
            //}
        }

        private void InitServerList()
        {
            var serializer = new XmlSerializer(typeof(Servers));
            FileStream stream = File.OpenRead("servers.xml");
            var temp = serializer.Deserialize(stream) ?? throw new Exception("Error parsing servers!");
            Servers = ((Servers)temp).ServerList.ToList();
            stream.Close();

            foreach (var server in Servers) server.AbbreviatedName = AbbreviatedName(server.Name);

            var defaultServer = Servers.FirstOrDefault(x => x.Name == Settings.Default.DefaultServer);
            if (defaultServer != null)
                DefaultServer = defaultServer;
        }

        string AbbreviatedName(string serverName)
        {
            string result = string.Concat(serverName.Where(x => Char.IsUpper(x))).ToLower();
            int num;
            if (int.TryParse(serverName.Last().ToString(), out num))
            {
                return result + num.ToString();
            }
            return result;
        }

        public static Server? FindServer(string name)
        {
            foreach (var server in Servers)
            {
                if (server.Name == name || server.AbbreviatedName == name) return server;
            }
            return null;
        }
        public static Server? FindServerByHost(string host) => Servers.FirstOrDefault(x => x.DNS == host);

        void InitPacketTypes()
        {
            var types = Assembly.GetAssembly(typeof(PacketType))?.GetTypes()?.Where(x => typeof(Packet).IsAssignableFrom(x) && x != typeof(Packet) && x != typeof(IncomingPacket) && x != typeof(OutgoingPacket));

            var dict = types?.ToDictionary(type =>
            (PacketType)Enum.Parse(typeof(PacketType), type.Name ?? ""));
            if (dict == null)
                throw new Exception("Error initializing packet types!");
            _packetTypes = dict;

            if (types == null)
                throw new Exception("Packet types are null for some reason!"); //Compiler made me put this here
            foreach (Type t in types)
            {
                FieldInfo[]? fields = t.GetFields().Where(x => x.DeclaringType == t).ToArray();
                if (fields != null)
                    _packetFields.Add(t, fields);
            }
        }

        bool IsHook(MethodInfo func)
        {
            if (!func.Name.StartsWith("Hook")) return false;
            var args = func.GetParameters();
            if (args[0].ParameterType != typeof(Client)) goto BadPacket;
            if (!typeof(Packet).IsAssignableFrom(args[1].ParameterType)) goto BadPacket;
            return true;

        BadPacket:
            throw new Exception($"Invalid Hook {func.Name} Detected! Make sure type is HookPacketName(Client client, Packet packet)!");
        }

        void InitPlugins()
        {
            List<Type> plugins = Assembly.GetAssembly(typeof(IPlugin))?.GetTypes()?.Where(x => 
            typeof(IPlugin).IsAssignableFrom(x) && x != typeof(IPlugin) && !Attribute.IsDefined(x, typeof(PluginDisabled))).ToList() ??
                throw new Exception("Error reading plugin types!");

            plugins.Remove(typeof(RelayEssentials)); //We want this plugin first
            var newPlugins = new List<Type>() { typeof(RelayEssentials) };
            newPlugins.AddRange(plugins);

            foreach (var plugin in newPlugins)
            {
                IPlugin instance = (IPlugin)(Activator.CreateInstance(plugin) ?? throw new Exception($"Error instanstiating plugin type {plugin.Name}!"));
                foreach (var hook in plugin.GetMethods().Where(IsHook))
                {
                    string name = hook.Name.Remove(0, 4);
                    object? value;
                    if (!Enum.TryParse(typeof(PacketType), name, out value) || value == null) continue;
                    PacketType packet = (PacketType)value;

                    if (!_hooks.TryGetValue(packet, out var foo))
                        _hooks.Add(packet, new List<(IPlugin, MethodInfo)>());
                    _hooks[packet].Add((instance, hook)); //We add this particular hook to the list of hooks associated with that
                                                      //packet id.
                }
            }
        }

        async Task TCPListen()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), Constants.Port);
            server.Start();

            await Task.Delay(500); //Idk why I think this delay is necessary
            while (Listen)
            {
                Console.WriteLine("Started Listening");
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Received TCP connection from multitool");
                await Task.Delay(500);
                StartClient(client);
            }
        }

        void StartClient(TcpClient client) => Clients.Add(new Client(this, client)); //We *want* our client instances to get collected to avoid memory leaks

        async Task LoadProxies()
        {
            await Proxy.LoadWebshare("https://proxy.webshare.io/api/v2/proxy/list/download/vypfzbswdoibhytiklpatdgpanwkjringjyiixgx/-/any/username/direct/-/",
            new List<Proxy>(), _frontProxies);
        }
    }
}