using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.API
{
    class SpeakerData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("speaker_uuid")]
        public Guid SpeakerUuid { get; set; }

        [JsonPropertyName("styles")]
        public StyleData[] Styles { get; set; } = [];

        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("supported_features")]
        public SupportedFeatureData? SupportedFeatures { get; set; }
    }

    class StyleData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    }

    class SupportedFeatureData
    {
        [JsonPropertyName("permitted_synthesis_morphing")]
        public string PermittedSynthesisMorphing { get; set; } = "";
    }
}
