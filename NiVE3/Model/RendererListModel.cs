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
        public IReadOnlyList<IRendererMetadata> RendererMetadatas { get; }

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
                RendererMetadatas = Renderers.Select(r => r.Metadata).ToList();
            }
            else
            {
                RendererMetadatas = new List<IRendererMetadata>();
            }
        }

        public Guid GetPluginId(Type rendererType)
        {
            if (Renderers == null)
            {
                throw new Exception(); // bug
            }
            return Guid.Parse(Renderers.First(f => f.Metadata.PluginType == rendererType).Metadata.RendererUuid);
        }

        public ExportLifetimeContext<IRenderer> CreateRenderer(Type rendererType)
        {
            if (Renderers == null)
            {
                throw new Exception(); // bug
            }
            var factory = Renderers.First(f => f.Metadata.PluginType == rendererType);
            var result = factory.CreateExport();
            if (factory.Metadata.IsSupportGpu)
            {
                result.Value.SetupAccelerator(AcceleratorModel); // TODO: Acceleratorの更新
            }
            return result;
        }
    }
}
