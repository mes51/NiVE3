using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.MediaFoundation;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(MediaFoundationInput), "MediaFoundationInput", "", "mes51", ID, "*.avi,*.mp4,*.m4a,*.wav,*.mp3,*.wma,*.aac", true)]
    public class MediaFoundationInput : IInput
    {
        const string ID = "3BB12986-32DF-4C41-8D36-46C5E402C6AC";

        VideoSourceReaderBase? VideoReader { get; set; }

        AudioSourceReader? AudioReader { get; set; }

        public string FilePath { get; private set; } = "";

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose()
        {
            VideoReader?.Dispose();
        }

        public bool Load(string filePath)
        {
            FilePath = filePath;

            try
            {
                VideoReader = new AcceleratedVideoSourceReader(filePath);
            }
            catch { }
            if (!VideoReader?.Succeeded ?? false)
            {
                try
                {
                    VideoReader = new SoftwareVideoSourceReader(filePath);
                }
                catch { }
            }

            try
            {
                AudioReader = new AudioSourceReader(filePath);
            }
            catch { }

            if ((VideoReader?.Succeeded ?? false) || (AudioReader?.Success ?? false))
            {
                if (VideoReader?.Succeeded ?? false)
                {
                    // NOTE: 読み込み時にサイズが変わる可能性があるため1フレームだけ読み込む
                    VideoReader.GetFrame(0.0);
                }
                else
                {
                    VideoReader?.Dispose();
                    VideoReader = null;
                }
                if (!(AudioReader?.Success ?? false))
                {
                    AudioReader?.Dispose();
                    AudioReader = null;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public FootageSourceGroup GetGroup()
        {
            IFootageSource? footageSource = null;
            if (VideoReader != null)
            {
                footageSource = new MediaFoundationFootageSource(VideoReader, AudioReader);
            }
            else if (AudioReader != null)
            {
                footageSource = new MediaFoundationAudioFootageSource(AudioReader);
            }


            if (footageSource != null)
            {
                return new FootageSourceGroup([footageSource]);
            }
            else
            {
                // == Loadがfalseの時に呼んだ
                throw new InvalidOperationException();
            }
        }
    }

    class MediaFoundationFootageSource : IFootageSource
    {
        public string SourceId => "0"; // TODO

        public double FrameRate { get; }

        public int Width { get; }

        public int Height { get; }

        public double Duration { get; }

        public SourceType SourceType { get; }

        VideoSourceReaderBase VideoReader { get; }

        AudioSourceReader? AudioReader { get; }

        public MediaFoundationFootageSource(VideoSourceReaderBase reader, AudioSourceReader? audio)
        {
            VideoReader = reader;
            AudioReader = audio;
            Width = reader.Width;
            Height = reader.Height;
            FrameRate = reader.FrameRate;
            Duration = reader.Duration;
            SourceType = audio != null ? SourceType.VideoAndAudio : SourceType.Video;
        }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            if (toGpu)
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                var result = new NManagedImage(Width, Height, false);
                var data = VideoReader.GetFrame(time);
                var pixelCount = Width * Height;
                ImageConversion.ConvertToBGRA128(data, result.Data, pixelCount);

                ArrayPool<byte>.Shared.Return(data);

                // TODO: MediaFoundation側でリサイズ出来るかどうかの調査
                if (downSamplingRate != 1.0)
                {
                    var resizedResult = new NManagedImage((int)(Width / downSamplingRate), (int)(Height / downSamplingRate));
                    var renderer = new CpuRenderer2D(resizedResult);
                    renderer.Draw(result, 1.0F, Matrix3x3.CreateScale((float)(1.0 / downSamplingRate), (float)(1.0 / downSamplingRate)), ImageInterpolationQuality.Level2, BlendMode.Replace, null);
                    result.Dispose();
                    result = resizedResult;
                }

                return result;
            }
        }

        public float[] ReadAudio(double time, double length)
        {
            if (AudioReader != null)
            {
                return AudioReader.Read(time, length);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    class MediaFoundationAudioFootageSource : IFootageSource
    {
        public string SourceId => "0"; // TODO

        public double FrameRate => throw new NotImplementedException();

        public int Width => throw new NotImplementedException();

        public int Height => throw new NotImplementedException();

        public double Duration => Reader.Duration;

        public SourceType SourceType => SourceType.Audio;

        AudioSourceReader Reader { get; }

        public MediaFoundationAudioFootageSource(AudioSourceReader reader)
        {
            Reader = reader;
        }

        public float[] ReadAudio(double time, double length)
        {
            return Reader.Read(time, length);
        }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            throw new NotImplementedException();
        }
    }
}