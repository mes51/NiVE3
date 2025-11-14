using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using Svg;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(SvgInput), "SvgInput", "", "mes51", ID, "*.svg", IsSupportLoadToGpu = false)]
    public sealed class SvgInput : IInput
    {
        const string ID = "AB1E3CFB-845A-486B-B218-09F2D5B5A39F";

        public string FilePath { get; set; } = "";

        NManagedImage? LoadedImage { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public FootageSourceGroup GetGroup()
        {
            if (LoadedImage != null)
            {
                return new FootageSourceGroup([new SingleImageFootageSource(LoadedImage)]);
            }
            else
            {
                return FootageSourceGroup.Empty;
            }
        }

        public bool Load(string filePath)
        {
            FilePath = filePath;

            var doc = SvgDocument.Open(filePath);
            using var image = doc.Draw();

            var data = ArrayPool<byte>.Shared.Rent(image.Width * image.Height * 4);
            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Marshal.Copy(bitmapData.Scan0, data, 0, image.Width * image.Height * 4);
            image.UnlockBits(bitmapData);

            LoadedImage = new NManagedImage(image.Width, image.Height);
            ImageConversion.ConvertToBGRA128(data, LoadedImage.GetDataSpan(), image.Width * image.Height);

            return true;
        }

        public void Dispose()
        {
            LoadedImage?.Dispose();
        }
    }
}
