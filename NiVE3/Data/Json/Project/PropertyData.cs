using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;
using NiVE3.Plugin.Property;

namespace NiVE3.Data.Json.Project
{
    public class PropertyData
    {
        public string PropertyId { get; set; } = "";

        public Guid InstanceId { get; set; }

        public string PropertyTypeName { get; set; } = "";

        public string Name { get; set; } = "";

        [JsonConverter(typeof(PluginOptionValueObjectConverter))]
        public object? Value { get; set; }

        public bool IsEnabled { get; set; } = true;

        public KeyFrameData[] KeyFrames { get; set; } = [];

        public PropertyData[]? Children { get; set; }
    }
}
