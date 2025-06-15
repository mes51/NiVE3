using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.ComputeShader.Input;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.MediaFoundation;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(MediaFoundationInput), "MediaFoundationInput", "", "mes51", ID, "*.avi,*.mp4,*.m4a,*.wav,*.mp3,*.wma,*.aac", IsSupportLoadToGpu = true)]
    public class MediaFoundationInput : IInput
    {
        const string ID = "3BB12986-32DF-4C41-8D36-46C5E402C6AC";

        VideoSourceReaderBase? VideoReader { get; set; }

        AudioSourceReader? AudioReader { get; set; }

        public string FilePath { get; private set; } = "";

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public void Dispose()
        {
            VideoReader?.Dispose();
        }

        public bool Load(string filePath)
        {
            FilePath = filePath;

            // NOTE: GPUを使用するとデコード結果が壊れるのでCPUを使用する
            // TODO: GPUでデコードしても壊れないようにする
            //try
            //{
            //    VideoReader = new AcceleratedVideoSourceReader(filePath);
            //}
            //catch { }
            if (!(VideoReader?.Succeeded ?? false))
            {
                try
                {
                    VideoReader?.Dispose();
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
                    VideoReader.GetFrame(0.0, []);
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
                footageSource = new MediaFoundationFootageSource(VideoReader, AudioReader, AcceleratorObject);
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
                return FootageSourceGroup.Empty;
            }
        }
    }

    class MediaFoundationFootageSource : IFootageSource
    {
        public string SourceId => "0"; // TODO

        public string? Name => null;

        public double FrameRate { get; }

        public int Width { get; }

        public int Height { get; }

        public Time Duration { get; }

        public SourceType SourceType { get; }

        VideoSourceReaderBase VideoReader { get; }

        AudioSourceReader? AudioReader { get; }

        IAcceleratorObject? AcceleratorObject { get; }

        public MediaFoundationFootageSource(VideoSourceReaderBase reader, AudioSourceReader? audio, IAcceleratorObject? acceleratorObject)
        {
            VideoReader = reader;
            AudioReader = audio;
            Width = reader.Width;
            Height = reader.Height;
            FrameRate = reader.FrameRate;
            Duration = (Time)reader.Duration;
            SourceType = audio != null ? SourceType.VideoAndAudio : SourceType.Video;
            AcceleratorObject = acceleratorObject;
        }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            // TODO: TerraFXへの移行が出来るかとComputeSharpに直接渡せるかの調査
            // TODO: MediaFoundation側でリサイズ出来るかどうかの調査

            if (toGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, VideoReader, time, Width, Height, downSamplingRate);
            }
            else
            {
                return ProcessCpu(VideoReader, time, Width, Height, downSamplingRate);
            }
        }

        public float[] ReadAudio(Time time, Time length)
        {
            if (AudioReader != null)
            {
                return AudioReader.Read((double)time, (double)length);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        static NManagedImage ProcessCpu(VideoSourceReaderBase videoReader, in Time time, int width, int height, double downSamplingRate)
        {
            var result = new NManagedImage(width, height, false);
            var bufferLength = width * height * 4;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
            if (!videoReader.GetFrame((double)time, buffer.AsSpan(0, bufferLength)))
            {
                ArrayPool<byte>.Shared.Return(buffer);
                return result;
            }

            ImageConversion.ConvertToBGRA128(buffer.AsSpan(0, bufferLength), result.Data, result.DataLength);

            ArrayPool<byte>.Shared.Return(buffer);

            if (downSamplingRate != 1.0)
            {
                var resizedResult = new NManagedImage((int)(width / downSamplingRate), (int)(height / downSamplingRate));
                var renderer = new CPURenderer2D(resizedResult);
                renderer.DrawSingleImage(new Int32Point(), result, 1.0F, Matrix3x3.CreateScale((float)(1.0 / downSamplingRate), (float)(1.0 / downSamplingRate)), ImageInterpolationQuality.Level2, BlendMode.Replace, null);
                result.Dispose();
                result = resizedResult;
            }

            return result;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, VideoSourceReaderBase videoReader, in Time time, int width, int height, double downSamplingRate)
        {
            var result = new NGPUImage(width, height, device);
            using var uploadBuffer = device.AllocateUploadBuffer<int>(result.DataLength);
            if (!videoReader.GetFrame((double)time, MemoryMarshal.Cast<int, byte>(uploadBuffer.Span)))
            {
                return result;
            }

            using var srcBuffer = device.AllocateReadOnlyBuffer<int>(uploadBuffer.Length);
            srcBuffer.CopyFrom(uploadBuffer);
            using (var context = device.CreateComputeContext())
            {
                context.For(width, height, new ConvertImageToBGRA128(srcBuffer, result.Data, width));
            }

            if (downSamplingRate != 1.0)
            {
                var resizedResult = new NGPUImage((int)(width / downSamplingRate), (int)(height / downSamplingRate), device);
                var renderer = new GPURenderer2D(resizedResult, device);
                renderer.DrawSingleImage(new Int32Point(), result, 1.0F, Matrix3x3.CreateScale((float)(1.0 / downSamplingRate), (float)(1.0 / downSamplingRate)), ImageInterpolationQuality.Level2, BlendMode.Replace, null);
                result.Dispose();
                result = resizedResult;
            }

            return result;
        }
    }

    class MediaFoundationAudioFootageSource : IFootageSource
    {
        public string SourceId => "0"; // TODO

        public string? Name => null;

        public double FrameRate => throw new NotImplementedException();

        public int Width => throw new NotImplementedException();

        public int Height => throw new NotImplementedException();

        public Time Duration => (Time)Reader.Duration;

        public SourceType SourceType => SourceType.Audio;

        AudioSourceReader Reader { get; }

        public MediaFoundationAudioFootageSource(AudioSourceReader reader)
        {
            Reader = reader;
        }

        public float[] ReadAudio(Time time, Time length)
        {
            return Reader.Read((double)time, (double)length);
        }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            throw new NotImplementedException();
        }
    }
}