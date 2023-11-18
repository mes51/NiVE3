using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Property;

namespace NiVE3.Plugin.Interfaces
{
    public interface IAcceleratorObject { }

    public interface ICompositionObject { }

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

        bool IsEnable3D { get; }
    }

    public interface IEffectObject { }

    public interface IPropertyViewModel : INotifyPropertyChanged
    {
        PropertyBase Property { get; }

        object? CurrentTimeValue { get; set; }

        ICommand BeginEditCommand { get; }

        ICommand EndEditCommand { get; }

        ICommand AbortEditCommand { get; }
    }
}
