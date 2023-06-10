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
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Internal.MediaFoundation;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(MediaFoundationInput), "MediaFoundationInput", "mes51", ID, "*.avi,*.mp4", true)]
    public class MediaFoundationInput : IInput
    {
        readonly Vector128<float> ByteToFloat128 = Vector128.Create(0.00392156862745098F);

        const string ID = "3BB12986-32DF-4C41-8D36-46C5E402C6AC";

        public string FilePath { get; private set; } = "";

        public double FrameRate { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public double Duration { get; private set; }

        public InputType InputType { get; private set; }

        VideoSourceReaderBase? Reader { get; set; }

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

                Width = Reader.Width;
                Height = Reader.Height;
                FrameRate = Reader.FrameRate;
                Duration = Reader.Duration;
                InputType = InputType.Video;
                return true;
            }
            else
            {
                return false;
            }
        }

        public NImage Read(double time, bool toGpu)
        {
            if (Reader == null)
            {
                throw new InvalidOperationException();
            }

            if (toGpu)
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                var pixelCount = Width * Height;
                var data = Reader.GetFrame(time);
                var dataSpan = MemoryMarshal.Cast<byte, int>(data.AsSpan(0, pixelCount * 4));

                var result = new NManagedImage(Width, Height, false);
                var imageData = MemoryMarshal.Cast<float, Vector128<float>>(result.Data.AsSpan(0, result.DataLength));
                for (var i = 0; i < pixelCount; i++)
                {
                    var c = Sse2.ConvertScalarToVector128Int32(dataSpan[i]).AsByte();
                    var cv = Sse2.UnpackLow(Sse2.UnpackLow(c, Vector128<byte>.Zero), Vector128<byte>.Zero).AsInt32();

                    imageData[i] = Sse.Multiply(Sse2.ConvertToVector128Single(cv), ByteToFloat128);
                }

                ArrayPool<byte>.Shared.Return(data);

                return result;
            }
        }
    }
}