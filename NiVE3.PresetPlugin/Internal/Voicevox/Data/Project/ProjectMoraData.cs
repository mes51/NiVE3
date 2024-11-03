using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.Project
{
    class ProjectMoraData
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("vowel")]
        public string Vowel { get; set; } = "";

        [JsonPropertyName("vowelLength")]
        public double VowelLength { get; set; }

        [JsonPropertyName("pitch")]
        public double Pitch { get; set; }

        [JsonPropertyName("consonant")]
        public string Consonant { get; set; } = "";

        [JsonPropertyName("consonantLength")]
        public double? ConsonantLength { get; set; }
    }
}
