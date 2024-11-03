using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.API
{
    class APIAccentPhaseData
    {
        [JsonPropertyName("moras")]
        public APIMoraData[] Moras { get; set; } = [];

        [JsonPropertyName("accent")]
        public int Accent { get; set; }

        [JsonPropertyName("pause_mora")]
        public APIMoraData? PauseMora { get; set; }

        [JsonPropertyName("is_interrogative")]
        public bool IsInterrogative { get; set; }
    }
}
