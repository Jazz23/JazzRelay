using JazzRelay.Enums;
using JazzRelay.Extensions;
using JazzRelay.Packets.Utils;
using Starksoft.Aspen.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay
{
    internal class Client
    {
        static FieldInfo[] ParentFields = typeof(Packet).GetFields();
        JazzRelay _proxy;
        TcpClient _client;
        RC4 _serverRecieveState = new RC4(RC4.HexStringToBytes(Constants.ServerKey));
        RC4 _serverSendState = new RC4(RC4.HexStringToBytes(Constants.ClientKey));

        RC4 _clientRecieveState = new RC4(RC4.HexStringToBytes(Constants.ClientKey));
        RC4 _clientSendState = new RC4(RC4.HexStringToBytes(Constants.ServerKey));
        public Client(JazzRelay proxy, TcpClient client)
        {
            _proxy = proxy;
            _client = client;
            //HandlePacket(PacketType.Hello, File.ReadAllBytes("test.bin"), true);
            _ = Task.Run(async () => await BeginRelay(client));
        }

        async Task Relay(TcpClient client, TcpClient server)
        {
            NetworkStream clientStream = client.GetStream(), serverStream = server.GetStream();
            bool isExalt = clientStream.IsExalt();
            RC4 cipherIn = isExalt ? _clientRecieveState : _serverRecieveState;
            RC4 cipherOut = isExalt ? _serverSendState : _clientSendState;

            while (client.Connected && server.Connected && _proxy.Listen)
            {
                var headers = new byte[5];
                await clientStream.ReceiveAll(headers);
                var data = new byte[headers.ToInt32() - 5];
                await clientStream.ReceiveAll(data);
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
                await (((Task?)hook.Item2.Invoke(hook.Item1, new object[1] { instance })) ?? Task.Delay(0));

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

        //I modify multitool to send the ip address and port to avoid having to do reconnect nonesense.
        //I could do reconnect nonesense though if I wanted
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

        async Task BeginRelay(TcpClient client)
        {
            Proxy proxy = _proxy.FrontProxy;
            Socks5ProxyClient proxyClient = new Socks5ProxyClient(proxy.Ip, proxy.Port, proxy.Username, proxy.Password);
            var connectInfo = await GetHost(client);
            string host = connectInfo.Item1; int port = connectInfo.Item2;
            Console.WriteLine($"Recieved host {host} and port {port}.");

            proxyClient.CreateConnectionAsyncCompleted += (sender, args) =>
            {
                TcpClient server = args.ProxyConnection;
                if (server.Connected) Console.WriteLine("Connected to rotmg!");

                _ = Task.Run(async () => await Relay(client, server));
                _ = Task.Run(async () => await Relay(server, client));
            };
            proxyClient.CreateConnectionAsync(host, port);
        }
    }
}
