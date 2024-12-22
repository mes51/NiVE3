using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

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

        public Time Duration => Time.Zero;

        public SourceType SourceType => SourceType.None;

        private EmptyFootageSource() { }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            throw new NotImplementedException();
        }

        public float[] ReadAudio(Time time, Time length)
        {
            throw new NotImplementedException();
        }
    }
}
