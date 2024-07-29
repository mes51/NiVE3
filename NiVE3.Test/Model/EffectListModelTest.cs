using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Model;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Test.Model
{
    public class EffectListModelTest
    {
        [Test]
        public void TestCreateEffect()
        {
            // TODO: モックライブラリを探す
            var model = new EffectListModel(null!);
            var catalog = new AssemblyCatalog(GetType().Assembly);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(model);

            // TODO: モックライブラリを探す
            //       候補: https://github.com/telerik/JustMockLite
            var effectModel = model.CreateEffect(Guid.Parse(TestEffect.ID), null!, null!, null!);

            Assert.That(effectModel, Is.Not.Null);
        }
    }

    [Export(typeof(IEffect))]
    [EffectMetadata("Test", "mes51", "Test", "Test effect", ID)]
    public sealed class TestEffect : IEffect
    {
        public const string ID = "ED0374C9-2227-445A-9C4A-DE8A4A9DFAE5";

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties()
        {
            return [];
        }

        public void Dispose() { }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            throw new NotImplementedException();
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }
    }
}
