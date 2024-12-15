using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Noise;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(FractalNoiseProceduralInput), LanguageResourceDictionary.Input_FractalNoiseProceduralInput_Name, "mes51", LanguageResourceDictionary.Input_Input_FractalNoiseProceduralInput_Description, ID, "", IsSupportLoadToGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class FractalNoiseProceduralInput : IProceduralInput
    {
        const string ID = "1154CFA7-1881-4052-8677-DC3386E78651";

        IAcceralatableProceduralFootageSource Footage { get; } = new FractalNoiseProceduralFootageSource();

        public string FilePath => "Fractal Noise";

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            Footage.SetupAccelerator(accelerator);
        }

        public FootageSourceGroup GetGroup()
        {
            throw new NotImplementedException();
        }

        public bool Load(string filePath)
        {
            throw new NotImplementedException();
        }

        public ICustomizableFootageSource GetFootage()
        {
            return Footage;
        }

        public void Dispose() { }
    }

    file class FractalNoiseProceduralFootageSource : IAcceralatableProceduralFootageSource
    {
        const string PropertyImageSizeId = nameof(PropertyImageSizeId);

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

        public string SourceId => "Fractal Noise";

        public string? Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Input_FractalNoiseProceduralInput_Name);

        public double FrameRate => 0.0;

        public int Width => 0;

        public int Height => 0;

        public double Duration => 0.0;

        public SourceType SourceType => SourceType.Image;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public SourceFootageRect CalcSize(double time, int compositionWidth, int compositionHeight, bool withInvisible, PropertyValueGroup properties)
        {
            properties.TryGetValue(PropertyImageSizeId, out Vector3d size);
            var width = Math.Max((int)size.X, 1);
            var height = Math.Max((int)size.Y, 1);

            return new SourceFootageRect(new Vector2d(width, height) * 0.5, width, height);
        }

        public PropertyBase[] GetOptionProperties()
        {
            return
            [
                new Vector3dProperty(PropertyImageSizeId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_ImageSize, new Vector3d(1920.0, 1080.0, 0.0), new Vector3d(1.0, 1.0, 0.0), digit: 0, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel, useLinkRatio: true),
                new EnumProperty(PropertyFractalTypeId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_FractalType, typeof(FractalType), typeof(LanguageResourceDictionary), FractalType.Normal, selectBoxWidth: 90.0),
                new EnumProperty(PropertyNoiseTypeId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_NoiseType, typeof(NoiseType), typeof(LanguageResourceDictionary), NoiseType.SmoothLinear, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyInvertId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Invert, false),
                new DoubleProperty(PropertyContrastId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Contrast, 100.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyLuminanceId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Luminance, 0.0, -10000.0, 10000.0, digit: 2),
                new PropertyGroup(PropertyTransformGroupId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Transform,
                [
                    new Vector3dProperty(PropertyTransformPositionId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Transform_Position, new Vector3d(), digit: 2),
                    new Vector3dProperty(PropertyTransformScaleId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Transform_Scale, new Vector3d(100.0), new Vector3d(0.01), digit: 2, useLinkRatio: true),
                    new AngleProperty(PropertyTransformAngleId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Transform_Angle, 0.0, digit: 2),
                ]),
                new DoubleProperty(PropertyOctaveId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Octave, 6.0, 1.0, 20.0, slideChangeValue: 0.1, digit: 1),
                new PropertyGroup(PropertyOctaveSettingGroupId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_OctaveSetting,
                [
                    new DoubleProperty(PropertyOctaveSettingAmountId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_OctaveSetting_Amount, 70.0, 0.0, 1000.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new Vector3dProperty(PropertyOctaveSettingPositionOffsetId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_OctaveSetting_PositionOffset, new Vector3d(), digit: 2),
                    new Vector3dProperty(PropertyOctaveSettingScaleId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_OctaveSetting_Scale, new Vector3d(56.0), new Vector3d(0.01), digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, useLinkRatio: true),
                    new AngleProperty(PropertyOctaveSettingAngleId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_OctaveSetting_Angle, 0.0, digit: 2),
                    new CheckBoxProperty(PropertyOctaveSettingCenteringScaleId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_OctaveSetting_CenteringScale, false)
                ]),
                new AngleProperty(PropertyEvolutionId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Evolution, 0.0, digit: 2, isOnlyPositiveDirection: true),
                new DoubleProperty(PropertyRandomSeedId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_RandomSeed, 0, 0, uint.MaxValue, digit: 0),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Input_Input_FractalNoiseProceduralInput_Opacity, 100.0, 0.0, 100.0, digit: 2)
            ];
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }

        public NImage ReadFrame(double time, double downSamplingRate, int compositionWidth, int compositionHeight, PropertyValueGroup properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu)
        {
            var fractalType = properties.GetValueOrDefault(PropertyFractalTypeId, FractalType.Normal);
            var noiseType = properties.GetValueOrDefault(PropertyNoiseTypeId, NoiseType.Parlin);
            var invert = properties.GetValueOrDefault(PropertyInvertId, false);
            var contrast = (float)properties.GetValueOrDefault(PropertyContrastId, 100.0) * 0.01F;
            var luminance = (float)properties.GetValueOrDefault(PropertyLuminanceId, 0.0) * 0.01F;
            var octave = (float)properties.GetValueOrDefault(PropertyOctaveId, 6.0);
            var evolution = (float)properties.GetValueOrDefault(PropertyEvolutionId, 0.0) * 0.1F;
            var randomSeed = (uint)properties.GetValueOrDefault(PropertyRandomSeedId, 0.0);
            var opacity = (float)properties.GetValueOrDefault(PropertyOpacityId, 100.0) * 0.01F;

            var position = (Vector3)properties.GetValueOrDefaultInTree(PropertyTransformPositionId, new Vector3d());
            var scale = (Vector3)properties.GetValueOrDefaultInTree(PropertyTransformScaleId, new Vector3d());
            var angle = (float)properties.GetValueOrDefaultInTree(PropertyTransformAngleId, 0.0);

            var octaveAmount = (float)properties.GetValueOrDefaultInTree(PropertyOctaveSettingAmountId, 70.0) * 0.01F;
            var octavePositionOffset = (Vector3)properties.GetValueOrDefaultInTree(PropertyOctaveSettingPositionOffsetId, new Vector3d());
            var octaveScale = (Vector3)properties.GetValueOrDefaultInTree(PropertyOctaveSettingScaleId, new Vector3d()) * 0.01F;
            var octaveAngle = (float)properties.GetValueOrDefaultInTree(PropertyOctaveSettingAngleId, 0.0);
            var octaveCenteringScale = properties.GetValueOrDefaultInTree(PropertyOctaveSettingCenteringScaleId, false);

            var size = properties.GetValueOrDefault(PropertyImageSizeId, Vector3d.Zero);
            var width = Math.Max((int)size.X, 1);
            var height = Math.Max((int)size.Y, 1);
            var origin = new Vector2d(width, height) * 0.5;
            var roi = new ROI(Int32Point.Zero, new Int32Size(width, height), 0, 0, width, height);

            if (toGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = new NGPUImage(width, height, device) { Origin = origin };
                FractalNoiseProcess.GenerateGpu(
                    device,
                    gpuImage,
                    roi,
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
                    opacity
                );
                return gpuImage;
            }
            else
            {
                var managedImage = new NManagedImage(width, height) { Origin = origin };
                FractalNoiseProcess.GenerateCpu(
                    managedImage,
                    roi,
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
                    opacity
                );
                return managedImage;
            }
        }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            return new NManagedImage(1, 1);
        }
    }
}
