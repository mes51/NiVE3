using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Util;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class FootageListModel : BindableBase
    {
        public IReadOnlyList<IInputMetadata> InputMetadatas { get; }

        [ImportMany]
        List<ExportFactory<IInput, IInputMetadata>>? Inputs { get; set; }

        private ObservableCollection<IFootageModel> footages = new ObservableCollection<IFootageModel>();
        public ObservableCollection<IFootageModel> Footages
        {
            get { return footages; }
            set{ SetProperty(ref footages, value); }
        }

        public FootageListModel()
        {
            var pluginCatalog = new DirectoryCatalog(Paths.PluginDirectory);
            var selfCatalog = new AssemblyCatalog(typeof(FootageListModel).Assembly);
            var catalog = new AggregateCatalog(pluginCatalog, selfCatalog);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            if (Inputs != null)
            {
                InputMetadatas = Inputs.Select(e => e.Metadata).ToList();
            }
            else
            {
                InputMetadatas = new List<IInputMetadata>();
            }
        }

        public void AddSolid()
        {
            var testInput = new Input.SolidInput();
            testInput.Load("");
            Footages.Add(new FootageModel(testInput));
        }

        public void AddFolder()
        {
            Footages.Add(new FootageFolderModel());
        }
    }
}
