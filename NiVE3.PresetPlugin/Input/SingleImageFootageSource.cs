using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal.Drawing;

namespace NiVE3.PresetPlugin.Input
{
    class SingleImageFootageSource : IFootageSource
    {
        public string SourceId => "0";

        public string? Name => null;

        public double FrameRate => 0.0;

        public int Width => Image.Width;

        public int Height => Image.Height;

        public Time Duration => Time.Zero;

        public SourceType SourceType => SourceType.Image;

        NManagedImage Image { get; }

        public SingleImageFootageSource(NManagedImage image)
        {
            Image = image;
        }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
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

        public float[] ReadAudio(Time time, Time length)
        {
            throw new NotImplementedException();
        }
    }
}
