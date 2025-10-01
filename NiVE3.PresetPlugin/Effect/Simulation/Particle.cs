using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Color;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Util;
using NiVE3.PresetPlugin.Property;
using NiVE3.PresetPlugin.Property.Properties;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Simulation
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Simulation_Particle_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Simulation, LanguageResourceDictionary.Simulation_Particle_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, UseCompositionCamera = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Particle : IEffect
    {
        const string ID = "9D42E697-F4E9-4253-9EE6-EF2D0B41E869";

        #region Property ids

        const string PropertyCannonGroupId = nameof(PropertyCannonGroupId);

        const string PropertyCannonParticleGenerationRateId = nameof(PropertyCannonParticleGenerationRateId);

        const string PropertyCannonPositionId = nameof(PropertyCannonPositionId);

        const string PropertyCannonRadiusId = nameof(PropertyCannonRadiusId);

        const string PropertyCannonDirectionId = nameof(PropertyCannonDirectionId);

        const string PropertyCannonRandomDirectionId = nameof(PropertyCannonRandomDirectionId);

        const string PropertyCannonInitialParticleSpeedId = nameof(PropertyCannonInitialParticleSpeedId);

        const string PropertyCannonRandomInitialParticleSpeedId = nameof(PropertyCannonRandomInitialParticleSpeedId);

        const string PropertyCannonAddCannonMoveVelocityId = nameof(PropertyCannonAddCannonMoveVelocityId);

        const string PropertyCannonParticleRotateSpeedGroupId = nameof(PropertyCannonParticleRotateSpeedGroupId);

        const string PropertyCannonParticleRotateSpeedXId = nameof(PropertyCannonParticleRotateSpeedXId);

        const string PropertyCannonParticleRotateSpeedYId = nameof(PropertyCannonParticleRotateSpeedYId);

        const string PropertyCannonParticleRotateSpeedZId = nameof(PropertyCannonParticleRotateSpeedZId);

        const string PropertyCannonRandomParticleRotateSpeedId = nameof(PropertyCannonRandomParticleRotateSpeedId);

        const string PropertyParticleGroupId = nameof(PropertyParticleGroupId);

        const string PropertyCannonParticleLifetimeId = nameof(PropertyCannonParticleLifetimeId);

        const string PropertyParticleBirthColorId = nameof(PropertyParticleBirthColorId);

        const string PropertyParticleBirthColorVariationGroupId = nameof(PropertyParticleBirthColorVariationGroupId);

        const string PropertyParticleBirthColorVariationHueId = nameof(PropertyParticleBirthColorVariationHueId);

        const string PropertyParticleBirthColorVariationSaturationId = nameof(PropertyParticleBirthColorVariationSaturationId);

        const string PropertyParticleBirthColorVariationValueId = nameof(PropertyParticleBirthColorVariationValueId);

        const string PropertyParticleDeadColorId = nameof(PropertyParticleDeadColorId);

        const string PropertyParticleDeadColorVariationGroupId = nameof(PropertyParticleDeadColorVariationGroupId);

        const string PropertyParticleDeadColorVariationHueId = nameof(PropertyParticleDeadColorVariationHueId);

        const string PropertyParticleDeadColorVariationSaturationId = nameof(PropertyParticleDeadColorVariationSaturationId);

        const string PropertyParticleDeadColorVariationValueId = nameof(PropertyParticleDeadColorVariationValueId);

        const string PropertyParticleColorGraphId = nameof(PropertyParticleColorGraphId);

        const string PropertyParticleBirthSizeId = nameof(PropertyParticleBirthSizeId);

        const string PropertyParticleBirthSizeVariationId = nameof(PropertyParticleBirthSizeVariationId);

        const string PropertyParticleDeadSizeId = nameof(PropertyParticleDeadSizeId);

        const string PropertyParticleDeadSizeVariationId = nameof(PropertyParticleDeadSizeVariationId);

        const string PropertyParticleSizeGraphId = nameof(PropertyParticleSizeGraphId);

        const string PropertyParticleBirthOpacityId = nameof(PropertyParticleBirthOpacityId);

        const string PropertyParticleBirthOpacityVariationId = nameof(PropertyParticleBirthOpacityVariationId);

        const string PropertyParticleDeadOpacityId = nameof(PropertyParticleDeadOpacityId);

        const string PropertyParticleDeadOpacityVariationId = nameof(PropertyParticleDeadOpacityVariationId);

        const string PropertyParticleOpacityGraphId = nameof(PropertyParticleOpacityGraphId);

        const string PropertyWorldGroupId = nameof(PropertyWorldGroupId);

        const string PropertyWorldGravityId = nameof(PropertyWorldGravityId);

        const string PropertyWorldGravityDirectionId = nameof(PropertyWorldGravityDirectionId);

        const string PropertyWorldAirRegistanceId = nameof(PropertyWorldAirRegistanceId);

        const string PropertyCameraGroupId = nameof(PropertyCameraGroupId);

        const string PropertySourceLayerGroupId = nameof(PropertySourceLayerGroupId);

        const string PropertySourceLayerLayerId = nameof(PropertySourceLayerLayerId);

        const string PropertySourceLayerUseSpecificReferenceTimeId = nameof(PropertySourceLayerUseSpecificReferenceTimeId);

        const string PropertySourceLayerSpecificReferenceTimeId = nameof(PropertySourceLayerSpecificReferenceTimeId);

        const string PropertyRenderingGroupId = nameof(PropertyRenderingGroupId);

        const string PropertyRenderingAntiAliasId = nameof(PropertyRenderingAntiAliasId);

        const string PropertyRenderingParticleBlendModeId = nameof(PropertyRenderingParticleBlendModeId);

        const string PropertyRenderingBlendModeId = nameof(PropertyRenderingBlendModeId);

        const string PropertyRenderingCompositeOrderId = nameof(PropertyRenderingCompositeOrderId);

        const string PropertyOptionGroupId = nameof(PropertyOptionGroupId);

        const string PropertyOptionSimulationRateId = nameof(PropertyOptionSimulationRateId);

        const string PropertyOptionSimulationStartTimeOffsetId = nameof(PropertyOptionSimulationStartTimeOffsetId);

        const string PropertyOptionRandomSeedId = nameof(PropertyOptionRandomSeedId);

        #endregion Property ids

        const int SpeedRandomKey = 1;

        const int CannonPositionRandomKey = 2;

        const int CannonDirectionRandomKey = 3;

        const int RotationRandomKey = 4;

        const int BirthColorHueRandomKey = 5;

        const int BirthColorSaturationRandomKey = 6;

        const int BirthColorValueRandomKey = 7;

        const int DeadColorHueRandomKey = 8;

        const int DeadColorSaturationRandomKey = 9;

        const int DeadColorValueRandomKey = 10;

        const int BirthSizeRandomKey = 11;

        const int DeadSizeRandomKey = 12;

        const int BirthOpacityRandomKey = 13;

        const int DeadOpacityRandomKey = 14;

        IAcceleratorObject? AcceleratorObject { get; set; }

        Dictionary<Time, SimulatedParticleData[]> SimulatedParticles { get; } = [];

        List<Time> SimulatedParticleTimes { get; } = [];

        List<ParticleData> CurrentParticles { get; } = [];

        Time LastSimulateTime { get; set; } = Time.Zero;

        double LastSimulateFrameRate { get; set; }

        int LastSimulationRate { get; set; }

        double LastSimulateStartTimeOffset { get; set; }

        uint LastSimulateRandomSeed { get; set; }

        Int128 LastPropertyHash { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var dialogOk = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var dialogCancel = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            var colorDialogTitle = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            var cameraZoom = sourceSize.Width / Const.DefaultCameraFov * 0.5;
            return
            [
                new PropertyGroup(
                    PropertyCannonGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon,
                    [
                        new DoubleProperty(PropertyCannonParticleGenerationRateId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_GenerationRate, 30.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(PropertyCannonParticleLifetimeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_ParticleLifeTime, 5.0, 0.001, double.MaxValue, digit: 3, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second),
                        new Vector3dProperty(PropertyCannonPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_Position, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, is3D: true),
                        new Vector3dProperty(PropertyCannonRadiusId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_Radius, Vector3d.Zero, digit: 2, is3D: true, useInteraction: true),
                        new DirectionProperty(PropertyCannonDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_Direction, Vector3d.Zero, digit: 2),
                        new DoubleProperty(PropertyCannonRandomDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_RandomDirection, 20.0, 0.0, 180.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Angle),
                        new DoubleProperty(PropertyCannonInitialParticleSpeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_InitialParticleSpeed, 100.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(PropertyCannonRandomInitialParticleSpeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_RandomInitialParticleSpeed, 20.0, 0.0, double.MaxValue, digit: 2),
                        new CheckBoxProperty(PropertyCannonAddCannonMoveVelocityId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_AddCannonMoveVelocity, true),
                        new PropertyGroup(
                            PropertyCannonParticleRotateSpeedGroupId,
                            LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_ParticleRotateSpeed,
                            [
                                new AngleProperty(PropertyCannonParticleRotateSpeedXId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_ParticleRotateSpeed_X, 0.0, digit: 2),
                                new AngleProperty(PropertyCannonParticleRotateSpeedYId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_ParticleRotateSpeed_Y, 0.0, digit: 2),
                                new AngleProperty(PropertyCannonParticleRotateSpeedZId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_ParticleRotateSpeed_Z, 0.0, digit: 2)
                            ]
                        ),
                        new DoubleProperty(PropertyCannonRandomParticleRotateSpeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_RandomParticleRotateSpeed, 20.0, 0.0, double.MaxValue, digit: 2)
                    ]
                ),
                new PropertyGroup(
                    PropertyParticleGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle,
                    [
                        new ColorProperty(PropertyParticleBirthColorId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthColor, colorDialogTitle, dialogOk, dialogCancel, Vector4.One),
                        new PropertyGroup(
                            PropertyParticleBirthColorVariationGroupId,
                            LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthColorVariation,
                            [
                                new DoubleProperty(PropertyParticleBirthColorVariationHueId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthColorVariation_Hue, 0.0, 0.0, 360.0, digit: 2),
                                new DoubleProperty(PropertyParticleBirthColorVariationSaturationId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthColorVariation_Saturation, 0.0, 0.0, 1.0, slideChangeValue: 0.01, digit: 2),
                                new DoubleProperty(PropertyParticleBirthColorVariationValueId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthColorVariation_Value, 0.0, 0.0, 1.0, slideChangeValue: 0.01, digit: 2),
                            ]
                        ),
                        new ColorProperty(PropertyParticleDeadColorId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadColor, colorDialogTitle, dialogOk, dialogCancel, Vector4.UnitW),
                        new PropertyGroup(
                            PropertyParticleDeadColorVariationGroupId,
                            LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadColorVariation,
                            [
                                new DoubleProperty(PropertyParticleDeadColorVariationHueId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadColorVariation_Hue, 0.0, 0.0, 180.0, digit: 2),
                                new DoubleProperty(PropertyParticleDeadColorVariationSaturationId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadColorVariation_Saturation, 0.0, 0.0, 1.0, digit: 2),
                                new DoubleProperty(PropertyParticleDeadColorVariationValueId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadColorVariation_Value, 0.0, 0.0, 1.0, digit: 2),
                            ]
                        ),
                        new GraphValueProperty(PropertyParticleColorGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_ColorGraph, true, GraphValueParameter.Identity),
                        new DoubleProperty(PropertyParticleBirthSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthSize, 6.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(PropertyParticleBirthSizeVariationId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthSizeVariation, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(PropertyParticleDeadSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadSize, 60.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(PropertyParticleDeadSizeVariationId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadSizeVariation, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new GraphValueProperty(PropertyParticleSizeGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_SizeGraph, true, GraphValueParameter.Identity),
                        new DoubleProperty(PropertyParticleBirthOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthOpacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(PropertyParticleBirthOpacityVariationId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthOpacityVariation, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(PropertyParticleDeadOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadOpacity, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new DoubleProperty(PropertyParticleDeadOpacityVariationId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadOpacityVariation, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new GraphValueProperty(PropertyParticleOpacityGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_OpacityGraph, true, GraphValueParameter.LinearDown)
                    ]
                ),
                new PropertyGroup(
                    PropertyWorldGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World,
                    [
                        new DoubleProperty(PropertyWorldGravityId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World_Gravity, 50.0, 0.0, double.MaxValue, digit: 2),
                        new DirectionProperty(PropertyWorldGravityDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World_GravityDirection, new Vector3d(0.0, 0.0, 180.0), digit: 2),
                        new DoubleProperty(PropertyWorldAirRegistanceId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World_AirRegistance, 0.001, 0.0, double.MaxValue, digit: 6)
                    ]
                ),
                new PropertyGroup(
                    PropertyCameraGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera,
                    [
                        new CheckBoxProperty(CameraProperties.PropertyCameraUseCompositionId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_UseComposition, false),
                        new Vector3dProperty(CameraProperties.PropertyCameraPointOfInterestId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_PointOfInterest, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, is3D: true, useInteraction : true),
                        new Vector3dProperty(CameraProperties.PropertyCameraPositionId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Position, new Vector3d(sourceSize.Width * 0.5, sourceSize.Height * 0.5, -cameraZoom), digit: 2, is3D: true, useInteraction : true),
                        new DirectionProperty(CameraProperties.PropertyCameraOrientationId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Orientation, Vector3d.Zero, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraXAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_XAngle, 0.0, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraYAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_YAngle, 0.0, digit: 2),
                        new AngleProperty(CameraProperties.PropertyCameraZAngleId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_ZAngle, 0.0, digit: 2),
                        new DoubleProperty(CameraProperties.PropertyCameraZoomId, LanguageResourceDictionary.ResourceKeys.Effect_General_Camera_Zoom, cameraZoom, 0.01, double.MaxValue, digit: 2)
                    ]
                ),
                new PropertyGroup(
                    PropertySourceLayerGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer,
                    [
                        new UseLayerImageProperty(PropertySourceLayerLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer_Layer, 90.0),
                        new CheckBoxProperty(PropertySourceLayerUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertySourceLayerSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, false, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Second)
                    ]
                ),
                new PropertyGroup(
                    PropertyRenderingGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Rendering,
                    [
                        new CheckBoxProperty(PropertyRenderingAntiAliasId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Rendering_AntiAlias, true),
                        new EnumProperty(PropertyRenderingParticleBlendModeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Rendering_ParticleBlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyRenderingBlendModeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Rendering_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0),
                        new EnumProperty(PropertyRenderingCompositeOrderId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Rendering_CompositeOrder, typeof(CompositeOrder), typeof(LanguageResourceDictionary), CompositeOrder.Front, selectBoxWidth: 90.0),
                    ]
                ),
                new PropertyGroup(
                    PropertyOptionGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option,
                    [
                        new DoubleProperty(PropertyOptionSimulationRateId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option_SimulationRate, 16.0, 1.0, 100.0, false, digit: 0),
                        new DoubleProperty(PropertyOptionSimulationStartTimeOffsetId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option_SimulationStartTimeOffset, 0.0, double.MinValue, double.MaxValue, false, digit: 2),
                        new DoubleProperty(PropertyOptionRandomSeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option_RandomSeed, 0, 0, int.MaxValue, false, digit: 0)
                    ]
                )
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var options = properties.First(p => p.Id == PropertyOptionGroupId).GetChildren() ?? [];
            var simulationRate = (int)options.GetValue(PropertyOptionSimulationRateId, layerTime, 0.0);
            var simulateionStartTimeOffset = options.GetValue(PropertyOptionSimulationStartTimeOffsetId, layerTime, 0.0);
            var randomSeed = (uint)options.GetValue(PropertyOptionRandomSeedId, layerTime, 0.0);

            var hash = new XxHash3();
            foreach (var property in properties)
            {
                property.CalcValuesHash(hash);
            }
            var propertyHash = hash.ToInt128();

            if (LastSimulateFrameRate != composition.FrameRate ||
                LastSimulationRate != simulationRate ||
                LastSimulateStartTimeOffset != simulateionStartTimeOffset ||
                LastSimulateRandomSeed != randomSeed ||
                LastPropertyHash != propertyHash)
            {
                SimulatedParticles.Clear();
                SimulatedParticleTimes.Clear();
                CurrentParticles.Clear();
                LastSimulateTime = Time.FromTime(simulateionStartTimeOffset, composition.FrameRate);
                LastSimulateStartTimeOffset = simulateionStartTimeOffset;
            }

            if (LastSimulateTime <= layerTime)
            {
                var cannon = properties.First(p => p.Id == PropertyCannonGroupId).GetChildren() ?? [];
                var particleRotateSpeeds = cannon.First(p => p.Id == PropertyCannonParticleRotateSpeedGroupId).GetChildren() ?? [];
                var particle = properties.First(p => p.Id == PropertyParticleGroupId).GetChildren() ?? [];
                var birthColorVariation = particle.First(p => p.Id == PropertyParticleBirthColorVariationGroupId).GetChildren() ?? [];
                var deadColorVariation = particle.First(p => p.Id == PropertyParticleDeadColorVariationGroupId).GetChildren() ?? [];
                var world = properties.First(p => p.Id == PropertyWorldGroupId).GetChildren() ?? [];

                SimulateParticle(
                    layerTime + new Time(1, composition.FrameRate),
                    composition.FrameRate,
                    cannon.First(p => p.Id == PropertyCannonParticleGenerationRateId),
                    cannon.First(p => p.Id == PropertyCannonParticleLifetimeId),
                    cannon.First(p => p.Id == PropertyCannonPositionId),
                    cannon.First(p => p.Id == PropertyCannonRadiusId),
                    cannon.First(p => p.Id == PropertyCannonDirectionId),
                    cannon.First(p => p.Id == PropertyCannonRandomDirectionId),
                    cannon.First(p => p.Id == PropertyCannonInitialParticleSpeedId),
                    cannon.First(p => p.Id == PropertyCannonRandomInitialParticleSpeedId),
                    cannon.First(p => p.Id == PropertyCannonAddCannonMoveVelocityId),
                    particleRotateSpeeds.First(p => p.Id == PropertyCannonParticleRotateSpeedXId),
                    particleRotateSpeeds.First(p => p.Id == PropertyCannonParticleRotateSpeedYId),
                    particleRotateSpeeds.First(p => p.Id == PropertyCannonParticleRotateSpeedZId),
                    cannon.First(p => p.Id == PropertyCannonRandomParticleRotateSpeedId),
                    particle.First(p => p.Id == PropertyParticleBirthColorId),
                    birthColorVariation.First(p => p.Id == PropertyParticleBirthColorVariationHueId),
                    birthColorVariation.First(p => p.Id == PropertyParticleBirthColorVariationSaturationId),
                    birthColorVariation.First(p => p.Id == PropertyParticleBirthColorVariationValueId),
                    particle.First(p => p.Id == PropertyParticleDeadColorId),
                    deadColorVariation.First(p => p.Id == PropertyParticleDeadColorVariationHueId),
                    deadColorVariation.First(p => p.Id == PropertyParticleDeadColorVariationSaturationId),
                    deadColorVariation.First(p => p.Id == PropertyParticleDeadColorVariationValueId),
                    particle.First(p => p.Id == PropertyParticleColorGraphId),
                    particle.First(p => p.Id == PropertyParticleBirthSizeId),
                    particle.First(p => p.Id == PropertyParticleBirthSizeVariationId),
                    particle.First(p => p.Id == PropertyParticleDeadSizeId),
                    particle.First(p => p.Id == PropertyParticleDeadSizeVariationId),
                    particle.First(p => p.Id == PropertyParticleSizeGraphId),
                    particle.First(p => p.Id == PropertyParticleBirthOpacityId),
                    particle.First(p => p.Id == PropertyParticleBirthOpacityVariationId),
                    particle.First(p => p.Id == PropertyParticleDeadOpacityId),
                    particle.First(p => p.Id == PropertyParticleDeadOpacityVariationId),
                    particle.First(p => p.Id == PropertyParticleOpacityGraphId),
                    world.First(p => p.Id == PropertyWorldGravityId),
                    world.First(p => p.Id == PropertyWorldGravityDirectionId),
                    world.First(p => p.Id == PropertyWorldAirRegistanceId),
                    simulationRate,
                    randomSeed
                );

                LastSimulateFrameRate = composition.FrameRate;
                LastSimulationRate = simulationRate;
                LastSimulateRandomSeed = randomSeed;
                LastPropertyHash = propertyHash;
            }

            var sourceLayer = properties.First(p => p.Id == PropertySourceLayerGroupId).GetChildren() ?? [];
            var sourceLayerTarget = sourceLayer.GetValue(PropertySourceLayerLayerId, layerTime, UseLayerImageTarget.Empty);
            var sourceLayerObject = composition.GetLayer(sourceLayerTarget.LayerId);
            var sourceLayerReferenceTime = layerTime + layer.SourceStartPoint;
            if (sourceLayer.GetValue(PropertySourceLayerUseSpecificReferenceTimeId, layerTime, false) && sourceLayerObject != null)
            {
                sourceLayerReferenceTime = Time.FromTime(sourceLayer.GetValue(PropertySourceLayerSpecificReferenceTimeId, layerTime, 0.0), composition.FrameRate) + sourceLayerObject.SourceStartPoint;
            }

            var realWidth = (int)Math.Round(image.Width * downSamplingRateX);
            var realHeight = (int)Math.Round(image.Height * downSamplingRateY);
            using var texture = GetLayerImage(sourceLayerObject, sourceLayerReferenceTime, sourceLayerTarget.ImageProcessType, downSamplingRateX, useGpu ? AcceleratorObject : null);
            NImage renderTarget;
            Renderer3DBase renderer;
            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                renderTarget = new NGPUImage(image.Width, image.Height, device);
                renderer = new GPURenderer3D((NGPUImage)renderTarget, device, realWidth, realHeight, [], [], [], []);
            }
            else
            {
                renderTarget = new NManagedImage(image.Width, image.Height);
                renderer = new CPURenderer3D((NManagedImage)renderTarget, realWidth, realHeight, [], [], [], []);
            }

            var camera = properties.First(p => p.Id == PropertyCameraGroupId).GetChildren() ?? [];
            var (viewMatrix, fov) = CameraProperties.GetViewMatrixAndFov(camera, composition, layer, layerTime, roi, image.Width, image.Height, downSamplingRateX, downSamplingRateY);
            renderer.FieldOfView = fov;
            renderer.ViewMatrix = viewMatrix;

            var particlesKeyIndex = SimulatedParticleTimes.FindIndex(t => t >= layerTime);
            var particles = particlesKeyIndex > -1 ? SimulatedParticles[SimulatedParticleTimes[particlesKeyIndex]] : [];
            var rendering = properties.First(p => p.Id == PropertyRenderingGroupId).GetChildren() ?? [];
            var particleBlendMode = rendering.GetValue(PropertyRenderingParticleBlendModeId, layerTime, BlendMode.Normal);
            var renderSize = Math.Max(realWidth, realHeight);
            var (aspectX, aspectY) = texture.Width >= texture.Height ? (1.0F, texture.Height / (float)texture.Width) : (texture.Width / (float)texture.Height, 1.0F);
            foreach (var particle in particles)
            {
                var particleSizeX = particle.Size * aspectX;
                var particleSizeY = particle.Size * aspectY;
                renderer.AddRect(
                    Int32Point.Zero,
                    texture,
                    ImageInterpolationQuality.Level1,
                    particle.Color,
                    particleSizeX,
                    particleSizeY,
                    particle.Opacity,
                    particleBlendMode,
                    Matrix4x4d.AffineTransform(new Vector3d(particleSizeX, particleSizeY, 0.0) * 0.5 / renderSize, Vector3d.One, particle.Angles, 0.0, 0.0, 0.0, particle.Position / renderSize),
                    ShadowCastMode.None,
                    0.0F,
                    false,
                    false,
                    1.0F,
                    1.0F,
                    1.0F,
                    1.0F,
                    1.0F,
                    null
                );
            }

            var antialias = rendering.GetValue(PropertyRenderingAntiAliasId, layerTime, false);
            switch (renderer)
            {
                case GPURenderer3D g:
                    g.Render(antialias, antialias);
                    break;
                case CPURenderer3D c:
                    c.Render(antialias, antialias);
                    break;
            }

            var blendMode = rendering.GetValue(PropertyRenderingBlendModeId, layerTime, BlendMode.Normal);
            var compositeOrder = rendering.GetValue(PropertyRenderingCompositeOrderId, layerTime, CompositeOrder.Front);
            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var result = image.ToGpu(device);
                switch (compositeOrder)
                {
                    case CompositeOrder.Front:
                        ImageBlendProcessor.SameSizeGpu(device, result, (NGPUImage)renderTarget, roi, blendMode);
                        image = result;
                        break;
                    default:
                        ImageBlendProcessor.SameSizeGpu(device, (NGPUImage)renderTarget, result, roi, blendMode);
                        if (image != result)
                        {
                            result.Dispose();
                        }
                        image = (NGPUImage)renderTarget;
                        break;
                }
            }
            else
            {
                var result = image.ToManaged();
                switch (compositeOrder)
                {
                    case CompositeOrder.Front:
                        ImageBlendProcessor.SameSizeCpu(result, (NManagedImage)renderTarget, roi, blendMode);
                        image = result;
                        break;
                    default:
                        ImageBlendProcessor.SameSizeCpu((NManagedImage)renderTarget, result, roi, blendMode);
                        if (result != image)
                        {
                            result.Dispose();
                        }
                        image = (NManagedImage)renderTarget;
                        break;
                }
            }

            if (compositeOrder != CompositeOrder.Back)
            {
                renderTarget.Dispose();
            }

            return image;
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        void SimulateParticle(
            Time toTime,
            double frameRate,
            IPropertyObject particleGenerationRateProperty,
            IPropertyObject particleLifeTimeProperty,
            IPropertyObject cannonPositionProperty,
            IPropertyObject cannonRadiusProperty,
            IPropertyObject cannonDirectionProperty,
            IPropertyObject randomDirectionProperty,
            IPropertyObject initialParticleSpeedProperty,
            IPropertyObject randomInitialParticleSpeedProperty,
            IPropertyObject addCannonMoveVelocityProperty,
            IPropertyObject particleRotateSpeedXProperty,
            IPropertyObject particleRotateSpeedYProperty,
            IPropertyObject particleRotateSpeedZProperty,
            IPropertyObject randomParticleRotateSpeedProperty,
            IPropertyObject birthColorProperty,
            IPropertyObject birthColorVariationHueProperty,
            IPropertyObject birthColorVariationSaturationProperty,
            IPropertyObject birthColorVariationValueProperty,
            IPropertyObject deadColorProperty,
            IPropertyObject deadColorVariationHueProperty,
            IPropertyObject deadColorVariationSaturationProperty,
            IPropertyObject deadColorVariationValueProperty,
            IPropertyObject colorGraphProperty,
            IPropertyObject birthSizeProperty,
            IPropertyObject birthSizeVariationProperty,
            IPropertyObject deadSizeProperty,
            IPropertyObject deadSizeVariationProperty,
            IPropertyObject sizeGraphProperty,
            IPropertyObject birthOpacityProperty,
            IPropertyObject birthOpacityVariationProperty,
            IPropertyObject deadOpacityProperty,
            IPropertyObject deadOpacityVariationProperty,
            IPropertyObject opacityGraphProperty,
            IPropertyObject gravityProperty,
            IPropertyObject gravityDirectionProperty,
            IPropertyObject airRegistanceProperty,
            int simulationRate,
            uint randomSeed
        )
        {
            var frameDuration = new Time(1, frameRate * simulationRate);
            var doubleFrameDuration = (double)frameDuration;
            var particleGeneration = 0.0;
            var isFirstParticleGeneration = CurrentParticles.Count < 1;
            for (var currentTime = LastSimulateTime; currentTime < toTime; currentTime += frameDuration)
            {
                if (SimulatedParticles.ContainsKey(currentTime))
                {
                    continue;
                }

                var doubleCurrentTime = (double)currentTime;
                var gravity = gravityProperty.GetValue(currentTime, 0.0);
                var gravityDirection = CalcRotatedVector(gravityDirectionProperty.GetValue(currentTime, Vector3d.Zero));
                var airRegistance = airRegistanceProperty.GetValue(currentTime, 0.0);
                var removeParticle = new List<ParticleData>();
                foreach (var particle in CurrentParticles)
                {
                    if (particle.DeadTime < doubleCurrentTime)
                    {
                        removeParticle.Add(particle);
                        continue;
                    }

                    particle.Advance(doubleFrameDuration, gravity, gravityDirection, airRegistance);
                }

                foreach (var particle in removeParticle)
                {
                    CurrentParticles.Remove(particle);
                }

                var particleGenerationRate = particleGenerationRateProperty.GetValue(currentTime, 0.0) / frameRate / simulationRate;
                if (particleGenerationRate > 0.0)
                {
                    if (isFirstParticleGeneration)
                    {
                        particleGeneration = particleGenerationRate > 1.0 ? 0.0 : 1.0;
                        isFirstParticleGeneration = false;
                    }

                    particleGeneration += particleGenerationRate;

                    var generate = (int)particleGeneration;
                    if (generate > 0)
                    {
                        var lifeTime = particleLifeTimeProperty.GetValue(currentTime, 0.0);
                        if (lifeTime <= 0.0)
                        {
                            continue;
                        }
                        var cannonPosition = cannonPositionProperty.GetValue(currentTime, Vector3d.Zero);
                        var cannonRadius = cannonRadiusProperty.GetValue(currentTime, Vector3d.Zero);
                        var cannonDirection = cannonDirectionProperty.GetValue(currentTime, Vector3d.Zero);
                        var randomDirection = randomDirectionProperty.GetValue(currentTime, 0.0);
                        var baseSpeed = initialParticleSpeedProperty.GetValue(currentTime, 0.0);
                        var baseRotationSpeed = new Vector3d(
                            particleRotateSpeedXProperty.GetValue(currentTime, 0.0),
                            particleRotateSpeedYProperty.GetValue(currentTime, 0.0),
                            particleRotateSpeedZProperty.GetValue(currentTime, 0.0)
                        );
                        var addCannonMoveVelocity = addCannonMoveVelocityProperty.GetValue<bool>(currentTime, false);
                        var randomSpeed = randomInitialParticleSpeedProperty.GetValue(currentTime, 0.0);
                        var randomRotationSpeed = randomParticleRotateSpeedProperty.GetValue(currentTime, 0.0);
                        var birthColor = Hsv.FromRgb(birthColorProperty.GetValue(currentTime, Vector4.One));
                        var birthColorVariationHue = birthColorVariationHueProperty.GetValue(currentTime, 0.0);
                        var birthColorVariationSaturation = birthColorVariationSaturationProperty.GetValue(currentTime, 0.0);
                        var birthColorVariationValue = birthColorVariationValueProperty.GetValue(currentTime, 0.0);
                        var deadColor = Hsv.FromRgb(deadColorProperty.GetValue(currentTime, Vector4.One));
                        var deadColorVariationHue = deadColorVariationHueProperty.GetValue(currentTime, 0.0);
                        var deadColorVariationSaturation = deadColorVariationSaturationProperty.GetValue(currentTime, 0.0);
                        var deadColorVariationValue = deadColorVariationValueProperty.GetValue(currentTime, 0.0);
                        var colorGraphValue = colorGraphProperty.GetValue(currentTime, GraphValueParameter.Identity);
                        var birthSize = birthSizeProperty.GetValue(currentTime, 0.0);
                        var birthSizeVariation = birthSize * birthSizeVariationProperty.GetValue(currentTime, 0.0) * 0.01;
                        var deadSize = deadSizeProperty.GetValue(currentTime, 0.0);
                        var deadSizeVariation = deadSize * deadSizeVariationProperty.GetValue(currentTime, 0.0) * 0.01;
                        var sizeGraphValue = sizeGraphProperty.GetValue(currentTime, GraphValueParameter.Identity);
                        var birthOpacity = birthOpacityProperty.GetValue(currentTime, 0.0);
                        var birthOpacityVariation = birthOpacity * birthOpacityVariationProperty.GetValue(currentTime, 0.0) * 0.01;
                        var deadOpacity = deadOpacityProperty.GetValue(currentTime, 0.0);
                        var deadOpacityVariation = deadOpacity * deadOpacityVariationProperty.GetValue(currentTime, 0.0) * 0.01;
                        var opacityGraphValue = opacityGraphProperty.GetValue(currentTime, GraphValueParameter.Identity);
                        var randomZValue = unchecked((uint)currentTime.GetHashCode());
                        var cannonVelocity = Vector3d.Zero;
                        if (addCannonMoveVelocity)
                        {
                            cannonVelocity = (cannonPosition - cannonPositionProperty.GetValue(currentTime - frameDuration, Vector3d.Zero)) / (double)frameDuration;
                        }
                        for (var g = 0; g < generate; g++)
                        {
                            var speedDirection = CalcRotatedVector(cannonDirection + (Vector3d)(Vector4.One - NoiseFunction.Pcg3D1Vector4Cpu(SpeedRandomKey, (uint)g, randomZValue, randomSeed) * 2.0F).AsVector3() * randomDirection);
                            var speed = speedDirection * baseSpeed;

                            var currentBirthColor = birthColor;
                            var currentDeadColor = deadColor;
                            currentBirthColor.Hue += (float)((1.0 - NoiseFunction.Pcg3D1FloatCpu(BirthColorHueRandomKey, (uint)g, randomZValue, randomSeed) * 2.0) * birthColorVariationHue);
                            currentBirthColor.Saturation += (float)((1.0 - NoiseFunction.Pcg3D1FloatCpu(BirthColorSaturationRandomKey, (uint)g, randomZValue, randomSeed) * 2.0) * birthColorVariationSaturation);
                            currentBirthColor.Value += (float)((1.0 - NoiseFunction.Pcg3D1FloatCpu(BirthColorValueRandomKey, (uint)g, randomZValue, randomSeed) * 2.0) * birthColorVariationValue);
                            currentDeadColor.Hue += (float)((1.0 - NoiseFunction.Pcg3D1FloatCpu(DeadColorHueRandomKey, (uint)g, randomZValue, randomSeed) * 2.0) * deadColorVariationHue);
                            currentDeadColor.Saturation += (float)((1.0 - NoiseFunction.Pcg3D1FloatCpu(DeadColorSaturationRandomKey, (uint)g, randomZValue, randomSeed) * 2.0) * deadColorVariationSaturation);
                            currentDeadColor.Value += (float)((1.0 - NoiseFunction.Pcg3D1FloatCpu(DeadColorValueRandomKey, (uint)g, randomZValue, randomSeed) * 2.0) * deadColorVariationValue);

                            CurrentParticles.Add(
                                new ParticleData(
                                    doubleCurrentTime,
                                    lifeTime,
                                    cannonPosition + cannonRadius * (Vector3d)(Vector4.One - NoiseFunction.Pcg3D1Vector4Cpu(CannonPositionRandomKey, (uint)g, randomZValue, randomSeed) * 2.0F).AsVector3(),
                                    speed + speedDirection * randomSpeed * (1.0 - NoiseFunction.Pcg3D1FloatCpu(CannonDirectionRandomKey, (uint)g, randomZValue, randomSeed) * 2.0) + cannonVelocity,
                                    baseRotationSpeed + (Vector3d)(Vector4.One - NoiseFunction.Pcg3D1Vector4Cpu(RotationRandomKey, (uint)g, randomZValue, randomSeed) * 2.0F).AsVector3(),
                                    currentBirthColor.ToRgb(),
                                    currentDeadColor.ToRgb(),
                                    colorGraphValue,
                                    (float)Math.Max(birthSize + (1.0 - NoiseFunction.Pcg3D1FloatCpu(BirthSizeRandomKey, (uint)g, randomZValue, randomSeed) * 2.0F) * birthSizeVariation, 0.0),
                                    (float)Math.Max(deadSize + (1.0 - NoiseFunction.Pcg3D1FloatCpu(DeadSizeRandomKey, (uint)g, randomZValue, randomSeed) * 2.0F) * deadSizeVariation, 0.0),
                                    sizeGraphValue,
                                    (float)Math.Clamp(birthOpacity + (1.0 - NoiseFunction.Pcg3D1FloatCpu(BirthOpacityRandomKey, (uint)g, randomZValue, randomSeed) * 2.0F) * birthOpacityVariation, 0.0, 100.0),
                                    (float)Math.Clamp(deadOpacity + (1.0 - NoiseFunction.Pcg3D1FloatCpu(DeadOpacityRandomKey, (uint)g, randomZValue, randomSeed) * 2.0F) * deadOpacityVariation, 0.0, 100.0),
                                    opacityGraphValue
                                )
                            );
                        }
                        particleGeneration -= generate;
                    }
                }

                SimulatedParticles.Add(currentTime, [..CurrentParticles.Select(p => p.ToSimulated(doubleCurrentTime))]);
                SimulatedParticleTimes.Add(currentTime);
                LastSimulateTime = currentTime;
            }
        }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3d CalcRotatedVector(in Vector3d angles)
        {
            return Matrix4x4d.CreateRotateZ(angles.Z + 180.0).RotateY(angles.Y).RotateX(angles.X).Transform(new Vector3d(0.0, 1.0, 0.0));
        }

        static NImage GetLayerImage(ILayerObject? layerObject, Time referenceTime, LayerImageProcessType type, double downSamplingRate, IAcceleratorObject? acceleratorObject)
        {
            NImage? result = null;
            if (layerObject != null)
            {
                result = type switch
                {
                    LayerImageProcessType.Masked => layerObject.GetMaskedImage(referenceTime, downSamplingRate, acceleratorObject != null),
                    LayerImageProcessType.Effected => layerObject.GetEffectedImage(referenceTime, downSamplingRate, acceleratorObject != null),
                    _ => layerObject.GetRawImage(referenceTime, downSamplingRate, acceleratorObject != null)
                };
            }

            if (result != null)
            {
                return result;
            }
            else
            {
                if (acceleratorObject != null)
                {
                    return new NGPUImage(1, 1, acceleratorObject.CurrentDevice, Vector4.One);
                }
                else
                {
                    return new NManagedImage(1, 1, Vector4.One);
                }
            }
        }
    }

    record SimulatedParticleData(
        Vector3d Position,
        Vector3d Angles,
        Vector4 Color,
        float Size,
        float Opacity
    );

    class ParticleData
    {
        public double BirthTime { get; set; }

        public double LifeTime { get; set; }

        public double DeadTime { get; set; }

        public Vector3d Position { get; set; }

        public Vector3d Angles { get; set; }

        public Vector3d Speed { get; set; }

        public Vector3d RotateSpeed { get; set; }

        public Vector4 BirthColor { get; set; }

        public Vector4 DeadColor { get; set; }

        public GraphValueParameter ColorGraphValue { get; }

        public float BirthSize { get; set; }

        public float DeadSize { get; set; }

        public GraphValueParameter SizeGraphValue { get; }

        public float BirthOpacity { get; set; }

        public float DeadOpacity { get; set; }

        public GraphValueParameter OpacityGraphValue { get; }

        public ParticleData(
            double birthTime,
            double lifeTime,
            in Vector3d position,
            in Vector3d speed,
            in Vector3d rotateSpeed,
            in Vector4 birthColor,
            in Vector4 deadColor,
            GraphValueParameter colorGraphValue,
            float birthSize,
            float deadSize,
            GraphValueParameter sizeGraphValue,
            float birthOpacity,
            float deadOpacity,
            GraphValueParameter opacityGraphValue
        )
        {
            BirthTime = birthTime;
            LifeTime = lifeTime;
            DeadTime = birthTime + lifeTime;
            Position = position;
            Speed = speed;
            RotateSpeed = rotateSpeed;
            BirthColor = birthColor;
            DeadColor = deadColor;
            ColorGraphValue = colorGraphValue;
            BirthSize = birthSize;
            DeadSize = deadSize;
            SizeGraphValue = sizeGraphValue;
            BirthOpacity = birthOpacity;
            DeadOpacity = deadOpacity;
            OpacityGraphValue = opacityGraphValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(double time, double gravity, in Vector3d gravityDirection, double airRegistance)
        {
            var newSpeed = Speed + gravity * gravityDirection * time;
            newSpeed -= newSpeed * airRegistance * time;

            Position += Speed * time;
            Angles += RotateSpeed * time;
            Speed = newSpeed;
            RotateSpeed -= RotateSpeed * airRegistance * time;
        }

        public SimulatedParticleData ToSimulated(double currentTime)
        {
            var t = (float)((currentTime - BirthTime) / LifeTime);
            var size = SizeGraphValue.Interpolation(DeadSize, BirthSize, t);
            return new SimulatedParticleData(
                Position + new Vector3d(size, size, 0.0) * 0.5,
                Angles,
                ColorGraphValue.Interpolation(DeadColor, BirthColor, t),
                size,
                OpacityGraphValue.Interpolation(DeadOpacity, BirthOpacity, t) * 0.01F
            );
        }
    }
}
