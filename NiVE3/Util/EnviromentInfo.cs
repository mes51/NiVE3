using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NiVE3.Util
{
    static class EnviromentInfo
    {
        static string GetRegistryKey(string keyName, string valueName)
        {
            try
            {
                return Registry.GetValue(keyName, valueName, "")?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetOSProductName()
        {
            return GetRegistryKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
        }

        public static string GetOSReleaseId()
        {
            return GetRegistryKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId");
        }

        public static string GetOSBuild()
        {
            return GetRegistryKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild");
        }
    }
}
