using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NiVE3.Audio
{
    class ScrubWavePlayer : IWavePlayer
    {
        public float Volume
        {
            get
            {
                return Player.Volume;
            }
            set
            {
                Player.Volume = value;
            }
        }

        public PlaybackState PlaybackState => Player.PlaybackState;

        public WaveFormat OutputWaveFormat => Player.OutputWaveFormat;

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        SampleBufferedWaveProvider Provider { get; }

        WasapiOut Player { get; }

        public ScrubWavePlayer(MMDevice device, int latency)
        {
            Provider = new SampleBufferedWaveProvider();
            Player = new WasapiOut(device, AudioClientShareMode.Shared, true, latency);
            Player.Init(Provider);
        }

        public void Init(IWaveProvider waveProvider)
        {
            throw new InvalidOperationException("already initialized");
        }

        public void Pause()
        {
            Player.Pause();
        }

        public void Play()
        {
            Player.Play();
        }

        public void Stop()
        {
            Player.Stop();
            Provider.ClearBuffer();
        }

        public bool IsDeviceAlive()
        {
            try
            {
                Player.GetPosition();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void AddSample(float[] sample)
        {
            Provider.ClearBuffer();
            Provider.AddSample(sample);
        }

        public void Dispose()
        {
            Player.Dispose();
        }
    }
}
