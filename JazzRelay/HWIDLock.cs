using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay
{
    internal static class HWIDLock
    {
        public static bool IsMike()
        {
            try
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().User.Value == "S-1-5-21-1853899583-3507715880-2321727073-1001";
            }
            catch { }
            return false;
        }
    }
}
