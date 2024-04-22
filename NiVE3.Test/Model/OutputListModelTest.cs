using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Model;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Test.Model
{
    public class OutputListModelTest
    {
        public void TestCreateOutput()
        {
            // TODO: モックライブラリを探す
            var model = new OutputListModel(null!);
            var catalog = new AssemblyCatalog(GetType().Assembly);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(model);

            model.GetType().GetMethod("InitializePlugin", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(model, null);
        }
    }

    [Export(typeof(IOutput))]
    [OutputMetadata(typeof(TestOutput), "Test", "mes51", "", ID, ".avi", SourceType.Video, false)]
    public sealed class TestOutput : IOutput
    {
        public const string ID = "D121D0DE-64E6-4476-8BF6-F5BEEF02DB26";

        public void BeginOutput(string filePath, double startTime, double duration, double frameRate, Int32Size? size, SourceType outputSources)
        {
            throw new NotImplementedException();
        }

        public void BeginPass(int pass)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void EndOutput()
        {
            throw new NotImplementedException();
        }

        public void EndPass()
        {
            throw new NotImplementedException();
        }

        public int GetPass()
        {
            throw new NotImplementedException();
        }

        public void ProcessAudio(float[] audio)
        {
            throw new NotImplementedException();
        }

        public void ProcessFrame(int pass, double time, NImage image, bool useGpu)
        {
            throw new NotImplementedException();
        }

        public string ProcessOutputFilePath(string baseFilePath)
        {
            throw new NotImplementedException();
        }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            throw new NotImplementedException();
        }
    }
}
