using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NiVE3.Audio
{
    class WrappedWavePlayer : IWavePlayer
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

        WasapiOut Player { get; }

        public WrappedWavePlayer(MMDevice device, int latency)
        {
            Player = new WasapiOut(device, AudioClientShareMode.Shared, true, latency);
        }

        public void Init(IWaveProvider waveProvider)
        {
            Player.Init(waveProvider);
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

        public long GetPosition()
        {
            return Player.GetPosition();
        }

        public void Dispose()
        {
            Player.Dispose();
        }
    }
}
