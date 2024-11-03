using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data
{
    class RuntimeInfoData
    {
        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; } = "";

        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("engineInfos")]
        public EngineInfoData[] EngineInfos { get; set; } = [];
    }

    class EngineInfoData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("uuid")]
        public Guid Uuid { get; set; }
    }
}
