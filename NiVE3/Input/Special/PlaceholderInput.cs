using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Input.Special
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(PlaceholderInput), nameof(PlaceholderInput), "", "mes51", ID, "", false)]
    [InternalInput]
    [SpecialInput]
    internal class PlaceholderInput : IInput
    {
        const string ID = "B863843A-70A2-42F0-B6D1-2FACDA49DB0B";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public string FilePath { get; set; } = "";

        string SourceId { get; }

        double FrameRate { get; }

        int Width { get; }

        int Height { get; }

        double Duration { get; }

        SourceType SourceType { get; }

        object? InputOption { get; set; }

        public PlaceholderInput(SourceType sourceType, int width, int height, double frameRate, double duration, object? inputOption, string sourceId)
        {
            SourceType = sourceType;
            Width = width;
            Height = height;
            FrameRate = frameRate;
            Duration = duration;
            InputOption = inputOption;
            SourceId = sourceId;
        }

        public FootageSourceGroup GetGroup()
        {
            IFootageSource source;
            if (SourceType.HasFlag(SourceType.Video) || SourceType.HasFlag(SourceType.Image))
            {
                source = new PlaceholderImageFootageSource(FilePath, SourceType, Width, Height, FrameRate, Duration, SourceId);
            }
            else if (SourceType.HasFlag(SourceType.Audio))
            {
                source = new PlaceholderAudioFootageSource(Duration, SourceId);
            }
            else
            {
                source = new PlaceholderOtherFootageSource(SourceId);
            }
            return new FootageSourceGroup(new IFootageSource[] { source});
        }

        public bool Load(string filePath)
        {
            FilePath = filePath;
            return true;
        }

        public object? SaveData()
        {
            return InputOption;
        }

        public bool LoadData(object? data)
        {
            InputOption = data;
            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }
    }

    file class PlaceholderImageFootageSource : IFootageSource
    {
        static readonly Vector4 Black0 = new Vector4(0.0627450980392157F, 0.0627450980392157F, 0.0627450980392157F, 1.0F);

        static readonly Vector4[] Top7BarColors = new Vector4[]
        {
            new Vector4(0.705882352941177F, 0.705882352941177F, 0.705882352941177F, 1.0F),
            new Vector4(0.0627450980392157F, 0.705882352941177F, 0.705882352941177F, 1.0F),
            new Vector4(0.705882352941177F, 0.705882352941177F, 0.0627450980392157F, 1.0F),
            new Vector4(0.0627450980392157F, 0.705882352941177F, 0.0627450980392157F, 1.0F),
            new Vector4(0.705882352941177F, 0.0627450980392157F, 0.705882352941177F, 1.0F),
            new Vector4(0.0627450980392157F, 0.0627450980392157F, 0.705882352941177F, 1.0F),
            new Vector4(0.705882352941177F, 0.0627450980392157F, 0.0627450980392157F, 1.0F),
        };

        static readonly Vector4[] Bottom4BarColors = new Vector4[]
        {
            new Vector4(0.415686274509804F, 0.274509803921569F, 0.0627450980392157F, 1.0F),
            new Vector4(0.92156862745098F, 0.92156862745098F, 0.92156862745098F, 1.0F),
            new Vector4(0.462745098039216F, 0.0627450980392157F, 0.282352941176471F, 1.0F),
            Black0
        };

        static readonly Vector4[] BlackBarColors = new Vector4[]
        {
            new Vector4(0.0274509803921569F, 0.0274509803921569F, 0.0274509803921569F, 1.0F),
            Black0,
            new Vector4(0.0941176470588235F, 0.0941176470588235F, 0.0941176470588235F, 1.0F)
        };

        public string SourceId { get; }

        public double FrameRate { get; }

        public int Width { get; }

        public int Height { get; }

        public double Duration { get; }

        public SourceType SourceType { get; }

        string FileName {  get; }

        public PlaceholderImageFootageSource(string fileName, SourceType sourceType, int width, int height, double frameRate, double duration, string sourceId)
        {
            FileName = fileName;
            SourceType = sourceType;
            Width = width;
            Height = height;
            FrameRate = frameRate;
            Duration = duration;
            SourceId = sourceId;
        }

        public NImage Read(double time, bool toGpu)
        {
            var result = new NManagedImage(Width, Height);

            var bar7Width = Width / 7.0;
            var bar21Width = Width / 21.0;
            var bar5Of28Width = Width / 28.0 * 5.0;
            var height12 = Height / 12.0;

            Parallel.For(0, Height, y =>
            {
                var dataSpan = MemoryMarshal.Cast<float, Vector4>(result.GetDataSpan()).Slice(y * Width);

                if (y <= height12 * 8.0)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var colorIndex = Math.Min((int)(x / bar7Width), Top7BarColors.Length - 1);
                        dataSpan[x] = Top7BarColors[colorIndex];
                    }
                }
                else if (y <= height12 * 9.0)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var colorIndex = Math.Min((int)(x / bar7Width), Top7BarColors.Length - 1);
                        dataSpan[x] = (colorIndex % 2 == 0) ? Top7BarColors[Top7BarColors.Length - colorIndex - 1] : Black0;
                    }
                }
                else
                {
                    for (var x = 0; x < Width; x++)
                    {
                        if (x <= bar5Of28Width * 4.0)
                        {
                            var colorIndex = Math.Min((int)(x / bar5Of28Width), Bottom4BarColors.Length - 1);
                            dataSpan[x] = Bottom4BarColors[colorIndex];
                        }
                        else if ((x - bar5Of28Width * 4) < bar7Width)
                        {
                            var colorIndex = Math.Min((int)((x - bar5Of28Width * 4) / bar21Width), BlackBarColors.Length - 1);
                            dataSpan[x] = BlackBarColors[colorIndex];
                        }
                        else
                        {
                            dataSpan[x] = Black0;
                        }
                    }
                }
            });

            return result;
        }
    }

    file class PlaceholderAudioFootageSource : IFootageSource
    {
        public string SourceId { get; }

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public double Duration { get; }

        public SourceType SourceType => SourceType.Audio;

        public PlaceholderAudioFootageSource(double duration, string sourceId)
        {
            SourceId = sourceId;
            Duration = duration;
        }

        public NImage Read(double time, bool toGpu)
        {
            throw new NotImplementedException();
        }
    }

    file class PlaceholderOtherFootageSource : IFootageSource
    {
        public string SourceId { get; }

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.None;

        public PlaceholderOtherFootageSource(string sourceId)
        {
            SourceId = sourceId;
        }

        public NImage Read(double time, bool toGpu)
        {
            throw new NotImplementedException();
        }
    }
}