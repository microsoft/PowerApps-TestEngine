using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testengine.user.storagestate
{
    public class PlatformHelper
    {
        public static bool IsWindows()
        {
#if NET48
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#elif NET8_0
            return OperatingSystem.IsWindows();
#elif NETSTANDARD2_0
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }
    }
}
