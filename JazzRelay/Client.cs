using JazzRelay.Enums;
using JazzRelay.Extensions;
using JazzRelay.Packets;
using JazzRelay.Packets.DataTypes;
using JazzRelay.Packets.Utils;
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
    internal class Client
    {
        JazzRelay _proxy;
        TcpClient _client;
        TcpClient? _server;
        RC4 _serverRecieveState = new RC4(RC4.HexStringToBytes(Constants.ServerKey));
        RC4 _serverSendState = new RC4(RC4.HexStringToBytes(Constants.ClientKey));

        RC4 _clientRecieveState = new RC4(RC4.HexStringToBytes(Constants.ClientKey));
        RC4 _clientSendState = new RC4(RC4.HexStringToBytes(Constants.ServerKey));
        public (string, int) ConnectionInfo;
        public ObjectList States = new();
        public int ObjectId = -1;
        public WorldPosData Position;

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
                    byte[] headers = new byte[5];
                       //BeginRelay READ STUFF IT'S NOT REACHING THE DISPOSE LINE'
                    if (!(await clientStream.ReceiveAll(headers))) break;
                    byte[] data = new byte[headers.ToInt32() - 5];
                    if (!(await clientStream.ReceiveAll(data))) break;
                if (data == null) break;

                    cipherIn.Cipher(data, 0);
                    PacketType packetType = (PacketType)headers[4];
                    var resultData = headers.Concat(data).ToArray();
                    if (_proxy.HasHook(packetType))
                        resultData = await HandlePacket(packetType, resultData, isExalt);

                    if (resultData.Length > 0)
                    {
                        cipherOut.Cipher(resultData, 5);
                        await serverStream.WriteAsync(resultData, 0, resultData.Length);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Console.WriteLine("Disconnected.");
            client.Dispose();
            server.Dispose();
        }

        async Task<byte[]> HandlePacket(PacketType packet, byte[] totalData, bool isExalt)
        {
            Type? type;
            byte[] data = totalData.Skip(5).ToArray();
            if (!_proxy.PacketTypes.TryGetValue(packet, out type))
                throw new Exception("Hooked packet that's not defined!");

            object? instance = Activator.CreateInstance(type);
            if (instance == null)
                throw new Exception($"Failed to instantiate packet instance {type.Name}.");

            PacketReader reader = new PacketReader(new MemoryStream(data));
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

        //I modify multitool to send the ip address and port to avoid having to do reconnect or default nexus nonesense.
        async Task<(string, int)> GetHost(TcpClient client)
        {
            byte[] lengthBuff = new byte[4];
            await client.GetStream().ReadAsync(lengthBuff, 0, 4);
            int length = BitConverter.ToInt32(lengthBuff);
            byte[] host = new byte[length]; 
            byte[] port = new byte[4];
            await client.GetStream().ReadAsync(host, 0, length);
            await client.GetStream().ReadAsync(port, 0, 4);
            return (Encoding.ASCII.GetString(host), BitConverter.ToInt32(port, 0));
        }

        async Task BeginRelay(TcpClient client, (string, int)? ipport = null)
        {
            Proxy proxy = _proxy.FrontProxy;
            //Socks5ProxyClient proxyClient = new Socks5ProxyClient(proxy.Ip, proxy.Port, proxy.Username, proxy.Password);
            var connectInfo = ipport == null ? await GetHost(client) : ((string, int))ipport;
            string host = connectInfo.Item1; int port = connectInfo.Item2;
            Console.WriteLine($"Recieved host {host} and port {port}.");
            TcpClient serverClient = new TcpClient();

            serverClient.BeginConnect(host, port, (ar) =>
            {
                if (serverClient.Connected)
                {
                    Console.WriteLine("Connected to rotmg!");
                    _client = client;
                    _server = serverClient;
                    ConnectionInfo = connectInfo;
                    _ = Task.Run(async () => await Relay(client, serverClient));
                    _ = Task.Run(async () => await Relay(serverClient, client));
                }
                else
                {
                    Console.WriteLine("Error connecting to rotmg!");
                }
            }, new object());

            //proxyClient.CreateConnectionAsyncCompleted += (sender, args) =>
            //{
            //    TcpClient? server = args.ProxyConnection;
            //    if (server == null)
            //    {
            //        Console.WriteLine("Unable to connect! Retrying");
            //        _ = Task.Run(async () => await BeginRelay(client, connectInfo));
            //    }
            //    else
            //    {
            //        if (server.Connected)
            //        {
            //            Console.WriteLine("Connected to rotmg!");
            //            _client = client;
            //            _server = server;
            //            ConnectionInfo = connectInfo;
            //            _ = Task.Run(async () => await Relay(client, server));
            //            _ = Task.Run(async () => await Relay(server, client));
            //        }
            //        else
            //        {
            //            Console.WriteLine("Error connecting to rotmg!");
            //        }
            //    }
            //};
            //proxyClient.CreateConnectionAsync(/*host*/"3.82.126.16", /*port*/2050);
        }

        //I could go hog wild with reflection and infer the cipher and client based on the packet type
        //but I gotta stop somewhere lol
        public async Task SendToClient(IncomingPacket packet) => await Send(packet, _client, _clientSendState);
        public async Task SendToServer(OutgoingPacket packet) => await Send(packet, _server, _serverSendState); 

        async Task Send(Packet packet, TcpClient? client, RC4 cipher)
        {
            if (client == null || !(client?.Connected ?? false)) return;
            var bytes = PacketToBytes(packet, false);
            if (bytes == null) return;
            cipher.Cipher(bytes, 5);
            await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
