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
using NiVE3.PresetPlugin.Internal.View;
using NiVE3.PresetPlugin.Internal.ViewModel;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using Vanara.Extensions;
using static Vanara.PInvoke.WinMm;

namespace NiVE3.PresetPlugin.Output
{
    [Export(typeof(IOutput))]
    [OutputMetadata(typeof(WaveOutput), LanguageResourceDictionary.Output_WaveOutput_Name, "mes51", LanguageResourceDictionary.Output_WaveOutput_Description, ID, "*.wav", SourceType.Audio, true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class WaveOutput : IOutput
    {
        const string ID = "F2375CE2-01EB-4B9C-B778-28222788A6FE";

        const int BaseAudioSamplingRate = 48000;

        string FilePath { get; set; } = "";

        AudioSetting Setting { get; set; } = new AudioSetting();

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public FrameworkElement? GetOutputSetting(string filePath, double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            var viewModel = new WaveOutputSettingViewModel
            {
                SamplingRate = Setting.SamplingRate,
                BitsPerSample = Setting.BitsPerSample
            };
            return new WaveOutputSettingView { DataContext = viewModel };
        }

        public object? SaveData()
        {
            return Setting;
        }

        public bool LoadData(object? state)
        {
            if (state is AudioSetting setting)
            {
                Setting = setting;
                return true;
            }
            else if (state is IDictionary<string, object?> dictionary &&
                dictionary.TryGetValue(nameof(AudioSetting.SamplingRate), out int samplingRate) &&
                dictionary.TryGetValue(nameof(AudioSetting.BitsPerSample), out int bitsPerSample))
            {
                Setting = new AudioSetting
                {
                    SamplingRate = samplingRate,
                    BitsPerSample = bitsPerSample
                };
                return true;
            }

            return false;
        }

        public bool ApplyOutputSetting(object? setting)
        {
            if (setting is not WaveOutputSettingViewModel viewModel)
            {
                return false;
            }

            Setting = new AudioSetting
            {
                SamplingRate = viewModel.SamplingRate,
                BitsPerSample = viewModel.BitsPerSample
            };
            return true;
        }

        public string ProcessOutputFilePath(string baseFilePath)
        {
            return Path.ChangeExtension(baseFilePath, ".wav");
        }

        public void BeginOutput(string filePath, double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            FilePath = filePath;
        }

        public void BeginPass(int pass) { }

        public int GetPassCount()
        {
            return 1;
        }

        public void ProcessAudio(float[] audio)
        {
            if (BaseAudioSamplingRate != Setting.SamplingRate)
            {
                var resampler = new WdlResampler();
                resampler.SetMode(true, 2, true);
                resampler.SetFilterParms();
                resampler.SetFeedMode(false);
                resampler.SetRates(BaseAudioSamplingRate, Setting.SamplingRate);

                var resampledAudio = new float[(int)(audio.Length / 2 / (double)BaseAudioSamplingRate * Setting.SamplingRate) * 2];
                var needed = resampler.ResamplePrepare(resampledAudio.Length / 2, 2, out var buffer, out var offset);
                audio.AsSpan(0, Math.Min(audio.Length, needed * 2)).CopyTo(buffer.AsSpan(offset));
                resampler.ResampleOut(resampledAudio, 0, needed, resampledAudio.Length / 2, 2);
                audio = resampledAudio;
            }

            var header = new WaveHeader(Setting.SamplingRate, Setting.BitsPerSample, audio.Length);
            using var fs = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var writer = new BinaryWriter(fs);

            writer.Write(header.RawRIFF, 0, 4);
            writer.Write(header.TotalSize);
            writer.Write(header.RawWAVE, 0, 4);
            writer.Write(header.Rawfmt, 0, 4);
            writer.Write(header.FmtSize);

            writer.Write((ushort)header.Format.wFormatTag);
            writer.Write(header.Format.nChannels);
            writer.Write(header.Format.nSamplesPerSec);
            writer.Write(header.Format.nAvgBytesPerSec);
            writer.Write(header.Format.nBlockAlign);
            writer.Write(header.Format.wBitsPerSample);

            writer.Write("data".ToCharArray(), 0, 4);
            writer.Write(header.TotalSize - (Marshal.SizeOf<WaveHeader>() - 2));

            writer.Flush();

            switch (Setting.BitsPerSample)
            {
                case 8:
                    {
                        var byteAudio = ArrayPool<byte>.Shared.Rent(audio.Length);
                        for (var i = 0; i < audio.Length; i++)
                        {
                            byteAudio[i] = (byte)(Math.Clamp(audio[i], -1.0F, 1.0F) * sbyte.MaxValue + 128.0F);
                        }
                        fs.Write(byteAudio);
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
                        fs.Write(byteAudio);
                        ArrayPool<short>.Shared.Return(shortAudio);
                    }
                    break;
                default:
                    {
                        var byteAudio = MemoryMarshal.Cast<float, byte>(audio);
                        fs.Write(byteAudio);
                    }
                    break;
            }
        }

        public void ProcessFrame(int pass, double time, NImage image, bool useGpu) { }

        public void EndOutput() { }

        public void EndPass() { }

        public void Dispose() { }
    }

    class AudioSetting
    {
        public int SamplingRate { get; set; } = 48000;

        public int BitsPerSample { get; set; } = 32;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    file readonly struct WaveHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly char[] RawRIFF = "RIFF".ToCharArray();

        public readonly int TotalSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly char[] RawWAVE = "WAVE".ToCharArray();

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly char[] Rawfmt = "fmt ".ToCharArray();

        public readonly int FmtSize = 16;

        public readonly WAVEFORMATEX Format;

        public WaveHeader(int samplingRate, int bitsPerSampling, int sampleLength)
        {
            var format = new WAVEFORMATEX
            {
                wFormatTag = bitsPerSampling == 32 ? WAVE_FORMAT.WAVE_FORMAT_IEEE_FLOAT : WAVE_FORMAT.WAVE_FORMAT_PCM,
                nChannels = 2,
                nSamplesPerSec = (uint)samplingRate,
                nBlockAlign = (ushort)(bitsPerSampling / 8 * 2),
                wBitsPerSample = (ushort)bitsPerSampling,
                cbSize = (ushort)Marshal.SizeOf<WAVEFORMATEX>()
            };
            TotalSize = sampleLength * format.nBlockAlign + Marshal.SizeOf<WaveHeader>() - 2; // 8 - sizeof(format.cbSize);
            Format = format;
        }
    }
}
