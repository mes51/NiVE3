using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Mvvm;

namespace NiVE3.PresetPlugin.Internal.ViewModel
{
    class ReinhardExtendedToneMapperSettingViewModel : BindableBase
    {
        private float maxLuminance = 1.0F;
        public float MaxLuminance
        {
            get { return maxLuminance; }
            set { SetProperty(ref maxLuminance, value); }
        }
    }
}
