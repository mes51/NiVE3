using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using NiVE3.View.Part;
using NiVE3.View.Primitive;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Bottom)]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [ManualViewModelWireable(nameof(CompositionModel), nameof(BindComposition), nameof(UnbindComposition), WithInitializeProperty = true)]
    [CommandHandling(nameof(BeginEditNameCommand), nameof(ShortcutKeySetting.BeginEditNameGesture))]
    [CommandHandling(nameof(AddSolidCommand), nameof(ShortcutKeySetting.AddSolidGesture), IsGlobal = true)]
    partial class TimelineViewModel : PaneViewModelBase, IDropTarget
    {
        private double frameRate;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public double FrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private double frameDuration;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public double FrameDuration
        {
            get { return frameDuration; }
            set { SetProperty(ref frameDuration, value); }
        }

        private double duration;
        [ManualWire(nameof(CompositionModel), IsOneWay = true)]
        public double Duration
        {
            get { return duration; }
            set { SetProperty(ref duration, value); }
        }

        private double timeBarRange;
        [ManualWire(nameof(CompositionModel))]
        public double TimeBarRange
        {
            get { return timeBarRange; }
            set { SetProperty(ref timeBarRange, value); }
        }

        private double timeBarRangeStart;
        [ManualWire(nameof(CompositionModel))]
        public double TimeBarRangeStart
        {
            get { return timeBarRangeStart; }
            set { SetProperty(ref timeBarRangeStart, value); }
        }

        private double currentTime;
        [ManualWire(nameof(CompositionModel))]
        public double CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private double workareaBegin;
        [ManualWire(nameof(CompositionModel))]
        public double WorkareaBegin
        {
            get { return workareaBegin; }
            set { SetProperty(ref workareaBegin, value); }
        }

        private double workareaEnd;
        [ManualWire(nameof(CompositionModel))]
        public double WorkareaEnd
        {
            get { return workareaEnd; }
            set { SetProperty(ref workareaEnd, value); }
        }

        private double layerNumberColumnWudth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnWidth))]
        public double LayerNumberColumnWidth
        {
            get { return layerNumberColumnWudth; }
            set { SetProperty(ref layerNumberColumnWudth, value); }
        }

        private double layerNameColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNameColumnWidth))]
        public double LayerNameColumnWidth
        {
            get { return layerNameColumnWidth; }
            set { SetProperty(ref layerNameColumnWidth, value); }
        }

        private double layerCommentColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnWidth))]
        public double LayerCommentColumnWidth
        {
            get { return layerCommentColumnWidth; }
            set { SetProperty(ref layerCommentColumnWidth, value); }
        }

        private double layerSwitchColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnWidth))]
        public double LayerSwitchColumnWidth
        {
            get { return layerSwitchColumnWidth; }
            set { SetProperty(ref layerSwitchColumnWidth, value); }
        }

        private double modeColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnWidth))]
        public double ModeColumnWidth
        {
            get { return modeColumnWidth; }
            set { SetProperty(ref modeColumnWidth, value); }
        }

        private double parentLayerColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnWidth))]
        public double ParentLayerColumnWidth
        {
            get { return parentLayerColumnWidth; }
            set { SetProperty(ref parentLayerColumnWidth, value); }
        }

        private bool isAVSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineAVSwitchColumnVisible))]
        public bool IsAVSwitchColumnVisible
        {
            get { return isAVSwitchColumnVisible; }
            set { SetProperty(ref isAVSwitchColumnVisible, value); }
        }

        private bool isTagColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineTagColumnVisible))]
        public bool IsTagColumnVisible
        {
            get { return isTagColumnVisible; }
            set { SetProperty(ref isTagColumnVisible, value); }
        }

        private bool isLayerNumberColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerNumberColumnVisible))]
        public bool IsLayerNumberColumnVisible
        {
            get { return isLayerNumberColumnVisible; }
            set { SetProperty(ref isLayerNumberColumnVisible, value); }
        }

        private bool isLayerCommentColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerCommentColumnVisible))]
        public bool IsLayerCommentColumnVisible
        {
            get { return isLayerCommentColumnVisible; }
            set { SetProperty(ref isLayerCommentColumnVisible, value); }
        }

        private bool isLayerSwitchColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineLayerSwitchColumnVisible))]
        public bool IsLayerSwitchColumnVisible
        {
            get { return isLayerSwitchColumnVisible; }
            set { SetProperty(ref isLayerSwitchColumnVisible, value); }
        }

        private bool isModeColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineModeColumnVisible))]
        public bool IsModeColumnVisible
        {
            get { return isModeColumnVisible; }
            set { SetProperty(ref isModeColumnVisible, value); }
        }

        private bool isParentLayerColumnVisible;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.TimelineParentLayerColumnVisible))]
        public bool IsParentLayerColumnVisible
        {
            get { return isParentLayerColumnVisible; }
            set { SetProperty(ref isParentLayerColumnVisible, value); }
        }

        private CompositionModel? compositionModel;
        public CompositionModel? CompositionModel
        {
            get { return compositionModel; }
            set
            {
                if (compositionModel == value)
                {
                    return;
                }

                if (compositionModel != null)
                {
                    UnbindComposition();
                    Layers = null;
                }
                SetProperty(ref compositionModel, value);
                if (value != null)
                {
                    BindComposition();
                    Layers = value.Layers.CreateViewCollection(m =>
                    {
                        var vm = new LayerViewModel(m, ViewState);
                        vm.LayerSwitchChangeRequest += LayerViewModel_LayerSwitchChangeRequest;
                        vm.BlendModeChangeRequest += Vm_BlendModeChangeRequest;
                        return vm;
                    });
                }
            }
        }

        private double timelineScrollBarMax;
        public double TimelineScrollBarMax
        {
            get { return timelineScrollBarMax; }
            set { SetProperty(ref timelineScrollBarMax, value); }
        }

        private ObservableCollectionView<LayerModel, LayerViewModel>? layers;
        public ObservableCollectionView<LayerModel, LayerViewModel>? Layers
        {
            get { return layers; }
            set { SetProperty(ref layers, value); }
        }

        private ObservableCollection<LayerViewModel> selectedLayers = new ObservableCollection<LayerViewModel>();
        public ObservableCollection<LayerViewModel> SelectedLayers
        {
            get { return selectedLayers; }
            set { SetProperty(ref selectedLayers, value); }
        }

        public ICommand BeginEditNameCommand { get; }

        public ICommand AddSolidCommand { get; }

        ViewStateModel ViewState { get; }

        public TimelineViewModel(ViewStateModel viewState)
        {
            ViewState = viewState;
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Timeline_EmptyTitle);

            WiringModel();

            PropertyChanged += TimelineViewModel_PropertyChanged;

            BeginEditNameCommand = new RequerySuggestedCommand(() => SelectedLayers.First().BeginEditNameCommand.Execute(null), () => SelectedLayers.Count > 0);

            AddSolidCommand = new RequerySuggestedCommand(() =>
            {
                if (CompositionModel == null || Layers == null)
                {
                    return;
                }

                if (selectedLayers.Count > 0)
                {
                    CompositionModel.AddSolid(Layers.IndexOf(SelectedLayers.First()) + 1);
                }
                else
                {
                    CompositionModel.AddSolid(Layers.Count);
                }
            }, () => CompositionModel != null);
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (Layers == null || CompositionModel == null)
            {
                return;
            }

            var target = dropInfo.TargetItem as LayerViewModel;

            switch (dropInfo.Data)
            {
                case IFootageViewModel:
                case IFootageViewModel[]:
                    dropInfo.Effects = DragDropEffects.Copy;
                    if (target != null)
                    {
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    }
                    break;
                case LayerViewModel:
                case ItemDragData<LayerViewModel>:
                    dropInfo.Effects = DragDropEffects.Move;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    break;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (Layers == null || CompositionModel == null)
            {
                return;
            }

            switch (dropInfo.Data)
            {
                case IFootageViewModel footage:
                    CompositionModel.InsertLayers(footage.FootageId, dropInfo.InsertIndex);
                    break;
                case IFootageViewModel[] footages:
                    CompositionModel.InsertLayers(footages.Select(f => f.FootageId).ToArray(), dropInfo.InsertIndex);
                    break;
                case LayerViewModel layer:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Layers.IndexOf(layer) < newIndex)
                        {
                            newIndex--;
                        }
                        CompositionModel.MoveLayer(layer.LayerId, newIndex);
                    }
                    break;
                case ItemDragData<LayerViewModel> itemDragData:
                    {
                        var newIndex = dropInfo.InsertIndex;
                        if (Layers.IndexOf(itemDragData.DragItem) < newIndex)
                        {
                            newIndex--;
                        }
                        CompositionModel.MoveLayers(itemDragData.SelectedItems.Select(l => l.LayerId).ToArray(), itemDragData.DragItem.LayerId, newIndex);
                    }
                    break;
            }
        }

        partial void WiringModel();

        partial void BindComposition();

        partial void UnbindComposition();

        private void TimelineViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CompositionModel):
                    if (CompositionModel == null)
                    {
                        Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Timeline_EmptyTitle);
                        Duration = 0.0;
                        FrameRate = 30.0;
                        FrameDuration = 1.0 / 30.0;
                    }
                    else
                    {
                        Title = CompositionModel.Name;
                    }
                    break;
                case nameof(Duration):
                case nameof(TimeBarRange):
                    TimelineScrollBarMax = Duration - TimeBarRange;
                    break;
            }
        }

        private void LayerViewModel_LayerSwitchChangeRequest(object? sender, LayerSwitchEventArgs e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            var targetLayers = new List<LayerViewModel>();
            if (SelectedLayers.Count == 0 || !SelectedLayers.Contains(layerViewModel))
            {
                targetLayers.Add(layerViewModel);
            }
            else
            {
                targetLayers.AddRange(SelectedLayers);
            }

            CompositionModel.ChangeLayerSwitches(targetLayers.Select(l => l.LayerId).ToArray(), e.SwitchName, e.Value);
        }

        private void Vm_BlendModeChangeRequest(object? sender, BlendModeEventArgs e)
        {
            if (sender is not LayerViewModel layerViewModel || CompositionModel == null || !(Layers?.Contains(layerViewModel) ?? false))
            {
                return;
            }

            var targetLayers = new List<LayerViewModel>();
            if (SelectedLayers.Count == 0 || !SelectedLayers.Contains(layerViewModel))
            {
                targetLayers.Add(layerViewModel);
            }
            else
            {
                targetLayers.AddRange(SelectedLayers);
            }

            CompositionModel.ChangeBlendModes(targetLayers.Select(l => l.LayerId).ToArray(), e.BlendMode);
        }
    }
}
