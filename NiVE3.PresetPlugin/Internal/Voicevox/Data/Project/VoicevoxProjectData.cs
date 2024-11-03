using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.Project
{
    class VoicevoxProjectData
    {
        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; } = "";

        [JsonPropertyName("talk")]
        public ProjectTalkData? Talk { get; set; }
    }
}
