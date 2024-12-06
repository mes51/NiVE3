using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    static class SystemInfo
    {
        public static readonly long MaxCacheLimit;

        public static int MaxCacheLimitMiB => (int)(MaxCacheLimit / Const.MiB);

        static SystemInfo()
        {
            var maxTotalVisibleMemory = int.MaxValue;
            using var management = new ManagementClass("Win32_OperatingSystem");
            using var collection = management.GetInstances();
            foreach (var managementObject in collection)
            {
                using (managementObject)
                {
                    var memorySize = (int)(Convert.ToInt64(managementObject["TotalVisibleMemorySize"]));
                    maxTotalVisibleMemory = Math.Min(maxTotalVisibleMemory, memorySize);
                }
            }
            MaxCacheLimit = (int)Math.Floor(maxTotalVisibleMemory / Const.KiB * 0.75) * Const.MiB; // 全体の3/4まで
        }
    }
}
