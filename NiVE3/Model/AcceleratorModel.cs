using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Config;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class AcceleratorModel : BindableBase, IAcceleratorObject, IDisposable
    {
        public GraphicsDevice CurrentDevice { get; private set; }

        public AcceleratorModel()
        {
            var device = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated && d.Luid.ToString() == ApplicationSetting.Setting.UseGpuLuid).FirstOrDefault();
            if (device == null)
            {
                device = GraphicsDevice.GetDefault();
                if (!device.IsHardwareAccelerated)
                {
                    device = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated).FirstOrDefault() ?? device;
                }
            }

            CurrentDevice = device;
        }

        public void Dispose() { }

        public static bool CanUseGpu()
        {
            using var hardwareAccelerated = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated).FirstOrDefault();
            return hardwareAccelerated != null;
        }
    }
}
