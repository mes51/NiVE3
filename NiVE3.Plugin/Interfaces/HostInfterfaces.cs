using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces
{
    public interface IAcceleratorObject
    {
        /// <summary>
        /// 現在使用しているGPUのデバイスを取得します
        /// </summary>
        GraphicsDevice CurrentDevice { get; }
    }

    public interface ICompositionObject
    {
        /// <summary>
        /// レイヤーを表す識別子の一覧を取得します
        /// </summary>
        IReadOnlyCollection<LayerInfo> LayerIdentifiers { get; }

        /// <summary>
        /// レイヤーの識別子からレイヤーを取得します
        /// </summary>
        /// <param name="layerIdentifier">レイヤーの識別子</param>
        /// <returns>レイヤーを表すILayerObject。一致するレイヤーがなかった場合はnull</returns>
        ILayerObject? GetLayer(Guid layerIdentifier);
    }

    public interface ILayerObject
    {
        public const string TransformAnchorPointId = nameof(TransformAnchorPointId);

        public const string TransformPositionId = nameof(TransformPositionId);

        public const string TransformDirectionId = nameof(TransformDirectionId);

        public const string TransformXAngleId = nameof(TransformXAngleId);

        public const string TransformYAngleId = nameof(TransformYAngleId);

        public const string TransformZAngleId = nameof(TransformZAngleId);

        public const string TransformScaleId = nameof(TransformScaleId);

        public const string TransformPropertyOpacityId = nameof(TransformPropertyOpacityId);

        public const string TransformPointOfInterestId = nameof(TransformPointOfInterestId);

        public const string TransformOrientationId = nameof(TransformOrientationId);

        public const string ImageLayerOptionIsCastShadowId = nameof(ImageLayerOptionIsCastShadowId);

        public const string ImageLayerOptionLightTransmissionId = nameof(ImageLayerOptionLightTransmissionId);

        public const string ImageLayerOptionIsAcceptShadowId = nameof(ImageLayerOptionIsAcceptShadowId);

        public const string ImageLayerOptionIsAcceptLightId = nameof(ImageLayerOptionIsAcceptLightId);

        public const string ImageLayerOptionAmbientId = nameof(ImageLayerOptionAmbientId);

        public const string ImageLayerOptionDiffuseId = nameof(ImageLayerOptionDiffuseId);

        public const string ImageLayerOptionSpecularIntensityId = nameof(ImageLayerOptionSpecularIntensityId);

        public const string ImageLayerOptionSpecularShininessId = nameof(ImageLayerOptionSpecularShininessId);

        public const string ImageLayerOptionMetalId = nameof(ImageLayerOptionMetalId);

        public const string CameraLayerOptionZoomId = nameof(CameraLayerOptionZoomId);

        public const string LightLayerOptionLightTypeId = nameof(LightLayerOptionLightTypeId);

        public const string LightLayerOptionColorId = nameof(LightLayerOptionColorId);

        public const string LightLayerOptionIntensityId = nameof(LightLayerOptionIntensityId);

        public const string LightLayerOptionConeAngleId = nameof(LightLayerOptionConeAngleId);

        public const string LightLayerOptionConeAttenuationId = nameof(LightLayerOptionConeAttenuationId);

        public const string LightLayerOptionFalloffTypeId = nameof(LightLayerOptionFalloffTypeId);

        public const string LightLayerOptionFalloffStartId = nameof(LightLayerOptionFalloffStartId);

        public const string LightLayerOptionFalloffLengthId = nameof(LightLayerOptionFalloffLengthId);

        public const string LightLayerOptionEnableShadowId = nameof(LightLayerOptionEnableShadowId);

        public const string LightLayerOptionShadowStrengthId = nameof(LightLayerOptionShadowStrengthId);

        public const string LightLayerOptionShadowScatterSizeId = nameof(LightLayerOptionShadowScatterSizeId);

        public const string AudioLevelId = nameof(AudioLevelId);

        NImage GetRawImage(double layerTime, double downSamplingRate, bool useGpu);

        NImage GetEffectedImage(double layerTime, double downSamplingRate, bool useGpu);
    }

    public interface IEffectObject { }

    public interface IPropertyObject
    {
        string Id { get; }

        bool IsEnable { get; }

        IReadOnlyCollection<IPropertyObject>? GetChildren();

        public object? GetValue(double tme);

        public PropertyValueGroup? GetValues(double time, bool withoutDisableProperty = false);
    }

    public interface ICompositionViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// レイヤーのViewModelの一覧を取得します。INotifyCollectionChangedでもあります。
        /// </summary>
        IReadOnlyCollection<ILayerViewModel> LayerViewModels { get; }
    }

    public interface ILayerViewModel : INotifyPropertyChanged
    {
        Guid LayerId { get; }

        string Name { get; }

        string SourceName { get; }

        SourceType SourceType { get; }

        bool IsEnable3D { get; }
    }

    public interface IEffectViewModel : INotifyPropertyChanged { }

    public interface IPropertyViewModel : INotifyPropertyChanged
    {
        PropertyBase Property { get; }

        object? CurrentTimeRawValue { get; set; }

        ICommand BeginEditCommand { get; }

        ICommand EndEditCommand { get; }

        ICommand AbortEditCommand { get; }
    }
}
