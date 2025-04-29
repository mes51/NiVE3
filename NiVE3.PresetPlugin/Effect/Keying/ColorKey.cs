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
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Keying
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Keying_ColorKey_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Keying, LanguageResourceDictionary.Keying_ColorKey_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class ColorKey : IEffect
    {
        const string ID = "8766F65A-26DA-4086-AEF8-1B756E224600";

        const double MaxTolerance = 1.7320508075688772; // Vector3.Distance(Vector3.Zero, Vector3.One) = Math.Sqrt(3.0);

        const string PropertyKeyColorId = nameof(PropertyKeyColorId);

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
                new ColorProperty(PropertyKeyColorId, LanguageResourceDictionary.ResourceKeys.Keying_ColorKey_KeyColor, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, new Vector4(1.0F, 0.0F, 0.0F, 1.0F)),
                new DoubleProperty(PropertyToleranceId, LanguageResourceDictionary.ResourceKeys.Keying_ColorKey_Tolerance, 10.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new DoubleProperty(PropertySoftnessId, LanguageResourceDictionary.ResourceKeys.Keying_ColorKey_Softness, 20.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var keyColor = properties.GetValue(PropertyKeyColorId, layerTime, Vector4.Zero).AsVector3();
            var tolerance = (float)(properties.GetValue(PropertyToleranceId, layerTime, 0.0) * 0.01 * MaxTolerance);
            var softness = (float)(properties.GetValue(PropertySoftnessId, layerTime, 0.0) * 0.01);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, keyColor, tolerance, softness * tolerance);
            }
            else
            {
                return ProcessCpu(image, roi, keyColor, tolerance, softness * tolerance);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector3 keyColor, float tolerance, float softnessRange)
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
                    var distance = Vector3.Distance(color.AsVector3(), keyColor);
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

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector3 keyColor, float tolerance, float softnessRange)
        {
            var gpuImage = image.ToGpu(device);

            var edgeSoftnessRange = tolerance - softnessRange;

            device.For(roi.Width, roi.Height, new ColorKeyProcess(gpuImage.Data, gpuImage.Width, keyColor, tolerance, softnessRange, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ColorKeyProcess(ReadWriteBuffer<Float4> image, int width, Float3 keyColor, float tolerance, float softnessRange, int startX, int startY) : IComputeShader
    {
        readonly float EdgeSoftnessRange = tolerance - softnessRange;

        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var color = image[pos];

            var distance = Hlsl.Distance(color.XYZ, keyColor);
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
