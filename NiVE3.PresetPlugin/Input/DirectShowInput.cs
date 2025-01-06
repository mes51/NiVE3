using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ComputeSharp;
using ComputeSharp.Resources;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Audio;
using NiVE3.PresetPlugin.Internal.DirectShow;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.View;
using NiVE3.PresetPlugin.Internal.ViewModel;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(DirectShowInput), "DirectShowInput", "", "mes51", ID, "*.avi", true, IsSupportLoadToGpu = true)]
    public sealed class DirectShowInput : IInput
    {
        const string ID = "BE18C71D-A752-4814-807A-2DD97D7656C3";

        public string FilePath { get; private set; } = "";

        DirectShowVideoReader? VideoReader { get; set; }

        DirectShowAudioReader? AudioReader { get; set; }

        VideoAlphaType VideoAlphaType { get; set; } = VideoAlphaType.Ignore;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public FootageSourceGroup GetGroup()
        {
            if (VideoReader?.IsLoaded ?? false)
            {
                return new FootageSourceGroup([new DirectShowVideoFootageSource(VideoReader, (AudioReader?.IsLoaded ?? false) ? AudioReader : null, VideoAlphaType, AcceleratorObject)]);
            }
            else if (AudioReader?.IsLoaded ?? false)
            {
                return new FootageSourceGroup([new DirectShowAudioFootageSource(AudioReader)]);
            }
            else
            {
                return FootageSourceGroup.Empty;
            }
        }

        public bool Load(string filePath)
        {
            if (Path.GetExtension(filePath) == ".wav")
            {
                AudioReader = new DirectShowAudioReader(filePath, true, false);
                if (!AudioReader.IsLoaded)
                {
                    AudioReader.Dispose();
                    AudioReader = new DirectShowAudioReader(filePath, true, true);
                }
            }
            else
            {
                VideoReader = new DirectShowVideoReader(filePath);
                AudioReader = new DirectShowAudioReader(filePath, false, false);
                if (!AudioReader.IsLoaded)
                {
                    AudioReader.Dispose();
                    AudioReader = new DirectShowAudioReader(filePath, false, true);
                }
            }
            FilePath = filePath;

            return (VideoReader?.IsLoaded ?? false) || (AudioReader?.IsLoaded ?? false);
        }

        public object? SaveSetting()
        {
            return new DirectShowInputData
            {
                VideoAlphaType = VideoAlphaType
            };
        }

        public bool LoadSetting(object? data)
        {
            if (data is IDictionary<string, object> dictionary && dictionary.TryGetValue(nameof(DirectShowInputData.VideoAlphaType), out int? videoAlphaType))
            {
                VideoAlphaType = (VideoAlphaType)videoAlphaType.Value;
            }
            else if (data is DirectShowInputData inputData)
            {
                VideoAlphaType = inputData.VideoAlphaType;
            }

            return true;
        }

        public FrameworkElement? GetLoadSetting(Int32Size? compositionSize)
        {
            if (VideoReader?.PossibilityArgb ?? false)
            {
                return new DirectShowInputSettingView
                {
                    DataContext = new DirectShowInputSettingViewModel { VideoAlphaType = VideoAlphaType.Straight }
                };
            }
            else
            {
                return null;
            }
        }

        public bool ApplySetting(object? setting)
        {
            if (setting is DirectShowInputSettingViewModel viewModel)
            {
                VideoAlphaType = viewModel.VideoAlphaType;
            }
            return true;
        }

        public void Dispose()
        {
            VideoReader?.Dispose();
            AudioReader?.Dispose();
        }
    }

    file class DirectShowVideoFootageSource : IFootageSource
    {
        const float ByteToFloat = 0.00392156862745098F;

        public string SourceId => "video";

        public string? Name => null;

        public double FrameRate => VideoReader.FrameRate;

        public int Width => VideoReader.Width;

        public int Height => VideoReader.Height;

        public Time Duration => (Time)VideoReader.Duration;

        public SourceType SourceType => SourceType.Video | (AudioReader != null ? SourceType.Audio : SourceType.None);

        DirectShowVideoReader VideoReader { get; }

        DirectShowAudioReader? AudioReader { get; }

        VideoAlphaType VideoAlphaType { get; }

        IAcceleratorObject? AcceleratorObject { get; }

        public DirectShowVideoFootageSource(DirectShowVideoReader videoReader, DirectShowAudioReader? audioReader,  VideoAlphaType videoAlphaType, IAcceleratorObject? acceleratorObject)
        {
            VideoReader = videoReader;
            AudioReader = audioReader;
            VideoAlphaType = videoAlphaType;
            AcceleratorObject = acceleratorObject;
        }

        public float[] ReadAudio(Time time, Time length)
        {
            if (AudioReader == null)
            {
                return [];
            }

            var data = AudioReader.GetAudio((double)time, (double)length);
            return AudioConverter.ConvertToSpecificFormat(data, AudioReader.SamplingRate, AudioReader.Channel, AudioReader.BitPerSample);
        }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            var ppb = VideoReader.BitsPerPixel / 8;
            var videoAlphaType = VideoReader.PossibilityArgb ? VideoAlphaType : VideoAlphaType.Straight;
            if (toGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, VideoReader.GetImage((double)time), Width, Height, videoAlphaType, ppb, downSamplingRate);
            }
            else
            {
                return ProcessCpu(VideoReader.GetImage((double)time), Width, Height, videoAlphaType, ppb, downSamplingRate);
            }
        }

        static NManagedImage ProcessCpu(byte[] buffer, int width, int height, VideoAlphaType videoAlphaType, int bytesPerPixel, double downSamplingRate)
        {
            var result = new NManagedImage(width, height);

            var srcStride = (int)Math.Ceiling(width * bytesPerPixel / 4.0) * 4;
            var imageData = result.Data;
            if (buffer.Length < srcStride * height)
            {
                return result;
            }

            var vectorAlignedBufferLineLength = (srcStride / Vector<byte>.Count) * Vector<byte>.Count;
            Parallel.For(0, height / 2, y =>
            {
                var topBufferSpan = buffer.AsSpan(y * srcStride, srcStride);
                var bottomBufferSpan = buffer.AsSpan((height - y - 1) * srcStride, srcStride);

                var vectorTopBufferSpan = MemoryMarshal.Cast<byte, Vector<byte>>(topBufferSpan[..vectorAlignedBufferLineLength]);
                var vectorBottomBufferSpan = MemoryMarshal.Cast<byte, Vector<byte>>(bottomBufferSpan[..vectorAlignedBufferLineLength]);
                for (var i = 0; i < vectorTopBufferSpan.Length; i++)
                {
                    (vectorTopBufferSpan[i], vectorBottomBufferSpan[i]) = (vectorBottomBufferSpan[i], vectorTopBufferSpan[i]);
                }
                for (var i = vectorAlignedBufferLineLength; i < topBufferSpan.Length; i++)
                {
                    (topBufferSpan[i], bottomBufferSpan[i]) = (bottomBufferSpan[i], topBufferSpan[i]);
                }
            });

            if (bytesPerPixel == 4)
            {
                switch (videoAlphaType)
                {
                    case VideoAlphaType.PreMultiply:
                        Parallel.For(0, height, y =>
                        {
                            var bufferSpan = buffer.AsSpan(y * srcStride, srcStride);
                            var intBufferSpan = MemoryMarshal.Cast<byte, int>(bufferSpan);
                            var imageDataSpan = imageData.AsSpan(y * width, width);
                            for (int x = 0, bi = 3; x < imageDataSpan.Length; x++, bi += 4)
                            {
                                if (bufferSpan[bi] > 0)
                                {
                                    var ci = Sse2.ConvertScalarToVector128Int32(intBufferSpan[x]).AsByte();
                                    var cv = Sse2.UnpackLow(Sse2.UnpackLow(ci, Vector128<byte>.Zero), Vector128<byte>.Zero).AsInt32();
                                    var color = (Sse2.ConvertToVector128Single(cv) * ByteToFloat).AsVector4();
                                    var a = color.W;
                                    color /= a;
                                    color.W = a;
                                    imageDataSpan[x] = color;
                                }
                                else
                                {
                                    imageDataSpan[x] = Const.EmptyPixel;
                                }
                            }
                        });
                        break;
                    case VideoAlphaType.Ignore:
                        Parallel.For(0, height, y =>
                        {
                            var bufferSpan = buffer.AsSpan(y * srcStride, srcStride);
                            var imageDataSpan = imageData.AsSpan(y * width, width);
                            for (int x = 0, bi = 0; x < imageDataSpan.Length; x++, bi += 4)
                            {
                                imageDataSpan[x] = new Vector4(bufferSpan[bi], bufferSpan[bi + 1], bufferSpan[bi + 2], 255.0F) * ByteToFloat;
                            }
                        });
                        break;
                    default:
                        ImageConversion.ConvertToBGRA128(buffer, imageData, result.DataLength);
                        break;
                }
            }
            else
            {
                Parallel.For(0, height, y =>
                {
                    var bufferSpan = buffer.AsSpan(y * srcStride, srcStride);
                    var imageDataSpan = imageData.AsSpan(y * width, width);
                    for (int x = 0, bi = 0; x < imageDataSpan.Length; x++, bi += 3)
                    {
                        imageDataSpan[x] = new Vector4(bufferSpan[bi], bufferSpan[bi + 1], bufferSpan[bi + 2], 255.0F) * ByteToFloat;
                    }
                });
            }

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

        static NGPUImage ProcessGpu(GraphicsDevice device, byte[] buffer, int width, int height, VideoAlphaType videoAlphaType, int bytesPerPixel, double downSamplingRate)
        {
            var result = new NGPUImage(width, height, device);
            var srcStride = (int)Math.Ceiling(width * bytesPerPixel / 4.0) * 4;
            if (buffer.Length < srcStride * height)
            {
                return result;
            }

            using var sourceBuffer = device.AllocateReadOnlyBuffer<int>(srcStride * height);
            sourceBuffer.CopyFrom(MemoryMarshal.Cast<byte, int>(buffer));
            using (var context = device.CreateComputeContext())
            {
                if (bytesPerPixel == 4)
                {
                    switch (videoAlphaType)
                    {
                        case VideoAlphaType.PreMultiply:
                            context.For(width, height, new DirectShowInputFlipPreMultiplyAlpha(sourceBuffer, result.Data, width, height));
                            break;
                        case VideoAlphaType.Ignore:
                            context.For(width, height, new DirectShowInputFlipIgnoreAlpha(sourceBuffer, result.Data, width, height));
                            break;
                        default:
                            context.For(width, height, new DirectShowInputFlipStraightAlpha(sourceBuffer, result.Data, width, height));
                            break;
                    }
                }
                else
                {
                    context.For(width, height, new DirectShowInputFlipAndConverFrom24Bpc(sourceBuffer, result.Data, width, height, srcStride / 4));
                }
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

    file class DirectShowAudioFootageSource : IFootageSource
    {
        public string SourceId => "audio";

        public string? Name => null;

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public Time Duration => (Time)AudioReader.Duration;

        public SourceType SourceType => SourceType.Audio;

        DirectShowAudioReader AudioReader { get; }

        public DirectShowAudioFootageSource(DirectShowAudioReader audioReader)
        {
            AudioReader = audioReader;
        }

        public float[] ReadAudio(Time time, Time length)
        {
            var data = AudioReader.GetAudio((double)time, (double)length);
            return AudioConverter.ConvertToSpecificFormat(data, AudioReader.SamplingRate, AudioReader.Channel, AudioReader.BitPerSample);
        }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            throw new NotImplementedException();
        }
    }

    enum VideoAlphaType
    {
        Straight,
        PreMultiply,
        Ignore
    }

    file class DirectShowInputData
    {
        public VideoAlphaType VideoAlphaType { get; set; }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DirectShowInputFlipAndConverFrom24Bpc(ReadOnlyBuffer<int> src, ReadWriteBuffer<Float4> dst, int width, int height, int srcStride) : IComputeShader
    {
        public void Execute()
        {
            var srcPos = ThreadIds.Y * srcStride + (ThreadIds.X * 3 / 4);
            var dstPos = (height - ThreadIds.Y - 1) * width + ThreadIds.X;

            switch (ThreadIds.X % 4)
            {
                case 1:
                    {
                        var srcPixel1 = src[srcPos];
                        var srcPixel2 = src[srcPos + 1];
                        dst[dstPos] = new Float4(
                            (srcPixel1 >> 24) & 0xFF,
                            srcPixel2 & 0xFF,
                            (srcPixel2 >> 8) & 0xFF,
                            255.0F
                        ) / 255.0F;
                    }
                    break;
                case 2:
                    {
                        var srcPixel1 = src[srcPos];
                        var srcPixel2 = src[srcPos + 1];
                        dst[dstPos] = new Float4(
                            (srcPixel1 >> 16) & 0xFF,
                            (srcPixel1 >> 24) & 0xFF,
                            srcPixel2 & 0xFF,
                            255.0F
                        ) / 255.0F;
                    }
                    break;
                case 3:
                    {
                        var srcPixel1 = src[srcPos];
                        dst[dstPos] = new Float4(
                            (srcPixel1 >> 8) & 0xFF,
                            (srcPixel1 >> 16) & 0xFF,
                            (srcPixel1 >> 24) & 0xFF,
                            255.0F
                        ) / 255.0F;
                    }
                    break;
                default:
                    {
                        var srcPixel = src[srcPos];
                        dst[dstPos] = new Float4(
                            srcPixel & 0xFF,
                            (srcPixel >> 8) & 0xFF,
                            (srcPixel >> 16) & 0xFF,
                            255.0F
                        ) / 255.0F;
                    }
                    break;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DirectShowInputFlipIgnoreAlpha(ReadOnlyBuffer<int> src, ReadWriteBuffer<Float4> dst, int width, int height) : IComputeShader
    {
        public void Execute()
        {
            var srcPos = ThreadIds.Y * width + ThreadIds.X;
            var dstPos = (height - ThreadIds.Y - 1) * width + ThreadIds.X;

            var srcPixel = src[srcPos];
            dst[dstPos] = new Float4(
                srcPixel & 0xFF,
                (srcPixel >> 8) & 0xFF,
                (srcPixel >> 16) & 0xFF,
                255.0F
            ) / 255.0F;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DirectShowInputFlipPreMultiplyAlpha(ReadOnlyBuffer<int> src, ReadWriteBuffer<Float4> dst, int width, int height) : IComputeShader
    {
        public void Execute()
        {
            var srcPos = ThreadIds.Y * width + ThreadIds.X;
            var dstPos = (height - ThreadIds.Y - 1) * width + ThreadIds.X;

            var srcPixel = src[srcPos];
            var dstColor = new Float4(
                srcPixel & 0xFF,
                (srcPixel >> 8) & 0xFF,
                (srcPixel >> 16) & 0xFF,
                (srcPixel >> 24) & 0xFF
            ) / 255.0F;
            if (dstColor.W > 0.0F)
            {
                dstColor.XYZ /= dstColor.W;
            }
            else
            {
                dstColor = Const.EmptyPixelFloat4;
            }
            dst[dstPos] = dstColor;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DirectShowInputFlipStraightAlpha(ReadOnlyBuffer<int> src, ReadWriteBuffer<Float4> dst, int width, int height) : IComputeShader
    {
        public void Execute()
        {
            var srcPos = ThreadIds.Y * width + ThreadIds.X;
            var dstPos = (height - ThreadIds.Y - 1) * width + ThreadIds.X;

            var srcPixel = src[srcPos];
            var dstColor = new Float4(
                srcPixel & 0xFF,
                (srcPixel >> 8) & 0xFF,
                (srcPixel >> 16) & 0xFF,
                (srcPixel >> 24) & 0xFF
            ) / 255.0F;
            if (dstColor.W <= 0.0F)
            {
                dstColor = Const.EmptyPixelFloat4;
            }
            dst[dstPos] = dstColor;
        }
    }
}
