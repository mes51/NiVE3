using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Data.Json.Project
{
    public class FootageListData
    {
        public FootageSortKeyData SortKey { get; set; }

        public bool SortIsAscending { get; set; }

        public InputData[] Inputs { get; set; } = [];

        public InputData[] Placeholders { get; set; } = [];

        public FootageData[] Footages { get; set; } = [];
    }

    public class FootageData
    {
        public FootageDataType DataType { get; set; }

        public Guid FootageId { get; set; }

        public string Name { get; set; } = "";

        public int Width { get; set; }

        public int Height { get; set; }

        public double FrameRate { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time Duration { get; set; }

        public string FilePath { get; set; } = "";

        public string Comment { get; set; } = "";

        public SourceType InputType { get; set; } = SourceType.None;

        public FootageData[]? Children { get; set; }

        // for FootageModel

        public Guid? InputId { get; set; }

        // Placeholder用
        public Guid? InputPluginId { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? InputOption { get; set; }

        public string? SourceId { get; set; }
    }

    public enum FootageSortKeyData : int
    {
        Name,
        Width,
        FrameRate,
        Duration,
        Comment,
        FilePath
    }

    public enum FootageDataType
    {
        Source,
        Folder
    }
}
