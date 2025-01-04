using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;
using NiVE3.Shared.Util;
using NiVE3.UI.Command;
using NiVE3.View.Dialog;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    class RenderSettingViewModel : BindableBase, IDialogAware
    {
        public const string CompositionParameterName = nameof(CompositionParameterName);

        public const string OutputParameterName = nameof(OutputParameterName);

        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
        }

        private RenderRangeType renderRangeType = RenderRangeType.Workarea;
        public RenderRangeType RenderRangeType
        {
            get { return renderRangeType; }
            set { SetProperty(ref renderRangeType, value); }
        }

        private Time beginTime;
        public Time BeginTime
        {
            get { return beginTime; }
            set { SetProperty(ref beginTime, value); }
        }

        private Time endTime;
        public Time EndTime
        {
            get { return endTime; }
            set { SetProperty(ref endTime, value); }
        }

        private double frameRate;
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private Time frameDuration;
        public Time FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private Time compositionDuration;
        public Time CompositionDuration
        {
            get { return compositionDuration; }
            set { SetProperty(ref compositionDuration, value); }
        }

        private Time compositionWorkareaBegin;
        public Time CompositionWorkareaBegin
        {
            get { return compositionWorkareaBegin; }
            set { SetProperty(ref compositionWorkareaBegin, value); }
        }

        private Time compositionWorkareaEnd;
        public Time CompositionWorkareaEnd
        {
            get { return compositionWorkareaEnd; }
            set { SetProperty(ref compositionWorkareaEnd, value); }
        }

        private bool hasOutputSetting;
        public bool HasOutputSetting
        {
            get { return hasOutputSetting; }
            set { SetProperty(ref hasOutputSetting, value); }
        }

        private ObservableCollection<Tuple<Guid, string>> outputPlugins = [];
        public ObservableCollection<Tuple<Guid, string>> OutputPlugins
        {
            get { return outputPlugins; }
            set { SetProperty(ref outputPlugins, value); }
        }

        private int selectedOutputPlugin;
        public int SelectedOutputPlugin
        {
            get { return selectedOutputPlugin; }
            set { SetProperty(ref selectedOutputPlugin, value); }
        }

        private bool isOutputVideo = true;
        public bool IsOutputVideo
        {
            get { return isOutputVideo; }
            set { SetProperty(ref isOutputVideo, value); }
        }

        private bool isOutputAudio = true;
        public bool IsOutputAudio
        {
            get { return isOutputAudio; }
            set { SetProperty(ref isOutputAudio, value); }
        }

        private SourceType supportedSourceType;
        public SourceType SupportedSourceType
        {
            get { return supportedSourceType; }
            set { SetProperty(ref supportedSourceType, value); }
        }

        private Time renderRangeBeginLimit;
        public Time RenderRangeBeginLimit
        {
            get { return renderRangeBeginLimit; }
            set { SetProperty(ref renderRangeBeginLimit, value); }
        }

        private Time renderRangeEndStart;
        public Time RenderRangeEndStart
        {
            get { return renderRangeEndStart; }
            set { SetProperty(ref renderRangeEndStart, value); }
        }

        private bool hasAudio;
        public bool HasAudio
        {
            get { return hasAudio; }
            set { SetProperty(ref hasAudio, value); }
        }

        private RenderSettingMode mode;
        public RenderSettingMode Mode
        {
            get { return mode; }
            set { SetProperty(ref mode, value); }
        }

        public ICommand ChangeSaveFilePathCommand { get; }

        public ICommand OpenOutputSettingCommand { get; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.RenderSettingView_Title);

        ProjectModel ProjectModel { get; }

        OutputListModel OutputListModel { get; }

        IDialogService DialogService { get; }

        ExportLifetimeContext<IOutput> Output { get; set; }

        ExportLifetimeContext<IOutput>? OriginalOutput { get; set; }

        Int32Size CompositionSize { get; set; }

        public DialogCloseListener RequestClose { get; }

        public RenderSettingViewModel(ProjectModel projectModel, OutputListModel outputListModel, IDialogService dialogService)
        {
            ProjectModel = projectModel;
            OutputListModel = outputListModel;
            DialogService = dialogService;
            OutputPlugins = [..outputListModel.OutputMetadatas.Values.Select(m => Tuple.Create(Guid.Parse(m.OutputUuid), m.Name))];

            ChangeOutputPlugin(OutputPlugins.FirstOrDefault()?.Item1 ?? Guid.Empty);

            ChangeSaveFilePathCommand = new DelegateCommand(() =>
            {
                var save = new SaveFileDialog
                {
                    Filter = GetSaveFileFilter(),
                    InitialDirectory = Path.GetDirectoryName(FilePath),
                    FileName = Path.GetFileName(FilePath)
                };
                if (save.ShowDialog() ?? false)
                {
                    FilePath = Output.Value.ProcessOutputFilePath(save.FileName);
                }
            });

            OpenOutputSettingCommand = new DelegateCommand(() =>
            {
                var view = GetSettingView();
                if (view == null)
                {
                    return;
                }

                var param = new DialogParameters
                {
                    { PluginSettingViewModel.TitleLanguageResourceName, LanguageResourceDictionary.OutputSettingView_Title },
                    { nameof(PluginSettingViewModel.SettingView), view }
                };
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(PluginSettingView), param, r => result = r);
                if (result?.Result == ButtonResult.OK)
                {
                    if (Output.Value.ApplySetting(view.DataContext))
                    {
                        FilePath = Output.Value.ProcessOutputFilePath(FilePath);
                    }
                }
            }, () => HasOutputSetting).ObservesProperty(() => HasOutputSetting);

            OKCommand = new RequerySuggestedCommand(() =>
            {
                var outputSourceTypes = GetOutputSourceType();
                var result = new DialogParameters
                {
                    { OutputParameterName, Output },
                    { nameof(FilePath), FilePath },
                    { nameof(RenderRangeType), RenderRangeType },
                    { nameof(BeginTime), BeginTime },
                    { nameof(EndTime), EndTime },
                    { nameof(IsOutputVideo), outputSourceTypes.HasFlag(SourceType.Video) },
                    { nameof(IsOutputAudio), outputSourceTypes.HasFlag(SourceType.Audio) }
                };

                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = result });
            }, () => GetOutputSourceType() != SourceType.None);

            CancelCommand = new DelegateCommand(() =>
            {
                if (Mode != RenderSettingMode.ChangeSetting)
                {
                    Output.Dispose();
                }

                RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
            });

            PropertyChanged += RenderingSettingViewModel_PropertyChanged;
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var composition = parameters.GetValue<CompositionModel>(CompositionParameterName);
            CompositionSize = new Int32Size(composition.Width, composition.Height);
            FrameRate = composition.FrameRate;
            FrameDuration = composition.FrameDuration;
            CompositionDuration = composition.Duration;
            CompositionWorkareaBegin = composition.WorkareaBegin;
            CompositionWorkareaEnd = composition.WorkareaEnd;
            RenderRangeType = parameters.GetValue<RenderRangeType>(nameof(RenderRangeType));
            RenderRangeBeginLimit = Time.Zero;
            RenderRangeEndStart = composition.FrameDuration;
            BeginTime = parameters.GetValue<Time>(nameof(BeginTime));
            EndTime = parameters.GetValue<Time>(nameof(EndTime));
            IsOutputVideo = parameters.GetValue<bool>(nameof(IsOutputVideo));
            IsOutputAudio = parameters.GetValue<bool>(nameof(IsOutputAudio));
            if (parameters.TryGetValue<ExportLifetimeContext<IOutput>>(OutputParameterName, out var output))
            {
                Output?.Dispose();
                Output = output;
                OriginalOutput = output;

                var outputPluginId = OutputListModel.GetId(output.Value.GetType());
                SelectedOutputPlugin = Math.Max(OutputPlugins.FindIndex(t => t.Item1 == outputPluginId), 0);
            }
            if (parameters.TryGetValue<string>(nameof(FilePath), out var filePath))
            {
                FilePath = filePath;
            }
            else
            {
                var baseFilePath = string.IsNullOrEmpty(ProjectModel.ProjectPath) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Path.GetDirectoryName(ProjectModel.ProjectPath);
                baseFilePath = Path.Combine(baseFilePath ?? Path.GetFullPath("."), composition.Name + ".avi");
                FilePath = Output.Value.ProcessOutputFilePath(baseFilePath);
            }
            Mode = parameters.GetValue<RenderSettingMode>(nameof(Mode));
            HasAudio = composition.HasAudio;
        }

        string GetSaveFileFilter()
        {
            var id = OutputPlugins[SelectedOutputPlugin].Item1;
            var supportedExtensions = OutputListModel.GetMetadata(id)?.SupportedFileType ?? "*.*";
            return string.Join("|", supportedExtensions.Split(',').Select(e => e + "|" + e));
        }

        FrameworkElement? GetSettingView()
        {
            var size = IsOutputVideo ? CompositionSize : (Int32Size?)null;
            var sourceTypes = GetOutputSourceType();

            var (beginTime, endTime) = GetTimeRange();
            return Output.Value.GetOutputSetting(FilePath, beginTime, endTime - beginTime, FrameRate, size, sourceTypes);
        }

        (Time, Time) GetTimeRange()
        {
            return RenderRangeType switch
            {
                RenderRangeType.All => (Time.Zero, CompositionDuration),
                RenderRangeType.Workarea => (CompositionWorkareaBegin, CompositionWorkareaEnd),
                _ => (BeginTime, EndTime)
            };
        }

        [MemberNotNull(nameof(Output))]
        void ChangeOutputPlugin(Guid newOutputPluginId)
        {
            var output = OutputListModel.CreateOutput(newOutputPluginId);
            OperationGuard.ThrowIfNull(output);

            Output = output;
            var metadata = OutputListModel.GetMetadata(newOutputPluginId);
            HasOutputSetting = metadata?.HasSettingView ?? false;
            SupportedSourceType = metadata?.SupportedSourceType ?? SourceType.None;
            FilePath = output.Value.ProcessOutputFilePath(FilePath);
        }

        SourceType GetOutputSourceType()
        {
            return SupportedSourceType switch
            {
                SourceType.VideoAndAudio => (IsOutputVideo ? SourceType.Video : SourceType.None) | (IsOutputAudio ?  SourceType.Audio : SourceType.None),
                _ => SupportedSourceType
            };
        }

        private void RenderingSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BeginTime):
                    RenderRangeEndStart = BeginTime + new Time(1, FrameRate);
                    break;
                case nameof(EndTime):
                    RenderRangeBeginLimit = EndTime - new Time(1, FrameRate);
                    break;
                case nameof(SelectedOutputPlugin):
                    if (Output != OriginalOutput)
                    {
                        Output.Dispose();
                    }
                    ChangeOutputPlugin(OutputPlugins[SelectedOutputPlugin].Item1);
                    break;
            }
        }
    }

    enum RenderSettingMode
    {
        Enqueue,
        ChangeSetting,
        QuickRendering
    }
}
