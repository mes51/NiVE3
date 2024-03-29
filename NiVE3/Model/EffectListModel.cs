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

        AcceleratorModel AcceleratorModel { get; set; }

        public EffectListModel(AcceleratorModel acceleratorModel)
        {
            var catalog = new DirectoryCatalog(Paths.PluginDirectory);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            AcceleratorModel = acceleratorModel;

            if (Effects != null)
            {
                EffectMetadatas = Effects.Select(e => e.Metadata).ToList();
            }
            else
            {
                EffectMetadatas = [];
            }
        }

        public EffectModel? CreateEffect(Guid effectUuid, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel, Guid? effectId = null)
        {
            var factory = Effects?.FirstOrDefault(f => Guid.Parse(f.Metadata.EffectUuid) == effectUuid);
            if (factory != null)
            {
                var effect = factory.CreateExport();
                if (factory.Metadata.IsSupportGpu)
                {
                    effect.Value.SetupAccelerator(AcceleratorModel); // TODO: Acceleratorの更新
                }
                return new EffectModel(effect, factory.Metadata, compositionModel, layerModel, historyModel, effectId);
            }
            else
            {
                return null;
            }
        }
    }
}
