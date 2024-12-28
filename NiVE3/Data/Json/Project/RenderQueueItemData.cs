using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;
using NiVE3.Model;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Data.Json.Project
{
    public class RenderQueueItemData
    {
        public Guid QueueId { get; set; }

        public bool IsRenderSelected { get; set; }

        public string FilePath { get; set; } = "";

        public RenderRangeType RenderRangeType { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time BeginTime { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time EndTime { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time FixedBeginTime { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time FixedEndTime { get; set; }

        public bool IsOutputVideo { get; set; }

        public bool IsOutputAudio {  get; set; }

        public Guid OutputPluginId { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? OutputSetting { get; set; }

        public RenderQueueItemState State { get; set; }

        public Guid CompositionId { get; set; }

        public TimeSpan RenderingTime { get; set; }
    }
}
