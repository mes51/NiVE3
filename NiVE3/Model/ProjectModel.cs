using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;
using NiVE3.Cache;
using NiVE3.Config;
using NiVE3.Data.Json.Project;
using NiVE3.Input;
using NiVE3.Model.UI;
using NiVE3.Util;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class ProjectModel : BindableBase
    {
        const string AutoSaveDateFormat = "yyyyMMddHHmmss";

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

        AcceleratorModel AcceleratorModel { get; }

        ApplicationModel ApplicationModel { get; }

        DispatcherTimer AutoSaveTimer { get; } = new DispatcherTimer();

        public event EventHandler<CompositionEventArgs>? OpenCompositionTimeline;

        public event EventHandler<CompositionEventArgs>? CompositionRemoved;

        public event EventHandler<EventArgs>? AutoSaveErrorRaised;

        public ProjectModel(
            FootageListModel footageListModel,
            RendererListModel rendererListModel,
            ToneMapperListModel toneMapperListModel,
            EffectListModel effectListModel,
            RenderQueueModel renderQueueModel,
            TextPropertyModel textPropertyModel,
            HistoryModel historyModel,
            AcceleratorModel acceleratorModel,
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
            AcceleratorModel = acceleratorModel;
            ApplicationModel = applicationModel;

            footageListModel.ShowFootagePreview += FootageListModel_ShowFootagePreview;
            footageListModel.ShowCompositionPreview += FootageListModel_ShowCompositionPreview;
            footageListModel.FootageDeleted += FootageListModel_FootageDeleted;
            footageListModel.DeleteFootageByUndo += FootageListModel_DeleteFootageByUndo;

            historyModel.HistoryChanged += HistoryModel_HistoryChanged;

            PropertyChanged += ProjectModel_PropertyChanged;

            ApplicationSetting.Setting.UpdateSetting += Setting_UpdateSetting;
            AutoSaveTimer.Tick += AutoSaveTimer_Tick;

            UpdateAutoSaveTimer();
        }

        public void CreateComposition(string name, int width, int height, double frameRate, double duration, bool isRetentionFrameRate, bool applyToneMappingWhenNested, int shutterAngle, int shutterPhase, int motionBlurSampleCount, Guid rendererPluginId, object? rendererSettingData, Guid toneMapperPluginId, object? toneMapperSettingData)
        {
            var composition = new CompositionModel(rendererPluginId, toneMapperPluginId, FootageListModel, EffectListModel, RenderQueueModel, TextPropertyModel, RendererListModel, ToneMapperListModel, this, HistoryModel, AcceleratorModel)
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
            composition.ApplyInitialSettingData(rendererSettingData, toneMapperSettingData);
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

        public void ClearToNewProject()
        {
            foreach (var c in CompositionModels)
            {
                OnCompositionRemoved(c);
                c.Dispose();
            }
            RenderQueueModel.Clear();
            FootageListModel.Clear();
            HistoryModel.Clear();
            CompositionModels.Clear();
            ImageCache.ClearAll();
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

            IsEdited = false;
        }

        public void SaveProject()
        {
            SaveProjectAs(ProjectPath);

            IsEdited = false;
        }

        public void SaveProjectForAutoSave()
        {
            if (!Directory.Exists(Paths.AutoSaveProjectDirectory))
            {
                Directory.CreateDirectory(Paths.AutoSaveProjectDirectory);
            }

            var projectName = string.IsNullOrEmpty(ProjectName) ? "unnamed_project" : ProjectName;
            var autosavedProjectFiles = new List<(string filePath, DateTime saveTime)>();
            foreach (var filePath in Directory.GetFiles(Paths.AutoSaveProjectDirectory, "*.nvp3"))
            {
                var fileName = Path.GetFileName(filePath).Split(".");
                var fileNameWithoutSuffix = string.Join(".", fileName[..^2]);
                if (fileNameWithoutSuffix == projectName && DateTime.TryParseExact(fileName[^2], AutoSaveDateFormat, null, DateTimeStyles.None, out var saveTime))
                {
                    autosavedProjectFiles.Add((filePath, saveTime));
                }
            }
            autosavedProjectFiles.Sort((a, b) => a.saveTime.CompareTo(b.saveTime));

            try
            {
                while (autosavedProjectFiles.Count >= ApplicationSetting.Setting.AutoSaveCount)
                {
                    var (filePath, _) = autosavedProjectFiles[0];
                    File.Delete(filePath);
                    autosavedProjectFiles.RemoveAt(0);
                }
            }
            catch { }

            var newAutoSaveProjectPath = Path.Combine(Paths.AutoSaveProjectDirectory, $"{projectName}.{DateTime.Now.ToString(AutoSaveDateFormat)}.nvp3");
            SaveProjectAs(newAutoSaveProjectPath);
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

            ClearToNewProject();

            HistoryModel.BeginLoadProject();
            try
            {
                var projectDir = Path.GetDirectoryName(filePath) ?? "";
                FootageListModel.LoadData(projectData.FootageList, projectDir);
                foreach (var compositionData in projectData.Compositions)
                {
                    var composition = new CompositionModel(compositionData.RendererPluginId, compositionData.ToneMapperPluginId, FootageListModel, EffectListModel, RenderQueueModel, TextPropertyModel, RendererListModel, ToneMapperListModel, this, HistoryModel, AcceleratorModel, compositionData.CompositionId);
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

            ProjectPath = filePath;
            IsEdited = false;
        }

        public void ShowCompositionPreview(CompositionModel composition)
        {
            var freePreviewModel = PreviewModels.OfType<CompositionPreviewModel>().FirstOrDefault(p => !p.IsLock);
            if (freePreviewModel != null)
            {
                freePreviewModel.Composition = composition;
                freePreviewModel.CurrentTime = composition.CurrentTime;
            }
            else
            {
                PreviewModels.Add(new CompositionPreviewModel(ApplicationModel) { Composition = composition, CurrentTime = composition.CurrentTime });
            }
        }

        public void AbortRendering()
        {
            if (IsRendering)
            {
                RenderQueueModel.AbortRendering();
            }
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

        void SaveProjectAs(string projectPath)
        {
            var projectDir = Path.GetDirectoryName(projectPath) ?? "";
            var data = new ProjectData(
                FootageListModel.SaveData(projectDir),
                CompositionModels.Select(c => c.SaveData()).ToArray(),
                RenderQueueModel.SaveData()
            );

            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(projectPath, json);
        }

        void UpdateAutoSaveTimer()
        {
            var newInterval = new TimeSpan(0, ApplicationSetting.Setting.AutoSaveInterval, 0);
            if (AutoSaveTimer.Interval != newInterval)
            {
                AutoSaveTimer.Stop();
                AutoSaveTimer.Interval = newInterval;

                if (ApplicationSetting.Setting.UseAutoSave)
                {
                    AutoSaveTimer.Start();
                }
            }
            else
            {
                if (ApplicationSetting.Setting.UseAutoSave && !AutoSaveTimer.IsEnabled)
                {
                    AutoSaveTimer.Start();
                }
                else if (!ApplicationSetting.Setting.UseAutoSave)
                {
                    AutoSaveTimer.Stop();
                }
            }
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
            ShowCompositionPreview(e.Composition);
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

        private void Setting_UpdateSetting(object? sender, EventArgs e)
        {
            UpdateAutoSaveTimer();
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                AutoSaveTimer.Stop();

                if (!HistoryModel.IsEmpty())
                {
                    SaveProjectForAutoSave();
                }

                AutoSaveTimer.Start();
            }
            catch
            {
                AutoSaveErrorRaised?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
