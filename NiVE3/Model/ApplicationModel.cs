using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Config;
using NiVE3.Exceptions;
using NiVE3.Mvvm;
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

        public GPUException? LastGPUException { get; private set; }

        WeakEventPublisher<EventArgs> RaiseGPUExceptionPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> RaiseGPUException
        {
            add { RaiseGPUExceptionPublisher.Subscribe(value); }
            remove { RaiseGPUExceptionPublisher.Unsubscribe(value); }
        }

        public ApplicationModel()
        {
            UseGpu = ApplicationSetting.Setting.UseGpu && AcceleratorModel.HasHardwareAcceleratedGPU;
        }

        public void CaughtGPUException(GPUException ex)
        {
            LastGPUException = ex;
            UseGpu = false;
            RaiseGPUExceptionPublisher.Publish(this, EventArgs.Empty);
        }
    }
}
