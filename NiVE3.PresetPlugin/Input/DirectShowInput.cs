using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Audio;
using NiVE3.PresetPlugin.Internal.DirectShow;
using NiVE3.PresetPlugin.Internal.View;
using NiVE3.PresetPlugin.Internal.ViewModel;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(DirectShowInput), "DirectShowInput", "", "mes51", ID, "*.avi", true)]
    public sealed class DirectShowInput : IInput
    {
        const string ID = "BE18C71D-A752-4814-807A-2DD97D7656C3";

        public string FilePath { get; private set; } = "";

        DirectShowVideoReader? VideoReader { get; set; }

        DirectShowAudioReader? AudioReader { get; set; }

        VideoAlphaType VideoAlphaType { get; set; } = VideoAlphaType.Ignore;

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public FootageSourceGroup GetGroup()
        {
            if (VideoReader?.IsLoaded ?? false)
            {
                return new FootageSourceGroup([new DirectShowVideoFootageSource(VideoReader, AudioReader, VideoAlphaType)]);
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
            VideoReader = new DirectShowVideoReader(filePath);
            AudioReader = new DirectShowAudioReader(filePath, false);
            if (!AudioReader.IsLoaded && Path.GetExtension(filePath) == ".wav")
            {
                AudioReader.Dispose();
                AudioReader = new DirectShowAudioReader(filePath, true);
            }
            FilePath = filePath;
            return VideoReader.IsLoaded || AudioReader.IsLoaded;
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

        int ChannelDataLength { get; }

        int BufferLineLength { get; }

        int VectorAlignedBufferLineLength { get; }

        public DirectShowVideoFootageSource(DirectShowVideoReader videoReader, DirectShowAudioReader? audioReader,  VideoAlphaType videoAlphaType)
        {
            VideoReader = videoReader;
            AudioReader = audioReader;
            VideoAlphaType = videoAlphaType;
            ChannelDataLength = VideoReader.Width * VideoReader.Height;
            BufferLineLength = VideoReader.Width * (VideoReader.PossibilityArgb ? 4 : 3);
            VectorAlignedBufferLineLength = (BufferLineLength / Vector<byte>.Count) * Vector<byte>.Count;
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
            var result = new NManagedImage(Width, Height);

            var buffer = VideoReader.GetImage((double)time);
            var width = Width;
            var height = Height;
            var imageData = result.Data;
            if (buffer.Length / ChannelDataLength < (VideoReader.PossibilityArgb ? 4 : 3))
            {
                return result;
            }

            Parallel.For(0, height / 2, y =>
            {
                var topBufferSpan = buffer.AsSpan(y * BufferLineLength, BufferLineLength);
                var bottomBufferSpan = buffer.AsSpan((height - y) * BufferLineLength, BufferLineLength);

                var vectorTopBufferSpan = MemoryMarshal.Cast<byte, Vector<byte>>(topBufferSpan[..VectorAlignedBufferLineLength]);
                var vectorBottomBufferSpan = MemoryMarshal.Cast<byte, Vector<byte>>(bottomBufferSpan[..VectorAlignedBufferLineLength]);
                for (var i = 0; i < vectorTopBufferSpan.Length; i++)
                {
                    (vectorTopBufferSpan[i], vectorBottomBufferSpan[i]) = (vectorBottomBufferSpan[i], vectorTopBufferSpan[i]);
                }
                for (var i = VectorAlignedBufferLineLength; i < topBufferSpan.Length; i++)
                {
                    (topBufferSpan[i], bottomBufferSpan[i]) = (bottomBufferSpan[i], topBufferSpan[i]);
                }
            });

            if (VideoReader.PossibilityArgb)
            {
                switch (VideoAlphaType)
                {
                    case VideoAlphaType.PreMultiply:
                        Parallel.For(0, height, y =>
                        {
                            var bufferSpan = buffer.AsSpan(y * BufferLineLength, BufferLineLength);
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
                            var bufferSpan = buffer.AsSpan(y * BufferLineLength, BufferLineLength);
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
                    var bufferSpan = buffer.AsSpan(y * width * 3, width * 3);
                    var imageDataSpan = imageData.AsSpan(y * width, width);
                    for (int x = 0, bi = 0; x < imageDataSpan.Length; x++, bi += 3)
                    {
                        imageDataSpan[x] = new Vector4(bufferSpan[bi], bufferSpan[bi + 1], bufferSpan[bi + 2], 255.0F) * ByteToFloat;
                    }
                });
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
}
