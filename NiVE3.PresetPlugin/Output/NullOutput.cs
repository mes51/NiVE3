using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Output
{
    [Export(typeof(IOutput))]
    [OutputMetadata(typeof(NullOutput), "NullOutput", "mes51", "", ID, "*.avi", SourceType.VideoAndAudio, true)]
    public sealed class NullOutput : IOutput
    {
        const string ID = "7C445162-8DA9-4DB9-AF88-A35D467AED1F";

        public void BeginOutput(string filePath, Time startTime, Time duration, double frameRate, Int32Size? size, SourceType outputSources) { }

        public void BeginPass(int pass) { }

        public void EndOutput() { }

        public void EndPass() { }

        public int GetPassCount()
        {
            return 1;
        }

        public void ProcessAudio(float[] audio) { }

        public void ProcessFrame(int pass, Time time, NImage image, bool useGpu) { }

        public string ProcessOutputFilePath(string baseFilePath)
        {
            return baseFilePath;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }
    }
}
