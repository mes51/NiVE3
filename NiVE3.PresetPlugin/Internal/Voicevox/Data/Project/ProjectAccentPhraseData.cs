using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.Project
{
    class ProjectAccentPhraseData
    {
        [JsonPropertyName("moras")]
        public ProjectMoraData[] Moras { get; set; } = [];

        [JsonPropertyName("accent")]
        public int Accent { get; set; }

        [JsonPropertyName("pauseMora")]
        public ProjectMoraData? PauseMora { get; set; }

        [JsonPropertyName("isInterrogative")]
        public bool? isInterrogative { get; set; }
    }
}
