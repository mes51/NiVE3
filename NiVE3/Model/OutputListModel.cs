using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.Runtime.Cuda;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Util;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class OutputListModel : BindableBase
    {
        public IReadOnlyDictionary<Type, IOutputMetadata> OutputMetadatas { get; private set; } = new Dictionary<Type, IOutputMetadata>();

        [ImportMany]
        List<ExportFactory<IOutput, IOutputMetadata>>? Outputs { get; set; }

        AcceleratorModel AcceleratorModel { get; set; }

        public OutputListModel(AcceleratorModel acceleratorModel)
        {
            var pluginCatalog = new DirectoryCatalog(Paths.PluginDirectory);
            var container = new CompositionContainer(pluginCatalog);
            container.ComposeParts(this);

            AcceleratorModel = acceleratorModel;

            InitializePlugin();
        }

        public ExportLifetimeContext<IOutput>? CreateOutput(Guid outputUuid)
        {
            var factory = Outputs?.FirstOrDefault(o => Guid.Parse(o.Metadata.OutputUuid) == outputUuid);
            if (factory != null)
            {
                var output = factory.CreateExport();
                if (factory.Metadata.IsSupportGpu)
                {
                    output.Value.SetupAccelerator(AcceleratorModel);
                }
                return output;
            }
            else
            {
                return null;
            }
        }

        public IOutputMetadata? GetMetadata(Guid outputUuid)
        {
            return Outputs?.FirstOrDefault(o => Guid.Parse(o.Metadata.OutputUuid) == outputUuid)?.Metadata;
        }

        public Guid GetId(Type pluginType)
        {
            if (OutputMetadatas.TryGetValue(pluginType, out var outputMetadata))
            {
                return Guid.Parse(outputMetadata.OutputUuid);
            }
            else
            {
                return Guid.Empty;
            }
        }

        // for test
        // NOTE: 本来は不要(直接コンストラクタに書きたい)が、MEFの都合上、テスト用のモッククラスを差し込めるようにするため、メソッドに切り出す
        // TODO: オブジェクト作成時にCatalogにモッククラスを差し込める方法があれば差し替える
        void InitializePlugin()
        {
            if (Outputs != null)
            {
                OutputMetadatas = Outputs.Select(e => e.Metadata).ToDictionary(m => m.PluginType, m => m);
            }
            else
            {
                OutputMetadatas = new ReadOnlyDictionary<Type, IOutputMetadata>(new Dictionary<Type, IOutputMetadata>());
            }
        }
    }
}
