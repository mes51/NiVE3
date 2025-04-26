using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
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
using NiVE3.PresetPlugin.Effect.Util.Noise;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Noise
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Noise_FractalNoise_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Noise, LanguageResourceDictionary.Noise_FractalNoise_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class FractalNoise : IEffect
    {
        const string ID = "1A578CBE-81BB-42CA-BF41-E0911DFCBDF7";

        const string PropertyFractalTypeId = nameof(PropertyFractalTypeId);

        const string PropertyNoiseTypeId = nameof(PropertyNoiseTypeId);

        const string PropertyInvertId = nameof(PropertyInvertId);

        const string PropertyContrastId = nameof(PropertyContrastId);

        const string PropertyLuminanceId = nameof(PropertyLuminanceId);

        const string PropertyTransformGroupId = nameof(PropertyTransformGroupId);

        const string PropertyTransformPositionId = nameof(PropertyTransformPositionId);

        const string PropertyTransformScaleId = nameof(PropertyTransformScaleId);

        const string PropertyTransformAngleId = nameof(PropertyTransformAngleId);

        const string PropertyOctaveId = nameof(PropertyOctaveId);

        const string PropertyOctaveSettingGroupId = nameof(PropertyOctaveSettingGroupId);

        const string PropertyOctaveSettingAmountId = nameof(PropertyOctaveSettingAmountId);

        const string PropertyOctaveSettingPositionOffsetId = nameof(PropertyOctaveSettingPositionOffsetId);

        const string PropertyOctaveSettingScaleId = nameof(PropertyOctaveSettingScaleId);

        const string PropertyOctaveSettingAngleId = nameof(PropertyOctaveSettingAngleId);

        const string PropertyOctaveSettingCenteringScaleId = nameof(PropertyOctaveSettingCenteringScaleId);

        const string PropertyEvolutionId = nameof(PropertyEvolutionId);

        const string PropertyRandomSeedId = nameof(PropertyRandomSeedId);

        const string PropertyOpacityId = nameof(PropertyOpacityId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        static readonly Vector2 AnchorPointOffset = new Vector2(0.3F, 0.7F);

        const uint CoordLimit = 1 << 16;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyFractalTypeId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_FractalType, typeof(FractalType), typeof(LanguageResourceDictionary), FractalType.Normal, selectBoxWidth: 90.0),
                new EnumProperty(PropertyNoiseTypeId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_NoiseType, typeof(NoiseType), typeof(LanguageResourceDictionary), NoiseType.SmoothLinear, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyInvertId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Invert, false),
                new DoubleProperty(PropertyContrastId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Contrast, 100.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyLuminanceId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Luminance, 0.0, -10000.0, 10000.0, digit: 2),
                new PropertyGroup(PropertyTransformGroupId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform,
                [
                    new Vector3dProperty(PropertyTransformPositionId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform_Position, new Vector3d(), digit: 2),
                    new Vector3dProperty(PropertyTransformScaleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform_Scale, new Vector3d(100.0), new Vector3d(0.01), digit: 2, useLinkRatio: true),
                    new AngleProperty(PropertyTransformAngleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Transform_Angle, 0.0, digit: 2),
                ]),
                new DoubleProperty(PropertyOctaveId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Octave, 6.0, 1.0, 20.0, slideChangeValue: 0.1, digit: 1),
                new PropertyGroup(PropertyOctaveSettingGroupId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting,
                [
                    new DoubleProperty(PropertyOctaveSettingAmountId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_Amount, 70.0, 0.0, 1000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new Vector3dProperty(PropertyOctaveSettingPositionOffsetId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_PositionOffset, new Vector3d(), digit: 2),
                    new Vector3dProperty(PropertyOctaveSettingScaleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_Scale, new Vector3d(56.0), new Vector3d(0.01), digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, useLinkRatio: true),
                    new AngleProperty(PropertyOctaveSettingAngleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_Angle, 0.0, digit: 2),
                    new CheckBoxProperty(PropertyOctaveSettingCenteringScaleId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_OctaveSetting_CenteringScale, false)
                ]),
                new AngleProperty(PropertyEvolutionId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Evolution, 0.0, digit: 2, isOnlyPositiveDirection: true),
                new DoubleProperty(PropertyRandomSeedId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_RandomSeed, 0, 0, uint.MaxValue, digit: 0),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_Opacity, 100.0, 0.0, 100.0, digit: 2),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Noise_FractalNoise_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var fractalType = properties.GetValue(PropertyFractalTypeId, layerTime, FractalType.Normal);
            var noiseType = properties.GetValue(PropertyNoiseTypeId, layerTime, NoiseType.Parlin);
            var invert = properties.GetValue(PropertyInvertId, layerTime, false);
            var contrast = (float)properties.GetValue(PropertyContrastId, layerTime, 100.0) * 0.01F;
            var luminance = (float)properties.GetValue(PropertyLuminanceId, layerTime, 0.0) * 0.01F;
            var octave = (float)properties.GetValue(PropertyOctaveId, layerTime, 6.0);
            var evolution = (float)properties.GetValue(PropertyEvolutionId, layerTime, 0.0) * 0.1F;
            var randomSeed = (uint)properties.GetValue(PropertyRandomSeedId, layerTime, 0.0);
            var opacity = (float)properties.GetValue(PropertyOpacityId, layerTime, 100.0) * 0.01F;
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);

            var transformGroup = properties.First(p => p.Id == PropertyTransformGroupId).GetChildren() ?? [];
            var position = (Vector3)transformGroup.GetValue(PropertyTransformPositionId, layerTime, new Vector3d());
            var scale = (Vector3)transformGroup.GetValue(PropertyTransformScaleId, layerTime, new Vector3d());
            var angle = (float)transformGroup.GetValue(PropertyTransformAngleId, layerTime, 0.0);

            var octaveSetting = properties.First(p => p.Id == PropertyOctaveSettingGroupId).GetChildren() ?? [];
            var octaveAmount = (float)octaveSetting.GetValue(PropertyOctaveSettingAmountId, layerTime, 70.0) * 0.01F;
            var octavePositionOffset = (Vector3)octaveSetting.GetValue(PropertyOctaveSettingPositionOffsetId, layerTime, new Vector3d());
            var octaveScale = (Vector3)octaveSetting.GetValue(PropertyOctaveSettingScaleId, layerTime, new Vector3d()) * 0.01F;
            var octaveAngle = (float)octaveSetting.GetValue(PropertyOctaveSettingAngleId, layerTime, 0.0);
            var octaveCenteringScale = octaveSetting.GetValue(PropertyOctaveSettingCenteringScaleId, layerTime, false);

            if (useGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                FractalNoiseProcessor.GenerateAndBlendGpu(
                    device,
                    gpuImage,
                    roi,
                    (float)downSamplingRateX,
                    (float)downSamplingRateY,
                    fractalType,
                    noiseType,
                    invert,
                    contrast,
                    luminance,
                    position,
                    scale,
                    angle,
                    octave,
                    octaveAmount,
                    octavePositionOffset,
                    octaveScale,
                    octaveAngle,
                    octaveCenteringScale,
                    evolution,
                    randomSeed,
                    opacity,
                    blendMode
                );
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                FractalNoiseProcessor.GenerateAndBlendsCpu(
                    managedImage,
                    roi,
                    (float)downSamplingRateX,
                    (float)downSamplingRateY,
                    fractalType,
                    noiseType,
                    invert,
                    contrast,
                    luminance,
                    position,
                    scale,
                    angle,
                    octave,
                    octaveAmount,
                    octavePositionOffset,
                    octaveScale,
                    octaveAngle,
                    octaveCenteringScale,
                    evolution,
                    randomSeed,
                    opacity,
                    blendMode
                );
                return managedImage;
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}
