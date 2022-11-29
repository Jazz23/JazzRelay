#pragma warning disable 8618

using JazzRelay.Packets.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.Packets
{
    public class Reconnect : IncomingPacket
    {
		public string MapName;

		// Token: 0x0400059B RID: 1435
		public string host;

		// Token: 0x0400059C RID: 1436
		public ushort RealPort;

		public int Port { get => (int)RealPort; set => RealPort = (ushort)value; }

		// Token: 0x0400059D RID: 1437
		public int GameId;

		// Token: 0x0400059E RID: 1438
		public int KeyTime;

		// Token: 0x0400059F RID: 1439
		public byte[] Key;
	}
}
