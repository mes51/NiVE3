using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Model;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using System.Reflection;

namespace NiVE3.Test.Model
{
    public class FootageListModelTest
    {
        [Test]
        public void TestLoadFile_FileNotFound()
        {
            var model = CreateModel();

            var filePath = GenerateFilePath("not_found_file.test1");
            model.LoadFile(filePath, null);
            Assert.That(model.Footages.Count, Is.Zero);
        }

        [Test]
        public void TestLoadFile_NotSupportedFile()
        {
            var model = CreateModel();

            var filePath = GenerateFilePath("not_supportefd_file_extension.nse");
            try
            {
                File.Create(filePath);
                model.LoadFile(filePath, null);
                Assert.That(model.Footages.Count, Is.Zero);
            }
            finally
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }
            }
        }

        [Test]
        public void TestLoadFile_SingleSource()
        {
            var model = CreateModel();


            var filePath = GenerateFilePath("supported_file.test1");
            try
            {
                File.Create(filePath);
                model.LoadFile(filePath, null);
                Assert.That(model.Footages.Count, Is.EqualTo(1));
            }
            finally
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }
            }
        }

        [Test]
        public void TestLoadFile_MultipleSource()
        {
            var model = CreateModel();


            var filePath = GenerateFilePath("supported_file.test2");
            try
            {
                File.Create(filePath);
                model.LoadFile(filePath, null);
                Assert.That(model.Footages.Count, Is.EqualTo(1));
                Assert.That(model.Footages.FirstOrDefault(f => f is FootageFolderModel)?.Children?.Count, Is.EqualTo(3));
                Assert.That(model.Footages.FirstOrDefault(f => f is FootageFolderModel)?.Children?.FirstOrDefault(c => c is FootageFolderModel)?.Children?.Count, Is.EqualTo(1));
            }
            finally
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }
            }
        }

        FootageListModel CreateModel()
        {
            // TODO: Mock化
            var model = new FootageListModel(null!, new HistoryModel());
            var testCatalog = new AssemblyCatalog(GetType().Assembly);
            var selfCatalog = new AssemblyCatalog(typeof(FootageListModel).Assembly);
            var catalog = new AggregateCatalog(selfCatalog, testCatalog);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(model);

            model.GetType().GetMethod("InitializePlugin", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(model, null);

            return model;
        }

        string GenerateFilePath(string fileName)
        {
            return Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location) ?? "", fileName);
        }
    }

    [Export(typeof(IInput))]
    [InputMetadata(typeof(TestInput), "TestInput", "", "mes51", ID, "*.test1")]
    public class TestInput : IInput
    {
        public const string ID = "D0D13BF8-2486-4452-840E-0AB4C5CC8745";

        public string FilePath => "";

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup(new IFootageSource[] { new TestFootageSource("") });
        }

        public bool Load(string filePath)
        {
            return true;
        }
    }

    [Export(typeof(IInput))]
    [InputMetadata(typeof(TestInput2), "TestInput2", "", "mes51", ID, "*.test2")]
    public class TestInput2 : IInput
    {
        public const string ID = "6F1D817C-CD8C-41B8-A6B1-58D987CFDD29";

        public string FilePath => "";

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }

        public FootageSourceGroup GetGroup()
        {
            return new FootageSourceGroup("Root", new FootageSourceGroup[] { new FootageSourceGroup("Child", new IFootageSource[] { new TestFootageSource("3") }) }, new IFootageSource[] { new TestFootageSource("1"), new TestFootageSource("2") });
        }

        public bool Load(string filePath)
        {
            return true;
        }
    }

    class TestFootageSource : IFootageSource
    {
        public string SourceId { get; }

        public double FrameRate => 0.0;

        public int Width => 1;

        public int Height => 1;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.None;

        public TestFootageSource(string id)
        {
            SourceId = id;
        }

        public NImage ReadFrame(double time, bool toGpu)
        {
            throw new NotImplementedException();
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }
    }
}
