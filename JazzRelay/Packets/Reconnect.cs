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
		public string _a2banwRB0uZoPHbIC0zXKW3IX3i;

		// Token: 0x0400059B RID: 1435
		public string host;

		// Token: 0x0400059C RID: 1436
		public ushort _XJmNMkJzG2oBMCp91qCtsWWPJUc;

		public int Port { get => (int)_XJmNMkJzG2oBMCp91qCtsWWPJUc; set => _XJmNMkJzG2oBMCp91qCtsWWPJUc = (ushort)value; }

		// Token: 0x0400059D RID: 1437
		public int _7emhxUTdr13gnLDpxCDdUT3o2De;

		// Token: 0x0400059E RID: 1438
		public int KeyTime;

		// Token: 0x0400059F RID: 1439
		public byte[] Key;
	}
}
