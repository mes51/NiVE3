using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(CompositionInput), "CompositionInput", "", "mes51", ID, "", false)]
    [InternalInput]
    class CompositionInput : IInput
    {
        const string ID = "4F4E8A0C-9E79-40F1-B76C-04D4B90AF53B";

        public static readonly Guid PluginId = Guid.Parse(ID);

        public string FilePath => Composition.Name;

        public CompositionModel Composition { get; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public CompositionInput(CompositionModel composition)
        {
            Composition = composition;
        }

        public CompositionInput(object? inputOption, CompositionModel[] compositions)
        {
            var compositionIdString = inputOption?.ToString();
            if (compositionIdString == null)
            {
                throw new Exception();
            }

            var compositionId = Guid.Parse(compositionIdString);
            Composition = compositions.First(c => c.CompositionId == compositionId);
        }

        public bool Load(string filePath)
        {
            return true;
        }

        public object? SaveData()
        {
            return Composition.CompositionId;
        }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup([new CompositionFootageSource(Composition)]);
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

        public SourceType SourceType => SourceType.VideoAndAudio;

        CompositionModel Composition { get; }

        public CompositionFootageSource(CompositionModel composition)
        {
            Composition = composition;
        }

        public NImage ReadFrame(double time, bool toGpu)
        {
            // TODO: 親のコンポジションのダウンサンプリングの反映
            return Composition.RenderFrame(time, 1.0, toGpu);
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }
    }
}
