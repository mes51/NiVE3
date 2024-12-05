using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;
using NiVE3.Model;

namespace NiVE3.Data.Json.Project
{
    public class RenderQueueItemData
    {
        public Guid QueueId { get; set; }

        public bool IsRenderSelected { get; set; }

        public string FilePath { get; set; } = "";

        public RenderRangeType RenderRangeType { get; set; }

        public double BeginTime { get; set; }

        public double EndTime { get; set; }

        public double FixedBeginTime { get; set; }

        public double FixedEndTime { get; set; }

        public bool IsOutputVideo { get; set; }

        public bool IsOutputAudio {  get; set; }

        public Guid OutputPluginId { get; set; }

        [JsonConverter(typeof(PluginOptionValueObjectJsonConverter))]
        public object? OutputSetting { get; set; }

        public RenderQueueItemState State { get; set; }

        public Guid CompositionId { get; set; }
    }
}
