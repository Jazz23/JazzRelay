using JazzRelay.DataTypes;
using JazzRelay.Enums;
using JazzRelay.Extensions;
using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
using JazzRelay.Plugins.Utils;
using JazzRelay.Properties;
using Starksoft.Aspen.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ObjectList = System.Collections.Generic.Dictionary<string, object>;

namespace JazzRelay
{
    public class Client
    {
        public ConnectInfo ConnectionInfo
        {
            get => (ConnectInfo)States["ConnectionInfo"];
            set => States["ConnectionInfo"] = value;
        }
        public ObjectList States { get; set; } = new();
        public int ObjectId { get; set; } = -1;
        public WorldPosData Position { get; set; }
        public string AccessToken { get; set; }
        public bool Connected { get; set; }

        JazzRelay _proxy;
        TcpClient _client;
        TcpClient? _server;

        RC4 _serverRecieveState = new RC4(RC4.HexStringToBytes(Constants.ServerKey));
        RC4 _serverSendState = new RC4(RC4.HexStringToBytes(Constants.ClientKey));

        RC4 _clientRecieveState = new RC4(RC4.HexStringToBytes(Constants.ClientKey));
        RC4 _clientSendState = new RC4(RC4.HexStringToBytes(Constants.ServerKey));
        private Proxy _socks5;

        public Client(JazzRelay proxy, TcpClient client)
        {
            _proxy = proxy;
            _client = client;
            _ = Task.Run(async () => await BeginRelay(client));
        }

        public void SetPersistantObjects(string accessToken) => States = _proxy.GetPersistantObjects(accessToken);


        //If the clientStream is the connection between multitool and JazzRelay, then isExalt = true. serverStream is is the connection between us and rotmg.
        //Otherwise, the clientstream is the connection between rotmg and us, and serverstream is the connection to multitool.
        //If isExalt = true, then to cipher incoming packets from multitool we use clientreceive, and our cipher out to rotgm is the serversend.
        //If it is not true, then to cipher incoming packets from rotmg, we use serverReceive, and our cipher out to multitool is clientsend.
        //We clientsend data to exalt, clientrecieve data from exalt. We serversend packets out to rotmg, and serverrecieve packets from rotmg.
        //F*** me this got confusing.
        async Task Relay(TcpClient client, TcpClient server)
        {
            NetworkStream clientStream = client.GetStream(), serverStream = server.GetStream();
            bool isExalt = clientStream.IsExalt();
            RC4 cipherIn = isExalt ? _clientRecieveState : _serverRecieveState;
            RC4 cipherOut = isExalt ? _serverSendState : _clientSendState;

            try
            {
                while (clientStream.Socket.Connected && serverStream.Socket.Connected && _proxy.Listen)
                {
                    var resultData = await ProcessPacket(clientStream, cipherIn, isExalt) ?? throw new Exception("Error reading packet!");
                    await SendData(serverStream, cipherOut, resultData);
                }
            }
            catch (Exception ex) { if (!ex.Message.Contains("transport") && !ex.Message.Contains("Network")) Console.WriteLine("{0}\n{1}", ex.Message, ex.StackTrace); }

            Connected = false;
            Console.WriteLine("Disconnected.");
            Dispose();
        }

