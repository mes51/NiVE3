using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Dock;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Document)]
    [ViewModelWireable(nameof(WiringModel))]
    partial class PreviewViewModel : PaneViewModelBase
    {
        private bool isFootage;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public bool IsFootage
        {
            get { return isFootage; }
            set { SetProperty(ref isFootage, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        private double duration;
        [NeedWire(nameof(PreviewModel), IsOneWay = true)]
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double currentTime;
        [NeedWire(nameof(PreviewModel))]
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private double timeBarRange;
        public double TimeBarRange
        {
            get { return timeBarRange; }
            set { SetProperty(ref timeBarRange, value); }
        }

        private double timeBarRangeStart;
        public double TimeBarRangeStart
        {
            get { return timeBarRangeStart; }
            set { SetProperty(ref timeBarRangeStart, value); }
        }

        PreviewModel PreviewModel { get; }

        public PreviewViewModel(PreviewModel previewModel)
        {
            Title = "プレビュー";

            PreviewModel = previewModel;
            IsFootage = previewModel.IsFootage;
            SourceType = previewModel.SourceType;
            TimeBarRange = previewModel.Duration;
            Duration = previewModel.Duration;

            WiringModel();
        }

        partial void WiringModel();
    }
}
