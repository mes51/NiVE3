using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU.IR;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.View.Dock;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    [ManualViewModelWireable(nameof(Composition), nameof(BindComposition), nameof(UnbindComposition), WithInitializeProperty = true)]
    [PaneLocation(PaneLocation.Left2Center)]
    partial class LayerPropertyControllerViewModel : SingletonePaneViewModelBase
    {
        private double layerNameColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.PropertyControllerLayerNameColumnWidth))]
        public double LayerNameColumnWidth
        {
            get { return layerNameColumnWidth; }
            set { SetProperty(ref layerNameColumnWidth, value); }
        }

        private double layerSwitchColumnWidth;
        [NeedWire(nameof(ViewState), BindTargetName = nameof(ViewStateModel.PropertyControllerLayerSwitchColumnWidth))]
        public double LayerSwitchColumnWidth
        {
            get { return layerSwitchColumnWidth; }
            set { SetProperty(ref layerSwitchColumnWidth, value); }
        }

        private Guid? lastSelectedLayerId;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public Guid? LastSelectedLayerId
        {
            get { return lastSelectedLayerId; }
            set { SetProperty(ref lastSelectedLayerId, value); }
        }

        private Guid? currentEditingCompositionId;
        [NeedWire(nameof(ViewState), IsOneWay = true)]
        public Guid? CurrentEditingCompositionId
        {
            get { return currentEditingCompositionId; }
            set { SetProperty(ref currentEditingCompositionId, value); }
        }

        private double timeBarRange;
        [ManualWire(nameof(Composition))]
        public double TimeBarRange
        {
            get { return timeBarRange; }
            set { SetProperty(ref timeBarRange, value); }
        }

        private double timeBarRangeStart;
        [ManualWire(nameof(Composition))]
        public double TimeBarRangeStart
        {
            get { return timeBarRangeStart; }
            set { SetProperty(ref timeBarRangeStart, value); }
        }
        private double frameRate;
        [ManualWire(nameof(Composition), BindTargetName = nameof(CompositionModel.FrameRate), IsOneWay = true)]
        public double CompositionFrameRate
        {
            get { return frameRate; }
            set { SetProperty(ref frameRate, value); }
        }

        private LayerViewModel? targetLayer;
        public LayerViewModel? TargetLayer
        {
            get { return targetLayer; }
            set { SetProperty(ref targetLayer, value); }
        }

        ProjectModel ProjectModel { get; }

        ViewStateModel ViewState { get; }

        private CompositionModel? compositionModel;
        public CompositionModel? Composition
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
                }
                SetProperty(ref compositionModel, value);
                if (value != null)
                {
                    BindComposition();
                }
            }
        }

        public LayerPropertyControllerViewModel(ProjectModel project, ViewStateModel viewState)
        {
            ProjectModel = project;
            ViewState = viewState;
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.LayerPropertyControllerView_Title_Empty);

            WiringModel();

            PropertyChanged += LayerPropertyEditorViewModel_PropertyChanged;
        }

        void UpdateTargetLayerAndTitle()
        {
            if (Composition != null)
            {
                var trackMatteCollectionView = Composition.Layers.CreateViewCollection(m => new LayerModelProxy(m));
                var parentLayerCollectionView = Composition.Layers.CreateViewCollection(m => new LayerModelProxy(m));
                var layerModel = Composition.Layers.FirstOrDefault(l => l.LayerId == LastSelectedLayerId);
                if (layerModel != null)
                {
                    TargetLayer = new LayerViewModel(layerModel, ViewState, trackMatteCollectionView, parentLayerCollectionView);
                    TargetLayer.LayerSwitchChangeRequest += LayerViewModel_LayerSwitchChangeRequest;
                }
                else
                {
                    TargetLayer = null;
                }
            }
            else
            {
                TargetLayer = null;
            }

            if (TargetLayer != null)
            {
                var format = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.LayerPropertyControllerView_Title);
                Title = string.Format(format, TargetLayer.Name);
            }
            else
            {
                Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.LayerPropertyControllerView_Title_Empty);
            }
        }

        partial void WiringModel();

        partial void BindComposition();

        partial void UnbindComposition();

        private void LayerPropertyEditorViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentEditingCompositionId):
                    if (CurrentEditingCompositionId != Composition?.CompositionId)
                    {
                        Composition = ProjectModel.CompositionModels.FirstOrDefault(c => c.CompositionId == CurrentEditingCompositionId);
                        UpdateTargetLayerAndTitle();
                    }
                    break;
                case nameof(LastSelectedLayerId):
                    UpdateTargetLayerAndTitle();
                    break;
            }
        }

        private void LayerViewModel_LayerSwitchChangeRequest(object? sender, LayerSwitchEventArgs e)
        {
            if (sender is not LayerViewModel layerViewModel || Composition == null)
            {
                return;
            }

            Composition.ChangeLayerSwitches([layerViewModel.LayerId], e.SwitchName, e.Value);
        }
    }
}
