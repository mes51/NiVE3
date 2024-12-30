using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ComputeSharp;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Audio;
using NiVE3.PresetPlugin.Internal.Encoder;
using NiVE3.PresetPlugin.Internal.View;
using NiVE3.PresetPlugin.Internal.ViewModel;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;

namespace NiVE3.PresetPlugin.Output
{
    [Export(typeof(IOutput))]
    [OutputMetadata(typeof(AviOutput), LanguageResourceDictionary.Output_AviOutput_Name, "mes51", LanguageResourceDictionary.Output_AviOutput_Description, ID, "*.avi", SourceType.VideoAndAudio, true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary), IsSupportGpu = true)]
    public sealed class AviOutput : IOutput
    {
        const string ID = "447492E0-E815-49DC-9D65-355CC3866285";

        const int BaseAudioSamplingRate = 48000;

        CompressSetting Setting { get; set; } = new CompressSetting();

        AviWriter? Writer { get; set; }

        IAviVideoStream? VideoStream { get; set; }

        IAviAudioStream? AudioStream { get; set; }

        ISourceFormatChangeableVideoEncoder? Encoder { get; set; }

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public FrameworkElement? GetOutputSetting(string filePath, Time startTime, Time duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            var viewModel = new AviOutputSettingViewModel(size?.Width ?? 0, size?.Height ?? 0, outputSources)
            {
                UseKeyFrameRate = Setting.UseKeyFrameRate,
                KeyFrameRate = Setting.KeyFrameRate,
                Quality = Setting.Quality,
                Codec = string.IsNullOrEmpty(Setting.Codec) ? new FourCC(0) : new FourCC(Setting.Codec),
                OutputChannel = (OutputChannel)Setting.OutputChannel,
                CodecState = Setting.State != null ? Convert.FromBase64String(Setting.State) : null,
                AudioSamplingRate = Setting.AudioSamplingRate,
                AudioBitsPerSample = Setting.AudioBitsPerSample
            };
            return new AviOutputSettingView { DataContext = viewModel };
        }

        public object? SaveSetting()
        {
            return Setting;
        }

        public bool LoadSetting(object? state)
        {
            if (state is CompressSetting setting)
            {
                Setting = setting;
                return true;
            }
            else if (state is IDictionary<string, object?> dictionary &&
                dictionary.TryGetValue(nameof(CompressSetting.UseKeyFrameRate), out bool useKeyFrameRate) &&
                dictionary.TryGetValue(nameof(CompressSetting.KeyFrameRate), out int keyFrameRate) &&
                dictionary.TryGetValue(nameof(CompressSetting.Quality), out int quality) &&
                dictionary.TryGetValue(nameof(CompressSetting.Codec), out string? codec) &&
                dictionary.TryGetValue(nameof(CompressSetting.OutputChannel), out int outputChannel) &&
                dictionary.TryGetValue(nameof(CompressSetting.State), out var codecState) && // NOTE: stateがnullである可能性があるため、通常のTryGetValueを使用する
                dictionary.TryGetValue(nameof(CompressSetting.AudioSamplingRate), out int samplingRate) &&
                dictionary.TryGetValue(nameof(CompressSetting.AudioBitsPerSample), out int bitsPerSample))
            {
                Setting = new CompressSetting
                {
                    UseKeyFrameRate = useKeyFrameRate,
                    KeyFrameRate = keyFrameRate,
                    Quality = quality,
                    Codec = codec,
                    OutputChannel = outputChannel,
                    State = codecState as string,
                    AudioSamplingRate = samplingRate,
                    AudioBitsPerSample = bitsPerSample
                };
                return true;
            }
            return false;
        }

        public bool ApplySetting(object? setting)
        {
            if (setting is not AviOutputSettingViewModel viewModel)
            {
                return false;
            }

            Setting = new CompressSetting
            {
                UseKeyFrameRate = viewModel.UseKeyFrameRate,
                KeyFrameRate = viewModel.KeyFrameRate,
                Quality = viewModel.Quality,
                Codec = viewModel.Codec == 0 ? "" : viewModel.Codec.ToString(),
                OutputChannel = (int)viewModel.OutputChannel,
                State = viewModel.CodecState != null ? Convert.ToBase64String(viewModel.CodecState) : null,
                AudioSamplingRate = viewModel.AudioSamplingRate,
                AudioBitsPerSample = viewModel.AudioBitsPerSample
            };
            return true;
        }

        public string ProcessOutputFilePath(string baseFilePath)
        {
            return Path.ChangeExtension(baseFilePath, ".avi");
        }

        public void BeginOutput(string filePath, Time startTime, Time duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            if (Writer != null)
            {
                throw new InvalidOperationException();
            }

            File.Open(filePath, FileMode.Create).Close();

            Writer = new AviWriter(filePath)
            {
                FramesPerSecond = (decimal)frameRate,
                EmitIndex1 = true
            };

            if (outputSources.HasFlag(SourceType.Video) && size.HasValue)
            {
                var bpc = ((OutputChannel)Setting.OutputChannel).ToBitsPerPixel();

                if (!string.IsNullOrEmpty(Setting.Codec))
                {
                    Encoder = new CompressedVideoEncoder(size.Value.Width, size.Value.Height, bpc, Setting.Codec, (int)Math.Ceiling((double)duration * frameRate), (int)frameRate)
                    {
                        Quality = Setting.Quality,
                        KeyFrameRate = Setting.UseKeyFrameRate ? Setting.KeyFrameRate : 0,
                    };
                    if (Setting.State != null)
                    {
                        ((CompressedVideoEncoder)Encoder).SetState(Convert.FromBase64String(Setting.State));
                    }
                }
                else
                {
                    Encoder = bpc switch
                    {
                        BitsPerPixel.Bpp32 => new UncompressedRgbaVideoEncoder(size.Value.Width, size.Value.Height),
                        BitsPerPixel.Bpp8 => new UncompressedAlphaVideoEncoder(size.Value.Width, size.Value.Height),
                        _ => new UncompressedRgbVideoEncoder(size.Value.Width, size.Value.Height)
                    };
                }
                VideoStream = Writer.AddEncodingVideoStream(Encoder, true, size.Value.Width, size.Value.Height);
            }

            if (outputSources.HasFlag(SourceType.Audio))
            {
                if (Setting.AudioBitsPerSample == 32)
                {
                    AudioStream = Writer.AddEncodingAudioStream(new UncompressedFloatAudioEncoder(Setting.AudioSamplingRate));
                }
                else
                {
                    AudioStream = Writer.AddAudioStream(2, Setting.AudioSamplingRate, Setting.AudioBitsPerSample);
                }
            }
        }

        public void EndOutput()
        {
            if (Writer == null)
            {
                throw new InvalidOperationException();
            }

            Writer.Close();

            VideoStream = null;
            AudioStream = null;
            Writer = null;
            Encoder = null;
        }

        public void BeginPass(int pass) { }

        public void EndPass() { }

        public int GetPassCount()
        {
            return 1;
        }

        public void ProcessFrame(int pass, Time time, NImage image, bool useGpu)
        {
            if (VideoStream == null || Encoder == null)
            {
                throw new InvalidOperationException();
            }

            if (useGpu && AcceleratorObject != null && image is NGPUImage gpuImage)
            {
                var device = AcceleratorObject.CurrentDevice;
                var bpc = ((OutputChannel)Setting.OutputChannel).ToBitsPerPixel();
                using var convertedImage = device.AllocateReadWriteBuffer<int>((int)Math.Ceiling((gpuImage.DataLength * 4) / (double)((int)bpc / 8)));
                using var readbackBuffer = device.AllocateReadBackBuffer<int>(convertedImage.Length);

                using (var context = device.CreateComputeContext())
                {
                    switch (bpc)
                    {
                        case BitsPerPixel.Bpp32:
                            context.For(gpuImage.Width, gpuImage.Height, new AviOutputFlipAndQuantizeBitmap(gpuImage.Data, convertedImage, gpuImage.Width, gpuImage.Height));
                            break;
                        case BitsPerPixel.Bpp24:
                            context.For(gpuImage.Width, gpuImage.Height, new AviOutputFlipAndConvertTo24Bpc(gpuImage.Data, convertedImage, gpuImage.Width, gpuImage.Height));
                            break;
                        case BitsPerPixel.Bpp8:
                            context.For(gpuImage.Width, gpuImage.Height, new AviOutputFlipAndConvertTo8Bpc(gpuImage.Data, convertedImage, gpuImage.Width, gpuImage.Height));
                            break;
                    }
                }

                convertedImage.CopyTo(readbackBuffer);
                Encoder.UseFormatConvertedSource = true;
                VideoStream.WriteFrame(true, MemoryMarshal.Cast<int, byte>(readbackBuffer.Span));
            }
            else
            {
                var data = ArrayPool<byte>.Shared.Rent(image.DataLength * 4);
                var managedImage = image.ToManaged();
                if (Setting.OutputChannel == (int)OutputChannel.Rgb)
                {
                    BlendBlack(managedImage);
                }
                ImageConversion.ConvertToBGRA32(managedImage.GetDataSpan(), data, managedImage.DataLength);

                Encoder.UseFormatConvertedSource = false;
                // NOTE: 中で画像反転&bpc変換をマルチスレッドで行えるようにするため、byte[]版の方を使用する
                VideoStream.WriteFrame(true, data, 0, image.DataLength * 4);

                if (managedImage != image)
                {
                    managedImage.Dispose();
                }
            }
        }

        public void ProcessAudio(float[] audio)
        {
            if (AudioStream == null)
            {
                throw new InvalidOperationException();
            }

            audio = AudioConverter.ConvertSamplingRate(audio, BaseAudioSamplingRate, AudioStream.SamplesPerSecond, Const.AudioChannelCount);

            switch (AudioStream.BitsPerSample)
            {
                case 8:
                    {
                        var byteAudio = ArrayPool<byte>.Shared.Rent(audio.Length);
                        for (var i = 0; i < audio.Length; i++)
                        {
                            byteAudio[i] = (byte)(Math.Clamp(audio[i], -1.0F, 1.0F) * sbyte.MaxValue + 128.0F);
                        }
                        AudioStream.WriteBlock(byteAudio);
                        ArrayPool<byte>.Shared.Return(byteAudio);
                    }
                    break;
                case 16:
                    {
                        var shortAudio = ArrayPool<short>.Shared.Rent(audio.Length);
                        for (var i = 0; i < audio.Length; i++)
                        {
                            shortAudio[i] = (short)(Math.Clamp(audio[i], -1.0F, 1.0F) * short.MaxValue);
                        }
                        var byteAudio = MemoryMarshal.Cast<short, byte>(shortAudio.AsSpan(0, audio.Length));
                        AudioStream.WriteBlock(byteAudio);
                        ArrayPool<short>.Shared.Return(shortAudio);
                    }
                    break;
                default:
                    {
                        var byteAudio = MemoryMarshal.Cast<float, byte>(audio);
                        AudioStream.WriteBlock(byteAudio);
                    }
                    break;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        static void BlendBlack(NManagedImage image)
        {
            Parallel.For(0, image.Height, h =>
            {
                var span = image.GetDataSpan().Slice(h * image.Width, image.Width);
                for (var i = 0; i < span.Length; i++)
                {
                    var p = span[i];
                    span[i] = Vector4.Min(Vector4.Max(p * p.W + Vector4.UnitW, Vector4.Zero), Vector4.One);
                }
            });
        }
    }

    class CompressSetting
    {
        public bool UseKeyFrameRate { get; set; }

        public int KeyFrameRate { get; set; }

        public int Quality { get; set; } = 100;

        public string Codec { get; set; } = "";

        public int OutputChannel { get; set; } = (int)Output.OutputChannel.Rgba;

        public string? State { get; set; }

        public int AudioSamplingRate { get; set; } = 48000;

        public int AudioBitsPerSample { get; set; } = 32;
    }

    enum OutputChannel : int
    {
        Rgb = 0,
        Rgba,
        AlphaOnly
    }

    static class OutputChannelExtensions
    {
        public static BitsPerPixel ToBitsPerPixel(this OutputChannel outputChannel)
        {
            return outputChannel switch
            {
                OutputChannel.Rgba => BitsPerPixel.Bpp32,
                OutputChannel.AlphaOnly => BitsPerPixel.Bpp8,
                _ => BitsPerPixel.Bpp24
            };
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct AviOutputFlipAndQuantizeBitmap(ReadWriteBuffer<Float4> src, ReadWriteBuffer<int> dst, int width, int height) : IComputeShader
    {
        public void Execute()
        {
            var srcPos = ThreadIds.Y * width + ThreadIds.X;
            var dstPos = ((height - ThreadIds.Y - 1) * width + ThreadIds.X);
            var pixel = (Int4)Hlsl.Round(Hlsl.Clamp(src[srcPos], 0.0F, 1.0F) * 255.0F);

            dst[dstPos] = (pixel.X & 0xFF) | (pixel.Y & 0xFF) << 8 | (pixel.Z & 0xFF) << 16 | (pixel.W & 0xFF) << 24;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct AviOutputFlipAndConvertTo24Bpc(ReadWriteBuffer<Float4> src, ReadWriteBuffer<int> dst, int width, int height) : IComputeShader
    {
        public void Execute()
        {
            var srcPos = ThreadIds.Y * width + ThreadIds.X;
            var dstBytePos = ((height - ThreadIds.Y - 1) * width + ThreadIds.X) * 3;
            var dstPos = dstBytePos / 4;
            var color = Hlsl.Clamp(src[srcPos], 0.0F, 1.0F);
            color.XYZ *= color.W;
            var pixel = (UInt3)Hlsl.Round(color.XYZ * 255.0F);

            switch (srcPos % 4)
            {
                case 1:
                    Hlsl.InterlockedOr(ref dst[dstPos], (int)((pixel.X & 0xFF) << 24));
                    Hlsl.InterlockedOr(ref dst[dstPos + 1], (int)((pixel.Y & 0xFF) | (pixel.Z & 0xFF) << 8));
                    break;
                case 2:
                    Hlsl.InterlockedOr(ref dst[dstPos], (int)((pixel.X & 0xFF) << 16 | (pixel.Y & 0xFF) << 24));
                    Hlsl.InterlockedOr(ref dst[dstPos + 1], (int)(pixel.Z & 0xFF));
                    break;
                case 3:
                    Hlsl.InterlockedOr(ref dst[dstPos], (int)((pixel.X & 0xFF) << 8 | (pixel.Y & 0xFF) << 16 | (pixel.Z & 0xFF) << 24));
                    break;
                default:
                    Hlsl.InterlockedOr(ref dst[dstPos], (int)((pixel.X & 0xFF) | (pixel.Y & 0xFF) << 8 | (pixel.Z & 0xFF) << 16));
                    break;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct AviOutputFlipAndConvertTo8Bpc(ReadWriteBuffer<Float4> src, ReadWriteBuffer<int> dst, int width, int height) : IComputeShader
    {
        public void Execute()
        {
            var srcPos = ThreadIds.Y * width + ThreadIds.X;
            var dstBytePos = ((height - ThreadIds.Y - 1) * width + ThreadIds.X);
            var dstPos = dstBytePos / 4;
            var color = (uint)(Hlsl.Clamp(src[srcPos], 0.0F, 1.0F).W * 255.0F);
            var pixel = (int)((color & 0xFF) << (8 * (srcPos % 4)));

            Hlsl.InterlockedOr(ref dst[dstPos], pixel);
        }
    }
}
