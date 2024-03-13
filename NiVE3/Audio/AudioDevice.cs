using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace NiVE3.Audio
{
    static class AudioDevice
    {
        public static string GetDefaultDeviceId()
        {
            try
            {
                using (var devices = new MMDeviceEnumerator())
                using (var device = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    return device.ID;
                }
            }
            catch
            {
                return "";
            }
        }

        public static MMDevice? GetDeviceOrDefaultDevice(string id)
        {
            using (var devices = new MMDeviceEnumerator())
            {
                foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (device.ID == id)
                    {
                        return device;
                    }
                    else
                    {
                        device.Dispose();
                    }
                }
            }

            try
            {
                using (var devices = new MMDeviceEnumerator())
                {
                    return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
