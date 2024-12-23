using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Expression.Wrapper
{
    class LayerOptionPropertiesWrapper
    {
        public LayerOptionPropertiesWrapper(PropertyGroupModel layerOptions, Time globalTime)
        {
            if (layerOptions.FindProperty(ILayerObject.CameraLayerOptionZoomId) is PropertyModel zoomProperty)
            {
                zoom = new PropertyWrapper(zoomProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionLightTypeId) is PropertyModel lightTypeProperty)
            {
                lightType = new PropertyWrapper(lightTypeProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionColorId) is PropertyModel colorProperty)
            {
                color = new PropertyWrapper(colorProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionIntensityId) is PropertyModel intensityProperty)
            {
                intensity = new PropertyWrapper(intensityProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionConeAngleId) is PropertyModel coneAngleProperty)
            {
                coneAngle = new PropertyWrapper(coneAngleProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionConeAttenuationId) is PropertyModel coneAttenuationProperty)
            {
                coneAttenuation = new PropertyWrapper(coneAttenuationProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionFalloffTypeId) is PropertyModel falloffTypeProperty)
            {
                falloffType = new PropertyWrapper(falloffTypeProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionFalloffStartId) is PropertyModel falloffStartProperty)
            {
                falloffStart = new PropertyWrapper(falloffStartProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionFalloffLengthId) is PropertyModel falloffLengthProperty)
            {
                falloffLength = new PropertyWrapper(falloffLengthProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionEnableShadowId) is PropertyModel enableShadowProperty)
            {
                enableShadow = new PropertyWrapper(enableShadowProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionShadowStrengthId) is PropertyModel shadowStrengthProperty)
            {
                shadowStrength = new PropertyWrapper(shadowStrengthProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.LightLayerOptionShadowScatterSizeId) is PropertyModel shadowScatterSizeProperty)
            {
                shadowScatterSize = new PropertyWrapper(shadowScatterSizeProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionIsCastShadowId) is PropertyModel isCastShadowProperty)
            {
                isCastShadow = new PropertyWrapper(isCastShadowProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionLightTransmissionId) is PropertyModel lightTransmissionIdProperty)
            {
                lightTransmission = new PropertyWrapper(lightTransmissionIdProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionIsAcceptShadowId) is PropertyModel acceptShadowProperty)
            {
                acceptShadow = new PropertyWrapper(acceptShadowProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionIsAcceptLightId) is PropertyModel acceptLightIdProperty)
            {
                acceptLight = new PropertyWrapper(acceptLightIdProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionAmbientId) is PropertyModel ambientProperty)
            {
                ambient = new PropertyWrapper(ambientProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionDiffuseId) is PropertyModel diffuseProperty)
            {
                diffuse = new PropertyWrapper(diffuseProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionSpecularIntensityId) is PropertyModel specularIntensityProperty)
            {
                specularIntensity = new PropertyWrapper(specularIntensityProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionSpecularShininessId) is PropertyModel specularShininessProperty)
            {
                specularShininess = new PropertyWrapper(specularShininessProperty, globalTime);
            }
            if (layerOptions.FindProperty(ILayerObject.ImageLayerOptionMetalId) is PropertyModel metalProperty)
            {
                metal = new PropertyWrapper(metalProperty, globalTime);
            }
        }

        #region Expression members
#pragma warning disable IDE1006 // NOTE: エクスプレッション用メソッドのため、命名規則は camelCase を許容する

        // camera
        [ExpressionPublicMember]
        public IPropertyWrapper? zoom { get; }

        // light
        [ExpressionPublicMember]
        public IPropertyWrapper? lightType { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? color { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? intensity { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? coneAngle { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? coneAttenuation { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? falloffType { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? falloffStart { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? falloffLength { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? enableShadow { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? shadowStrength { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? shadowScatterSize { get; }

        // normal layer
        [ExpressionPublicMember]
        public IPropertyWrapper? isCastShadow { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? lightTransmission { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? acceptShadow { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? acceptLight { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? ambient { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? diffuse { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? specularIntensity { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? specularShininess { get; }

        [ExpressionPublicMember]
        public IPropertyWrapper? metal { get; }

#pragma warning restore IDE1006 // 命名スタイル
        #endregion Expression members
    }
}
