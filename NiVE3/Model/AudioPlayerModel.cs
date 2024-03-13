using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Audio;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class AudioPlayerModel : BindableBase
    {
        ScrubWavePlayer? ScrubPlayer { get; set; }

        public AudioPlayerModel()
        {
            var device = AudioDevice.GetDeviceOrDefaultDevice(AudioDevice.GetDefaultDeviceId());
            if (device != null)
            {
                ScrubPlayer = new ScrubWavePlayer(device, 50);
            }
        }

        public void PlayScrub()
        {
            ScrubPlayer?.Play();
        }

        public void StopScrub()
        {
            ScrubPlayer?.Stop();
        }

        public void AddScrubSample(float[] samples)
        {
            ScrubPlayer?.AddSample(samples);
        }
    }
}
