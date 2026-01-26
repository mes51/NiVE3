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
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Property.Properties;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Simulation
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Simulation_SphereGrid_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Simulation, LanguageResourceDictionary.Simulation_SphereGrid_Description, ID, IsSupportGpu = true, UseCompositionCamera = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
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

        const string PropertyFractalNoiseOctarveId = nameof(PropertyFractalNoiseOctarveId);

        const string PropertyFractalNoiseScaleId = nameof(PropertyFractalNoiseScaleId);

        const string PropertyFractalNoisePositionId = nameof(PropertyFractalNoisePositionId);

        const string PropertyFractalNoiseEvolutionId = nameof(PropertyFractalNoiseEvolutionId);

        const string PropertyFractalNoiseRandomSeedId = nameof(PropertyFractalNoiseRandomSeedId);

        const string PropertyFractalNoiseApplySizeId = nameof(PropertyFractalNoiseApplySizeId);

        const string PropertyFractalNoiseApplyOpacityId = nameof(PropertyFractalNoiseApplyOpacityId);

        const string PropertyFractalNoiseApplyScattering = nameof(PropertyFractalNoiseApplyScattering);

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

        const string PropertyLayerMapValueMapDirectionId = nameof(PropertyLayerMapValueMapDirectionId);

        const string PropertyLayerMapValueChannelId = nameof(PropertyLayerMapValueChannelId);

        const string PropertyLayerMapValueApplyId = nameof(PropertyLayerMapValueApplyId);

        const string PropertyRenderingGroupId = nameof(PropertyRenderingGroupId);

        const string PropertyRenderingAntiAliasId = nameof(PropertyRenderingAntiAliasId);

        const string PropertyRenderingParticleBlendModeId = nameof(PropertyRenderingParticleBlendModeId);

        const string PropertyRenderingParticleOnlyId = nameof(PropertyRenderingParticleOnlyId);

        const string PropertyRenderingBlendModeId = nameof(PropertyRenderingBlendModeId);

        const string PropertyRenderingCompositeOrderId = nameof(PropertyRenderingCompositeOrderId);

        #endregion Property ids

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
                    new Vector3dProperty(PropertyArrangementParticleCountId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_ParticleCount, new Vector3d(25.0, 25.0, 3.0), digit: 0, is3D: true),
                    new Vector3dProperty(PropertyArrangementTwistId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_Twist, Vector3d.Zero, digit: 2, is3D: true, separator: ",", unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                    new DoubleProperty(PropertyArrangementScatteringId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_Scattering, 0.0, 0.0, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyArrangementScatteringRandomSeedId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Arrangment_ScatteringRandomSeed, 0, int.MinValue, int.MaxValue, digit: 0)
                ]),
                new PropertyGroup(PropertyParticleGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle,
                [
                    new DoubleProperty(PropertyParticleSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Size, 5.0, 0.0, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyParticleSoftnessId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Softness, 50.0, 0.0, 100.0, digit: 2),
                    new ColorProperty(PropertyParticleColorId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Color, colorDialogTitle, dialogOk, dialogCancel, Vector4.One),
                    new DoubleProperty(PropertyParticleOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Particle_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new EnumProperty(PropertyRenderingParticleBlendModeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_Rendering_ParticleBlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Add, selectBoxWidth: 90.0)
                ]),
                new PropertyGroup(PropertyFractalNoiseGroupId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise,
                [
                    new DoubleProperty(PropertyFractalNoiseOctarveId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Octave, 6.0, 1.0, 20.0, digit: 2, slideChangeValue: 0.1),
                    new Vector3dProperty(PropertyFractalNoiseScaleId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Scale, new Vector3d(100.0), Vector3d.Zero, new Vector3d(double.MaxValue), digit: 2, is3D: true, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, separator: ",", useLinkRatio: true),
                    new Vector3dProperty(PropertyFractalNoisePositionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Position, center, digit: 2, is3D: true, useInteraction: true),
                    new AngleProperty(PropertyFractalNoiseEvolutionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_Evolution, 0.0, digit: 2),
                    new DoubleProperty(PropertyFractalNoiseRandomSeedId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_RandomSeed, 0, int.MinValue, int.MaxValue, digit: 0),
                    new DoubleProperty(PropertyFractalNoiseApplySizeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_ApplySize, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyFractalNoiseApplyOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_ApplyOpacity, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new DoubleProperty(PropertyFractalNoiseApplyScattering, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_FractalNoise_ApplyScattering, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
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
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Apply, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new EnumProperty(PropertyLayerMapLayerColorChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_LayerColor_Channel, typeof(SphereGridLayerMapLayerColorType), typeof(LanguageResourceDictionary), SphereGridLayerMapLayerColorType.RGB, selectBoxWidth: 90.0)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapSizeItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Size, () => new PropertyGroup(PropertyLayerMapSizeItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Size,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Apply, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapOpacityItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Opacity, () => new PropertyGroup(PropertyLayerMapOpacityItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Opacity,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Apply, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapScatteringItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Scattering, () => new PropertyGroup(PropertyLayerMapScatteringItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Scattering,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Apply, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
                    ])),
                    new AppendablePropertyItem(PropertyLayerMapFractalNoiseItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_FractalNoise, () => new PropertyGroup(PropertyLayerMapFractalNoiseItemId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_FractalNoise,
                    [
                        new UseLayerImageProperty(PropertyLayerMapValueLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Layer, selectBoxWidth: 90.0),
                        new CheckBoxProperty(PropertyLayerMapValueUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertyLayerMapValueSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new EnumProperty(PropertyLayerMapValueMapDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_MapDirection, typeof(SphereGridLayerMapDirectionType), typeof(LanguageResourceDictionary), SphereGridLayerMapDirectionType.XY, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyLayerMapValueChannelId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Channel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.Luminance, selectBoxWidth: 90.0),
                        new DoubleProperty(PropertyLayerMapValueApplyId, LanguageResourceDictionary.ResourceKeys.Simulation_SphereGrid_LayerMap_Value_Apply, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
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

            var particleSize = particleGroup.GetValue(PropertyParticleSizeId, layerTime, 0.0) * 0.5;
            var particleCount = arrangementGroup.GetValue(PropertyArrangementParticleCountId, layerTime, Vector3d.Zero);
            var particleCountX = (int)particleCount.X;
            var particleCountY = (int)particleCount.Y;
            var particleCountZ = (int)particleCount.Z;
            if (particleCountX < 1 || particleCountY < 0 || particleCountZ < 0 || particleSize <= 0.0)
            {
                return image;
            }

            var softness = (float)(particleGroup.GetValue(PropertyParticleSoftnessId, layerTime, 0.0) * 0.01);
            var color = particleGroup.GetValue(PropertyParticleColorId, layerTime, Vector4.Zero);
            var gridSize = arrangementGroup.GetValue(PropertyArrangementGridSizeId, layerTime, Vector3d.Zero);
            var gridCenter = new Vector3d(
                particleCountX > 1 ? gridSize.X : 0.0,
                particleCountY > 1 ? gridSize.Y : 0.0,
                particleCountZ > 1 ? gridSize.Z : 0.0
            ) * 0.5;
            var sphereOffset = new Vector3d(image.Width, image.Height, 0.0) * 0.5 - gridCenter;

            var gridSizeDiff = new Vector3d(
                particleCountX > 1 ? gridSize.X / (particleCountX - 1) : 0.0,
                particleCountY > 1 ? gridSize.Y / (particleCountY - 1) : 0.0,
                particleCountZ > 1 ? gridSize.Z / (particleCountZ - 1) : 0.0
            );
            var spheres = new Sphere[particleCountX * particleCountY * particleCountZ];
            for (int z = 0, p = 0; z < particleCountZ; z++)
            {
                for (var y = 0; y < particleCountY; y++)
                {
                    for (var x = 0; x < particleCountX; x++, p++)
                    {
                        spheres[p] = new Sphere(gridSizeDiff * new Vector3d(x, y, z) + sphereOffset, Vector4.One, particleSize, softness);
                    }
                }
            }

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

            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                using var canvas = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
                var renderer = new SphereRendererGpu(mvt, fov, device, canvas);
                for (var i = 0; i < spheres.Length; i++)
                {
                    var s = spheres[i];
                    var radius = s.Radius * s.InfluenceRadius;
                    if (radius <= 0.0)
                    {
                        continue;
                    }
                    renderer.AddSphere(s.Position, s.Color, s.Radius, s.Softness);
                }

                renderer.Render(roi, BlendMode.Add);
                ImageBlendProcessor.SameSizeNoSkipTransparentFrontGpu(device, gpuImage, canvas, roi, BlendMode.Replace);

                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                using var canvas = new NManagedImage(managedImage.Width, managedImage.Height);
                var renderer = new SphereRendererCpu(mvt, fov, canvas);
                for (var i = 0; i < spheres.Length; i++)
                {
                    var s = spheres[i];
                    var radius = s.Radius * s.InfluenceRadius;
                    if (radius <= 0.0)
                    {
                        continue;
                    }
                    renderer.AddSphere(s.Position, s.Color, s.Radius, s.Softness);
                }

                renderer.Render(roi, BlendMode.Add);
                ImageBlendProcessor.SameSizeNoSkipTransparentFrontCpu(managedImage, canvas, roi, BlendMode.Replace);

                return managedImage;
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
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

        public Vector4 Color { get; set; }

        public double Radius { get; set; }

        public float Softness { get; set; }

        public double InfluenceRadius { get; set; } = 1.0;

        public double InfluenceScattering { get; set; } = 1.0;

        public double InfluenceFractalNoise { get; set; } = 1.0;

        public Sphere(in Vector3d position, in Vector4 color, double radius, float softness)
        {
            Position = position;
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
                        if (s.InvertedSoftness < float.MaxValue)
                        {
                            color.W *= (1.0F - length * s.InvertedRadius) * s.InvertedSoftness;
                        }
                        imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], color);
                    }
                });
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

                context.For(right - left, bottom - top, new SphereGridRenderProcess(imageData, Canvas.Width, OffsetX, OffsetY, s.Radius, s.ScreenPosition, s.Color, s.InvertedSoftness, intBlendMode, left, top));
                context.Barrier(imageData);
            }

            ArrayPool<RasterizableSphere>.Shared.Return(rasterizableSpheres);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SphereGridRenderProcess(ReadWriteBuffer<Float4> image, int width, float offsetX, float offsetY, float radius, Float2 screenPosition, Float4 color, float invertedSoftness, int blendMode, int startX, int startY) : IComputeShader
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
                resultColor.W *= (1.0F - length * (1.0F / radius)) * invertedSoftness;
            }
            var pos = y * width + x;
            image[pos] = BlendMethods.Process(blendMode, image[pos], resultColor);
        }
    }
}
