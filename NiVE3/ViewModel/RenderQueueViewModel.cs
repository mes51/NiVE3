using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.UI.Command;
using NiVE3.View.Dock;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Bottom)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class RenderQueueViewModel : SingletonePaneViewModelBase
    {
        private bool isRendering;
        public bool IsRendering
        {
            get { return isRendering; }
            set { SetProperty(ref isRendering, value); }
        }

        private ObservableCollectionView<RenderQueueItemModel, RenderQueueItemViewModel> queue;
        public ObservableCollectionView<RenderQueueItemModel, RenderQueueItemViewModel> Queue
        {
            get { return queue; }
            set { SetProperty(ref queue, value); }
        }

        public ICommand StartRenderCommand { get; }

        public ICommand StopRenderCommand { get; }

        RenderQueueModel RenderQueueModel { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public RenderQueueViewModel(RenderQueueModel renderQueueModel)
#pragma warning restore CS8618
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.RenderQueueView_Title);
            Visibility = Visibility.Collapsed;

            RenderQueueModel = renderQueueModel;
            Queue = renderQueueModel.Queue.CreateViewCollection(m => new RenderQueueItemViewModel(m));

            StartRenderCommand = new RequerySuggestedCommand(() => { }, () => Queue.Any(q => q.State == RenderQueueItemState.Ready));

            StopRenderCommand = new RequerySuggestedCommand(() => { }, () => IsRendering);
        }

        partial void WiringModel();
    }
}
