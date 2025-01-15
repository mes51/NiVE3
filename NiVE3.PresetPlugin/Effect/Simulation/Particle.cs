using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Property.Properties;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Simulation
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Simulation_Particle_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Simulation, LanguageResourceDictionary.Simulation_Particle_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Particle : IEffect
    {
        const string ID = "9D42E697-F4E9-4253-9EE6-EF2D0B41E869";

        #region Property ids

        const string PropertyCannonGroupId = nameof(PropertyCannonGroupId);

        const string PropertyCannonParticleGenerationRateId = nameof(PropertyCannonParticleGenerationRateId);

        const string PropertyCannonRadiusId = nameof(PropertyCannonRadiusId);

        const string PropertyCannonDirectionId = nameof(PropertyCannonDirectionId);

        const string PropertyCannonRandomDirectionRateId = nameof(PropertyCannonRandomDirectionRateId);

        const string PropertyCannonInitialParticleSpeedId = nameof(PropertyCannonInitialParticleSpeedId);

        const string PropertyCannonRandomInitialParticleSpeedId = nameof(PropertyCannonRandomInitialParticleSpeedId);

        const string PropertyCannonParticleRotateSpeedGroupId = nameof(PropertyCannonParticleRotateSpeedGroupId);

        const string PropertyCannonParticleRotateSpeedXId = nameof(PropertyCannonParticleRotateSpeedXId);

        const string PropertyCannonParticleRotateSpeedYId = nameof(PropertyCannonParticleRotateSpeedYId);

        const string PropertyCannonParticleRotateSpeedZId = nameof(PropertyCannonParticleRotateSpeedZId);

        const string PropertyCannonRandomParticleRotateSpeedId = nameof(PropertyCannonRandomParticleRotateSpeedId);

        const string PropertyParticleGroupId = nameof(PropertyParticleGroupId);

        const string PropertyParticleLifetimeId = nameof(PropertyParticleLifetimeId);

        const string PropertyParticleBirthColorId = nameof(PropertyParticleBirthColorId);

        const string PropertyParticleDeadColorId = nameof(PropertyParticleDeadColorId);

        const string PropertyParticleColorGraphId = nameof(PropertyParticleColorGraphId);

        const string PropertyParticleBirthSizeId = nameof(PropertyParticleBirthSizeId);

        const string PropertyParticleDeadSizeId = nameof(PropertyParticleDeadSizeId);

        const string PropertyParticleSizeGraphId = nameof(PropertyParticleSizeGraphId);

        const string PropertyParticleBirthOpacityId = nameof(PropertyParticleBirthOpacityId);

        const string PropertyParticleDeadOpacityId = nameof(PropertyParticleDeadOpacityId);

        const string PropertyParticleOpacityGraphId = nameof(PropertyParticleOpacityGraphId);

        const string PropertyWorldGroupId = nameof(PropertyWorldGroupId);

        const string PropertyWorldGravityId = nameof(PropertyWorldGravityId);

        const string PropertyWorldGravityDirectionId = nameof(PropertyWorldGravityDirectionId);

        const string PropertyWorldAirRegistanceId = nameof(PropertyWorldAirRegistanceId);

        const string PropertyCameraGroupId = nameof(PropertyCameraGroupId);

        const string PropertyCameraUseCompositionId = nameof(PropertyCameraUseCompositionId);

        const string PropertyCameraPointOfInterestId = nameof(PropertyCameraPointOfInterestId);

        const string PropertyCameraPositionId = nameof(PropertyCameraPositionId);

        const string PropertyCameraOrientationId = nameof(PropertyCameraOrientationId);

        const string PropertyCameraXAngleId = nameof(PropertyCameraXAngleId);

        const string PropertyCameraYAngleId = nameof(PropertyCameraYAngleId);

        const string PropertyCameraZAngleId = nameof(PropertyCameraZAngleId);

        const string PropertyCameraZoomId = nameof(PropertyCameraZoomId);

        const string PropertySourceLayerGroupId = nameof(PropertySourceLayerGroupId);

        const string PropertySourceLayerLayerId = nameof(PropertySourceLayerLayerId);

        const string PropertySourceLayerUseSpecificReferenceTimeId = nameof(PropertySourceLayerUseSpecificReferenceTimeId);

        const string PropertySourceLayerSpecificReferenceTimeId = nameof(PropertySourceLayerSpecificReferenceTimeId);

        const string PropertyOptionGroupId = nameof(PropertyOptionGroupId);

        const string PropertyOptionSimulationRateId = nameof(PropertyOptionSimulationRateId);

        const string PropertyOptionAntiAliasId = nameof(PropertyOptionAntiAliasId);

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
            var cameraZoom = sourceSize.Width / Const.DefaultCameraFov * 0.5;
            return
            [
                new PropertyGroup(
                    PropertyCannonGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon,
                    [
                        new DoubleProperty(PropertyCannonParticleGenerationRateId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_GenerationRate, 30.0, 0.0, double.MaxValue, digit: 2),
                        new Vector3dProperty(PropertyCannonRadiusId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_Radius, new Vector3d(100.0, 100.0, 100.0), digit: 2, is3D: true),
                        new DirectionProperty(PropertyCannonDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_Direction, Vector3d.Zero, digit: 2),
                        new DoubleProperty(PropertyCannonRandomDirectionRateId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_RandomDirection, 20.0, 0.0, 180.0, digit: 2),
                        new DoubleProperty(PropertyCannonInitialParticleSpeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_InitialParticleSpeed, 100.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(PropertyCannonRandomInitialParticleSpeedId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Cannon_RandomInitialParticleSpeed, 20.0, 0.0, double.MaxValue, digit: 2),
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
                        new ColorProperty(PropertyParticleDeadColorId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadColor, colorDialogTitle, dialogOk, dialogCancel, Vector4.One),
                        new GraphValueProperty(PropertyParticleColorGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_ColorGraph, true),
                        new DoubleProperty(PropertyParticleBirthSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthSize, 6.0, 0.0, double.MaxValue, digit: 2),
                        new DoubleProperty(PropertyParticleDeadSizeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadSize, 6.0, 0.0, double.MaxValue, digit: 2),
                        new GraphValueProperty(PropertyParticleSizeGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_SizeGraph, true),
                        new DoubleProperty(PropertyParticleBirthOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_BirthOpacity, 100.0, 0.0, 100.0, digit: 2),
                        new DoubleProperty(PropertyParticleDeadOpacityId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_DeadOpacity, 0.0, 0.0, 100.0, digit: 2),
                        new GraphValueProperty(PropertyParticleOpacityGraphId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Partucle_OpacityGraph, true)
                    ]
                ),
                new PropertyGroup(
                    PropertyWorldGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World,
                    [
                        new DoubleProperty(PropertyWorldGravityId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World_Gravity, 10.0, 0.0, double.MaxValue, digit: 2),
                        new DirectionProperty(PropertyWorldGravityDirectionId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World_GravityDirection, new Vector3d(0.0, 0.0, 180.0), digit: 2),
                        new DoubleProperty(PropertyWorldAirRegistanceId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_World_AirRegistance, 0.001, 0.0, double.MaxValue, digit: 6)
                    ]
                ),
                new PropertyGroup(
                    PropertyCameraGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera,
                    [
                        new Vector3dProperty(PropertyCameraPointOfInterestId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera_PointOfInterest, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, is3D: true),
                        new Vector3dProperty(PropertyCameraPositionId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera_Position, new Vector3d(sourceSize.Width * 0.5, sourceSize.Height * 0.5, -cameraZoom), digit: 2, is3D: true),
                        new DirectionProperty(PropertyCameraOrientationId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera_Orientation, Vector3d.Zero, digit: 2),
                        new AngleProperty(PropertyCameraXAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera_XAngle, 0.0, digit: 2),
                        new AngleProperty(PropertyCameraYAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera_YAngle, 0.0, digit: 2),
                        new AngleProperty(PropertyCameraZAngleId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera_ZAngle, 0.0, digit: 2),
                        new DoubleProperty(PropertyCameraZoomId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Camera_Zoom, cameraZoom, 0.01, double.MaxValue, digit: 2)
                    ]
                ),
                new PropertyGroup(
                    PropertySourceLayerGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer,
                    [
                        new UseLayerImageProperty(PropertySourceLayerLayerId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer_Layer, 90.0),
                        new CheckBoxProperty(PropertySourceLayerUseSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer_UseSpecificReferenceTime, false),
                        new DoubleProperty(PropertySourceLayerSpecificReferenceTimeId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_SourceLayer_SpecificReferenceTime, 0.0, 0.0, double.MaxValue, digit: 2)
                    ]
                ),
                new PropertyGroup(
                    PropertyOptionGroupId,
                    LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option,
                    [
                        new DoubleProperty(PropertyOptionSimulationRateId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option_SimulationRate, 16.0, 1.0, 100.0, digit: 0),
                        new CheckBoxProperty(PropertyOptionAntiAliasId, LanguageResourceDictionary.ResourceKeys.Simulation_Particle_Option_AntiAlias, true)
                    ]
                )
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            return image;
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}
