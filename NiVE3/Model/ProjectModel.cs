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

        public ObservableCollection<PreviewModelBase> PreviewModels { get; } = new ObservableCollection<PreviewModelBase>();

        FootageListModel FootageListModel { get; }

        public ProjectModel(FootageListModel footageListModel)
        {
            FootageListModel= footageListModel;

            FootageListModel.ShowFootagePreview += FootageListModel_ShowFootagePreview;
        }

        public void CreateComposition()
        {
            CompositionModels.Add(new CompositionModel());
        }

        public void CreatePreview()
        {
            //PreviewModels.Add(new FootagePreviewModel());
        }

        private void FootageListModel_ShowFootagePreview(object? sender, ShowFootagePreviewEventArgs e)
        {
            var freePreviewModel = PreviewModels.OfType<FootagePreviewModel>().FirstOrDefault(p => !p.IsLock);
            if (freePreviewModel != null)
            {
                freePreviewModel.Footage = e.Footage;
            }
            else
            {
                PreviewModels.Add(new FootagePreviewModel { Footage = e.Footage });
            }
        }
    }
}
