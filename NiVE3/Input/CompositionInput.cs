using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.Runtime;
using NiVE3.Model;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(CompositionInput), "CompositionInput", "", "mes51", ID, "", false)]
    [InternalInput]
    class CompositionInput : IInput
    {
        const string ID = "4F4E8A0C-9E79-40F1-B76C-04D4B90AF53B";

        public string FilePath => Composition.Name;

        public CompositionModel Composition { get; }

        public void SetupAccelerator(Accelerator? accelerator) { }

        public CompositionInput(CompositionModel composition)
        {
            Composition = composition;
        }

        public bool Load(string filePath)
        {
            return true;
        }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup(new IFootageSource[] { new CompositionFootageSource(Composition) });
        }

        public void Dispose() { }
    }

    class CompositionFootageSource : IFootageSource
    {
        public string SourceId => "Composition";

        public double FrameRate => Composition.FrameRate;

        public int Width => Composition.Width;

        public int Height => Composition.Height;

        public double Duration => Composition.Duration;

        public SourceType SourceType => Composition.HasAudio ? SourceType.VideoAndAudio : SourceType.Video;

        CompositionModel Composition { get; }

        public CompositionFootageSource(CompositionModel composition)
        {
            Composition = composition;
        }

        public NImage Read(double time, bool toGpu)
        {
            return Composition.Render(time, toGpu);
        }
    }
}
