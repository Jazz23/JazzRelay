using JazzRelay.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Plugins.Utils
{
    internal static class Utils
    {
        public static async Task SendNotification(this Client client, string text)
        {
            await client.SendToClient(new Notification()
            {
                Message = text,
                ObjectId = client.ObjectId,
                Byte1 = 6,
                Color = 65535
            });
        }
    }
}
