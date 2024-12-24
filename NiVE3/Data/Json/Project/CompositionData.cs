using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Data.Json.Project
{
    public class CompositionData
    {
        public Guid CompositionId { get; set; }

        public string Name { get; set; } = "";

        public int Width { get; set; }

        public int Height { get; set; }

        public double FrameRate { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time Duration { get; set; }

        public bool IsRetentionFrameRate { get; set; }

        public bool ApplyToneMappingWhenNested { get; set; }

        public int ShutterAngle { get; set; }

        public int ShutterPhase { get; set; }

        public int MotionBlurSampleCount { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time WorkareaBegin { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time WorkareaEnd { get; set; }

        public LayerData[] Layers { get; set; } = [];

        public Guid RendererPluginId { get; set; }

        public Guid ToneMapperPluginId { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? RendererSetting { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? ToneMapperSetting { get; set; }

        // for timeline

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time TimeBarRange { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time TimeBarRangeStart { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time CurrentTime { get; set; }
    }
}
