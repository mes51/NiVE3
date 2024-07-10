using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Config;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class ApplicationModel : BindableBase
    {
        private bool useGpu;
        public bool UseGpu
        {
            get { return useGpu; }
            set { SetProperty(ref useGpu, value); }
        }

        public ApplicationModel()
        {
            UseGpu = ApplicationSetting.Setting.UseGpu && AcceleratorModel.HasHardwareAcceleratedGPU;
        }
    }
}
