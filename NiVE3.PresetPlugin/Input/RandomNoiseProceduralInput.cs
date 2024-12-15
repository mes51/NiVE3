using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
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
    [InputMetadata(typeof(RandomNoiseProceduralInput), LanguageResourceDictionary.Input_RandomNoiseProceduralInput_Name, "mes51", LanguageResourceDictionary.Input_RandomNoiseProceduralInput_Description, ID, "", IsSupportLoadToGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class RandomNoiseProceduralInput : IProceduralInput
    {
        const string ID = "2E791214-8F64-44DF-9D25-1DA5386E2D99";

        IAcceralatableProceduralFootageSource Footage { get; } = new RandomNoiseProceduralFootageSource();

        public string FilePath => "Random Noise";

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

    file class RandomNoiseProceduralFootageSource : IAcceralatableProceduralFootageSource
    {
        const string PropertyImageSizeId = nameof(PropertyImageSizeId);

        const string PropertyIsColorNoiseId = nameof(PropertyIsColorNoiseId);

        const string PropertyRandomSeedId = nameof(PropertyRandomSeedId);

        const string PropertyAdvanceId = nameof(PropertyAdvanceId);

        public string SourceId => "Random Noise";

        public string? Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Input_RandomNoiseProceduralInput_Name);

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
                new Vector3dProperty(PropertyImageSizeId, LanguageResourceDictionary.ResourceKeys.Input_RandomNoiseProceduralInput_ImageSize, new Vector3d(1920.0, 1080.0, 0.0), new Vector3d(1.0, 1.0, 0.0), digit: 0, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Pixel, useLinkRatio: true),
                new CheckBoxProperty(PropertyIsColorNoiseId, LanguageResourceDictionary.ResourceKeys.Input_RandomNoiseProceduralInput_IsColorNoise, false),
                new DoubleProperty(PropertyRandomSeedId, LanguageResourceDictionary.ResourceKeys.Input_RandomNoiseProceduralInput_RandomSeed, 0.0, 0.0, uint.MaxValue, digit: 0),
                new DoubleProperty(PropertyAdvanceId, LanguageResourceDictionary.ResourceKeys.Input_RandomNoiseProceduralInput_Advance, 0.0, 0.0, double.MaxValue, digit: 7)
            ];
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }

        public NImage ReadFrame(double time, double downSamplingRate, int compositionWidth, int compositionHeight, PropertyValueGroup properties, ImageInterpolationQuality imageInterpolationQuality, bool toGpu)
        {
            var size = properties.GetValueOrDefault(PropertyImageSizeId, Vector3d.Zero);
            var isColorNoise = properties.GetValueOrDefault(PropertyIsColorNoiseId, false);
            var randomSeed = (uint)properties.GetValueOrDefault(PropertyRandomSeedId, 0.0);
            var advance = properties.GetValueOrDefault(PropertyAdvanceId, 0.0);
            var width = Math.Max((int)size.X, 1);
            var height = Math.Max((int)size.Y, 1);
            var origin = new Vector2d(width, height) * 0.5;
            var roi = new ROI(Int32Point.Zero, new Int32Size(width, height), 0, 0, width, height);

            if (toGpu && AcceleratorObject != null)
            {
                var device = AcceleratorObject.CurrentDevice;
                var gpuImage = new NGPUImage(width, height, device) { Origin = origin };
                RandomNoiseProcess.ProcessGpu(device, gpuImage, roi, 1.0F, isColorNoise, (uint)randomSeed, advance);
                return gpuImage;
            }
            else
            {
                var image = new NManagedImage(width, height) { Origin = origin };
                RandomNoiseProcess.ProcessCpu(image, roi, 1.0F, isColorNoise, (uint)randomSeed, advance);
                return image;
            }
        }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            return new NManagedImage(1, 1);
        }
    }
}
