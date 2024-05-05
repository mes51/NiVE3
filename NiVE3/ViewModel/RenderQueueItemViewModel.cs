using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;
using System.Windows.Input;
using NiVE3.UI.Command;
using Microsoft.Win32;
using System.IO;
using NiVE3.View.Dialog;
using NiVE3.ViewModel.Dialog;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class RenderQueueItemViewModel : BindableBase
    {
        private Guid queueId;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public Guid QueueId
        {
            get { return queueId; }
            set { SetProperty(ref queueId, value); }
        }

        private bool isRenderSelected;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public bool IsRenderSelected
        {
            get { return isRenderSelected; }
            set { SetProperty(ref isRenderSelected, value); }
        }

        private string filePath = "";
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
        }

        private RenderRangeType renderRangeType;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public RenderRangeType RenderRangeType
        {
            get { return renderRangeType; }
            set { SetProperty(ref renderRangeType, value); }
        }

        private double beginTime;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double BeginTime
        {
            get { return beginTime; }
            set { SetProperty(ref beginTime, value); }
        }

        private double endTime;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double EndTime
        {
            get { return endTime; }
            set { SetProperty(ref endTime, value); }
        }

        private double fixedBeginTime;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double FixedBeginTime
        {
            get { return fixedBeginTime; }
            set { SetProperty(ref fixedBeginTime, value); }
        }

        private double fixedEndTime;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double FixedEndTime
        {
            get { return fixedEndTime; }
            set { SetProperty(ref fixedEndTime, value); }
        }

        private bool isOutputVideo;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public bool IsOutputVideo
        {
            get { return isOutputVideo; }
            set { SetProperty(ref isOutputVideo, value); }
        }

        private bool isOutputAudio;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public bool IsOutputAudio
        {
            get { return isOutputAudio; }
            set { SetProperty(ref isOutputAudio, value); }
        }

        private Guid outputPluginId;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public Guid OutputPluginId
        {
            get { return outputPluginId; }
            set { SetProperty(ref outputPluginId, value); }
        }

        private string outputPluginName = "";
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public string OutputPluginName
        {
            get { return outputPluginName; }
            set { SetProperty(ref outputPluginName, value); }
        }

        private ExportLifetimeContext<IOutput>? output;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public ExportLifetimeContext<IOutput>? Output
        {
            get { return output; }
            set { SetProperty(ref output, value); }
        }

        private RenderQueueItemState state;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public RenderQueueItemState State
        {
            get { return state; }
            set { SetProperty(ref state, value); }
        }

        private string compositionName = "";
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public string CompositionName
        {
            get { return compositionName; }
            set { SetProperty(ref compositionName, value); }
        }

        private double compositionWorkareaBegin;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double CompositionWorkareaBegin
        {
            get { return compositionWorkareaBegin; }
            set { SetProperty(ref compositionWorkareaBegin, value); }
        }

        private double compositionWorkareaEnd;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double CompositionWorkareaEnd
        {
            get { return compositionWorkareaEnd; }
            set { SetProperty(ref compositionWorkareaEnd, value); }
        }

        private double compositionDuration;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double CompositionDuration
        {
            get { return compositionDuration; }
            set { SetProperty(ref compositionDuration, value); }
        }

        private double compositionFrameRate;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double CompositionFrameRate
        {
            get { return compositionFrameRate; }
            set { SetProperty(ref compositionFrameRate, value); }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        public ICommand ChangeFilePathCommand { get; }

        public ICommand ChangeSettingCommand { get; }

        RenderQueueItemModel RenderQueueItemModel { get; }

        IDialogService DialogService { get; }

        public RenderQueueItemViewModel(RenderQueueItemModel renderQueueItemModel, IDialogService dialogService)
        {
            RenderQueueItemModel = renderQueueItemModel;
            DialogService = dialogService;

            WiringModel();

            ChangeFilePathCommand = new RequerySuggestedCommand(() =>
            {
                var save = new SaveFileDialog();
                save.Filter = RenderQueueItemModel.GetSaveFileFilter();
                save.InitialDirectory = Path.GetDirectoryName(FilePath);
                save.FileName = Path.GetFileName(FilePath);
                if (save.ShowDialog() ?? false)
                {
                    RenderQueueItemModel.ChangeFilePath(save.FileName);
                }
            }, () => State == RenderQueueItemState.NotReady || State == RenderQueueItemState.Ready);

            ChangeSettingCommand = new RequerySuggestedCommand(() =>
            {
                var prevSetting = (object?)null;
                var settingParams = new DialogParameters
                {
                    { RenderSettingViewModel.CompositionParameterName, RenderQueueItemModel.CompositionModel },
                    { nameof(RenderSettingViewModel.FilePath), FilePath },
                    { nameof(RenderSettingViewModel.RenderRangeType), RenderRangeType },
                    { nameof(RenderSettingViewModel.BeginTime), BeginTime },
                    { nameof(RenderSettingViewModel.EndTime), EndTime },
                    { nameof(RenderSettingViewModel.IsOutputVideo), IsOutputVideo },
                    { nameof(RenderSettingViewModel.IsOutputAudio), IsOutputAudio },
                    { nameof(RenderSettingViewModel.Mode), RenderSettingMode.ChangeSetting },
                };
                if (Output != null)
                {
                    prevSetting = Output.Value.SaveData();
                    settingParams.Add(RenderSettingViewModel.OutputParameterName, Output);
                }
                IDialogResult? settingResult = null;
                DialogService.ShowDialog(nameof(RenderSettingView), settingParams, r => settingResult = r);

                if (settingResult?.Result == ButtonResult.OK)
                {
                    RenderQueueItemModel.UpdateSetting(
                        settingResult.Parameters.GetValue<string>(nameof(RenderSettingViewModel.FilePath)),
                        settingResult.Parameters.GetValue<RenderRangeType>(nameof(RenderSettingViewModel.RenderRangeType)),
                        settingResult.Parameters.GetValue<double>(nameof(RenderSettingViewModel.BeginTime)),
                        settingResult.Parameters.GetValue<double>(nameof(RenderSettingViewModel.EndTime)),
                        settingResult.Parameters.GetValue<bool>(nameof(RenderSettingViewModel.IsOutputVideo)),
                        settingResult.Parameters.GetValue<bool>(nameof(RenderSettingViewModel.IsOutputAudio)),
                        prevSetting,
                        settingResult.Parameters.GetValue<ExportLifetimeContext<IOutput>>(RenderSettingViewModel.OutputParameterName)
                    );
                }
            }, () => State == RenderQueueItemState.NotReady || State == RenderQueueItemState.Ready);
        }

        partial void WiringModel();
    }
}
