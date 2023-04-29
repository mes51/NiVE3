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

        public void MoveFootage(Guid sourceFootageId, Guid targetFolderId)
        {
            var model = FindModel(sourceFootageId, Footages);
            var targetFolder = FindModel(targetFolderId, Footages) as FootageFolderModel;

            if (model == null || targetFolder == null)
            {
                return;
            }

            var oldParent = FindParent(sourceFootageId);
            if (oldParent == null)
            {
                // root
                Footages.Remove(model);
            }
            else
            {
                oldParent.Children.Remove(model);
            }
            targetFolder.Children.Add(model);
        }

        public void MoveFootageToRoot(Guid sourceFootageId)
        {
            var model = FindModel(sourceFootageId, Footages);
            if (model == null)
            {
                return;
            }

            var oldParent = FindParent(sourceFootageId);
            if (oldParent != null)
            {
                oldParent.Children.Remove(model);
                Footages.Add(model);
            }
        }

        FootageFolderModel? FindParent(Guid targetId)
        {
            // rootの場合はnull
            if (Footages.Any(f => f.FootageId == targetId))
            {
                return null;
            }

            return Footages.OfType<FootageFolderModel>()
                .Select(f => FindParent(targetId, f))
                .SkipWhile(p => p == null)
                .FirstOrDefault();
        }

        static IFootageModel? FindModel(Guid targetId, IEnumerable<IFootageModel> list)
        {
            var model = list.FirstOrDefault(f => f.FootageId == targetId);
            if (model != null)
            {
                return model;
            }

            return list.OfType<FootageFolderModel>()
                .Select(l => FindModel(targetId, l.Children))
                .SkipWhile(r => r == null).FirstOrDefault();
        }

        static FootageFolderModel? FindParent(Guid targetId, FootageFolderModel parent)
        {
            if (parent.Children?.Any(f => f.FootageId == targetId) ?? false)
            {
                return parent;
            }

            return parent.Children?.OfType<FootageFolderModel>()
                ?.Select(f => FindParent(targetId, f))
                ?.SkipWhile(p => p == null)
                ?.FirstOrDefault();
        }
    }
}
