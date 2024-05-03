using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.View.Dock;
using Prism.Mvvm;
using System.Windows.Input;
using NiVE3.UI.Command;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Bottom)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class RenderQueueViewModel : SingletonePaneViewModelBase
    {
        private ObservableCollectionView<RenderQueueItemModel, RenderQueueItemViewModel> items;
        public ObservableCollectionView<RenderQueueItemModel, RenderQueueItemViewModel> Items
        {
            get { return items; }
            set { SetProperty(ref items, value); }
        }

        private double progress;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public double Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        private bool isRendering;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public bool IsRendering
        {
            get { return isRendering; }
            set { SetProperty(ref isRendering, value); }
        }

        private bool isPaused;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public bool IsPaused
        {
            get { return isPaused; }
            set { SetProperty(ref isPaused, value); }
        }

        private bool isAborting;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public bool IsAborting
        {
            get { return isAborting; }
            set { SetProperty(ref isAborting, value); }
        }

        private TimeSpan eta;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public TimeSpan Eta
        {
            get { return eta; }
            set { SetProperty(ref eta, value); }
        }

        public ICommand RenderStartCommand { get; }

        public ICommand AbortCommand { get; }

        public ICommand DeleteCommand { get; }

        RenderQueueModel RenderQueueModel { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public RenderQueueViewModel(RenderQueueModel renderQueueModel, IDialogService dialogService)
#pragma warning restore CS8618
        {
            Title = "レンダーキュー";
            Visibility = Visibility.Hidden;

            RenderQueueModel = renderQueueModel;
            Items = renderQueueModel.Items.CreateViewCollection(m => new RenderQueueItemViewModel(m, dialogService));

            WiringModel();

            RenderStartCommand = new RequerySuggestedCommand(() =>
            {
                IsRendering = true;
            }, () => Items.Any(i => i.State == RenderQueueItemState.Ready) && !IsAborting);

            AbortCommand = new RequerySuggestedCommand(() => IsAborting = true, () => IsRendering);

            DeleteCommand = new RequerySuggestedCommand(() =>
            {

            }, () => !IsRendering);
        }

        partial void WiringModel();
    }
}
