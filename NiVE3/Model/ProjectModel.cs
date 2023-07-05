using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class ProjectModel : BindableBase
    {
        public ObservableCollection<CompositionModel> CompositionModels { get; } = new ObservableCollection<CompositionModel>();

        public ObservableCollection<PreviewModel> PreviewModels { get; } = new ObservableCollection<PreviewModel>();

        FootageListModel FootageListModel { get; }

        public ProjectModel(FootageListModel footageListModel)
        {
            FootageListModel= footageListModel;
        }

        public void CreateComposition()
        {
            CompositionModels.Add(new CompositionModel());
        }

        public void CreatePreview()
        {
            PreviewModels.Add(new PreviewModel());
        }
    }
}
