using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Image;
using ILGPU.Runtime;

namespace NiVE3.Test.Model
{
    class FootageListModelTest
    {
    }

    [Export(typeof(IInput))]
    [InputMetadata(typeof(TestInput), "TestInput", "mes51", ID, "*.*")]
    public class TestInput : IInput
    {
        public const string ID = "D0D13BF8-2486-4452-840E-0AB4C5CC8745";

        public string FilePath => throw new NotImplementedException();

        public double FrameRate => throw new NotImplementedException();

        public double Duration => throw new NotImplementedException();

        public int Width => throw new NotImplementedException();

        public int Height => throw new NotImplementedException();

        public string SupportedFileExtensions => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool Load(string filePath)
        {
            return true;
        }

        public NImage Read(double time, bool toGpu)
        {
            throw new NotImplementedException();
        }
    }
}
