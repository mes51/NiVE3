using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Project;
using NiVE3.Input;
using NiVE3.View.Resource;
using Prism.Mvvm;
using SpanJson;

namespace NiVE3.Model
{
    partial class ProjectModel : BindableBase
    {
        public ObservableCollection<CompositionModel> CompositionModels { get; } = new ObservableCollection<CompositionModel>();

        public ObservableCollection<PreviewModelBase> PreviewModels { get; } = new ObservableCollection<PreviewModelBase>();

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

        FootageListModel FootageListModel { get; }

        RendererListModel RendererListModel { get; }

        EffectListModel EffectListModel { get; }

        HistoryModel HistoryModel { get; }

        ApplicationModel ApplicationModel { get; }

        public event EventHandler<CompositionEventArgs>? OpenCompositionTimeline;

        public event EventHandler<CompositionEventArgs>? CompositionRemoved;

        public ProjectModel(FootageListModel footageListModel, RendererListModel rendererListModel, EffectListModel effectListModel, HistoryModel historyModel, ApplicationModel applicationModel)
        {
            FootageListModel = footageListModel;
            RendererListModel = rendererListModel;
            EffectListModel = effectListModel;
            HistoryModel = historyModel;
            ApplicationModel = applicationModel;

            FootageListModel.ShowFootagePreview += FootageListModel_ShowFootagePreview;
            FootageListModel.ShowCompositionPreview += FootageListModel_ShowCompositionPreview;
            FootageListModel.FootageDeleted += FootageListModel_FootageDeleted;
            FootageListModel.DeleteFootageByUndo += FootageListModel_DeleteFootageByUndo;

            historyModel.HistoryChanged += HistoryModel_HistoryChanged;

            PropertyChanged += ProjectModel_PropertyChanged;
        }

        public void CreateComposition(string name, int width, int height, double frameRate, double duration, bool isRetentionFrameRate, int shutterAngle, int shutterPhase, int motionBlurSampleCount, Type rendererType)
        {
            var renderer = RendererListModel.CreateRenderer(rendererType);
            var rendererPluginId = RendererListModel.GetPluginId(rendererType);
            var composition = new CompositionModel(renderer, rendererPluginId, FootageListModel, EffectListModel, HistoryModel)
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
                CompositionModels.Select(c => c.SaveData()).ToArray()
            );

            var json = JsonSerializer.Generic.Utf16.Serialize(data);
            File.WriteAllText(ProjectPath, json);

            IsEdited = false;
        }

        public void LoadProject(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var projectData = JsonSerializer.Generic.Utf16.Deserialize<ProjectData>(json);

            foreach (var c in CompositionModels)
            {
                OnCompositionRemoved(c);
                c.Dispose();
            }
            FootageListModel.Clear();
            HistoryModel.Clear();
            CompositionModels.Clear();

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
                    var composition = new CompositionModel(renderer, compositionData.RendererPluginId, FootageListModel, EffectListModel, HistoryModel, compositionData.CompositionId);
                    composition.LoadData(compositionData);
                    CompositionModels.Add(composition);
                }
                
                var compositionFootages = FootageListModel.LoadCompositionFootageFromData(projectData.FootageList, CompositionModels.ToArray());
                foreach (var composition in CompositionModels)
                {
                    foreach (var footage in compositionFootages)
                    {
                        composition.ReplacePlaceholder(footage);
                    }
                }
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
