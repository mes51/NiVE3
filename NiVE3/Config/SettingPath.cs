using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Config
{
    public static class SettingPath
    {
        public static readonly string ExecutionFileDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

        public static readonly string ConfigDirectory = Path.Combine(ExecutionFileDirectory, "Config");

        static SettingPath()
        {
            try
            {
                Directory.CreateDirectory(SettingPath.ConfigDirectory);
            }
            catch { }
        }
    }
}
