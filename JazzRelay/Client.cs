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
        public ConnectInfo ConnectionInfo //Persitant connection information loaded on hello and before server connect
        {
            get => (ConnectInfo)States["ConnectionInfo"];
            set => States["ConnectionInfo"] = value;
        }

        public ConnectInfo OriginalConnInfo { get; private set; }
        public ObjectList States { get; set; } = new();
        public int ObjectId { get; set; } = -1;
        public WorldPosData Position { get; set; }
        public string AccessToken { get; set; }
        public bool Connected { get; set; }
        public Entity Self { get; set; }
        public string Name { get; private set; }
        public int Speed { get; private set; }
        public int Health { get => _health.statValue; private set => _health.statValue = value; }
        public int PrevHealth { get; private set; }
        public bool Dead { get; private set; } = false;
        StatData _health { get; set; }
        public Dictionary<int, Entity> Entities { get; set; } = new();
        public delegate void PlayerCommand(Client client, string command, string[] args);
        public event PlayerCommand Command;

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

        public void Escape()
        {
            Server server = (Server)States["defaultServer"];
            Task.Run(async () => await ConnectTo(server.DNS, 2050, -2, "Nexus", new byte[0], -1));
        }

        public void KillServerConnection()
        {
            Connected = false;
            _server?.Dispose();
        }

        public void Nexus() => Escape();

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

                if (packetType != PacketType.AllyShoot && packetType != PacketType.Move && packetType != PacketType.NewTick && packetType != PacketType.UpdateAck
                     && packetType != PacketType.Update && packetType != PacketType.Ping && packetType != PacketType.Pong
                      && packetType != PacketType.EnemyShoot) Console.WriteLine(packetType);

                if (packetType == PacketType.OtherHit)
                {

                }

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
            if (type.GetMethod("Read")?.DeclaringType == type) //We have a custom read
                ((Packet)instance).Read(reader);
            else
                foreach (FieldInfo field in _proxy.GetFields(type)) //Generic packet
                    field.SetFromReader(instance, reader);

            foreach (var hook in _proxy.GetHooks(packet))
                await (((Task?)hook.Item2.Invoke(hook.Item1, new object[2] { this, instance })) ?? Task.Delay(0));

            Packet thePacket = (Packet)instance;

            if (thePacket.GetType().IsEquivalentTo(typeof(PlayerHit)))
                Console.WriteLine(thePacket.Send);

            return thePacket.Send ? (PacketToBytes((Packet)instance, isExalt) ?? totalData) : new byte[0];
        }

        byte[]? PacketToBytes(Packet packet, bool isExalt)
        {
            var writer = new PacketWriter(new MemoryStream());
            writer.Write(0);
            writer.Write((byte)packet.PacketType);
            if (packet.GetType().GetMethod("Write")?.DeclaringType == packet.GetType()) //Custom write
                packet.Write(writer);
            else
                foreach (FieldInfo field in _proxy.GetFields(packet.GetType())) //Generic
                    field.WriteToWriter(packet, writer);
            var array = (writer.BaseStream as MemoryStream)?.ToArray();
            if (array == null) return null;
            var num = array.Length;
            array[0] = (byte)(num >> 24); //Converting length to a differnet endian
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
                    Server? nexus = JazzRelay.FindServerByHost(host);
                    string name = nexus == null ? host : nexus.Name;
                    Console.WriteLine($"Error connecting to {name}!");
                    States["pause"] = true;
                }
                else
                {
                    if (server.Connected)
                    {
                        Console.WriteLine("Connected to rotmg!");
                        try
                        {
                            await SendData(server.GetStream(), _serverSendState, hello);
                        }
                        catch
                        {
                            States["pause"] = true;
                            return;
                        }
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

            if (States.ContainsKey("pause"))
            {
                await Task.Delay(1000);
                States.Remove("pause");
            }
            Console.WriteLine($"Connecting with {_socks5.Ip}");
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

        public async Task ConnectTo(string host, int port, int gameId, string mapName, byte[] key, int keyTime)
        {
            ConnectionInfo = new ConnectInfo(_socks5, new Reconnect()
            {
                host = host,
                Port = port,
                GameId = gameId,
                MapName = mapName,
                Key = key,
                KeyTime = keyTime
            });

            int pport = Constants.Port;
#if !DEBUG
            pport += Convert.ToInt32(System.Security.Principal.WindowsIdentity.GetCurrent().User.Value != "S-1-5-21-1853899583-3507715880-2321727073-1001");
#endif
            await SendToClient(new Reconnect()
            {
                host = "127.0.0.1",
                Port = pport,
                Key = key,
                KeyTime = keyTime,
                GameId = gameId,
                MapName = mapName
            });
            Dispose();
        }

        public async Task ConnectTo(Reconnect recon) =>
            await ConnectTo(recon.host, recon.Port, recon.GameId, recon.MapName, recon.Key, recon.KeyTime);

        [PluginEnabled]
        internal class RelayEssentials : IPlugin
        {
            public void HookEscape(Client client, Escape packet)
            {
                packet.Send = false;
                client.Escape();
            }

            public void HookUpdate(Client client, Update packet)
            {
                foreach (var entity in packet.Entities) //I'm not doing newtick update crap unless I have to
                {
                    if (entity.Stats.ObjectId == client.ObjectId)
                    {
                        client.Self = entity;
                        client.Name = entity.Stats.Stats.First(x => x.statType == (byte)StatDataType.Name).stringValue;
                        client.Speed = entity.Stats.Stats.First(x => x.statType == (byte)StatDataType.Speed).statValue;
                        client._health = entity.Stats.Stats.First(x => x.statType == (byte)StatDataType.HP);
                    }
                    client.Entities[entity.Stats.ObjectId] = entity;
                }

                foreach (var drop in packet.Drops)
                    client.Entities.Remove(drop);
            }

            public void HookReconnect(Client client, Reconnect packet)
            {
                packet.host = packet.host == "" ? client.ConnectionInfo.Reconnect.host : packet.host;
                packet.Port = packet.Port == 0 ? client.ConnectionInfo.Reconnect.Port : packet.Port;
                client.ConnectionInfo = new ConnectInfo(client._socks5, packet.CloneReconnect());
                packet.host = "127.0.0.1";
                packet.Port = Constants.Port;
                //await client.ConnectTo(host, port, packet.GameId, packet.MapName, packet.Key, packet.KeyTime);
            }

            //I'm choosing no persistance between Exalt restarts/accesstokens. I don't want to wait until update.
            public void HookHello(Client client, Hello packet)
            {
                client.SetPersistantObjects(packet.AccessToken);
                client.AccessToken = packet.AccessToken;
                client.OriginalConnInfo = new ConnectInfo(client.ConnectionInfo.Proxy, client.ConnectionInfo.Reconnect.CloneReconnect());

                if (packet.GameId == -2 && !client.ConnectionInfo.IsNexus()) //We got manually sent here
                    client.ConnectionInfo.Reconnect.GameId = 0;
            }

            public void HookCreateSuccess(Client client, CreateSuccess packet)
            {
                client.ObjectId = packet.objectId;
                client.Command += OnCommand;
            }

            public void HookNewTick(Client client, NewTick packet)
            {
                foreach (var status in packet.statuses)
                {
                    if (status.ObjectId == client.ObjectId)
                    {
                        UpdateStats(client, status);
                        return;
                    }
                }
            }

            void UpdateStats(Client client, ObjectStatusData newStats)
            {
                client.Position = newStats.Position;
                client.PrevHealth = client.Health;
                for (int i = 0; i < newStats.Stats.Length; i++)
                {
                    StatData newStat = newStats.Stats[i];
                    StatData? oldStat = client.Self.Stats.Stats.FirstOrDefault(x => x.statType == newStat.statType);
                    if (oldStat != null)
                    {
                        oldStat.stringValue = newStat.stringValue;
                        oldStat.magicNumber = newStat.magicNumber;
                        oldStat.statValue = newStat.statValue;
                    }
                    else
                        client.Self.Stats.Stats.Append(newStat);
                }
            }

            public void HookFailure(Client client, Failure packet)
            {
                Console.WriteLine($"Failure {packet.errorId}. {packet.errorDescription}");
            }

            public void HookPlayerText(Client client, PlayerText packet)
            {
                //Missing prefix or contains no command
                if (!packet.Text.StartsWith(Constants.CommandPrefix) || packet.Text.Length < 2) return;
                packet.Send = false;
                string[] info = packet.Text.Remove(0, 1).Split(' ');

                Task.Run(() => client.Command?.Invoke(client, info[0], info.Length > 1 ? info.Skip(1).ToArray() : new string[0]));
            }

            public void HookPlayerHit(Client client, PlayerHit packet)
            {
                Console.WriteLine($"{client.Health}  {client.PrevHealth} {packet.Damage}");
                client.Health -= packet.Damage;
                client.PrevHealth -= packet.Damage;
                if (client.PrevHealth <= 0) client.Dead = true;
            }

            void OnCommand(Client client, string command, string[] args)
            {
                if (command == "con" && args.Length > 0)
                {
                    Server? server = JazzRelay.FindServer(args[0]);
                    if (server != null)
                    {
                        client.States["defaultServer"] = server;
                        client._proxy.DefaultServer = server;
                        Settings.Default.DefaultServer = server.Name;
                        Settings.Default.Save();
                        client.Escape();
                    }
                }
                else if (command == "server")
                {
                    Console.WriteLine(client.ConnectionInfo.Reconnect.host);
                    Server? server = JazzRelay.FindServerByHost(client.OriginalConnInfo.Reconnect.host);
                    Console.WriteLine(server?.Name);
                }
                else if (command == "loc")
                    Console.WriteLine(client.Position);
            }
        }
    }
}
