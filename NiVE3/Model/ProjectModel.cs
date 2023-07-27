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

        RendererListModel RendererListModel { get; }

        public ProjectModel(FootageListModel footageListModel, RendererListModel rendererListModel)
        {
            FootageListModel= footageListModel;
            RendererListModel= rendererListModel;

            FootageListModel.ShowFootagePreview += FootageListModel_ShowFootagePreview;
            FootageListModel.RemoveFootageByUndo += FootageListModel_RemoveFootageByUndo;
        }

        public void CreateComposition(string name, int width, int height, double frameRate, double duration, bool isRetentionFrameRate, int shutterAngle, int shutterPhase, int motionBlurSampleCount, Type rendererType)
        {
            var renderer = RendererListModel.CreateRenderer(rendererType);
            CompositionModels.Add(new CompositionModel(renderer)
            {
                Name = name,
                Width = width,
                Height = height,
                FrameRate = frameRate,
                Duration = duration,
                IsRetentionFrameRate = isRetentionFrameRate,
                ShutterAngle = shutterAngle,
                ShutterPhase = shutterPhase,
                MotionBlurSampleCount = motionBlurSampleCount
            });
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

        private void FootageListModel_RemoveFootageByUndo(object? sender, FootageEventArgs e)
        {
            foreach (var f in e.Footages)
            {
                var preview = PreviewModels.OfType<FootagePreviewModel>().FirstOrDefault(p => p.Footage == f);
                if (preview != null)
                {
                    preview.Footage = null;
                }
            }
        }
    }
}
