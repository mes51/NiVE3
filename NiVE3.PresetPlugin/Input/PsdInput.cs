using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Psd;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(PsdInput), "PsdInput", "", "mes51", ID, "*.psd,*.psb", IsSupportLoadToGpu = false)]
    public sealed class PsdInput : IInput
    {
        const string ID = "BEFBFF5B-734D-49C7-8283-D09E028DCEA2";

        public string FilePath { get; private set; } = "";

        PsdFile? PsdFile { get; set; }

        NManagedImage? CompositedImage { get; set; }

        public FootageSourceGroup GetGroup()
        {
            if (CompositedImage != null)
            {
                return new FootageSourceGroup([new PsdCompositedImageFootageSource(CompositedImage)]);
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

            try
            {
                PsdFile = PsdFile.Parse(filePath);
            }
            catch
            {
                return false;
            }

            var rawCompositedImage = PsdFile.DebugReadFirstLayer();
            if (rawCompositedImage.Length < PsdFile.Width * PsdFile.Height)
            {
                rawCompositedImage = PsdFile.ReadImageData();
            }
            if (rawCompositedImage.Length < PsdFile.Width * PsdFile.Height)
            {
                return false;
            }

            CompositedImage = new NManagedImage(PsdFile.Width, PsdFile.Height);
            rawCompositedImage.AsSpan().CopyTo(CompositedImage.GetDataSpan());

            return true;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose()
        {
            CompositedImage?.Dispose();
        }
    }

    file class PsdCompositedImageFootageSource : IFootageSource
    {
        public string SourceId => "composited image";

        public string? Name => null;

        public double FrameRate => 0.0;

        public int Width => Image.Width;

        public int Height => Image.Height;

        public Time Duration => Time.Zero;

        public SourceType SourceType => SourceType.Image;

        NManagedImage Image { get; }

        public PsdCompositedImageFootageSource(NManagedImage image)
        {
            Image = image;
        }

        public float[] ReadAudio(Time time, Time length)
        {
            throw new NotImplementedException();
        }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            if (downSamplingRate != 1.0)
            {
                var result = new NManagedImage((int)(Width / downSamplingRate), (int)(Height / downSamplingRate));
                var renderer = new CPURenderer2D(result);
                renderer.DrawSingleImage(new Int32Point(), Image, 1.0F, Matrix3x3.CreateScale(result.Width / (float)Width, result.Height / (float)Height), ImageInterpolationQuality.Level2, BlendMode.Replace, null);
                return result;
            }
            else
            {
                return Image.Copy();
            }
        }
    }
}
