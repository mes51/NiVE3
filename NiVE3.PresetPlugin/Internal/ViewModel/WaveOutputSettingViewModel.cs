using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Mvvm;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;

namespace NiVE3.PresetPlugin.Internal.ViewModel
{
    [UseReactiveProperty]
    partial class WaveOutputSettingViewModel : BindableBase
    {
        [ReactiveProperty]
        public partial int SamplingRate { get; set; }

        [ReactiveProperty]
        public partial int BitsPerSample { get; set; }
    }
}
