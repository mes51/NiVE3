using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Input;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class ProjectModel : BindableBase
    {
        public ObservableCollection<CompositionModel> CompositionModels { get; } = [];

        public ObservableCollection<PreviewModelBase> PreviewModels { get; } = [];

        private string projectPath = "";
        public string ProjectPath
        {
            get { return projectPath; }
            set { SetProperty(ref projectPath, value); }
        }

        private string projectName = "";
        public string ProjectName
        {
            get { return projectName; }
            set { SetProperty(ref projectName, value); }
        }

        private bool isEdited;
        public bool IsEdited
        {
            get { return isEdited; }
            set { SetProperty(ref isEdited, value); }
        }

        private bool isRendering;
        public bool IsRendering
        {
            get { return isRendering; }
            set { SetProperty(ref isRendering, value); }
        }

        private bool gpuErrorRaised;
        public bool GpuErrorRaised
        {
            get { return gpuErrorRaised; }
            set { SetProperty(ref gpuErrorRaised, value); }
        }

        public bool UseGpu => ApplicationModel.UseGpu && !GpuErrorRaised;

        FootageListModel FootageListModel { get; }

        RendererListModel RendererListModel { get; }

        ToneMapperListModel ToneMapperListModel { get; }

        EffectListModel EffectListModel { get; }

        RenderQueueModel RenderQueueModel { get; }

        TextPropertyModel TextPropertyModel { get; }

        HistoryModel HistoryModel { get; }

        ApplicationModel ApplicationModel { get; }

        public event EventHandler<CompositionEventArgs>? OpenCompositionTimeline;

        public event EventHandler<CompositionEventArgs>? CompositionRemoved;

        public ProjectModel(
            FootageListModel footageListModel,
            RendererListModel rendererListModel,
            ToneMapperListModel toneMapperListModel,
            EffectListModel effectListModel,
            RenderQueueModel renderQueueModel,
            TextPropertyModel textPropertyModel,
            HistoryModel historyModel,
            ApplicationModel applicationModel
        )
        {
            FootageListModel = footageListModel;
            RendererListModel = rendererListModel;
            ToneMapperListModel = toneMapperListModel;
            EffectListModel = effectListModel;
            RenderQueueModel = renderQueueModel;
            TextPropertyModel = textPropertyModel;
            HistoryModel = historyModel;
            ApplicationModel = applicationModel;

            footageListModel.ShowFootagePreview += FootageListModel_ShowFootagePreview;
            footageListModel.ShowCompositionPreview += FootageListModel_ShowCompositionPreview;
            footageListModel.FootageDeleted += FootageListModel_FootageDeleted;
            footageListModel.DeleteFootageByUndo += FootageListModel_DeleteFootageByUndo;

            historyModel.HistoryChanged += HistoryModel_HistoryChanged;

            PropertyChanged += ProjectModel_PropertyChanged;
        }

        public void CreateComposition(string name, int width, int height, double frameRate, double duration, bool isRetentionFrameRate, bool applyToneMappingWhenNested, int shutterAngle, int shutterPhase, int motionBlurSampleCount, Type rendererType, Type toneMapperType)
        {
            var renderer = RendererListModel.CreateRenderer(rendererType);
            var toneMapper = ToneMapperListModel.CreateToneMapper(toneMapperType);
            var rendererPluginId = RendererListModel.GetPluginId(rendererType);
            var toneMapperPluginId = ToneMapperListModel.GetPluginId(toneMapperType);
            var composition = new CompositionModel(renderer, toneMapper, rendererPluginId, toneMapperPluginId, FootageListModel, EffectListModel, RenderQueueModel, TextPropertyModel, HistoryModel)
            {
                Name = name,
                Width = width,
                Height = height,
                FrameRate = frameRate,
                Duration = duration,
                IsRetentionFrameRate = isRetentionFrameRate,
                ApplyToneMappingWhenNested = applyToneMappingWhenNested,
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
                PreviewModels.Add(new CompositionPreviewModel(ApplicationModel) { Composition = composition });
            }

            OnOpenCompositionTimeline(composition);
        }

        public void RemovePreview(PreviewModelBase previewModel)
        {
            PreviewModels.Remove(previewModel);
        }

        public void SaveProject()
        {
            var data = new ProjectData(
                FootageListModel.SaveData(),
                CompositionModels.Select(c => c.SaveData()).ToArray(),
                RenderQueueModel.SaveData()
            );

            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(ProjectPath, json);

            IsEdited = false;
        }

        public void LoadProject(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var projectData = JsonSerializer.Deserialize<ProjectData>(json);

            if (projectData == null)
            {
                // TODO: エラー表示
                return;
            }

            foreach (var c in CompositionModels)
            {
                OnCompositionRemoved(c);
                c.Dispose();
            }
            RenderQueueModel.Clear();
            FootageListModel.Clear();
            HistoryModel.Clear();
            CompositionModels.Clear();
            GpuErrorRaised = false;

            foreach (var p in PreviewModels)
            {
                switch (p)
                {
                    case FootagePreviewModel fp:
                        fp.Footage = null;
                        break;
                    case CompositionPreviewModel cp:
                        cp.Composition = null;
                        break;
                }
            }

            HistoryModel.BeginLoadProject();
            try
            {
                FootageListModel.LoadData(projectData.FootageList);
                foreach (var compositionData in projectData.Compositions)
                {
                    var renderer = RendererListModel.CreateRenderer(compositionData.RendererPluginId);
                    var toneMapper = ToneMapperListModel.CreateToneMapper(compositionData.ToneMapperPluginId);
                    var composition = new CompositionModel(renderer, toneMapper, compositionData.RendererPluginId, compositionData.ToneMapperPluginId, FootageListModel, EffectListModel, RenderQueueModel, TextPropertyModel, HistoryModel, compositionData.CompositionId);
                    composition.LoadData(compositionData);
                    CompositionModels.Add(composition);
                }
                
                var compositionFootages = FootageListModel.LoadCompositionFootageFromData(projectData.FootageList, [..CompositionModels]);
                foreach (var composition in CompositionModels)
                {
                    foreach (var footage in compositionFootages)
                    {
                        composition.ReplacePlaceholder(footage);
                    }
                }

                RenderQueueModel.LoadData(projectData.RenderQueueItems, [..CompositionModels]);
            }
            //catch (Exception)
            //{
            //    FootageListModel.Clear();
            //    CompositionModels.Clear();
            //}
            finally
            {
                HistoryModel.EndLoadProject();
            }

            IsEdited = false;
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
                PreviewModels.Add(new FootagePreviewModel(ApplicationModel) { Footage = e.Footage });
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
                PreviewModels.Add(new CompositionPreviewModel(ApplicationModel) { Composition = e.Composition });
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
                    RenderQueueModel.RemoveQueuesByComposition(input.Composition);

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

        private void HistoryModel_HistoryChanged(object? sender, EventArgs e)
        {
            IsEdited = true;
        }

        private void ProjectModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectPath))
            {
                ProjectName = Path.GetFileName(ProjectPath);
            }
        }
    }
}
