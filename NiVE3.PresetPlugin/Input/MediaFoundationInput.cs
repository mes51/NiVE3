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
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Internal.MediaFoundation;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(MediaFoundationInput), "MediaFoundationInput", "", "mes51", ID, "*.avi,*.mp4", true)]
    public class MediaFoundationInput : IInput
    {
        readonly Vector128<float> ByteToFloat128 = Vector128.Create(0.00392156862745098F);

        const string ID = "3BB12986-32DF-4C41-8D36-46C5E402C6AC";

        VideoSourceReaderBase? Reader { get; set; }

        public string FilePath { get; private set; } = "";

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose()
        {
            Reader?.Dispose();
        }

        public bool Load(string filePath)
        {
            FilePath = filePath;

            Reader = new AcceleratedVideoSourceReader(filePath);
            if (!Reader.Succeeded)
            {
                Reader = new SoftwareVideoSourceReader(filePath);
            }

            if (Reader.Succeeded)
            {
                // NOTE: 読み込み時にサイズが変わる可能性があるため1フレームだけ読み込む
                Reader.GetFrame(0.0);
                return true;
            }
            else
            {
                return false;
            }
        }

        public FootageSourceGroup GetGroup()
        {
            if (Reader != null)
            {
                return new FootageSourceGroup(new IFootageSource[] { new MediaFoundationFootageSource(Reader) });
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
        readonly Vector128<float> ByteToFloat128 = Vector128.Create(0.00392156862745098F);

        public string SourceId => "0"; // TODO

        public double FrameRate { get; }

        public int Width { get; }

        public int Height { get; }

        public double Duration { get; }

        public SourceType SourceType { get; }

        VideoSourceReaderBase Reader { get; set; }

        public MediaFoundationFootageSource(VideoSourceReaderBase reader)
        {
            Reader = reader;
            Width = reader.Width;
            Height = reader.Height;
            FrameRate = reader.FrameRate;
            Duration = reader.Duration;
            SourceType = SourceType.Video;
        }

        public NImage Read(double time, bool toGpu)
        {
            if (toGpu)
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                var result = new NManagedImage(Width, Height, false);
                var data = Reader.GetFrame(time);
                var pixelCount = Width * Height;
                ImageConversion.ConvertToBGRA128(data, result.Data, pixelCount);

                ArrayPool<byte>.Shared.Return(data);

                return result;
            }
        }
    }
}