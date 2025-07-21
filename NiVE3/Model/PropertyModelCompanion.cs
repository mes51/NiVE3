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

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true, KeepStrongReferenceBinder = true)]
    partial class LayerViewModelProxy : WeakPropertyChangedBindingBase, ILayerViewModel
    {
        private Guid layerId;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public Guid LayerId
        {
            get { return layerId; }
            set { SetProperty(ref layerId, value); }
        }

        private bool isEnable3D;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public bool IsEnable3D
        {
            get { return isEnable3D; }
            set { SetProperty(ref isEnable3D, value); }
        }

        private string name = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private string sourceName = "";
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public string SourceName
        {
            get { return sourceName; }
            set { SetProperty(ref sourceName, value); }
        }

        private SourceType sourceType;
        [NeedWire(nameof(LayerModel), IsOneWay = true)]
        public SourceType SourceType
        {
            get { return sourceType; }
            set { SetProperty(ref sourceType, value); }
        }

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

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class EffectViewModelProxy : WeakPropertyChangedBindingBase, IEffectViewModel
    {
        private Guid effectId;
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public Guid EffectId
        {
            get { return effectId; }
            set { SetProperty(ref effectId, value); }
        }

        private string name = "";
        [NeedWire(nameof(EffectModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

#pragma warning disable IDE0052 // 読み取られていないプライベート メンバーを削除
        EffectModel EffectModel { get; }
#pragma warning restore IDE0052 // 読み取られていないプライベート メンバーを削除

        public EffectViewModelProxy(EffectModel effectModel)
        {
            EffectModel = effectModel;

            WiringModel();
        }

        partial void WiringModel();
    }

    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class MaskViewModelProxy : WeakPropertyChangedBindingBase, IMaskViewModel
    {
        private Guid maskId;
        [NeedWire(nameof(MaskModel), IsOneWay = true)]
        public Guid MaskId
        {
            get { return maskId; }
            set { SetProperty(ref maskId, value); }
        }

        private string name = "";
        [NeedWire(nameof(MaskModel), IsOneWay = true)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

#pragma warning disable IDE0052 // 読み取られていないプライベート メンバーを削除
        MaskModel MaskModel { get; }
#pragma warning restore IDE0052 // 読み取られていないプライベート メンバーを削除

        public MaskViewModelProxy(MaskModel maskModel)
        {
            MaskModel = maskModel;

            WiringModel();
        }

        partial void WiringModel();
    }
}
