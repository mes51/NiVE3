using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.Distortion;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Effect.Util.Noise;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Property;
using NiVE3.PresetPlugin.Property.Properties;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using NiVE3.Shared.Util;

namespace NiVE3.PresetPlugin.Effect.Simulation
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Simulation_SphereGrid_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Simulation, LanguageResourceDictionary.Simulation_SphereGrid_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, UseCompositionCamera = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class SphereGrid : IEffect
    {
        const string ID = "45D64F57-D4EC-4E37-9B94-4564F6544486";

        #region Property ids

        const string PropertyArrangmentGroupId = nameof(PropertyArrangmentGroupId);

        const string PropertyArrangementGridSizeId = nameof(PropertyArrangementGridSizeId);

        const string PropertyArrangementParticleCountId = nameof(PropertyArrangementParticleCountId);

        const string PropertyArrangementTwistId = nameof(PropertyArrangementTwistId);

        const string PropertyArrangementScatteringId = nameof(PropertyArrangementScatteringId);

        const string PropertyArrangementScatteringRandomSeedId = nameof(PropertyArrangementScatteringRandomSeedId);

        const string PropertyParticleGroupId = nameof(PropertyParticleGroupId);

        const string PropertyParticleSizeId = nameof(PropertyParticleSizeId);

        const string PropertyParticleSoftnessId = nameof(PropertyParticleSoftnessId);

        const string PropertyParticleColorId = nameof(PropertyParticleColorId);

        const string PropertyParticleOpacityId = nameof(PropertyParticleOpacityId);

        const string PropertyFractalNoiseGroupId = nameof(PropertyFractalNoiseGroupId);

        const string PropertyFractalNoiseContrastId = nameof(PropertyFractalNoiseContrastId);

        const string PropertyFractalNoiseLuminanceId = nameof(PropertyFractalNoiseLuminanceId);

        const string PropertyFractalNoiseIsInvertId = nameof(PropertyFractalNoiseIsInvertId);

        const string PropertyFractalNoiseOctaveId = nameof(PropertyFractalNoiseOctaveId);

        const string PropertyFractalNoiseScaleId = nameof(PropertyFractalNoiseScaleId);

        const string PropertyFractalNoisePositionId = nameof(PropertyFractalNoisePositionId);

        const string PropertyFractalNoiseEvolutionId = nameof(PropertyFractalNoiseEvolutionId);

        const string PropertyFractalNoiseRandomSeedId = nameof(PropertyFractalNoiseRandomSeedId);

        const string PropertyFractalNoiseApplySizeId = nameof(PropertyFractalNoiseApplySizeId);

        const string PropertyFractalNoiseApplyOpacityId = nameof(PropertyFractalNoiseApplyOpacityId);

        const string PropertyFractalNoiseApplyScatteringId = nameof(PropertyFractalNoiseApplyScatteringId);

        const string PropertyFractalNoiseApplyDisplacement = nameof(PropertyFractalNoiseApplyDisplacement);

        const string PropertyTransformGroupId = nameof(PropertyTransformGroupId);

        const string PropertyTransformAnchorPointId = nameof(PropertyTransformAnchorPointId);

        const string PropertyTransformPositionId = nameof(PropertyTransformPositionId);

        const string PropertyTransformScaleId = nameof(PropertyTransformScaleId);

        const string PropertyTransformXAngleId = nameof(PropertyTransformXAngleId);

        const string PropertyTransformYAngleId = nameof(PropertyTransformYAngleId);

        const string PropertyTransformZAngleId = nameof(PropertyTransformZAngleId);

        const string PropertyCameraGroupId = nameof(PropertyCameraGroupId);

        const string PropertyGraphMapId = nameof(PropertyGraphMapId);

        const string PropertyGraphMapSizeItemId = nameof(PropertyGraphMapSizeItemId);

        const string PropertyGraphMapOpacityItemId = nameof(PropertyGraphMapOpacityItemId);

        const string PropertyGraphMapColorItemId = nameof(PropertyGraphMapColorItemId);

        const string PropertyGraphMapColorColorId = nameof(PropertyGraphMapColorColorId);

        const string PropertyGraphMapScatteringItemId = nameof(PropertyGraphMapScatteringItemId);

        const string PropertyGraphMapFractalNoiseItemId = nameof(PropertyGraphMapFractalNoiseItemId);

        const string PropertyGraphMapValueGraphId = nameof(PropertyGraphMapValueGraphId);

        const string PropertyGraphMapValueDirectionId = nameof(PropertyGraphMapValueDirectionId);

        const string PropertyLayerMapId = nameof(PropertyLayerMapId);

        const string PropertyLayerMapDisplacementItemId= nameof(PropertyLayerMapDisplacementItemId);

        const string PropertyLayerMapDisplacementXChannelId = nameof(PropertyLayerMapDisplacementXChannelId);

        const string PropertyLayerMapDisplacementYChannelId = nameof(PropertyLayerMapDisplacementYChannelId);

        const string PropertyLayerMapDisplacementZChannelId = nameof(PropertyLayerMapDisplacementZChannelId);

        const string PropertyLayerMapDisplacementMoveId = nameof(PropertyLayerMapDisplacementMoveId);

        const string PropertyLayerMapLayerColorItemId = nameof(PropertyLayerMapLayerColorItemId);

        const string PropertyLayerMapLayerColorChannelId = nameof(PropertyLayerMapLayerColorChannelId);

        const string PropertyLayerMapSizeItemId = nameof(PropertyLayerMapSizeItemId);

        const string PropertyLayerMapOpacityItemId = nameof(PropertyLayerMapOpacityItemId);

        const string PropertyLayerMapScatteringItemId = nameof(PropertyLayerMapScatteringItemId);

        const string PropertyLayerMapFractalNoiseItemId = nameof(PropertyLayerMapFractalNoiseItemId);

        const string PropertyLayerMapValueLayerId = nameof(PropertyLayerMapValueLayerId);

        const string PropertyLayerMapValueUseSpecificReferenceTimeId = nameof(PropertyLayerMapValueUseSpecificReferenceTimeId);

        const string PropertyLayerMapValueSpecificReferenceTimeId = nameof(PropertyLayerMapValueSpecificReferenceTimeId);

        const string PropertyLayerMapValueLayerPositionId = nameof(PropertyLayerMapValueLayerPositionId);

        const string PropertyLayerMapValueMapDirectionId = nameof(PropertyLayerMapValueMapDirectionId);

        const string PropertyLayerMapValueChannelId = nameof(PropertyLayerMapValueChannelId);

        const string PropertyLayerMapValueApplyRateId = nameof(PropertyLayerMapValueApplyRateId);

        const string PropertyRenderingGroupId = nameof(PropertyRenderingGroupId);

        const string PropertyRenderingAntiAliasId = nameof(PropertyRenderingAntiAliasId);

        const string PropertyRenderingParticleBlendModeId = nameof(PropertyRenderingParticleBlendModeId);

        const string PropertyRenderingParticleOnlyId = nameof(PropertyRenderingParticleOnlyId);

        const string PropertyRenderingBlendModeId = nameof(PropertyRenderingBlendModeId);

        const string PropertyRenderingCompositeOrderId = nameof(PropertyRenderingCompositeOrderId);

        #endregion Property ids

        const float FractalNoiseOctaveAmount = 0.7F;

        const double FractalNoiseOctarveScale = 0.56;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var dialogOk = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var dialogCancel = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            var colorDialogTitle = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            var center = new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5;
            var cameraZoom = sourceSize.Width / Const.DefaultCameraFov * 0.5;
            return
            [
                new PropertyGroup(PropertyArrangmentGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment,
                [
                    new Vector3dProperty(PropertyArrangementGridSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_GridSize, new Vector3d(300.0, 300.0, 300.0), Vector3d.Zero, new Vector3d(double.MaxValue), digit: 2, is3D: true),
                    new Vector3dProperty(PropertyArrangementParticleCountId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_ParticleCount, new Vector3d(25.0, 25.0, 3.0), Vector3d.Zero, new Vector3d(int.MaxValue), digit: 0, is3D: true),
                    new Vector3dProperty(PropertyArrangementTwistId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_Twist, Vector3d.Zero, digit: 2, is3D: true, separator: ",", unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                    new Vector3dProperty(PropertyArrangementScatteringId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_Scattering, Vector3d.Zero, Vector3d.Zero, new Vector3d(double.MaxValue), digit: 2, is3D: true),
                    new DoubleProperty(PropertyArrangementScatteringRandomSeedId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_ScatteringRandomSeed, 0, int.MinValue, int.MaxValue, digit: 0)
                ]),
                new PropertyGroup(PropertyParticleGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle,
                [
                    new DoubleProperty(PropertyParticleSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Size, 5.0, 0.0, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyParticleSoftnessId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Softness, 50.0, 0.0, 100.0, digit: 2),
                    new ColorProperty(PropertyParticleColorId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Color, colorDialogTitle, dialogOk, dialogCancel, Vector4.One),
                    new DoubleProperty(PropertyParticleOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                ]),
                new PropertyGroup(PropertyFractalNoiseGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise,
                [
                    new DoubleProperty(PropertyFractalNoiseContrastId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Contrast, 100.0, 0.0, 10000.0, digit: 2),
                    new DoubleProperty(PropertyFractalNoiseLuminanceId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Luminance, 0.0, -10000.0, 10000.0, digit: 2),
                    new CheckBoxProperty(PropertyFractalNoiseIsInvertId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_IsInvert, false),
                    new DoubleProperty(PropertyFractalNoiseOctaveId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Octave, 6.0, 1.0, 20.0, digit: 2, slideChangeValue: 0.1),
                    new Vector3dProperty(PropertyFractalNoiseScaleId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Scale, new Vector3d(100.0), new Vector3d(0.01), new Vector3d(double.MaxValue), digit: 2, is3D: true, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, separator: ",", useLinkRatio: true),
                    new Vector3dProperty(PropertyFractalNoisePositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Position, Vector3d.Zero, digit: 2, is3D: true, useInteraction: true),
                    new AngleProperty(PropertyFractalNoiseEvolutionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Evolution, 0.0, digit: 2),
                    new DoubleProperty(PropertyFractalNoiseRandomSeedId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_RandomSeed, 0, int.MinValue, int.MaxValue, digit: 0),
                    new DoubleProperty(PropertyFractalNoiseApplySizeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_ApplySize, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyFractalNoiseApplyOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_ApplyOpacity, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyFractalNoiseApplyScatteringId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_ApplyScattering, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new Vector3dProperty(PropertyFractalNoiseApplyDisplacement, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_ApplyDisplacement, Vector3d.Zero, digit: 2, is3D: true)
                ]),
                new PropertyGroup(PropertyTransformGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Transform,
                [
                    new Vector3dProperty(PropertyTransformAnchorPointId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Transform_AnchorPoint, center, digit: 2, is3D: true, useInteraction: true),
                    new Vector3dProperty(PropertyTransformPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Transform_Position, center, digit: 2, is3D: true, useInteraction: true),
                    new Vector3dProperty(PropertyTransformScaleId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Transform_Scale, new Vector3d(100.0), digit: 2, is3D: true, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, separator: ",", useLinkRatio: true),
                    new AngleProperty(PropertyTransformXAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Transform_XAngle, 0.0, digit: 2),
                    new AngleProperty(PropertyTransformYAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Transform_YAngle, 0.0, digit: 2),
                    new AngleProperty(PropertyTransformZAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Transform_ZAngle, 0.0, digit: 2)
                ]),
                new PropertyGroup(PropertyCameraGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Camera,
                [
                    new CheckBoxProperty(CameraProperties.PropertyCameraUseCompositionId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_UseComposition, false),
                    new Vector3dProperty(CameraProperties.PropertyCameraPointOfInterestId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_PointOfInterest, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, is3D: true, useInteraction : true),
                    new Vector3dProperty(CameraProperties.PropertyCameraPositionId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Position, new Vector3d(sourceSize.Width * 0.5, sourceSize.Height * 0.5, -cameraZoom), digit: 2, is3D: true, useInteraction : true),
                    new DirectionProperty(CameraProperties.PropertyCameraOrientationId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Orientation, Vector3d.Zero, digit: 2),
                    new AngleProperty(CameraProperties.PropertyCameraXAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_XAngle, 0.0, digit: 2),
                    new AngleProperty(CameraProperties.PropertyCameraYAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_YAngle, 0.0, digit: 2),
                    new AngleProperty(CameraProperties.PropertyCameraZAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_ZAngle, 0.0, digit: 2),
                    new DoubleProperty(CameraProperties.PropertyCameraZoomId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Zoom, cameraZoom, 0.01, double.MaxValue, digit: 2)
                ]),
                new AppendableProperty(PropertyGraphMapId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap,
                [
                    new AppendablePropertyItem(PropertyGraphMapSizeItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Size, () => new PropertyGroup(PropertyGraphMapSizeItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Size,
                    [
                        new GraphValueProperty(PropertyGraphMapValueGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Graph, true),
                        new EnumProperty(PropertyGraphMapValueDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Direction, typeof(SphereGridGraphMapDirectionType), typeof(LanguageResourceDictionary), SphereGridGraphMapDirectionType.X, selectBoxWidth: 90.0)
                    ])),
                    new AppendablePropertyItem(PropertyGraphMapOpacityItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Opacity, () => new PropertyGroup(PropertyGraphMapOpacityItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Opacity,
                    [
                        new GraphValueProperty(PropertyGraphMapValueGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Graph, true),
                        new EnumProperty(PropertyGraphMapValueDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Direction, typeof(SphereGridGraphMapDirectionType), typeof(LanguageResourceDictionary), SphereGridGraphMapDirectionType.X, selectBoxWidth: 90.0)
                    ])),
                    new AppendablePropertyItem(PropertyGraphMapColorItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Color, () => new PropertyGroup(PropertyGraphMapColorItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Color,
                    [
                        new ColorProperty(PropertyGraphMapColorColorId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Color_Color, colorDialogTitle, dialogOk, dialogCancel, new Vector4(0.0F, 0.0F, 1.0F, 1.0F)),
                        new GraphValueProperty(PropertyGraphMapValueGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Graph, true),
                        new EnumProperty(PropertyGraphMapValueDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Direction, typeof(SphereGridGraphMapDirectionType), typeof(LanguageResourceDictionary), SphereGridGraphMapDirectionType.X, selectBoxWidth: 90.0)
                    ])),
                    new AppendablePropertyItem(PropertyGraphMapScatteringItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Scattering, () => new PropertyGroup(PropertyGraphMapScatteringItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Scattering,
                    [
                        new GraphValueProperty(PropertyGraphMapValueGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Graph, true),
                        new EnumProperty(PropertyGraphMapValueDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Direction, typeof(SphereGridGraphMapDirectionType), typeof(LanguageResourceDictionary), SphereGridGraphMapDirectionType.X, selectBoxWidth: 90.0)
                    ])),
                    new AppendablePropertyItem(PropertyGraphMapFractalNoiseItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_FractalNoise, () => new PropertyGroup(PropertyGraphMapFractalNoiseItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_FractalNoise,
                    [
                        new GraphValueProperty(PropertyGraphMapValueGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Graph, true),
                        new EnumProperty(PropertyGraphMapValueDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_GraphMap_Value_Direction, typeof(SphereGridGraphMapDirectionType), typeof(LanguageResourceDictionary), SphereGridGraphMapDirectionType.X, selectBoxWidth: 90.0)
                    ])),
                ], useEnableSwitch: true),
                new AppendableProperty(PropertyLayerMapId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap,
                [
                    new AppendablePropertyItem(PropertyLayerMapDisplacementItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Displacement, () => new PropertyGroup(PropertyLayerMapDisplacementItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Displacement,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueLayerPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_LayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapDisplacementXChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Displacement_XChannel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.R, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapDisplacementYChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Displacement_YChannel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.G, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapDisplacementZChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Displacement_ZChannel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.B, selectBoxWidth: 90.0),
                        new Vector3dProperty(PropertyLayerMapDisplacementMoveId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Displacement_Move, Vector3d.Zero, digit: 2, is3D: true)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapLayerColorItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_LayerColor, () => new PropertyGroup(PropertyLayerMapLayerColorItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_LayerColor,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueLayerPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_LayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyRateId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_ApplyRate, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new EnumProperty(PropertyLayerMapLayerColorChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_LayerColor_Channel, typeof(SphereGridLayerMapLayerColorType), typeof(LanguageResourceDictionary), SphereGridLayerMapLayerColorType.RGB, selectBoxWidth: 90.0)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapSizeItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Size, () => new PropertyGroup(PropertyLayerMapSizeItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Size,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueLayerPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_LayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyRateId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_ApplyRate, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapOpacityItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Opacity, () => new PropertyGroup(PropertyLayerMapOpacityItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Opacity,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueLayerPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_LayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyRateId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_ApplyRate, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapScatteringItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Scattering, () => new PropertyGroup(PropertyLayerMapScatteringItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Scattering,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueLayerPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_LayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyRateId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_ApplyRate, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapFractalNoiseItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_FractalNoise, () => new PropertyGroup(PropertyLayerMapFractalNoiseItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_FractalNoise,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueLayerPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_LayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyRateId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_ApplyRate, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                    ])),
                ], useEnableSwitch: true),
                new PropertyGroup(PropertyRenderingGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Rendering,
                [
                    new CheckBoxProperty(PropertyRenderingAntiAliasId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Rendering_AntiAlias, true),
                    new EnumProperty(PropertyRenderingParticleBlendModeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Rendering_ParticleBlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Add, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyRenderingParticleOnlyId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Rendering_ParticleOnly, true),
                    new EnumProperty(PropertyRenderingBlendModeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Rendering_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyRenderingCompositeOrderId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Rendering_CompositeOrder, typeof(CompositeOrder), typeof(LanguageResourceDictionary), CompositeOrder.Front, selectBoxWidth: 90.0)
                ])
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var renderSize = Math.Max(image.Width, image.Height);

            var arrangementGroup = properties.First(p => p.Id == PropertyArrangmentGroupId).GetChildren() ?? [];
            var particleGroup = properties.First(p => p.Id == PropertyParticleGroupId).GetChildren() ?? [];
            var fractalNoiseGroup = properties.First(p => p.Id == PropertyFractalNoiseGroupId).GetChildren() ?? [];
            var transformGroup = properties.First(p => p.Id == PropertyTransformGroupId).GetChildren() ?? [];
            var cameraGroup = properties.First(p => p.Id == PropertyCameraGroupId).GetChildren() ?? [];
            var graphMap = properties.First(p => p.Id == PropertyGraphMapId).GetChildren() ?? [];
            var layerMap = properties.First(p => p.Id == PropertyLayerMapId).GetChildren() ?? [];
            var renderGroup = properties.First(p => p.Id == PropertyRenderingGroupId).GetChildren() ?? [];

            var (gridSize, spheres) = GenerateSpheres(arrangementGroup, particleGroup, fractalNoiseGroup, layerTime, image.Width, image.Height, downSamplingRateX, downSamplingRateY);
            if (spheres.Length < 1)
            {
                if (renderGroup.GetValue(PropertyRenderingParticleOnlyId, layerTime, false))
                {
                    if (useGpu && AcceleratorObject != null)
                    {
                        var gpuImage = image.ToGpu(AcceleratorObject.CurrentDevice);
                        ImageClearProcessor.ClearGpu(AcceleratorObject.CurrentDevice, gpuImage, roi);
                        return gpuImage;
                    }
                    else
                    {
                        var managedImage = image.ToManaged();
                        ImageClearProcessor.ClearCpu(managedImage, roi);
                        return managedImage;
                    }
                }
                else
                {
                    return image;
                }
            }

            ApplyGraphMap(spheres, graphMap, layerTime);
            ApplyLayerMap(spheres, layerMap, composition, gridSize, layerTime, downSamplingRateX, useGpu);

            var transformMatrix = Matrix4x4d.AffineTransform(
                transformGroup.GetValue(PropertyTransformAnchorPointId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0) / renderSize,
                transformGroup.GetValue(PropertyTransformScaleId, layerTime, Vector3d.Zero) * 0.01,
                Vector3d.Zero,
                transformGroup.GetValue(PropertyTransformXAngleId, layerTime, 0.0),
                transformGroup.GetValue(PropertyTransformYAngleId, layerTime, 0.0),
                transformGroup.GetValue(PropertyTransformZAngleId, layerTime, 0.0),
                transformGroup.GetValue(PropertyTransformPositionId, layerTime, Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0) / renderSize
            );
            var (viewMatrix, fov) = CameraProperties.GetViewMatrixAndFov(cameraGroup, composition, layer, layerTime, roi, image.Width, image.Height, downSamplingRateX, downSamplingRateY);
            var offsetX = (renderSize - image.Width) * 0.5 / renderSize;
            var offsetY = (renderSize - image.Height) * 0.5 / renderSize;
            var offsetMatrix = Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0);
            var mvt = transformMatrix * viewMatrix * offsetMatrix;

            NImage canvas;
            SphereRendererBase renderer;
            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                canvas = new NGPUImage(image.Width, image.Height, device);
                renderer = new SphereRendererGpu(mvt, fov, device, (NGPUImage)canvas);
            }
            else
            {
                canvas = new NManagedImage(image.Width, image.Height);
                renderer = new SphereRendererCpu(mvt, fov, (NManagedImage)canvas);
            }

            for (var i = 0; i < spheres.Length; i++)
            {
                var s = spheres[i];
                var radius = s.Radius * s.InfluenceRadius * double.Lerp(1.0, s.FractalNoiseValue, double.Lerp(0.0, s.FractalNoiseSizeApplyRate, s.InfluenceFractalNoise));
                if (radius <= 0.0)
                {
                    continue;
                }
                var color = s.Color;
                color.W *= float.Lerp(1.0F, s.FractalNoiseValue, float.Lerp(0.0F, s.FractalNoiseOpacityApplyRate, s.InfluenceFractalNoise));
                var pos = s.Position + s.ScatteringPosition * s.InfluenceScattering * double.Lerp(1.0, s.FractalNoiseValue, double.Lerp(0.0, s.FractalNoiseScatteringApplyRate, s.InfluenceFractalNoise)) + s.FractalNoiseDisplacement * (s.FractalNoiseValue - 0.5) * 2.0 * s.InfluenceFractalNoise;
                renderer.AddSphere(pos, color, radius, s.Softness);
            }

            var antiAlias = renderGroup.GetValue(PropertyRenderingAntiAliasId, layerTime, false);
            var sphereBlendMode = renderGroup.GetValue(PropertyRenderingParticleBlendModeId, layerTime, BlendMode.Add);
            if (antiAlias)
            {
                renderer.RenderAntiAlias(roi, sphereBlendMode);
            }
            else
            {
                renderer.Render(roi, sphereBlendMode);
            }

            var blendMode = renderGroup.GetValue(PropertyRenderingBlendModeId, layerTime, BlendMode.Normal);
            var particleOnly = renderGroup.GetValue(PropertyRenderingParticleOnlyId, layerTime, false);
            var compositeOrder = renderGroup.GetValue(PropertyRenderingCompositeOrderId, layerTime, CompositeOrder.Front);
            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                if (particleOnly)
                {
                    ImageBlendProcessor.SameSizeNoSkipTransparentFrontGpu(device, gpuImage, (NGPUImage)canvas, roi, BlendMode.Replace);
                    canvas.Dispose();
                    return gpuImage;
                }
                else
                {
                    if (compositeOrder == CompositeOrder.Front)
                    {
                        ImageBlendProcessor.SameSizeGpu(device, gpuImage, (NGPUImage)canvas, roi, blendMode);
                        canvas.Dispose();
                        return gpuImage;
                    }
                    else
                    {
                        ImageBlendProcessor.SameSizeGpu(device, (NGPUImage)canvas, gpuImage, roi, blendMode);
                        if (gpuImage != image)
                        {
                            gpuImage.Dispose();
                        }
                        return canvas;
                    }
                }
            }
            else
            {
                var managedImage = image.ToManaged();
                if (particleOnly)
                {
                    ImageBlendProcessor.SameSizeNoSkipTransparentFrontCpu(managedImage, (NManagedImage)canvas, roi, BlendMode.Replace);
                    canvas.Dispose();
                    return managedImage;
                }
                else
                {
                    if (compositeOrder == CompositeOrder.Front)
                    {
                        ImageBlendProcessor.SameSizeNoSkipTransparentFrontCpu(managedImage, (NManagedImage)canvas, roi, blendMode);
                        canvas.Dispose();
                        return managedImage;
                    }
                    else
                    {
                        ImageBlendProcessor.SameSizeNoSkipTransparentFrontCpu((NManagedImage)canvas, managedImage, roi, blendMode);
                        if (managedImage != image)
                        {
                            managedImage.Dispose();
                        }
                        return canvas;
                    }
                }
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static (Vector3d gridSize, Sphere[] spheres) GenerateSpheres(IReadOnlyCollection<IPropertyObject> arrangementGroup, IReadOnlyCollection<IPropertyObject> particleGroup, IReadOnlyCollection<IPropertyObject> fractalNoiseGroup, in Time layerTime, int imageWidth, int imageHeight, double downSamplingRateX, double downSamplingRateY)
        {
            var downSamplingRateVector = new Vector3d(downSamplingRateX, downSamplingRateY, imageWidth > imageHeight ? downSamplingRateX : downSamplingRateY);
            var particleSize = particleGroup.GetValue(PropertyParticleSizeId, layerTime, 0.0) * 0.5 / downSamplingRateX;
            var particleCount = arrangementGroup.GetValue(PropertyArrangementParticleCountId, layerTime, Vector3d.Zero);
            var particleCountX = (int)particleCount.X;
            var particleCountY = (int)particleCount.Y;
            var particleCountZ = (int)particleCount.Z;
            if (particleCountX < 1 || particleCountY < 0 || particleCountZ < 0 || particleSize <= 0.0)
            {
                return (Vector3d.Zero, []);
            }

            var softness = (float)(particleGroup.GetValue(PropertyParticleSoftnessId, layerTime, 0.0) * 0.01);
            var color = particleGroup.GetValue(PropertyParticleColorId, layerTime, Vector4.Zero);
            var opacity = (float)(particleGroup.GetValue(PropertyParticleOpacityId, layerTime, 0.0) * 0.01);
            var gridSize = arrangementGroup.GetValue(PropertyArrangementGridSizeId, layerTime, Vector3d.Zero) / downSamplingRateVector;
            var twist = arrangementGroup.GetValue(PropertyArrangementTwistId, layerTime, Vector3d.Zero);
            var gridCenter = new Vector3d(
                particleCountX > 1 ? gridSize.X : 0.0,
                particleCountY > 1 ? gridSize.Y : 0.0,
                particleCountZ > 1 ? gridSize.Z : 0.0
            ) * 0.5;
            var screenCenter = new Vector3d(imageWidth, imageHeight, 0.0) * 0.5;
            var twistRate = new Vector3d(
                particleCountX > 1 ? twist.X / (particleCountX - 1) : 0.0,
                particleCountY > 1 ? twist.Y / (particleCountY - 1) : 0.0,
                particleCountZ > 1 ? twist.Z / (particleCountZ - 1) : 0.0
            );
            var twistOffset = twistRate * new Vector3d(particleCountX, particleCountY, particleCountZ) * 0.5 - twist;

            var gridSizeDiff = new Vector3d(
                particleCountX > 1 ? gridSize.X / (particleCountX - 1) : 0.0,
                particleCountY > 1 ? gridSize.Y / (particleCountY - 1) : 0.0,
                particleCountZ > 1 ? gridSize.Z / (particleCountZ - 1) : 0.0
            );
            var normalizedGridDiffX = particleCountX > 1 ? 1.0 / (particleCountX - 1) : 0.0;
            var normalizedGridDiffY = particleCountY > 1 ? 1.0 / (particleCountY - 1) : 0.0;
            var normalizedGridDiffZ = particleCountZ > 1 ? 1.0 / (particleCountZ - 1) : 0.0;

            var fractalNoiseSizeApplyRate = fractalNoiseGroup.GetValue(PropertyFractalNoiseApplySizeId, layerTime, 0.0) * 0.01;
            var fractalNoiseOpacityApplyRate = (float)(fractalNoiseGroup.GetValue(PropertyFractalNoiseApplyOpacityId, layerTime, 0.0) * 0.01);
            var fractalNoiseScatteringApplyRate = fractalNoiseGroup.GetValue(PropertyFractalNoiseApplyScatteringId, layerTime, 0.0) * 0.01;
            var fractalNoiseDisplacement = fractalNoiseGroup.GetValue(PropertyFractalNoiseApplyDisplacement, layerTime, Vector3d.Zero);
            var spheres = new Sphere[particleCountX * particleCountY * particleCountZ];
            for (int z = 0, p = 0; z < particleCountZ; z++)
            {
                var normalizedGridZ = particleCountZ > 1 ?(float)(z * normalizedGridDiffZ) : 1.0F;
                for (var y = 0; y < particleCountY; y++)
                {
                    var normalizedGridY = particleCountY > 1 ? (float)(y * normalizedGridDiffY) : 1.0F;
                    for (var x = 0; x < particleCountX; x++, p++)
                    {
                        var gridPosition = gridSizeDiff * new Vector3d(x, y, z) - gridCenter;
                        var matrix = Matrix4x4d.AffineTransform(-gridPosition, Vector3d.One, twistOffset + twistRate * new Vector3d(x, y, z), 0.0, 0.0, 0.0, screenCenter);
                        var pos = (Vector3d)matrix.Transform(Vector256.Create(0.0, 0.0, 0.0, 1.0));
                        spheres[p] = new Sphere(pos, gridPosition, color * new Vector4(1.0F, 1.0F, 1.0F, opacity), particleSize, softness)
                        {
                            FractalNoiseSizeApplyRate = fractalNoiseSizeApplyRate,
                            FractalNoiseOpacityApplyRate = fractalNoiseOpacityApplyRate,
                            FractalNoiseScatteringApplyRate = fractalNoiseScatteringApplyRate,
                            FractalNoiseDisplacement = fractalNoiseDisplacement,
                            NormalizedGridX = particleCountX > 1 ? (float)(x * normalizedGridDiffX) : 1.0F,
                            NormalizedGridY = normalizedGridY,
                            NormalizedGridZ = normalizedGridZ,
                        };
                    }
                }
            }

            var scattering = arrangementGroup.GetValue(PropertyArrangementScatteringId, layerTime, Vector3d.Zero) / downSamplingRateVector;
            if (scattering != Vector3d.Zero)
            {
                var scatteringRandomSeed = (int)arrangementGroup.GetValue(PropertyArrangementScatteringRandomSeedId, layerTime, 0.0);
                var rand = new Xoroshiro(scatteringRandomSeed);

                for (var i = 0; i < spheres.Length; i++)
                {
                    spheres[i].ScatteringPosition = (new Vector3d(rand.NextDouble(), rand.NextDouble(), rand.NextDouble()) * 2.0 - new Vector3d(1.0)) * scattering;
                }
            }

            if (fractalNoiseSizeApplyRate > 0.0 || fractalNoiseOpacityApplyRate > 0.0F || fractalNoiseScatteringApplyRate > 0.0F || fractalNoiseDisplacement != Vector3d.Zero)
            {
                var contrast = (float)(fractalNoiseGroup.GetValue(PropertyFractalNoiseContrastId, layerTime, 0.0) * 0.01);
                var luminance = (float)(fractalNoiseGroup.GetValue(PropertyFractalNoiseLuminanceId, layerTime, 0.0) * 0.01);
                var isInvert = fractalNoiseGroup.GetValue(PropertyFractalNoiseIsInvertId, layerTime, false);
                var octave = (float)fractalNoiseGroup.GetValue(PropertyFractalNoiseOctaveId, layerTime, 0.0);
                var fractalNoiseScale = fractalNoiseGroup.GetValue(PropertyFractalNoiseScaleId, layerTime, Vector3d.Zero) * 0.01 / downSamplingRateVector;
                var fractalNoisePosition = fractalNoiseGroup.GetValue(PropertyFractalNoisePositionId, layerTime, Vector3d.Zero) / downSamplingRateVector;
                var fractalNoiseRandomSeed = (long)fractalNoiseGroup.GetValue(PropertyFractalNoiseRandomSeedId, layerTime, 0.0);
                var evolution = fractalNoiseGroup.GetValue(PropertyFractalNoiseEvolutionId, layerTime, 0.0);
                var octaveLimit = (int)Math.Ceiling(octave);
                var matrix = Matrix4x4d.AffineTransform(Vector3d.Zero, fractalNoiseScale * 40.0, Vector3d.Zero, 0.0, 0.0, 0.0, fractalNoisePosition);
                Parallel.For(0, spheres.Length, i =>
                {
                    var s = spheres[i];
                    var denom = 0.0F;
                    var noise = 0.0F;
                    for (var o = 0; o < octaveLimit; o++)
                    {
                        var subTransform = Matrix4x4d.AffineTransform(new Vector3d(0.3, 0.7, 0.1) * o * 2.0, new Vector3d(Math.Pow(FractalNoiseOctarveScale, o)), Vector3d.Zero, 0.0, 0.0, 0.0, Vector3d.Zero) * matrix;
                        if (!Matrix4x4d.Invert(subTransform, out var inverted))
                        {
                            continue;
                        }

                        var amount = MathF.Pow(FractalNoiseOctaveAmount, Math.Min(o, (int)octave)) * FractalNoiseOctaveAmount * Math.Min(octave - o, 1.0F);
                        if (amount <= 0.0F)
                        {
                            if (o > 0)
                            {
                                break;
                            }
                            else
                            {
                                amount = 1.0F;
                            }
                        }
                        denom += amount;

                        var noisePosition = inverted.Transform(s.GridPosition);
                        noise += Simplex4D.Noise(fractalNoiseRandomSeed, noisePosition.X, noisePosition.Y, noisePosition.Z, evolution);
                    }

                    var value = noise / denom + 0.5F;
                    if (isInvert)
                    {
                        value = 1.0F - value;
                    }
                    s.FractalNoiseValue = Math.Clamp((value - 0.5F) * contrast + 0.5F + luminance, 0.0F, 1.0F);
                });
            }

            return (gridSize, spheres);
        }

        static void ApplyGraphMap(Sphere[] spheres, IReadOnlyCollection<IPropertyObject> graphMap, in Time layerTime)
        {
            foreach (var map in graphMap.Where(p => p.IsEnable))
            {
                var graphMapProperties = map.GetChildren() ?? [];
                var direction = graphMapProperties.GetValue(PropertyGraphMapValueDirectionId, layerTime, SphereGridGraphMapDirectionType.None);
                if (direction == SphereGridGraphMapDirectionType.None)
                {
                    continue;
                }

                var mapValue = graphMapProperties.GetValue(PropertyGraphMapValueGraphId, layerTime, GraphValueParameter.Identity);
                var newColor = graphMapProperties.GetValue(PropertyGraphMapColorColorId, layerTime, Vector4.One);
                Action<Sphere, double> mapFunction = map.Id switch
                {
                    PropertyGraphMapSizeItemId => static (Sphere s, double value) => s.InfluenceRadius *= value,
                    PropertyGraphMapOpacityItemId => static (Sphere s, double value) => s.Color *= new Vector4(1.0F, 1.0F, 1.0F, (float)value),
                    PropertyGraphMapColorItemId => (Sphere s, double value) => s.Color = Vector4.Lerp(s.Color, newColor, (float)value),
                    PropertyGraphMapScatteringItemId => static (Sphere s, double value) => s.InfluenceScattering *= value,
                    PropertyGraphMapFractalNoiseItemId => static (Sphere s, double value) => s.InfluenceFractalNoise *= (float)value,
                    _ => static (Sphere s, double value) => { }
                };

                switch (direction)
                {
                    case SphereGridGraphMapDirectionType.X:
                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            mapFunction(s, mapValue.Interpolation(0.0F, 1.0F, s.NormalizedGridX));
                        });
                        break;
                    case SphereGridGraphMapDirectionType.Y:
                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            mapFunction(s, mapValue.Interpolation(0.0F, 1.0F, s.NormalizedGridY));
                        });
                        break;
                    case SphereGridGraphMapDirectionType.Z:
                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            mapFunction(s, mapValue.Interpolation(0.0F, 1.0F, s.NormalizedGridZ));
                        });
                        break;
                }
            }
        }

        static void ApplyLayerMap(Sphere[] spheres, IReadOnlyCollection<IPropertyObject> layerMap, ICompositionObject composition, Vector3d gridSize, in Time layerTime, double downSamplingRate, bool useGpu)
        {
            var imageCache = new Dictionary<LayerMapCacheKey, NManagedImage>();

            foreach (var map in layerMap.Where(p => p.IsEnable))
            {
                var layerMapProperties = map.GetChildren() ?? [];
                var mapDirection = layerMapProperties.GetValue(PropertyLayerMapValueMapDirectionId, layerTime, SphereGridLayerMapDirectionType.None);
                if (mapDirection == SphereGridLayerMapDirectionType.None)
                {
                    continue;
                }
                var layerKey = layerMapProperties.GetValue(PropertyLayerMapValueLayerId, layerTime, UseLayerImageTarget.Empty);
                if (layerKey == UseLayerImageTarget.Empty)
                {
                    continue;
                }

                var useSpecificReferenceTime = layerMapProperties.GetValue(PropertyLayerMapValueUseSpecificReferenceTimeId, layerTime, false);
                var referenceTime = useSpecificReferenceTime ? layerMapProperties.GetValue(PropertyLayerMapValueSpecificReferenceTimeId, layerTime, Time.Zero) : layerTime;
                var cacheKey = new LayerMapCacheKey(layerKey.LayerId, referenceTime);
                if (!imageCache.ContainsKey(cacheKey))
                {
                    var image = layerKey.GetImage(composition, referenceTime, downSamplingRate, useGpu);
                    if (image == null)
                    {
                        continue;
                    }

                    var managedImage = image.ToManaged();
                    imageCache.Add(cacheKey, managedImage);

                    if (image != managedImage)
                    {
                        image.Dispose();
                    }
                }

                var mapImage = imageCache[cacheKey];

                switch (map.Id)
                {
                    case PropertyLayerMapDisplacementItemId:
                        ApplyDisplacementLayerMap(spheres, mapDirection, layerMapProperties, mapImage, gridSize, layerTime);
                        break;
                    case PropertyLayerMapLayerColorItemId:
                        ApplyLayerColorLayerMap(spheres, mapDirection, layerMapProperties, mapImage, gridSize, layerTime);
                        break;
                    case PropertyLayerMapSizeItemId:
                        ApplyLayerMapAction(spheres, mapDirection, layerMapProperties, mapImage, gridSize, layerTime, static (Sphere s, float value, float applyRate) => s.InfluenceRadius *= float.Lerp(1.0F, value, applyRate));
                        break;
                    case PropertyLayerMapOpacityItemId:
                        ApplyLayerMapAction(spheres, mapDirection, layerMapProperties, mapImage, gridSize, layerTime, static (Sphere s, float value, float applyRate) => s.Color *= new Vector4(1.0F, 1.0F, 1.0F, float.Lerp(1.0F, value, applyRate)));
                        break;
                    case PropertyLayerMapScatteringItemId:
                        ApplyLayerMapAction(spheres, mapDirection, layerMapProperties, mapImage, gridSize, layerTime, static (Sphere s, float value, float applyRate) => s.InfluenceScattering *= float.Lerp(1.0F, value, applyRate));
                        break;
                    case PropertyLayerMapFractalNoiseItemId:
                        ApplyLayerMapAction(spheres, mapDirection, layerMapProperties, mapImage, gridSize, layerTime, static (Sphere s, float value, float applyRate) => s.InfluenceFractalNoise *= float.Lerp(1.0F, value, applyRate));
                        break;
                }
            }

            foreach (var image in imageCache.Values)
            {
                image.Dispose();
            }
        }

        static void ApplyDisplacementLayerMap(Sphere[] spheres, SphereGridLayerMapDirectionType mapDirection, IReadOnlyCollection<IPropertyObject> layerMapProperties, NManagedImage mapImage, Vector3d gridSize, in Time layerTime)
        {
            var displacement = layerMapProperties.GetValue(PropertyLayerMapDisplacementMoveId, layerTime, Vector3d.Zero);
            if (displacement == Vector3d.Zero)
            {
                return;
            }

            var channelX = layerMapProperties.GetValue(PropertyLayerMapDisplacementXChannelId, layerTime, WithHSLLOnOffChannelType.Luminance);
            var channelY = layerMapProperties.GetValue(PropertyLayerMapDisplacementYChannelId, layerTime, WithHSLLOnOffChannelType.Luminance);
            var channelZ = layerMapProperties.GetValue(PropertyLayerMapDisplacementZChannelId, layerTime, WithHSLLOnOffChannelType.Luminance);
            if (channelX == WithHSLLOnOffChannelType.Half && channelY == WithHSLLOnOffChannelType.Half && channelZ == WithHSLLOnOffChannelType.Half)
            {
                return;
            }

            var layerPosition = layerMapProperties.GetValue(PropertyLayerMapValueLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var gridCenter = gridSize * 0.5;

            switch (mapDirection)
            {
                case SphereGridLayerMapDirectionType.XY:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.X) * 0.5, (mapImage.Height - gridSize.Y) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.X - 1.0), (mapImage.Height - 1) / (gridSize.Y - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            var sourceDataSpan = mapImage.GetDataSpan();
                            var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                            var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Y + gridCenter.Y));

                            var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                            s.Position += new Vector3d(
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelX),
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelY),
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelZ)
                            ) * displacement;
                        });
                    }
                    break;
                case SphereGridLayerMapDirectionType.XZ:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.X) * 0.5, (mapImage.Height - gridSize.Z) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.X - 1.0), (mapImage.Height - 1) / (gridSize.Z - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            var sourceDataSpan = mapImage.GetDataSpan();
                            var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                            var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                            var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                            s.Position += new Vector3d(
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelX),
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelY),
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelZ)
                            ) * displacement;
                        });
                    }
                    break;
                case SphereGridLayerMapDirectionType.YZ:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.Y) * 0.5, (mapImage.Height - gridSize.Z) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.Y - 1.0), (mapImage.Height - 1) / (gridSize.Z - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            var sourceDataSpan = mapImage.GetDataSpan();
                            var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.Y + gridCenter.Y));
                            var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                            var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                            s.Position += new Vector3d(
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelX),
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelY),
                                DisplacementMapGenerator.CalcMoveRate(mapColor, channelZ)
                            ) * displacement;
                        });
                    }
                    break;
            }
        }

        static void ApplyLayerColorLayerMap(Sphere[] spheres, SphereGridLayerMapDirectionType mapDirection, IReadOnlyCollection<IPropertyObject> layerMapProperties, NManagedImage mapImage, Vector3d gridSize, in Time layerTime)
        {
            var applyColorChannel = layerMapProperties.GetValue(PropertyLayerMapLayerColorChannelId, layerTime, SphereGridLayerMapLayerColorType.None);
            var applyRate = (float)(layerMapProperties.GetValue(PropertyLayerMapValueApplyRateId, layerTime, 0.0) * 0.01);
            if (applyColorChannel == SphereGridLayerMapLayerColorType.None || applyRate <= 0.0F)
            {
                return;
            }

            var layerPosition = layerMapProperties.GetValue(PropertyLayerMapValueLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var gridCenter = gridSize * 0.5;

            switch (mapDirection)
            {
                case SphereGridLayerMapDirectionType.XY:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.X) * 0.5, (mapImage.Height - gridSize.Y) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.X - 1.0), (mapImage.Height - 1) / (gridSize.Y - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        switch (applyColorChannel)
                        {
                            case SphereGridLayerMapLayerColorType.RGB:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Y + gridCenter.Y));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);
                                    mapColor.W = s.Color.W;

                                    s.Color = Vector4.Lerp(s.Color, mapColor, applyRate);
                                });
                                break;
                            case SphereGridLayerMapLayerColorType.ARGB:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Y + gridCenter.Y));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                                    s.Color = Vector4.Lerp(s.Color, mapColor, applyRate);
                                });
                                break;
                            case SphereGridLayerMapLayerColorType.A:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Y + gridCenter.Y));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);
                                    var newColor = s.Color;
                                    newColor.W = float.Lerp(newColor.W, mapColor.W, applyRate);

                                    s.Color = newColor;
                                });
                                break;
                        }
                    }
                    break;
                case SphereGridLayerMapDirectionType.XZ:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.X) * 0.5, (mapImage.Height - gridSize.Z) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.X - 1.0), (mapImage.Height - 1) / (gridSize.Z - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        switch (applyColorChannel)
                        {
                            case SphereGridLayerMapLayerColorType.RGB:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);
                                    mapColor.W = s.Color.W;

                                    s.Color = Vector4.Lerp(s.Color, mapColor, applyRate);
                                });
                                break;
                            case SphereGridLayerMapLayerColorType.ARGB:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                                    s.Color = Vector4.Lerp(s.Color, mapColor, applyRate);
                                });
                                break;
                            case SphereGridLayerMapLayerColorType.A:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);
                                    var newColor = s.Color;
                                    newColor.W = float.Lerp(newColor.W, mapColor.W, applyRate);

                                    s.Color = newColor;
                                });
                                break;
                        }
                    }
                    break;
                case SphereGridLayerMapDirectionType.YZ:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.Y) * 0.5, (mapImage.Height - gridSize.Z) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.Y - 1.0), (mapImage.Height - 1) / (gridSize.Z - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        switch (applyColorChannel)
                        {
                            case SphereGridLayerMapLayerColorType.RGB:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.Y + gridCenter.Y));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);
                                    mapColor.W = s.Color.W;

                                    s.Color = Vector4.Lerp(s.Color, mapColor, applyRate);
                                });
                                break;
                            case SphereGridLayerMapLayerColorType.ARGB:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.Y + gridCenter.Y));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                                    s.Color = Vector4.Lerp(s.Color, mapColor, applyRate);
                                });
                                break;
                            case SphereGridLayerMapLayerColorType.A:
                                Parallel.For(0, spheres.Length, i =>
                                {
                                    var s = spheres[i];
                                    var sourceDataSpan = mapImage.GetDataSpan();
                                    var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.Y + gridCenter.Y));
                                    var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                                    var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                        ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);
                                    var newColor = s.Color;
                                    newColor.W = float.Lerp(newColor.W, mapColor.W, applyRate);

                                    s.Color = newColor;
                                });
                                break;
                        }
                    }
                    break;
            }
        }

        static void ApplyLayerMapAction(Sphere[] spheres, SphereGridLayerMapDirectionType mapDirection, IReadOnlyCollection<IPropertyObject> layerMapProperties, NManagedImage mapImage, Vector3d gridSize, in Time layerTime, Action<Sphere, float, float> action)
        {
            var applyRate = (float)(layerMapProperties.GetValue(PropertyLayerMapValueApplyRateId, layerTime, 0.0) * 0.01);
            if (applyRate <= 0.0F)
            {
                return;
            }

            var channel = layerMapProperties.GetValue(PropertyLayerMapValueChannelId, layerTime, WithHSLLOnOffChannelType.Luminance);
            var layerPosition = layerMapProperties.GetValue(PropertyLayerMapValueLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var gridCenter = gridSize * 0.5;

            switch (mapDirection)
            {
                case SphereGridLayerMapDirectionType.XY:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.X) * 0.5, (mapImage.Height - gridSize.Y) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.X - 1.0), (mapImage.Height - 1) / (gridSize.Y - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            var sourceDataSpan = mapImage.GetDataSpan();
                            var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                            var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Y + gridCenter.Y));

                            var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                            action(s, Math.Clamp(DisplacementMapGenerator.CalcMoveRate(mapColor, channel) * 0.5F + 0.5F, 0.0F, 1.0F), applyRate);
                        });
                    }
                    break;
                case SphereGridLayerMapDirectionType.XZ:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.X) * 0.5, (mapImage.Height - gridSize.Z) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.X - 1.0), (mapImage.Height - 1) / (gridSize.Z - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            var sourceDataSpan = mapImage.GetDataSpan();
                            var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.X + gridCenter.X));
                            var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                            var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                            action(s, Math.Clamp(DisplacementMapGenerator.CalcMoveRate(mapColor, channel) * 0.5F + 0.5F, 0.0F, 1.0F), applyRate);
                        });
                    }
                    break;
                case SphereGridLayerMapDirectionType.YZ:
                    {
                        var (sourceStartX, sourceStartY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => (0.0, 0.0),
                            _ => ((mapImage.Width - gridSize.Y) * 0.5, (mapImage.Height - gridSize.Z) * 0.5)
                        };
                        var (sourceDiffX, sourceDiffY) = layerPosition switch
                        {
                            SourceLayerPositionType.Stretch => ((mapImage.Width - 1) / (gridSize.Y - 1.0), (mapImage.Height - 1) / (gridSize.Z - 1.0)),
                            _ => (1.0, 1.0)
                        };

                        Parallel.For(0, spheres.Length, i =>
                        {
                            var s = spheres[i];
                            var sourceDataSpan = mapImage.GetDataSpan();
                            var sourceX = (float)(sourceStartX + sourceDiffX * (s.GridPosition.Y + gridCenter.Y));
                            var sourceY = (float)(sourceStartY + sourceDiffY * (s.GridPosition.Z + gridCenter.Z));

                            var mapColor = layerPosition == SourceLayerPositionType.Loop ?
                                ImageInterpolation.BilinearLoop(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY) :
                                ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, mapImage.Width, mapImage.Height, sourceX, sourceY);

                            action(s, Math.Clamp(DisplacementMapGenerator.CalcMoveRate(mapColor, channel) * 0.5F + 0.5F, 0.0F, 1.0F), applyRate);
                        });
                    }
                    break;
            }
        }
    }

    enum SphereGridGraphMapDirectionType
    {
        None,
        X,
        Y,
        Z
    }

    enum SphereGridLayerMapDirectionType
    {
        None,
        XY,
        XZ,
        YZ
    }

    enum SphereGridLayerMapLayerColorType
    {
        None,
        RGB,
        ARGB,
        A
    }

    class Sphere
    {
        public Vector3d Position { get; set; }

        public Vector3d GridPosition { get; set; }

        public Vector4 Color { get; set; }

        public double Radius { get; set; }

        public float Softness { get; set; }

        public Vector3d ScatteringPosition { get; set; }

        public float FractalNoiseValue { get; set; } = 1.0F;

        public double FractalNoiseSizeApplyRate { get; set; } = 0.0;

        public float FractalNoiseOpacityApplyRate { get; set; } = 0.0F;

        public double FractalNoiseScatteringApplyRate { get; set; } = 0.0;

        public Vector3d FractalNoiseDisplacement { get; set; }

        public double InfluenceRadius { get; set; } = 1.0;

        public double InfluenceScattering { get; set; } = 1.0;

        public float InfluenceFractalNoise { get; set; } = 1.0F;

        public float NormalizedGridX { get; init; }

        public float NormalizedGridY { get; init; }

        public float NormalizedGridZ { get; init; }

        public Sphere(in Vector3d position, in Vector3d gridPosition, in Vector4 color, double radius, float softness)
        {
            Position = position;
            GridPosition = gridPosition;
            Color = color;
            Radius = radius;
            Softness = softness;
        }
    }

    record PreProcessedSphere(Vector256<double> Position, Vector4 Color, double Radius, float Softness);

    file record RasterizableSphere(Vector3 Position, Vector4 Color, float Radius, float Softness) : IComparable<RasterizableSphere>
    {
        public float Top { get; } = Position.Y - Radius;

        public float Bottom { get; } = Position.Y + Radius;

        public float Left { get; } = Position.X - Radius;

        public float Right { get; } = Position.X + Radius;

        public Vector2 ScreenPosition { get; } = Position.AsVector2();

        public float Z { get; } = Position.Z;

        public float RadiusSquared { get; } = Radius * Radius;

        public float InvertedRadius { get; } = 1.0F / Radius;

        public float InvertedSoftness { get; } = Softness > 0.0F ? 1.0F / Softness : float.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(RasterizableSphere? other)
        {
            return Z.CompareTo(other?.Z ?? float.MinValue);
        }
    }

    file abstract class SphereRendererBase
    {
        protected Matrix4x4d ModelViewMatrix { get; }

        protected double FieldOfView { get; }

        protected List<PreProcessedSphere> Spheres { get; } = [];

        protected double MinZ { get; private set; } = double.MaxValue;

        protected double MaxZ { get; private set; } = double.MinValue;

        protected float OffsetX { get; }

        protected float OffsetY { get; }

        protected int RenderSize { get; }

        public SphereRendererBase(in Matrix4x4d modelViewMatrix, double fov, int canvasWidth, int canvasHeight)
        {
            ModelViewMatrix = modelViewMatrix;
            FieldOfView = fov;
            RenderSize = Math.Max(canvasWidth, canvasHeight);
            OffsetX = (RenderSize - canvasWidth) * 0.5F;
            OffsetY = (RenderSize - canvasHeight) * 0.5F;
        }

        public abstract void Render(ROI roi, BlendMode blendMode);

        public abstract void RenderAntiAlias(ROI roi, BlendMode blendMode);

        public void AddSphere(in Vector3d position, in Vector4 color, double radius, float softness)
        {
            var p = (position.AsVector256() + Vector256.Create(0.0, 0.0, 0.0, RenderSize)) / RenderSize;
            var mvp = ModelViewMatrix.Transform(p);
            if (mvp.GetElement(2) <= TriangleDivider.NearZ - TriangleDivider.Epsilon)
            {
                return;
            }

            Spheres.Add(new PreProcessedSphere(mvp, color, radius / RenderSize, softness));
            MinZ = Math.Min(MinZ, mvp.GetElement(2));
            MaxZ = Math.Max(MaxZ, mvp.GetElement(2));
        }

        /// <summary>
        /// 要ArrayPool<RasterizableSphere>.Shared.Return(rasterizableSpheres);
        /// </summary>
        /// <param name="roi"></param>
        /// <returns></returns>
        protected (int rasterizableCount, RasterizableSphere[] rasterizableSpheres) GenerateRasterizableSpheres(ROI roi)
        {
            var rasterizableSpheres = ArrayPool<RasterizableSphere>.Shared.Rent(Spheres.Count);
            var rasterizableCount = 0;
            var projectionMatrix = Matrix4x4d.CreatePerspectiveFieldOfView(FieldOfView, 1.0, MinZ, MaxZ);
            foreach (var s in Spheres)
            {
                var pos = projectionMatrix.Transform(s.Position);
                pos /= pos.GetElement(3);
                var top = projectionMatrix.Transform(s.Position + Vector256.Create(0.0, s.Radius, 0.0, 0.0));
                top /= top.GetElement(3);
                var length = (pos - top).Length();
                var dv = (pos + Vector256.Create(1.0, 1.0, 0.0, 0.0)) * Vector256.Create(RenderSize * 0.5, RenderSize * 0.5, 1.0, 1.0);
                var transformedRadius = length * RenderSize;

                if (length > 0.0 &&
                    dv.GetElement(0) + transformedRadius - OffsetX >= roi.Left &&
                    dv.GetElement(0) - transformedRadius - OffsetX <= roi.Right &&
                    dv.GetElement(1) + transformedRadius - OffsetY >= roi.Top &&
                    dv.GetElement(1) - transformedRadius - OffsetY <= roi.Bottom)
                {
                    rasterizableSpheres[rasterizableCount] = new RasterizableSphere(new Vector3((float)dv.GetElement(0), (float)dv.GetElement(1), (float)dv.GetElement(2)), s.Color, (float)transformedRadius, s.Softness);
                    rasterizableCount++;
                }
            }

            if (rasterizableCount < 1)
            {
                ArrayPool<RasterizableSphere>.Shared.Return(rasterizableSpheres);
                return (0, []);
            }

            rasterizableSpheres.AsSpan(0, rasterizableCount).Sort();

            return (rasterizableCount, rasterizableSpheres);
        }
    }

    file class SphereRendererCpu : SphereRendererBase
    {
        NManagedImage Canvas { get; }

        public SphereRendererCpu(in Matrix4x4d modelViewMatrix, double fov, NManagedImage canvas) : base(modelViewMatrix, fov, canvas.Width, canvas.Height)
        {
            Canvas = canvas;
        }

        public override void Render(ROI roi, BlendMode blendMode)
        {
            var (rasterizableCount, rasterizableSpheres) = GenerateRasterizableSpheres(roi);

            if (rasterizableCount < 1)
            {
                return;
            }

            var imageWidth = Canvas.Width;
            var imageData = Canvas.Data;
            var rasterizableSphereSpan = rasterizableSpheres.AsSpan(0, rasterizableCount);
            for (var i = 0; i < rasterizableSphereSpan.Length; i++)
            {
                var s = rasterizableSphereSpan[i];
                var top = Math.Max(roi.Top, (int)MathF.Floor(s.Top - OffsetY));
                var bottom = Math.Min(roi.Bottom, (int)MathF.Ceiling(s.Bottom - OffsetY));
                if (top >= bottom)
                {
                    continue;
                }
                if (s.InvertedSoftness < float.MaxValue)
                {
                    Parallel.For(top, bottom, y =>
                    {
                        var bx = Math.Max(roi.Left, (int)MathF.Floor(s.Left - OffsetX));
                        var ex = Math.Min(roi.Right, (int)MathF.Ceiling(s.Right - OffsetX));
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = bx; x < ex; x++)
                        {
                            var length = Vector2.Distance(new Vector2(x + OffsetX, y + OffsetY), s.ScreenPosition);
                            if (s.Radius < length)
                            {
                                continue;
                            }

                            var color = s.Color;
                            color.W *= Math.Clamp((1.0F - length * s.InvertedRadius) * s.InvertedSoftness, 0.0F, 1.0F);
                            imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], color);
                        }
                    });
                }
                else
                {
                    Parallel.For(top, bottom, y =>
                    {
                        var bx = Math.Max(roi.Left, (int)MathF.Floor(s.Left - OffsetX));
                        var ex = Math.Min(roi.Right, (int)MathF.Ceiling(s.Right - OffsetX));
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = bx; x < ex; x++)
                        {
                            var length = Vector2.Distance(new Vector2(x + OffsetX, y + OffsetY), s.ScreenPosition);
                            if (s.Radius < length)
                            {
                                continue;
                            }

                            imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], s.Color);
                        }
                    });
                }
            }

            ArrayPool<RasterizableSphere>.Shared.Return(rasterizableSpheres);
        }

        public override void RenderAntiAlias(ROI roi, BlendMode blendMode)
        {
            const int SuperSamplingCount = 2;
            const int TotalSuperSamplingCount = SuperSamplingCount * SuperSamplingCount;
            const float SuperSamplingDiff = 1.0F / SuperSamplingCount;

            var (rasterizableCount, rasterizableSpheres) = GenerateRasterizableSpheres(roi);

            if (rasterizableCount < 1)
            {
                return;
            }

            var imageWidth = Canvas.Width;
            var imageData = Canvas.Data;
            var rasterizableSphereSpan = rasterizableSpheres.AsSpan(0, rasterizableCount);
            for (var i = 0; i < rasterizableSphereSpan.Length; i++)
            {
                var s = rasterizableSphereSpan[i];
                var top = Math.Max(roi.Top, (int)MathF.Floor(s.Top - OffsetY));
                var bottom = Math.Min(roi.Bottom, (int)MathF.Ceiling(s.Bottom - OffsetY));
                if (top >= bottom)
                {
                    continue;
                }
                if (s.InvertedSoftness < float.MaxValue)
                {
                    Parallel.For(top, bottom, y =>
                    {
                        var bx = Math.Max(roi.Left, (int)MathF.Floor(s.Left - OffsetX));
                        var ex = Math.Min(roi.Right, (int)MathF.Ceiling(s.Right - OffsetX));
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = bx; x < ex; x++)
                        {
                            var alpha = 0.0F;
                            var sp = new Vector2(x + OffsetX, y + OffsetY);
                            for (var fy = 0; fy < SuperSamplingCount; fy++)
                            {
                                for (var fx = 0; fx < SuperSamplingCount; fx++)
                                {
                                    var length = Vector2.Distance(sp + new Vector2(fx, fy) * SuperSamplingDiff, s.ScreenPosition);
                                    if (s.Radius < length)
                                    {
                                        continue;
                                    }

                                    alpha += Math.Clamp((1.0F - length * s.InvertedRadius) * s.InvertedSoftness, 0.0F, 1.0F);
                                }
                            }

                            var color = s.Color;
                            color.W *= alpha / TotalSuperSamplingCount;
                            imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], color);
                        }
                    });
                }
                else
                {
                    Parallel.For(top, bottom, y =>
                    {
                        var bx = Math.Max(roi.Left, (int)MathF.Floor(s.Left - OffsetX));
                        var ex = Math.Min(roi.Right, (int)MathF.Ceiling(s.Right - OffsetX));
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = bx; x < ex; x++)
                        {
                            var alpha = 0.0F;
                            var sp = new Vector2(x + OffsetX, y + OffsetY);
                            for (var fy = 0; fy < SuperSamplingCount; fy++)
                            {
                                for (var fx = 0; fx < SuperSamplingCount; fx++)
                                {
                                    var length = Vector2.Distance(sp + new Vector2(fx, fy) * SuperSamplingDiff, s.ScreenPosition);
                                    if (s.Radius < length)
                                    {
                                        continue;
                                    }

                                    alpha += 1.0F;
                                }
                            }

                            var color = s.Color;
                            color.W *= alpha / TotalSuperSamplingCount;
                            imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], color);
                        }
                    });
                }
            }

            ArrayPool<RasterizableSphere>.Shared.Return(rasterizableSpheres);
        }
    }

    file class SphereRendererGpu : SphereRendererBase
    {
        GraphicsDevice Device { get; }

        NGPUImage Canvas { get; }

        public SphereRendererGpu(in Matrix4x4d modelViewMatrix, double fov, GraphicsDevice device, NGPUImage canvas) : base(modelViewMatrix, fov, canvas.Width, canvas.Height)
        {
            Device = device;
            Canvas = canvas;
        }

        public override void Render(ROI roi, BlendMode blendMode)
        {
            var (rasterizableCount, rasterizableSpheres) = GenerateRasterizableSpheres(roi);

            if (rasterizableCount < 1)
            {
                return;
            }

            using var context = Device.CreateComputeContext();
            var imageData = Canvas.Data;
            var rasterizableSphereSpan = rasterizableSpheres.AsSpan(0, rasterizableCount);
            var intBlendMode = (int)blendMode;
            for (var i = 0; i < rasterizableSphereSpan.Length; i++)
            {
                var s = rasterizableSphereSpan[i];
                var top = Math.Max(roi.Top, (int)MathF.Floor(s.Top - OffsetY));
                var bottom = Math.Min(roi.Bottom, (int)MathF.Ceiling(s.Bottom - OffsetY));
                if (top >= bottom)
                {
                    continue;
                }

                var left = Math.Max(roi.Left, (int)MathF.Floor(s.Left - OffsetX));
                var right = Math.Min(roi.Right, (int)MathF.Ceiling(s.Right - OffsetX));
                if (left >= right)
                {
                    continue;
                }

                if (s.InvertedSoftness < float.MaxValue)
                {
                    context.For(right - left, bottom - top, new SphereGridRenderSoftProcess(imageData, Canvas.Width, OffsetX, OffsetY, s.Radius, s.ScreenPosition, s.Color, s.InvertedSoftness, intBlendMode, left, top));
                }
                else
                {
                    context.For(right - left, bottom - top, new SphereGridRenderProcess(imageData, Canvas.Width, OffsetX, OffsetY, s.Radius, s.ScreenPosition, s.Color, intBlendMode, left, top));
                }
                context.Barrier(imageData);
            }

            ArrayPool<RasterizableSphere>.Shared.Return(rasterizableSpheres);
        }

        public override void RenderAntiAlias(ROI roi, BlendMode blendMode)
        {
            var (rasterizableCount, rasterizableSpheres) = GenerateRasterizableSpheres(roi);

            if (rasterizableCount < 1)
            {
                return;
            }

            using var context = Device.CreateComputeContext();
            var imageData = Canvas.Data;
            var rasterizableSphereSpan = rasterizableSpheres.AsSpan(0, rasterizableCount);
            var intBlendMode = (int)blendMode;
            for (var i = 0; i < rasterizableSphereSpan.Length; i++)
            {
                var s = rasterizableSphereSpan[i];
                var top = Math.Max(roi.Top, (int)MathF.Floor(s.Top - OffsetY));
                var bottom = Math.Min(roi.Bottom, (int)MathF.Ceiling(s.Bottom - OffsetY));
                if (top >= bottom)
                {
                    continue;
                }

                var left = Math.Max(roi.Left, (int)MathF.Floor(s.Left - OffsetX));
                var right = Math.Min(roi.Right, (int)MathF.Ceiling(s.Right - OffsetX));
                if (left >= right)
                {
                    continue;
                }

                if (s.InvertedSoftness < float.MaxValue)
                {
                    context.For(right - left, bottom - top, new SphereGridRenderSoftAntiAliasProcess(imageData, Canvas.Width, OffsetX, OffsetY, s.Radius, s.ScreenPosition, s.Color, s.InvertedSoftness, intBlendMode, left, top));
                }
                else
                {
                    context.For(right - left, bottom - top, new SphereGridRenderAntiAliasProcess(imageData, Canvas.Width, OffsetX, OffsetY, s.Radius, s.ScreenPosition, s.Color, intBlendMode, left, top));
                }
                context.Barrier(imageData);
            }

            ArrayPool<RasterizableSphere>.Shared.Return(rasterizableSpheres);
        }
    }

    file readonly record struct LayerMapCacheKey(Guid LayerId, Time layerTime);

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SphereGridRenderProcess(ReadWriteBuffer<Float4> image, int width, float offsetX, float offsetY, float radius, Float2 screenPosition, Float4 color, int blendMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var length = Hlsl.Distance(new Float2(x + offsetX, y + offsetY), screenPosition);
            if (radius < length)
            {
                return;
            }

            var pos = y * width + x;
            image[pos] = BlendMethods.Process(blendMode, image[pos], color);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SphereGridRenderSoftProcess(ReadWriteBuffer<Float4> image, int width, float offsetX, float offsetY, float radius, Float2 screenPosition, Float4 color, float invertedSoftness, int blendMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var length = Hlsl.Distance(new Float2(x + offsetX, y + offsetY), screenPosition);
            if (radius < length)
            {
                return;
            }

            var resultColor = color;
            if (invertedSoftness < float.MaxValue)
            {
                resultColor.W *= Hlsl.Clamp((1.0F - length * (1.0F / radius)) * invertedSoftness, 0.0F, 1.0F);
            }
            var pos = y * width + x;
            image[pos] = BlendMethods.Process(blendMode, image[pos], resultColor);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SphereGridRenderAntiAliasProcess(ReadWriteBuffer<Float4> image, int width, float offsetX, float offsetY, float radius, Float2 screenPosition, Float4 color, int blendMode, int startX, int startY) : IComputeShader
    {
        const int SuperSamplingCount = 2;

        const int TotalSuperSamplingCount = SuperSamplingCount * SuperSamplingCount;

        const float SuperSamplingDiff = 1.0F / SuperSamplingCount;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var p = new Float2(x + offsetX, y + offsetY);
            var alpha = 0.0F;
            for (var fy = 0; fy < SuperSamplingCount; fy++)
            {
                for (var fx = 0; fx < SuperSamplingCount; fx++)
                {
                    var length = Hlsl.Distance(p + new Float2(fx, fy) * SuperSamplingDiff, screenPosition);
                    if (radius < length)
                    {
                        continue;
                    }

                    alpha += 1.0F;
                }
            }

            var resultColor = color;
            resultColor.W *= alpha / TotalSuperSamplingCount;
            var pos = y * width + x;
            image[pos] = BlendMethods.Process(blendMode, image[pos], resultColor);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SphereGridRenderSoftAntiAliasProcess(ReadWriteBuffer<Float4> image, int width, float offsetX, float offsetY, float radius, Float2 screenPosition, Float4 color, float invertedSoftness, int blendMode, int startX, int startY) : IComputeShader
    {
        const int SuperSamplingCount = 2;

        const int TotalSuperSamplingCount = SuperSamplingCount * SuperSamplingCount;

        const float SuperSamplingDiff = 1.0F / SuperSamplingCount;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var p = new Float2(x + offsetX, y + offsetY);
            var alpha = 0.0F;
            for (var fy = 0; fy < SuperSamplingCount; fy++)
            {
                for (var fx = 0; fx < SuperSamplingCount; fx++)
                {
                    var length = Hlsl.Distance(p + new Float2(fx, fy) * SuperSamplingDiff, screenPosition);
                    if (radius < length)
                    {
                        continue;
                    }

                    alpha += Hlsl.Clamp((1.0F - length * (1.0F / radius)) * invertedSoftness, 0.0F, 1.0F);
                }
            }

            var resultColor = color;
            resultColor.W *= alpha / TotalSuperSamplingCount;
            var pos = y * width + x;
            image[pos] = BlendMethods.Process(blendMode, image[pos], resultColor);
        }
    }
}
