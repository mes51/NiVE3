using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Input.Special
{
    class EmptyFootageSource : IFootageSource
    {
        public static EmptyFootageSource Instance { get; } = new EmptyFootageSource();

        public string SourceId => "empty";

        public string? Name => null;

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.None;

        private EmptyFootageSource() { }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            throw new NotImplementedException();
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }
    }
}
