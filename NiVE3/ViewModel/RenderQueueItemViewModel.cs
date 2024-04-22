using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Prism.Commands;
using Microsoft.Win32;
using NiVE3.UI.Command;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class RenderQueueItemViewModel : BindableBase
    {
        private string filePath = "";
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public string FilePath
        {
            get { return filePath; }
            set { SetProperty(ref filePath, value); }
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

        private double compositionDuration;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double CompositionDuration
        {
            get { return compositionDuration; }
            set { SetProperty(ref compositionDuration, value); }
        }

        private double frameDuration;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private double frameRate;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private bool useRenderQueueItemTimeRange;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public bool UseRenderQueueItemTimeRange
        {
            get { return useRenderQueueItemTimeRange; }
            set { SetProperty(ref useRenderQueueItemTimeRange, value); }
        }

        private string compositionName = "";
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public string CompositionName
        {
            get { return compositionName; }
            set { SetProperty(ref compositionName, value); }
        }

        private RenderQueueItemState state;
        [NeedWire(nameof(RenderQueueItemModel))]
        public RenderQueueItemState State
        {
            get { return state; }
            set { SetProperty(ref state, value); }
        }

        private bool hasOutputSetting;
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
        public bool HasOutputSetting
        {
            get { return hasOutputSetting; }
            set { SetProperty(ref hasOutputSetting, value); }
        }

        private ObservableCollection<Tuple<Guid, string>> outputPlugins = [];
        [NeedWire(nameof(RenderQueueItemModel), IsOneWay = true)]
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

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
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

        public ICommand ChangeSaveFilePathCommand { get; }

        public ICommand OpenOutputSettingCommand { get; }

        RenderQueueItemModel RenderQueueItemModel { get; }

        public RenderQueueItemViewModel(RenderQueueItemModel renderQueueItemModel)
        {
            RenderQueueItemModel = renderQueueItemModel;

            WiringModel();

            ChangeSaveFilePathCommand = new RequerySuggestedCommand(() =>
            {
                var save = new SaveFileDialog();
                save.Filter = RenderQueueItemModel.GetSaveFileFilter();
                if (save.ShowDialog() ?? false)
                {
                    RenderQueueItemModel.ChangeFilePath(save.FileName);
                }
            }, () => State == RenderQueueItemState.Ready || State == RenderQueueItemState.NotReady);

            OpenOutputSettingCommand = new RequerySuggestedCommand(() =>
            {

            }, () => (State == RenderQueueItemState.Ready || State == RenderQueueItemState.NotReady) && HasOutputSetting);

            PropertyChanged += RenderQueueItemViewModel_PropertyChanged;

            RenderRangeBeginLimit = EndTime - FrameDuration;
        }

        private void RenderQueueItemViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedOutputPlugin):
                    RenderQueueItemModel.ChangeOutputPlugin(OutputPlugins[SelectedOutputPlugin].Item1);
                    break;
                case nameof(EndTime):
                    RenderRangeBeginLimit = EndTime - FrameDuration;
                    break;
                case nameof(BeginTime):
                    RenderRangeEndStart = BeginTime + FrameDuration;
                    break;
                case nameof(FrameDuration):
                    RenderRangeBeginLimit = EndTime - FrameDuration;
                    RenderRangeEndStart = BeginTime + FrameDuration;
                    break;
                case nameof(UseRenderQueueItemTimeRange) when !UseRenderQueueItemTimeRange:
                    BeginTime = 0;
                    EndTime = CompositionDuration;
                    break;
            }
        }

        partial void WiringModel();
    }
}
