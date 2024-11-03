using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Voicevox.Data.Project
{
    class ProjectAudioQueryData
    {
        [JsonPropertyName("accentPhrases")]
        public ProjectAccentPhraseData[] AccentPhrases { get; set; } = [];

        [JsonPropertyName("speedScale")]
        public double SpeedScale { get; set; }

        [JsonPropertyName("pitchScale")]
        public double PitchScale { get; set; }

        [JsonPropertyName("intonationScale")]
        public double IntonationScale { get; set; }

        [JsonPropertyName("volumeScale")]
        public double VolumeScale { get; set; }

        [JsonPropertyName("prePhonemeLength")]
        public double PrePhonemeLength { get; set; }

        [JsonPropertyName("postPhonemeLength")]
        public double PostPhonemeLength { get; set; }

        [JsonPropertyName("outputSamplingRate")]
        public int OutputSamplingRate { get; set; }

        [JsonPropertyName("outputStereo")]
        public bool OutputStereo { get; set; }

        [JsonPropertyName("kana")]
        public string Kana { get; set; } = "";
    }
}
