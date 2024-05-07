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
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using NiVE3.View.Resource;

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

        private int renderedFrameCount;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public int RenderedFrameCount
        {
            get { return renderedFrameCount; }
            set { SetProperty(ref renderedFrameCount, value); }
        }

        private int totalFrameCount;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public int TotalFrameCount
        {
            get { return totalFrameCount; }
            set { SetProperty(ref totalFrameCount, value); }
        }

        private bool isRendering;
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public bool IsRendering
        {
            get { return isRendering; }
            set { SetProperty(ref isRendering, value); }
        }

        private bool isPaused;
        [NeedWire(nameof(RenderQueueModel))]
        public bool IsPaused
        {
            get { return isPaused; }
            set { SetProperty(ref isPaused, value); }
        }

        private bool isAborting;
        [NeedWire(nameof(RenderQueueModel))]
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

        private TimeSpan currentEta;
        public TimeSpan CurrentEta
        {
            get { return currentEta; }
            set { SetProperty(ref currentEta, value); }
        }

        public ICommand RenderStartCommand { get; }

        public ICommand AbortCommand { get; }

        public ICommand DeleteCommand { get; }

        RenderQueueModel RenderQueueModel { get; }

        Task? CalcEtaTask { get; set; }

        long LastEtaUpdateTimestamp { get; set; }

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
                if (!IsRendering)
                {
                    if (RenderQueueModel.HasSameOutputFilePathQueue())
                    {
                        var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_ConfirmRenderOverwriteByQueueingItem_Title);
                        var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_ConfirmRenderOverwriteByQueueingItem_Text);
                        if (MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                    RenderQueueModel.StartRender();
                }
                else
                {
                    IsPaused = !IsPaused;
                }
            }, () => (Items.Any(i => i.State == RenderQueueItemState.Ready) || IsRendering) && !IsAborting);

            AbortCommand = new RequerySuggestedCommand(() => IsAborting = true, () => IsRendering);

            DeleteCommand = new RequerySuggestedCommand<RenderQueueItemViewModel>(vm =>
            {
                if (vm.IsSelected)
                {
                    RenderQueueModel.RemoveQueues([..Items.Where(q => q.IsSelected).Select(q => q.QueueId)]);
                }
                else
                {
                    RenderQueueModel.RemoveQueue(vm.QueueId);
                }
            }, _ => !IsRendering);

            renderQueueModel.RenderQueueItemAdded += RenderQueueModel_RenderQueueItemAdded;
            PropertyChanged += RenderQueueViewModel_PropertyChanged;
        }

        void StartCalcEtaTask()
        {
            var dispatcher = Application.Current.Dispatcher;
            CalcEtaTask = Task.Run(() =>
            {
                while (IsRendering && !IsPaused)
                {
                    dispatcher.Invoke(() =>
                    {
                        if (LastEtaUpdateTimestamp < long.MaxValue)
                        {
                            CurrentEta = new TimeSpan(Math.Max((Eta - Stopwatch.GetElapsedTime(LastEtaUpdateTimestamp)).Ticks, 0));
                        }
                    });
                    Thread.Sleep(1000);
                }
            });
        }

        partial void WiringModel();

        private void RenderQueueModel_RenderQueueItemAdded(object? sender, EventArgs e)
        {
            OpenPane();
        }

        private void RenderQueueViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IsRendering) when IsRendering:
                    LastEtaUpdateTimestamp = long.MaxValue;
                    StartCalcEtaTask();
                    break;
                case nameof(IsPaused) when IsRendering:
                    LastEtaUpdateTimestamp = long.MaxValue;
                    StartCalcEtaTask();
                    break;
                case nameof(Eta):
                    CurrentEta = Eta;
                    LastEtaUpdateTimestamp = Stopwatch.GetTimestamp();
                    break;
            }
        }
    }
}
