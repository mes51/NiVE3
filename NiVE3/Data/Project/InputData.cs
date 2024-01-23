using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Data.Project
{
    public class InputData
    {
        public Guid InputId { get; set; }

        public Guid PluginId { get; set; }

        public string FilePath { get; set; } = "";

        public object? InputOption { get; set; }

        public SourceData[] Sources { get; set; } = Array.Empty<SourceData>();
    }

    public class SourceData
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public double FrameRate { get; set; }

        public double Duration { get; set; }

        public SourceType SourceType { get; set; } = SourceType.None;

        public string SourceId { get; set; } = "";
    }
}