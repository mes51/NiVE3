using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.Project
{
    class ProjectAudioItemData
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("voice")]
        public ProjectVoiceData Voice { get; set; } = new ProjectVoiceData();

        [JsonPropertyName("query")]
        public ProjectAudioQueryData? Query { get; set; }

        [JsonPropertyName("morphingInfo")]
        public ProjectMorphingInfoData? MorphingInfo { get; set; }
    }

    class ProjectVoiceData
    {
        [JsonPropertyName("engineId")]
        public Guid EngineId { get; set; }

        [JsonPropertyName("speakerId")]
        public Guid SpeakerId { get; set; }

        [JsonPropertyName("styleId")]
        public int StyleId { get; set; }
    }

    class ProjectMorphingInfoData
    {
        [JsonPropertyName("rate")]
        public double Rate { get; set; }

        [JsonPropertyName("targetEngineId")]
        public Guid TargetEngineId { get; set; }

        [JsonPropertyName("targetSpeakerId")]
        public Guid TargetSpeakerId { get; set; }

        [JsonPropertyName("targetStyleId")]
        public int TargetStyleId { get; set; }
    }
}
