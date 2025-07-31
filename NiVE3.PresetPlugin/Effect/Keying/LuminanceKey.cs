using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Keying
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Keying_LuminanceKey_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Keying, LanguageResourceDictionary.Keying_LuminanceKey_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class LuminanceKey : IEffect
    {
        const string ID = "EA9E7C2E-C4EB-4A76-BC3A-BCF1F20461B9";

        const string PropertyKeyLuminanceId = nameof(PropertyKeyLuminanceId);

        const string PropertyToleranceId = nameof(PropertyToleranceId);

        const string PropertySoftnessId = nameof(PropertySoftnessId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyKeyLuminanceId, LanguageResourceDictionary.ResourceKeys.Keying_LuminanceKey_KeyLuminance, 0.5, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyToleranceId, LanguageResourceDictionary.ResourceKeys.Keying_LuminanceKey_Tolerance, 10.0, 0.0, 100.0, digit: 2),
                new DoubleProperty(PropertySoftnessId, LanguageResourceDictionary.ResourceKeys.Keying_LuminanceKey_Softness, 50.0, 0.0, 100.0, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var keyLuminance = (float)properties.GetValue(PropertyKeyLuminanceId, layerTime, 0.0);
            var tolerance = (float)(properties.GetValue(PropertyToleranceId, layerTime, 0.0) * 0.01);
            var softness = (float)(properties.GetValue(PropertySoftnessId, layerTime, 0.0) * 0.01);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, keyLuminance, tolerance, softness * tolerance);
            }
            else
            {
                return ProcessCpu(image, roi, keyLuminance, tolerance, softness * tolerance);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float keyLuminance, float tolerance, float softnessRange)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;

            var edgeSoftnessRange = tolerance - softnessRange;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = imageDataSpan[x];
                    var distance = Math.Abs(Vector4.Dot(color, Const.ConvertToGrayScale) - keyLuminance);
                    if (distance > tolerance)
                    {
                        continue;
                    }

                    if (distance > edgeSoftnessRange)
                    {
                        color.W *= (distance - edgeSoftnessRange) / softnessRange;
                    }
                    else
                    {
                        color.W = 0.0F;
                    }
                    imageDataSpan[x] = color;
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float keyLuminance, float tolerance, float softnessRange)
        {
            var gpuImage = image.ToGpu(device);

            device.For(roi.Width, roi.Height, new LuminanceKeyProcess(gpuImage.Data, gpuImage.Width, keyLuminance, tolerance, softnessRange, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LuminanceKeyProcess(ReadWriteBuffer<Float4> image, int width, float keyLuminance, float tolerance, float softnessRange, int startX, int startY) : IComputeShader
    {
        readonly float EdgeSoftnessRange = tolerance - softnessRange;

        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var color = image[pos];

            var distance = Hlsl.Abs(Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) - keyLuminance);
            if (distance <= tolerance)
            {
                if (distance > EdgeSoftnessRange)
                {
                    color.W *= (distance - EdgeSoftnessRange) / softnessRange;
                }
                else
                {
                    color.W = 0.0F;
                }

                image[pos] = color;
            }
        }
    }
}
