using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Input;
using NiVE3.PresetPlugin.Internal.Mvvm;

namespace NiVE3.PresetPlugin.Internal.ViewModel
{
    class DirectShowInputSettingViewModel : BindableBase
    {
        private VideoAlphaType videoAlphaType;
        public VideoAlphaType VideoAlphaType
        {
            get { return videoAlphaType; }
            set { SetProperty(ref videoAlphaType, value); }
        }
    }
}
