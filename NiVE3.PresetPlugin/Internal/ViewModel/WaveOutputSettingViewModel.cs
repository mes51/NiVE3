using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Mvvm;

namespace NiVE3.PresetPlugin.Internal.ViewModel
{
    class WaveOutputSettingViewModel : BindableBase
    {
        private int samplingRate;
        public int SamplingRate
        {
            get { return samplingRate; }
            set { SetProperty(ref samplingRate, value); }
        }

        private int bitsPerSample;
        public int BitsPerSample
        {
            get { return bitsPerSample; }
            set { SetProperty(ref bitsPerSample, value); }
        }
    }
}
