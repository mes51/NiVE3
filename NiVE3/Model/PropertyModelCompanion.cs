using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property;
using NiVE3.Mvvm;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using NiVE3.Plugin.ValueObject;
using System.Collections.Specialized;

namespace NiVE3.Model
{
    interface IPropertyModel : IPropertyObject
    {
        string Name { get; }

        Time SourceStartPoint { get; }

        ObservableCollection<KeyFrame>? KeyFrames { get; }

        ObservableCollection<IPropertyModel>? Children { get; }

        PropertyBase Property { get; }

        IPropertyModel? ParentPropertyModel { get; }

        Int128 ObjectId { get; }

        bool ParentLayerIsLock { get; }

        event EventHandler<EventArgs>? ValueUpdated;

        event EventHandler<EventArgs>? ValueCommited;

        PropertyControlBase CreateControl(IPropertyViewModel viewModel);

        PropertyViewState CreateState(IPropertyViewModel propertyViewModel);

        bool ClearExpressionError();

        void UpdateValueByCompositionStateChanged();

        void UpdateValueByLayerStateChanged();

        void UpdateValueByReplacedEffectId(Dictionary<Guid, Guid> effectIdMap);

        void UpdateValueByReplacedMaskId(Dictionary<Guid, Guid> maskIdMap);

        void UpdateValueByReplacedLayerId(Dictionary<Guid, Guid> layerIdMap);

        bool HasCompositionDependProperty();

        bool HasKeyFrames();

        bool IsChangeableByTime();

        PropertyData SaveData();

        void LoadData(PropertyData data);

        void CoerceValues();

        void PasteProperty(PropertyData data);

        object? GetValue(Time time, Time globalTime);

        void OverwriteProperty(PropertyData data);

        bool IsAlive(IPropertyModel child);
    }

    class CompositionViewModelProxy : WeakPropertyChangedBindingBase, ICompositionViewModel
    {
        public IReadOnlyCollection<ILayerViewModel> LayerViewModels { get; }

        public CompositionViewModelProxy(CompositionModel composition)
        {
            LayerViewModels = composition.Layers.CreateViewCollection(l => new LayerViewModelProxy(l));
        }
    }

    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true, KeepStrongReferenceBinder = true)]
    partial class LayerViewModelProxy : WeakPropertyChangedBindingBase, ILayerViewModel
    {
        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial Guid LayerId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial bool IsEnable3D { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial string SourceName { get; set; } = "";

        [ReactiveProperty]
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public partial SourceType SourceType { get; set; }

        public IReadOnlyCollection<IEffectViewModel> EffectViewModels { get; }

        public IReadOnlyCollection<IMaskViewModel> MaskViewModels { get; }

        LayerModel LayerModel { get; }

        public LayerViewModelProxy(LayerModel layerModel)
        {
            LayerModel = layerModel;

            EffectViewModels = LayerModel.Effects.CreateViewCollection(e => new EffectViewModelProxy(e));
            MaskViewModels = LayerModel.Masks.CreateViewCollection(m => new MaskViewModelProxy(m));

            WiringModel();
        }

        partial void WiringModel();
    }

    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class EffectViewModelProxy : WeakPropertyChangedBindingBase, IEffectViewModel
    {
        [ReactiveProperty]
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public partial Guid EffectId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        EffectModel EffectModel { get; }

        public EffectViewModelProxy(EffectModel effectModel)
        {
            EffectModel = effectModel;

            WiringModel();
        }

        partial void WiringModel();
    }

    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class MaskViewModelProxy : WeakPropertyChangedBindingBase, IMaskViewModel
    {
        [ReactiveProperty]
        [NeedWire(nameof(MaskModel), IsOneWay = true)]
        public partial Guid MaskId { get; set; }

        [ReactiveProperty]
        [NeedWire(nameof(MaskModel), IsOneWay = true)]
        public partial string Name { get; set; } = "";

        MaskModel MaskModel { get; }

        public MaskViewModelProxy(MaskModel maskModel)
        {
            MaskModel = maskModel;

            WiringModel();
        }

        partial void WiringModel();
    }
}
