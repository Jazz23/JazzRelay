using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Extensions
{
    public static class SocketExt //No credit to me for this
    {
        public static async Task ReceiveAll(this NetworkStream stream, byte[] data)
        {
            int num;
            for (var i = 0; i < data.Length; i += num)
            {
                num = await stream.ReadAsync(data, i, data.Length - i);
                if (num == 0) throw new Exception("We have reached the end of the stream.");
            }
        }

        public static bool IsExalt(this NetworkStream stream) => stream.Socket.RemoteEndPoint?.ToString()?.Split(':')[0] == "127.0.0.1";

        public static int ToInt32(this byte[] buffer, int index = 0)
        {
            return buffer[index + 3] | (buffer[index + 2] << 8) | (buffer[index + 1] << 16) | (buffer[index] << 24);
        }
    }
}
