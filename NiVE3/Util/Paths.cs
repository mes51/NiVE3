using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    // TODO: 名前空間はここで良いのか?
    public static class Paths
    {
        public static readonly string ExecutionFileDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

        public static readonly string PluginDirectory = Path.Combine(ExecutionFileDirectory, "Plugins");

        public static readonly string ConfigDirectory = Path.Combine(ExecutionFileDirectory, "Config");

        public static readonly string AutoSaveProjectDirectory = Path.Combine(ExecutionFileDirectory, "AutoSave");

        public static readonly string ErrorLogDirectory = Path.Combine(ExecutionFileDirectory, "ErrorLog");

        public static readonly string CompositionPresetFilePath = Path.Combine(ConfigDirectory, "composition_preset.json");

        static Paths()
        {
            try
            {
                Directory.CreateDirectory(ConfigDirectory);
                Directory.CreateDirectory(PluginDirectory);
            }
            catch { }
        }
    }
}
