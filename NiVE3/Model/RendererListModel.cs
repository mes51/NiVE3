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
    class RendererListModel : BindableBase
    {
        public IReadOnlyList<IRendererMetadata> RendererMetadata { get; }

        [ImportMany]
        List<ExportFactory<IRenderer, IRendererMetadata>>? Renderers { get; set; }

        AcceleratorModel AcceleratorModel { get; }

        public RendererListModel(AcceleratorModel acceleratorModel)
        {
            var catalog = new DirectoryCatalog(Paths.PluginDirectory);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            AcceleratorModel = acceleratorModel;

            if (Renderers != null)
            {
                RendererMetadata = [..Renderers.Select(r => r.Metadata)];
            }
            else
            {
                RendererMetadata = [];
            }
        }

        public ExportLifetimeContext<IRenderer> CreateRenderer(Guid rendererPluginId)
        {
            if (Renderers == null)
            {
                throw new Exception(); // bug
            }
            var factory = Renderers.First(f => Guid.Parse(f.Metadata.RendererUuid) == rendererPluginId);
            var result = factory.CreateExport();
            if (factory.Metadata.IsSupportGpu)
            {
                result.Value.SetupAccelerator(AcceleratorModel); // TODO: Acceleratorの更新
            }
            return result;
        }

        public ITransformer CreateTransfomer(Guid rendererPluginId)
        {
            if (Renderers == null)
            {
                throw new Exception(); // bug
            }

            var transformerType = Renderers.First(f => Guid.Parse(f.Metadata.RendererUuid) == rendererPluginId).Metadata.TransformerType;
            var transformer = Activator.CreateInstance(transformerType) as ITransformer;
            if (transformer == null)
            {
                throw new InvalidOperationException("renderer haven't transformer");
            }

            return transformer;
        }
    }
}
