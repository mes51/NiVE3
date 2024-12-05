using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;

namespace NiVE3.Data.Json.Project
{
    public class CompositionData
    {
        public Guid CompositionId { get; set; }

        public string Name { get; set; } = "";

        public int Width { get; set; }

        public int Height { get; set; }

        public double FrameRate { get; set; }

        public double Duration { get; set; }

        public bool IsRetentionFrameRate { get; set; }

        public bool ApplyToneMappingWhenNested { get; set; }

        public int ShutterAngle { get; set; }

        public int ShutterPhase { get; set; }

        public int MotionBlurSampleCount { get; set; }

        public double WorkareaBegin { get; set; }

        public double WorkareaEnd { get; set; }

        public LayerData[] Layers { get; set; } = [];

        public Guid RendererPluginId { get; set; }

        public Guid ToneMapperPluginId { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? RendererSetting { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? ToneMapperSetting { get; set; }

        // for timeline

        public double TimeBarRange { get; set; }

        public double TimeBarRangeStart { get; set; }

        public double CurrentTime { get; set; }
    }
}
