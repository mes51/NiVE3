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
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

        PreviewModel PreviewModel { get; }

        public PreviewViewModel(PreviewModel previewModel)
        {
            Title = "プレビュー";

            PreviewModel = previewModel;
            IsFootage = previewModel.IsFootage;
            SourceType = previewModel.SourceType;

            WiringModel();
        }

        partial void WiringModel();
    }
}
