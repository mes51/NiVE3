using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Data.Project
{
    public class InputData
    {
        public Guid InputId { get; set; }

        public Guid PluginId { get; set; }

        public string FilePath { get; set; } = "";

        public object? InputOption { get; set; }
    }
}