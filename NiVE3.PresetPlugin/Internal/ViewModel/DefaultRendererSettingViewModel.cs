using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Mvvm;

namespace NiVE3.PresetPlugin.Internal.ViewModel
{
    class DefaultRendererSettingViewModel : BindableBase
    {
        private bool enableAntiAlias;
        public bool EnableAntiAlias
        {
            get { return enableAntiAlias; }
            set { SetProperty(ref enableAntiAlias, value); }
        }

        private bool enableShadowAntiAlias;
        public bool EnableShadowAntiAlias
        {
            get { return enableShadowAntiAlias; }
            set { SetProperty(ref enableShadowAntiAlias, value); }
        }
    }
}
