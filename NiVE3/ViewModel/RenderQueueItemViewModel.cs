using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using Prism.Mvvm;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using NiVE3.View.Dialog;
using NiVE3.ViewModel.Dialog;
using Prism.Commands;
using Prism.Dialogs;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ViewModel
{
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class RenderQueueItemViewModel : BindableBase
    {
        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Guid QueueId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel))]
        public partial bool IsRenderSelected { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial string FilePath { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial RenderRangeType RenderRangeType { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Time BeginTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Time EndTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Time FixedBeginTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Time FixedEndTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial bool IsOutputVideo { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial bool IsOutputAudio { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Guid OutputPluginId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial string OutputPluginName { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial ExportLifetimeContext<IOutput>? Output { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial RenderQueueItemState State { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial TimeSpan RenderingTime { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial string CompositionName { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Time CompositionWorkareaBegin { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Time CompositionWorkareaEnd { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial Time CompositionDuration { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public partial double CompositionFrameRate { get; set; }

        [ReactiveProperty]
        public partial bool IsSelected { get; set; }

        [ReactiveProperty]
        public partial bool IsExpanded { get; set; }

        public ICommand ChangeFilePathCommand { get; }

        public ICommand ChangeSettingCommand { get; }

        RenderQueueItemModel RenderQueueItemModel { get; }

        IDialogService DialogService { get; }

        public RenderQueueItemViewModel(RenderQueueItemModel renderQueueItemModel, IDialogService dialogService)
        {
            RenderQueueItemModel = renderQueueItemModel;
            DialogService = dialogService;

            WiringModel();

            ChangeFilePathCommand = new DelegateCommand(() =>
            {
                var save = new SaveFileDialog
                {
                    Filter = RenderQueueItemModel.GetSaveFileFilter(),
                    InitialDirectory = Path.GetDirectoryName(FilePath),
                    FileName = Path.GetFileName(FilePath)
                };
                if (save.ShowDialog() ?? false)
                {
                    RenderQueueItemModel.ChangeFilePath(save.FileName);
                }
            }, () => State == RenderQueueItemState.NotReady || State == RenderQueueItemState.Ready).ObservesProperty(() => State);

            ChangeSettingCommand = new DelegateCommand(() =>
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
                    prevSetting = Output.Value.SaveSetting();
                    settingParams.Add(RenderSettingViewModel.OutputParameterName, Output);
                }
                IDialogResult? settingResult = null;
                DialogService.ShowDialog(nameof(RenderSettingView), settingParams, r => settingResult = r);

                if (settingResult?.Result == ButtonResult.OK)
                {
                    RenderQueueItemModel.UpdateSetting(
                        settingResult.Parameters.GetValue<string>(nameof(RenderSettingViewModel.FilePath)),
                        settingResult.Parameters.GetValue<RenderRangeType>(nameof(RenderSettingViewModel.RenderRangeType)),
                        settingResult.Parameters.GetValue<Time>(nameof(RenderSettingViewModel.BeginTime)),
                        settingResult.Parameters.GetValue<Time>(nameof(RenderSettingViewModel.EndTime)),
                        settingResult.Parameters.GetValue<bool>(nameof(RenderSettingViewModel.IsOutputVideo)),
                        settingResult.Parameters.GetValue<bool>(nameof(RenderSettingViewModel.IsOutputAudio)),
                        prevSetting,
                        settingResult.Parameters.GetValue<ExportLifetimeContext<IOutput>>(RenderSettingViewModel.OutputParameterName)
                    );
                }
            }, () => State == RenderQueueItemState.NotReady || State == RenderQueueItemState.Ready).ObservesProperty(() => State);
        }

        partial void WiringModel();
    }
}
