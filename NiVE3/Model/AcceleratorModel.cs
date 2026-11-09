using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Cache;
using NiVE3.Config;
using NiVE3.Image.Internal;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class AcceleratorModel : BindableBase, IAcceleratorObject, IDisposable
    {
        public static readonly bool HasHardwareAcceleratedGPU;

        private GraphicsDevice? currentDevice;
        public GraphicsDevice CurrentDevice
        {
            get
            {
                // NOTE: HasHardwareAcceleratedGPUよりも先に使用するGraphicsDeviceを生成しないようにする
                if (currentDevice == null)
                {
                    currentDevice = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated && d.Luid.ToString() == ApplicationSetting.Setting.UseGpuLuid).FirstOrDefault();
                    if (currentDevice == null)
                    {
                        currentDevice = GraphicsDevice.GetDefault();
                        if (!currentDevice.IsHardwareAccelerated)
                        {
                            currentDevice = GraphicsDevice.QueryDevices(d => d.IsHardwareAccelerated).FirstOrDefault() ?? currentDevice;
                        }
                    }
                    currentDevice.DeviceLost += GraphicsDevice_DeviceLost;
                }

                return currentDevice;
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

        public AcceleratorModel()
        {
            ApplicationSetting.Setting.UpdateSetting += Setting_UpdateSetting;
        }

        public void Dispose()
        {
            ReleaseDevice(currentDevice);
            currentDevice = null;
        }

        static void ReleaseDevice(GraphicsDevice? device)
        {
            if (device == null)
            {
                return;
            }

            ImageCache.ReleaseGpuResources();
            GPUBufferCache.ReleaseDevice(device);
            device.Dispose();
        }

        private void GraphicsDevice_DeviceLost(object? sender, DeviceLostEventArgs e)
        {
            currentDevice = null;
        }

        private void Setting_UpdateSetting(object? sender, EventArgs e)
        {
            if (currentDevice == null)
            {
                return;
            }

            var oldDevice = currentDevice;
            currentDevice = null;
            if (CurrentDevice != oldDevice && oldDevice != GraphicsDevice.GetDefault())
            {
                ReleaseDevice(oldDevice);
            }
        }
    }
}
