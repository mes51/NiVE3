using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.Project
{
    class ProjectTalkData
    {
        [JsonPropertyName("audioKeys")]
        public Guid[] AudioKeys { get; set; } = [];

        [JsonPropertyName("audioItems")]
        public Dictionary<Guid, ProjectAudioItemData> AudioItems { get; set; } = [];
    }
}
