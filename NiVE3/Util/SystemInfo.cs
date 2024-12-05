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
        const long MiB = 1024 * 1024;

        const long KiB = 1024;

        public static readonly long MaxImageCacheLimit;

        public static int MaxImageCacheLimitMiB => (int)(MaxImageCacheLimit / MiB);

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
            MaxImageCacheLimit = (int)Math.Floor(maxTotalVisibleMemory / KiB * 0.75) * MiB; // 全体の3/4まで
        }
    }
}
