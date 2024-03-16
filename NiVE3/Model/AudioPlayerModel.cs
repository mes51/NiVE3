using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Audio;
using NiVE3.Util;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class AudioPlayerModel : BindableBase
    {
        public double PreviewSpeed
        {
            get => PreviewWaveProvider.Speed;
            set => PreviewWaveProvider.Speed = value;
        }

        WrappedWavePlayer? ScrubPlayer { get; set; }

        WrappedWavePlayer? PreviewPlayer { get; set; }

        ScrubWaveProvider ScrubWaveProvider { get; }

        SpeedChangeableWaveProvider PreviewWaveProvider { get; }

        public AudioPlayerModel()
        {
            ScrubWaveProvider = new ScrubWaveProvider();
            PreviewWaveProvider = new SpeedChangeableWaveProvider();

            var device = AudioDevice.GetDeviceOrDefaultDevice(AudioDevice.GetDefaultDeviceId());
            if (device != null)
            {
                ScrubPlayer = new WrappedWavePlayer(device, 50);
                ScrubPlayer.Init(ScrubWaveProvider);

                PreviewPlayer = new WrappedWavePlayer(device, 50);
                PreviewPlayer.Init(PreviewWaveProvider);
            }
        }

        public void PlayScrub()
        {
            PreviewPlayer?.Stop();
            ScrubPlayer?.Play();
        }

        public void StopScrub()
        {
            ScrubPlayer?.Stop();
            ScrubWaveProvider.ClearBuffer();
        }

        public void PlayPreview()
        {
            ScrubPlayer?.Stop();
            PreviewWaveProvider.ClearHistory();
            PreviewPlayer?.Play();
        }

        public void StopPreview()
        {
            PreviewPlayer?.Stop();
        }

        public void AddScrubSample(float[] samples)
        {
            ScrubWaveProvider.AddSample(samples);
        }

        public void SetPreviewAudio(float[] audio, double loopStart, double loopEnd)
        {
            PreviewWaveProvider.SetAudio(audio);
            PreviewWaveProvider.LoopStart = Math.Max((int)(loopStart * Const.AudioSamplingRate) * Const.AudioChannelCount, 0);
            PreviewWaveProvider.LoopEnd = Math.Min((int)(loopEnd * Const.AudioSamplingRate) * Const.AudioChannelCount, audio.Length);
        }

        public double GetPlayingPosition()
        {
            if (PreviewPlayer != null)
            {
                return (PreviewWaveProvider.GetActualPosition(PreviewPlayer.GetPosition()) / Const.AudioChannelCount) / (double)Const.AudioSamplingRate;
            }
            else
            {
                return 0.0;
            }
        }

        public void SetPlayingPosition(double position)
        {
            PreviewWaveProvider.Position = (int)(position * Const.AudioSamplingRate) * Const.AudioChannelCount;
        }

        ~AudioPlayerModel()
        {
            ScrubPlayer?.Stop();
            ScrubPlayer?.Dispose();

            PreviewPlayer?.Stop();
            PreviewPlayer?.Dispose();
        }
    }
}