        async Task SendData(NetworkStream stream, RC4 cipher, byte[] data)
        {
            if (data.Length > 0)
            {
                cipher.Cipher(data, 5);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        async Task<byte[]?> ProcessPacket(NetworkStream stream, RC4 cipher, bool isExalt)
        {
            bool idk = stream.Socket.Connected;
            byte[] headers = new byte[5];
            bool broken = false;
            try
            {
                if (!(await stream.ReceiveAll(headers))) throw new Exception("Error recieving headers");
                byte[] data = new byte[headers.ToInt32() - 5];
                if (!(await stream.ReceiveAll(data))) throw new Exception("Error recieving data");
                if (data == null) broken = true;

                if (broken)
                {
                    Console.WriteLine((PacketType)headers[4]);
                    return null;
                }

                cipher.Cipher(data, 0);
                PacketType packetType = (PacketType)headers[4];
                WeGot(packetType);
                var resultData = headers.Concat(data).ToArray();
                if (_proxy.HasHook(packetType))
                    resultData = await HandlePacket(packetType, resultData, isExalt);

                return resultData;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        async Task<byte[]> HandlePacket(PacketType packet, byte[] totalData, bool isExalt)
        {
            Type? type;
            byte[] data = totalData.Skip(5).ToArray();
            if (!_proxy.PacketTypes.TryGetValue(packet, out type))
                throw new Exception("Hooked packet that's not defined!");

            object instance = Activator.CreateInstance(type) ?? throw new Exception($"Failed to instantiate packet instance {type.Name}."); ;


            PacketReader reader = new PacketReader(new MemoryStream(data));
            if (type.GetMethod("Read")?.DeclaringType == type)
                ((Packet)instance).Read(reader);
            else
                foreach (FieldInfo field in _proxy.GetFields(type))
                    field.SetFromReader(instance, reader);

            foreach (var hook in _proxy.GetHooks(packet))
                await (((Task?)hook.Item2.Invoke(hook.Item1, new object[2] { this, instance })) ?? Task.Delay(0));

            Packet thePacket = (Packet)instance;
            return thePacket.Send ? (PacketToBytes((Packet)instance, isExalt) ?? totalData) : new byte[0];
        }

        byte[]? PacketToBytes(Packet packet, bool isExalt)
        {
            var writer = new PacketWriter(new MemoryStream());
            writer.Write(0);
            writer.Write((byte)packet.PacketType);
            if (packet.GetType().GetMethod("Write")?.DeclaringType == packet.GetType())
                packet.Write(writer);
            else
                foreach (FieldInfo field in _proxy.GetFields(packet.GetType()))
                    field.WriteToWriter(packet, writer);
            var array = (writer.BaseStream as MemoryStream)?.ToArray();
            if (array == null) return null;
            var num = array.Length;
            array[0] = (byte)(num >> 24);
            array[1] = (byte)(num >> 16);
            array[2] = (byte)(num >> 8);
            array[3] = (byte)num;
            return array;
        }

        void WeGot(PacketType pt)
        {
            switch (pt)
            {
                case PacketType.MapInfo:
                    Connected = true;
                    break;
            }
        }

        //I modify multitool to send the ip address and port to avoid having to do reconnect or default nexus nonesense.
        //Depreciated
        async Task<(string, int)> GetHost(TcpClient client)
        {
            byte[] lengthBuff = new byte[4];
            await client.GetStream().ReadAsync(lengthBuff, 0, 4+Convert.ToInt32(!HWIDLock.IsMike()));
            int length = BitConverter.ToInt32(lengthBuff);
            byte[] host = new byte[length]; 
            byte[] port = new byte[4];
            await client.GetStream().ReadAsync(host, 0, length);
            await client.GetStream().ReadAsync(port, 0, 4);
            return (Encoding.ASCII.GetString(host), BitConverter.ToInt32(port, 0));
        }

        public async Task BeginRelay(TcpClient client)
        {
            //Process first hello to recover persistant variables
            var hello = await ProcessPacket(client.GetStream(), _clientRecieveState, true) ?? throw new Exception("Error reading first hello!");

            _socks5 = ConnectionInfo.Proxy;
            Socks5ProxyClient proxyClient = new Socks5ProxyClient(_socks5.Ip, _socks5.Port, _socks5.Username, _socks5.Password);
            
            string host = ConnectionInfo.Reconnect.host;
            int port = ConnectionInfo.Reconnect.Port;
            Console.WriteLine($"Connecting to {host}:{port}");

            proxyClient.CreateConnectionAsyncCompleted += async (sender, args) =>
            {
                TcpClient? server = args.ProxyConnection;
                if (server == null)
                {
                    throw new Exception("Unable to connect!");
                }
                else
                {
                    if (server.Connected)
                    {
                        Console.WriteLine("Connected to rotmg!");
                        await SendData(server.GetStream(), _serverSendState, hello);
                        _client = client;
                        _server = server;
                        _ = Task.Run(async () => await Relay(client, server));
                        _ = Task.Run(async () => await Relay(server, client));
                    }
                    else
                    {
                        Console.WriteLine("Error connecting to rotmg!");
                    }
                }
            };
            proxyClient.CreateConnectionAsync(host, port);
        }

        //I could go hog wild with reflection and infer the cipher and client based on the packet type
        //but I gotta stop somewhere lol
        public async Task SendToClient(IncomingPacket packet) => await Send(packet, _client, _clientSendState);
        public async Task SendToServer(OutgoingPacket packet) => await Send(packet, _server, _serverSendState); 

        async Task Send(Packet packet, TcpClient? client, RC4 cipher)
        {
            Console.WriteLine("sending");
            if (client == null || !(client?.Connected ?? false)) return;
            var bytes = PacketToBytes(packet, false);
            if (bytes == null) return;
            cipher.Cipher(bytes, 5);
            await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            _server?.Close();
            _client.Close();
            _server?.Dispose();
            _client.Dispose();
            _proxy.Clients.Remove(this);
        }

        [PluginEnabled]
        internal class RelayEssentials : IPlugin
        {
            public void HookEscape(Client client, Escape packet)
            {
                Server server = (Server)client.States["defaultServer"];
                client.ConnectionInfo.Reconnect.host = server.DNS;
                client.ConnectionInfo.Reconnect.Port = 2050;
            }

            public async void HookReconnect(Client client, Reconnect packet)
            {
                client.ConnectionInfo = new ConnectInfo(client._socks5, new Reconnect()
                {
                    GameId = packet.GameId,
                    host = packet.host == "" ? client.ConnectionInfo.Reconnect.host : packet.host,
                    Port = packet.Port == 0 ? client.ConnectionInfo.Reconnect.Port : packet.Port,
                    Key = packet.Key.ToArray(),
                    KeyTime = packet.KeyTime
                });
                packet.host = "127.0.0.1";
                packet.Port = Constants.Port/* + Convert.ToInt32(System.Security.Principal.WindowsIdentity.GetCurrent()?.User?.Value != "S-1-5-21-1853899583-3507715880-2321727073-1001")*/;
                await client.SendToClient(packet);
                client.Dispose();
            }

            //I'm choosing no persistance between Exalt restarts/accesstokens. I don't want to wait until update.
            public void HookHello(Client client, Hello packet)
            {
                client.SetPersistantObjects(packet.AccessToken);
                client.AccessToken = packet.AccessToken;
                if (packet.Key == null && packet.GameId != -2)
                {
                    packet.Key = client.ConnectionInfo.Reconnect.Key;
                    packet.KeyTime = client.ConnectionInfo.Reconnect.KeyTime;
                }
            }

            public void HookCreateSuccess(Client client, CreateSuccess packet) => client.ObjectId = packet.objectId;

            public void HookNewTick(Client client, NewTick packet)
            {
                foreach (var status in packet.statuses)
                {
                    if (status.objectId == client.ObjectId)
                    {
                        client.Position = status.position;
                        return;
                    }
                }
            }

            public async Task HookPlayerText(Client client, PlayerText packet)
            {
                if (packet.Text.StartsWith("con"))
                {
                    packet.Send = false;
                    string name = packet.Text.ToLower().Remove(0, 4);
                    Server? server = client._proxy.FindServer(name);
                    if (server != null)
                    {
                        client.States["defaultServer"] = server;
                        client._proxy.DefaultServer = server;
                        Settings.Default.DefaultServer = server.Name;
                        Settings.Default.Save();
                        client.ConnectionInfo = new ConnectInfo(client._socks5, new Reconnect() { host = server.DNS, Port = 2050 });
                        await client.SendToClient(new Reconnect()
                        { 
                            host = "127.0.0.1",
                            Port = Constants.Port,
                            Key = new byte[0],
                            KeyTime = -1,
                            GameId = -2,
                            MapName = "{\"t\":\"s.nexus\"}"
                        });
                        client.Dispose();
                    }
                }
                else if (packet.Text == "server")
                {
                    packet.Send = false;
                    Console.WriteLine(client.ConnectionInfo.Reconnect.host);
                }
            }
        }
    }
}
