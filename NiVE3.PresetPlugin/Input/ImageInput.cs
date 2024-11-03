using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.Drawing;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(ImageInput), "ImageInput", "", "mes51", ID, "*.png,*.jpg,*.jpeg,*.webp,*.gif,*.tiff,*.tif,*.bmp", IsSupportLoadToGpu = false)]
    public sealed class ImageInput : IInput
    {
        const string ID = "F2683121-9952-457C-8917-A18FBD6755F8";

        public string FilePath { get; private set; } = "";

        NManagedImage? LoadedImage { get; set; }

        public FootageSourceGroup GetGroup()
        {
            if (LoadedImage != null)
            {
                return new FootageSourceGroup([new TestPngFootageSource(LoadedImage)]);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public bool Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }
            FilePath = filePath;

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            FormatConvertedBitmap converter;
            try
            {
                var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None);
                if (decoder.Frames.Count < 1)
                {
                    return false;
                }
                converter = new FormatConvertedBitmap(decoder.Frames.First(), PixelFormats.Bgra32, null, 0.0);
            }
            catch
            {
                return false;
            }

            var data = ArrayPool<byte>.Shared.Rent(converter.PixelWidth * converter.PixelHeight * 4);
            converter.CopyPixels(data, converter.PixelWidth * 4, 0);

            LoadedImage = new NManagedImage(converter.PixelWidth, converter.PixelHeight, false);
            ImageConversion.ConvertToBGRA128(data, LoadedImage.GetData(), converter.PixelWidth * converter.PixelHeight);

            ArrayPool<byte>.Shared.Return(data);

            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose()
        {
            LoadedImage?.Dispose();
        }
    }

    file class TestPngFootageSource : IFootageSource
    {
        public string SourceId => "0";

        public string? Name => null;

        public double FrameRate => 0.0;

        public int Width => Image.Width;

        public int Height => Image.Height;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.Image;

        NManagedImage Image { get; }

        public TestPngFootageSource(NManagedImage image)
        {
            Image = image;
        }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            if (downSamplingRate != 1.0)
            {
                var result = new NManagedImage((int)(Width / downSamplingRate), (int)(Height / downSamplingRate));
                var renderer = new CPURenderer2D(result);
                renderer.DrawSingleImage(new Int32Point(), Image, 1.0F, Matrix3x3.CreateScale(result.Width / (float)Width, result.Height / (float)Height), ImageInterpolationQuality.Level2, NiVE3.Image.Drawing.BlendMode.Replace, null);
                return result;
            }
            else
            {
                return Image.Copy();
            }
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }
    }
}
