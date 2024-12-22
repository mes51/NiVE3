using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Data.Json.Project
{
    public class KeyFrameData
    {
        [JsonConverter(typeof(TimeJsonConverter))]
        public Time Time { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? Value { get; set; }

        public Ease EaseIn { get; set; } = new Ease(0.0, 0.0);

        public Ease EaseOut { get; set; } = new Ease(0.0, 0.0);

        public InterpolationType InterpolationType { get; set; }

        public int Id { get; set; }
    }
}
