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
        public bool UseGpu
        {
            get { return !ApplicationSetting.Setting.ForceUseCpu && field; }
            set { SetProperty(ref field, value); }
        }

        public GPUException? LastGPUException { get; private set; }

        WeakEventPublisher<RaiseGPUExceptionEventArgs> RaiseGPUExceptionPublisher { get; } = new WeakEventPublisher<RaiseGPUExceptionEventArgs>();
        public event EventHandler<RaiseGPUExceptionEventArgs> RaiseGPUException
        {
            add { RaiseGPUExceptionPublisher.Subscribe(value); }
            remove { RaiseGPUExceptionPublisher.Unsubscribe(value); }
        }

        public ApplicationModel()
        {
            UseGpu = AcceleratorModel.HasHardwareAcceleratedGPU;
        }

        public void CaughtGPUException(GPUException ex)
        {
            LastGPUException = ex;
            UseGpu = false;
            RaiseGPUExceptionPublisher.Publish(this, new RaiseGPUExceptionEventArgs(ex));
        }
    }
}
