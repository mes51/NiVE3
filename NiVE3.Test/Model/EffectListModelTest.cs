using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;

namespace NiVE3.Test.Model
{
    public class EffectListModelTest
    {
        [Test]
        public void TestCreateEffect()
        {
            var model = new EffectListModel();
            var catalog = new AssemblyCatalog(GetType().Assembly);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(model);

            // TODO: モックライブラリを探す
            //       候補: https://github.com/telerik/JustMockLite
            var effectModel = model.CreateEffect(Guid.Parse(TestEffect.ID), null!, null!);

            Assert.NotNull(effectModel);
        }
    }

    [Export(typeof(IEffect))]
    [EffectMetadata("Test", "mes51", "Test", "Test effect", ID)]
    public class TestEffect : IEffect
    {
        public const string ID = "ED0374C9-2227-445A-9C4A-DE8A4A9DFAE5";

        public PropertyBase[] GetProperties()
        {
            throw new NotImplementedException();
        }
    }
}
