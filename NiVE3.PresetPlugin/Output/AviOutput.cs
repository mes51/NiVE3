using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
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
    [OutputMetadata(typeof(AviOutput), LanguageResourceDictionary.AviOutput_Name, "mes51", LanguageResourceDictionary.AviOutput_Description, ID, ".avi", SourceType.VideoAndAudio, true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class AviOutput : IOutput
    {
        const string ID = "447492E0-E815-49DC-9D65-355CC3866285";

        const int BaseAudioSamplingRate = 48000;

        CompressSetting? Setting { get; set; }

        AviWriter? Writer { get; set; }

        IAviVideoStream? VideoStream { get; set; }

        IAviAudioStream? AudioStream { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public FrameworkElement? GetOutputSetting(double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            var viewModel = new AviOutputSettingViewModel(size?.Width ?? 0, size?.Height ?? 0, outputSources)
            {
                UseKeyFrameRate = Setting?.UseKeyFrameRate ?? false,
                KeyFrameRate = Setting?.KeyFrameRate ?? 0,
                Quality = Setting?.Quality ?? 100,
                Codec = string.IsNullOrEmpty(Setting?.Codec) ? new FourCC(0) : new FourCC(Setting.Codec),
                OutputChannel = (OutputChannel)(Setting?.OutputChannel ?? 0),
                CodecState = Setting?.State,
                AudioSamplingRate = Setting?.AudioSamplingRate ?? 0,
                AudioBitsPerSample = Setting?.AudioBitsPerSample ?? 0
            };
            return new AviOutputSettingView { DataContext = viewModel };
        }

        public object? SaveData()
        {
            return Setting;
        }

        public bool LoadData(object? state)
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
                    State = codecState as byte[],
                    AudioSamplingRate = samplingRate,
                    AudioBitsPerSample = bitsPerSample
                };
            }
            return false;
        }

        public bool ApplyLoadSetting(object? setting)
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
                Codec = viewModel.Codec.ToString(),
                OutputChannel = (int)viewModel.OutputChannel,
                State = viewModel.CodecState,
                AudioSamplingRate = viewModel.AudioSamplingRate,
                AudioBitsPerSample = viewModel.AudioBitsPerSample
            };
            return false;
        }

        public void BeginOutput(string filePath, double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            if (Writer != null)
            {
                throw new InvalidOperationException();
            }

            Setting ??= new CompressSetting();

            File.Open(filePath, FileMode.Create).Close();

            Writer = new AviWriter(filePath)
            {
                FramesPerSecond = (decimal)frameRate,
                EmitIndex1 = true
            };

            if (outputSources.HasFlag(SourceType.Video) && size.HasValue)
            {
                var bpc = ((OutputChannel)Setting.OutputChannel).ToBitsPerPixel();

                IVideoEncoder encoder;
                if (!string.IsNullOrEmpty(Setting.Codec))
                {
                    encoder = new CompressedVideoEncoder(size.Value.Width, size.Value.Height, bpc, Setting.Codec, (int)Math.Ceiling(duration * frameRate), (int)frameRate)
                    {
                        Quality = Setting.Quality,
                        KeyFrameRate = Setting.UseKeyFrameRate ? Setting.KeyFrameRate : 0,
                    };
                    if (Setting.State != null)
                    {
                        ((CompressedVideoEncoder)encoder).SetState(Setting.State);
                    }
                }
                else
                {
                    encoder = bpc switch
                    {
                        BitsPerPixel.Bpp32 => new UncompressedRgbaVideoEncoder(size.Value.Width, size.Value.Height),
                        BitsPerPixel.Bpp8 => new UncompressedAlphaVideoEncoder(size.Value.Width, size.Value.Height),
                        _ => new UncompressedVideoEncoder(size.Value.Width, size.Value.Height)
                    };
                }
                VideoStream = Writer.AddEncodingVideoStream(encoder, true, size.Value.Width, size.Value.Height);
            }

            if (outputSources.HasFlag(SourceType.Audio))
            {
                AudioStream = Writer.AddAudioStream(2, Setting.AudioSamplingRate, Setting.AudioBitsPerSample);
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
        }

        public void BeginPass(int pass) { }

        public void EndPass() { }

        public int GetPass()
        {
            return 1;
        }

        public void ProcessFrame(int pass, double time, NImage image, bool useGpu)
        {
            if (VideoStream == null)
            {
                throw new InvalidOperationException();
            }

            var data = ArrayPool<byte>.Shared.Rent(image.DataLength);
            if (image is NGPUImage gpuImage)
            {
                using var managedImage = gpuImage.CopyToCpu();
                ImageConversion.ConvertToBGR32(managedImage.GetDataSpan(), data, managedImage.DataLength);
            }
            else if (image is NManagedImage managedImage)
            {
                ImageConversion.ConvertToBGR32(managedImage.GetDataSpan(), data, managedImage.DataLength);
            }

            VideoStream.WriteFrame(true, data.AsSpan(0, image.DataLength * 4));
        }

        public void ProcessAudio(float[] audio)
        {
            if (AudioStream == null)
            {
                throw new InvalidOperationException();
            }

            if (BaseAudioSamplingRate != AudioStream.SamplesPerSecond)
            {
                new WdlResamplingSampleProvider(null, 0);
                var resampler = new WdlResampler();
                resampler.SetMode(true, 2, true);
                resampler.SetFilterParms();
                resampler.SetFeedMode(false);
                resampler.SetRates(BaseAudioSamplingRate, AudioStream.SamplesPerSecond);

                var resampledAudio = new float[(int)(audio.Length / 2 / (double)BaseAudioSamplingRate * AudioStream.SamplesPerSecond) * 2];
                var needed = resampler.ResamplePrepare(resampledAudio.Length / 2, 2, out var buffer, out var offset);
                audio.AsSpan(0, Math.Min(audio.Length, needed * 2)).CopyTo(buffer.AsSpan(offset));
                resampler.ResampleOut(resampledAudio, 0, needed, resampledAudio.Length / 2, 2);
                audio = resampledAudio;
            }

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

        public void Dispose() { }
    }

    class CompressSetting
    {
        public bool UseKeyFrameRate { get; set; }

        public int KeyFrameRate { get; set; }

        public int Quality { get; set; } = 100;

        public string Codec { get; set; } = "";

        public int OutputChannel { get; set; }

        public byte[]? State { get; set; }

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
}
