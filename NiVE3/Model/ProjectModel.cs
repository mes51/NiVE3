using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Input;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class ProjectModel : BindableBase
    {
        public ObservableCollection<CompositionModel> CompositionModels { get; } = new ObservableCollection<CompositionModel>();

        public ObservableCollection<PreviewModelBase> PreviewModels { get; } = new ObservableCollection<PreviewModelBase>();

        FootageListModel FootageListModel { get; }

        RendererListModel RendererListModel { get; }

        EffectListModel EffectListModel { get; }

        HistoryModel HistoryModel { get; }

        public event EventHandler<CompositionEventArgs>? OpenCompositionTimeline;

        public event EventHandler<CompositionEventArgs>? CompositionRemoved;

        public ProjectModel(FootageListModel footageListModel, RendererListModel rendererListModel, EffectListModel effectListModel, HistoryModel historyModel)
        {
            FootageListModel = footageListModel;
            RendererListModel = rendererListModel;
            EffectListModel = effectListModel;
            HistoryModel = historyModel;

            FootageListModel.ShowFootagePreview += FootageListModel_ShowFootagePreview;
            FootageListModel.ShowCompositionPreview += FootageListModel_ShowCompositionPreview;
            FootageListModel.FootageDeleted += FootageListModel_FootageDeleted;
            FootageListModel.DeleteFootageByUndo += FootageListModel_DeleteFootageByUndo;
        }

        public void CreateComposition(string name, int width, int height, double frameRate, double duration, bool isRetentionFrameRate, int shutterAngle, int shutterPhase, int motionBlurSampleCount, Type rendererType)
        {
            var renderer = RendererListModel.CreateRenderer(rendererType);
            var composition = new CompositionModel(renderer, FootageListModel, EffectListModel, HistoryModel)
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
            };
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddComposition));
            HistoryModel.Add(new AddCompositionCommand(this, composition));
            CompositionModels.Add(composition);
            FootageListModel.AddComposition(composition);
            HistoryModel.EndGroup();

            var preview = PreviewModels.OfType<CompositionPreviewModel>().FirstOrDefault();
            if (preview != null)
            {
                preview.Composition = composition;
            }
            else
            {
                PreviewModels.Add(new CompositionPreviewModel { Composition = composition });
            }

            OnOpenCompositionTimeline(composition);
        }

        public void CreatePreview()
        {
            //PreviewModels.Add(new FootagePreviewModel());
        }

        public void RemovePreview(PreviewModelBase previewModel)
        {
            PreviewModels.Remove(previewModel);
        }

        void AddCompositionModel(CompositionModel composition)
        {
            CompositionModels.Add(composition);
        }

        void RemoveCompositionModel(CompositionModel composition)
        {
            CompositionModels.Remove(composition);
            var preview = PreviewModels.OfType<CompositionPreviewModel>().FirstOrDefault();
            if (preview != null)
            {
                preview.Composition = null;
            }
            OnCompositionRemoved(composition);
        }

        void OnOpenCompositionTimeline(CompositionModel composition)
        {
            OpenCompositionTimeline?.Invoke(this, new CompositionEventArgs(composition));
        }

        void OnCompositionRemoved(CompositionModel composition)
        {
            CompositionRemoved?.Invoke(this, new CompositionEventArgs(composition));
        }

        private void FootageListModel_ShowFootagePreview(object? sender, FootageModelEventArgs e)
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

        private void FootageListModel_ShowCompositionPreview(object? sender, CompositionEventArgs e)
        {
            var freePreviewModel = PreviewModels.OfType<CompositionPreviewModel>().FirstOrDefault(p => !p.IsLock);
            if (freePreviewModel != null)
            {
                freePreviewModel.Composition = e.Composition;
            }
            else
            {
                PreviewModels.Add(new CompositionPreviewModel { Composition = e.Composition });
            }
            OnOpenCompositionTimeline(e.Composition);
        }

        private void FootageListModel_FootageDeleted(object? sender, FootageEventArgs e)
        {
            foreach (var f in e.Footages.OfType<FootageModel>())
            {
                foreach (var c in CompositionModels)
                {
                    c.DeleteLayersByFootage(f);
                }

                if (f.InputModel.Input is CompositionInput input)
                {
                    RemoveCompositionModel(input.Composition);
                    HistoryModel.Add(new DeleteCompositionCommand(this, input.Composition));
                }

                var preview = PreviewModels.OfType<FootagePreviewModel>().FirstOrDefault(p => p.Footage == f);
                if (preview != null)
                {
                    preview.Footage = null;
                }
            }
        }

        private void FootageListModel_DeleteFootageByUndo(object? sender, FootageEventArgs e)
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
