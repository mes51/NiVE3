using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ILGPU.Runtime.Cuda;
using Microsoft.Win32;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Dialog;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

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

        private bool useItemTimeRange;
        public bool UseItemTimeRange
        {
            get { return useItemTimeRange; }
            set { SetProperty(ref useItemTimeRange, value); }
        }

        private double beginTime;
        public double BeginTime
        {
            get { return beginTime; }
            set { SetProperty(ref beginTime, value); }
        }

        private double endTime;
        public double EndTime
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

        private double frameDuration;
        public double FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private double compositionDuration;
        public double CompositionDuration
        {
            get { return compositionDuration; }
            set { SetProperty(ref compositionDuration, value); }
        }

        private double compositionWorkareaBegin;
        public double CompositionWorkareaBegin
        {
            get { return compositionWorkareaBegin; }
            set { SetProperty(ref compositionWorkareaBegin, value); }
        }

        private double compositionWorkareaEnd;
        public double CompositionWorkareaEnd
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

        private double renderRangeBeginLimit;
        public double RenderRangeBeginLimit
        {
            get { return renderRangeBeginLimit; }
            set { SetProperty(ref renderRangeBeginLimit, value); }
        }

        private double renderRangeEndStart;
        public double RenderRangeEndStart
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

        public ICommand ChangeSaveFilePathCommand { get; }

        public ICommand OpenOutputSettingCommand { get; }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.RenderSettingView_Title);

        ProjectModel ProjectModel { get; }

        OutputListModel OutputListModel { get; }

        IDialogService DialogService { get; }

        ExportLifetimeContext<IOutput> Output { get; set; }

        Int32Size CompositionSize { get; set; }

        public event Action<IDialogResult>? RequestClose;

        public RenderSettingViewModel(ProjectModel projectModel, OutputListModel outputListModel, IDialogService dialogService)
        {
            ProjectModel = projectModel;
            OutputListModel = outputListModel;
            DialogService = dialogService;
            OutputPlugins = [..outputListModel.OutputMetadatas.Values.Select(m => Tuple.Create(Guid.Parse(m.OutputUuid), m.Name))];

            var defaultOutputPluginId = OutputPlugins.FirstOrDefault()?.Item1 ?? Guid.Empty;
            var output = outputListModel.CreateOutput(defaultOutputPluginId);
            if (output == null)
            {
                // NOTE: 出力プラグイン0個の場合はQueue側で作らせないようにする
                throw new InvalidOperationException();
            }
            Output = output;
            HasOutputSetting = OutputListModel.GetMetadata(defaultOutputPluginId)?.HasSettingView ?? false;

            ChangeSaveFilePathCommand = new DelegateCommand(() =>
            {
                var save = new SaveFileDialog();
                save.Filter = GetSaveFileFilter();
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
                    { nameof(OutputSettingViewModel.SettingView), view }
                };
                IDialogResult? result = null;
                DialogService.ShowDialog(nameof(OutputSettingView), param, r => result = r);
                if (result?.Result == ButtonResult.OK)
                {
                    if (Output.Value.ApplyOutputSetting(view.DataContext))
                    {
                        FilePath = Output.Value.ProcessOutputFilePath(FilePath);
                    }
                }
            }, () => HasOutputSetting);

            OKCommand = new DelegateCommand(() =>
            {
                var result = new DialogParameters
                {
                    { OutputParameterName, Output },
                    { nameof(FilePath), FilePath },
                    { nameof(BeginTime), UseItemTimeRange ? BeginTime : CompositionWorkareaBegin },
                    { nameof(EndTime), UseItemTimeRange ? EndTime : CompositionWorkareaEnd },
                    { nameof(IsOutputVideo), IsOutputVideo },
                    { nameof(IsOutputAudio), IsOutputAudio }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, result));
            });

            CancelCommand = new DelegateCommand(() =>
            {
                Output.Dispose();

                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, null));
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
            BeginTime = composition.WorkareaBegin;
            EndTime = composition.WorkareaEnd;
            HasAudio = composition.HasAudio;

            var baseFilePath = string.IsNullOrEmpty(ProjectModel.ProjectPath) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Path.GetDirectoryName(ProjectModel.ProjectPath);
            baseFilePath = Path.Combine(baseFilePath ?? Path.GetFullPath("."), composition.Name + ".avi");
            FilePath = Output.Value.ProcessOutputFilePath(baseFilePath);
        }

        string GetSaveFileFilter()
        {
            var id = OutputPlugins[SelectedOutputPlugin].Item1;
            var supportedExtensions = OutputListModel.GetMetadata(id)?.SupportedFileType ?? "*.*";
            return string.Join("|", supportedExtensions.Split(',').Select(e => e + "|*" + e));
        }

        FrameworkElement? GetSettingView()
        {
            var size = IsOutputVideo ? CompositionSize : (Int32Size?)null;
            var sourceTypes = (IsOutputVideo ? SourceType.Video : SourceType.None) | (HasAudio && IsOutputAudio ? SourceType.Audio : SourceType.None);

            var beginTime = UseItemTimeRange ? BeginTime : CompositionWorkareaBegin;
            var endTime = UseItemTimeRange ? EndTime : CompositionWorkareaEnd;
            return Output.Value.GetOutputSetting(beginTime, endTime - beginTime, FrameRate, size, sourceTypes);
        }

        private void RenderingSettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BeginTime):
                    RenderRangeEndStart = BeginTime + FrameDuration;
                    break;
                case nameof(EndTime):
                    RenderRangeBeginLimit = EndTime - FrameDuration;
                    break;
                case nameof(SelectedOutputPlugin):
                    {
                        var newOutputPluginId = OutputPlugins[SelectedOutputPlugin].Item1;
                        var output = OutputListModel.CreateOutput(newOutputPluginId);
                        if (output == null)
                        {
                            throw new InvalidOperationException();
                        }
                        Output.Dispose();
                        Output = output;
                        HasOutputSetting = OutputListModel.GetMetadata(newOutputPluginId)?.HasSettingView ?? false;
                    }
                    break;
            }
        }
    }
}
