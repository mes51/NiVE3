using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.View.Dock;
using System.Windows.Input;
using NiVE3.UI.Command;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using NiVE3.View.Resource;
using Prism.Commands;
using Prism.Dialogs;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Bottom)]
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class RenderQueueViewModel : SingletonePaneViewModelBase
    {
        [ReactiveProperty]
        public partial ObservableCollectionView<RenderQueueItemModel, RenderQueueItemViewModel> Items { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public partial double Progress { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public partial int RenderedFrameCount { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public partial int TotalFrameCount { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public partial bool IsRendering { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueModel))]
        public partial bool IsPaused { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public partial bool IsAborting { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(RenderQueueModel), IsOneWay = true)]
        public partial TimeSpan Eta { get; set; }

        [ReactiveProperty]
        public partial TimeSpan CurrentEta { get; set; }

        public ICommand RenderStartCommand { get; }

        public ICommand AbortCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand DuplicateCommand { get; }

        RenderQueueModel RenderQueueModel { get; }

        Task? CalcEtaTask { get; set; }

        long LastEtaUpdateTimestamp { get; set; }

        public RenderQueueViewModel(RenderQueueModel renderQueueModel, IDialogService dialogService)
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

            AbortCommand = new DelegateCommand(() => RenderQueueModel.AbortRendering(), () => IsRendering).ObservesProperty(() => IsRendering);

            DeleteCommand = new DelegateCommand<RenderQueueItemViewModel>(vm =>
            {
                if (vm.IsSelected)
                {
                    RenderQueueModel.RemoveQueues([..Items.Where(q => q.IsSelected).Select(q => q.QueueId)]);
                }
                else
                {
                    RenderQueueModel.RemoveQueues([vm.QueueId]);
                }
            }, _ => !IsRendering).ObservesProperty(() => IsRendering);

            DuplicateCommand = new DelegateCommand<RenderQueueItemViewModel>(vm =>
            {
                if (vm.IsSelected)
                {
                    RenderQueueModel.DuplicateQueue([.. Items.Where(q => q.IsSelected).Select(q => q.QueueId)]);
                }
                else
                {
                    RenderQueueModel.DuplicateQueue([vm.QueueId]);
                }
            }, _ => !IsRendering).ObservesProperty(() => IsRendering);

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
