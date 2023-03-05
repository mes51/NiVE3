using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Util;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class EffectListModel : BindableBase
    {
        public IReadOnlyList<IEffectMetadata> EffectMetadatas { get; }

        [ImportMany]
        List<ExportFactory<IEffect, IEffectMetadata>>? Effects { get; set; }

        public EffectListModel()
        {
            var catalog = new DirectoryCatalog(Paths.PluginDirectory);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            if (Effects != null)
            {
                EffectMetadatas = Effects.Select(e => e.Metadata).ToList();
            }
            else
            {
                EffectMetadatas = new List<IEffectMetadata>();
            }
        }

        public EffectModel? CreateEffect(Guid effectUuid)
        {
            var factory = Effects?.FirstOrDefault(f => Guid.Parse(f.Metadata.EffectUuid) == effectUuid);
            if (factory != null)
            {
                return new EffectModel(factory.CreateExport().Value);
            }
            else
            {
                return null;
            }
        }
    }
}
