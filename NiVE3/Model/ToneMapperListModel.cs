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
    class ToneMapperListModel : BindableBase
    {
        public IReadOnlyList<IToneMapperMetadata> ToneMapperMetadata { get; set; }

        [ImportMany]
        List<ExportFactory<IToneMapper, IToneMapperMetadata>>? ToneMappers { get; set; }

        AcceleratorModel AcceleratorModel { get; }

        public ToneMapperListModel(AcceleratorModel acceleratorModel)
        {
            var pluginCatalog = new DirectoryCatalog(Paths.PluginDirectory);
            var selfCatalog = new AssemblyCatalog(typeof(ToneMapperListModel).Assembly);
            var catalog = new AggregateCatalog(pluginCatalog, selfCatalog);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            AcceleratorModel = acceleratorModel;

            if (ToneMappers != null)
            {
                ToneMapperMetadata = [..ToneMappers.Select(t => t.Metadata)];
            }
            else
            {
                ToneMapperMetadata = [];
            }
        }

        public Guid GetPluginId(Type toneMapperType)
        {
            if (ToneMappers == null)
            {
                throw new Exception(); // bug
            }
            return Guid.Parse(ToneMappers.First(f => f.Metadata.PluginType == toneMapperType).Metadata.ToneMapperUuid);
        }

        public ExportLifetimeContext<IToneMapper> CreateToneMapper(Type toneMapperType)
        {
            if (ToneMappers == null)
            {
                throw new Exception(); // bug
            }
            var facotry = ToneMappers.First(f => f.Metadata.PluginType == toneMapperType);
            var result = facotry.CreateExport();
            if (facotry.Metadata.IsSupportGpu)
            {
                result.Value.SetupAccelerator(AcceleratorModel); // TODO: Acceleratorの更新
            }
            return result;
        }

        public ExportLifetimeContext<IToneMapper> CreateToneMapper(Guid toneMapperPluginId)
        {
            if (ToneMappers == null)
            {
                throw new Exception(); // bug
            }
            var facotry = ToneMappers.First(f => Guid.Parse(f.Metadata.ToneMapperUuid) == toneMapperPluginId);
            var result = facotry.CreateExport();
            if (facotry.Metadata.IsSupportGpu)
            {
                result.Value.SetupAccelerator(AcceleratorModel); // TODO: Acceleratorの更新
            }
            return result;
        }
    }
}
