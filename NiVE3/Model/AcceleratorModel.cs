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
        public static readonly bool HasHardwareAcceleratedGPU;

        private GraphicsDevice? graphicsDevice;
        public GraphicsDevice CurrentDevice
        {
            get
            {
                // NOTE: HasHardwareAcceleratedGPUよりも先に使用するGraphicsDeviceを生成しないようにする
                if (graphicsDevice == null)
                {
                    graphicsDevice = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated && d.Luid.ToString() == ApplicationSetting.Setting.UseGpuLuid).FirstOrDefault();
                    if (graphicsDevice == null)
                    {
                        graphicsDevice = GraphicsDevice.GetDefault();
                        if (!graphicsDevice.IsHardwareAccelerated)
                        {
                            graphicsDevice = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated).FirstOrDefault() ?? graphicsDevice;
                        }
                    }
                }

                return graphicsDevice;
            }
        }

        static AcceleratorModel()
        {
            var device = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated).FirstOrDefault();
            HasHardwareAcceleratedGPU = device != null;

            if (device != GraphicsDevice.GetDefault())
            {
                device?.Dispose();
            }
        }

        public void Dispose()
        {
            graphicsDevice?.Dispose();
        }
    }
}
