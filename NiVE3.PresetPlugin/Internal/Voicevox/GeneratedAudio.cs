using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWaves.Signals;

namespace NiVE3.PresetPlugin.Internal.Voicevox
{
    record GeneratedAudio(DiscreteSignal Wave, string SpeakerName, string Text)
    {
        public string DisplayName => $"{SpeakerName} - {Text}";
    }
}
