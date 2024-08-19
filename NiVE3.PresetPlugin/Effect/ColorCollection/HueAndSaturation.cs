using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Color;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_HueAndSaturation_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_HueAndSaturation_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class HueAndSaturation : IEffect
    {
        const string ID = "83DFF7FD-CABE-4E36-8D07-326446B238E3";

        const string PropertyHueId = nameof(PropertyHueId);

        const string PropertySaturationId = nameof(PropertySaturationId);

        const string PropertyLightnessId = nameof(PropertyLightnessId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new AngleProperty(PropertyHueId, LanguageResourceDictionary.ResourceKeys.ColorCollection_HueAndSaturation_Hue, 0.0, digit: 1),
                new DoubleProperty(PropertySaturationId, LanguageResourceDictionary.ResourceKeys.ColorCollection_HueAndSaturation_Saturation, 0.0, -100.0, 100.0, digit: 2),
                new DoubleProperty(PropertyLightnessId, LanguageResourceDictionary.ResourceKeys.ColorCollection_HueAndSaturation_Lightness, 0.0, -100.0, 100.0, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var hue = (float)properties.GetValue(PropertyHueId, layerTime, 0.0);
            var saturation = (float)properties.GetValue(PropertySaturationId, layerTime, 0.0) * 0.01F + 1.0F;
            var lightness = (float)properties.GetValue(PropertyLightnessId, layerTime, 0.0) * 0.01F;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, hue, saturation, lightness);
            }
            else
            {
                return ProcessCpu(image, roi, hue, saturation, lightness);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float hue, float saturation, float lightness)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * managedImage.Width + roi.Left, roi.Width);
                for (var i = 0; i < imageDataSpan.Length; i++)
                {
                    var color = Vector4.Clamp(imageDataSpan[i], Vector4.Zero, Vector4.One);
                    var hsl = Hsl.FromRgb(color);
                    hsl.Hue += hue;
                    hsl.Saturation *= saturation;
                    hsl.Lightness += lightness;

                    var newColor = hsl.ToRgb();
                    newColor.W = color.W;
                    imageDataSpan[i] = newColor;
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float hue, float saturation, float lightness)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var context = device.CreateComputeContext();

            context.For(roi.Width, roi.Height, new HueAndSaturationProcess(gpuImage.Data, gpuImage.Width, roi.Left, roi.Top, hue, saturation, lightness));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct HueAndSaturationProcess(ReadWriteBuffer<Float4> image, int width, int startX, int startY, float hue, float saturation, float lightness) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var color = image[pos];

            var hsl = ColorSpaceConversion.RgbToHsl(color);
            hsl.X += hue;
            hsl.Y *= saturation;
            hsl.Z += lightness;

            image[pos] = ColorSpaceConversion.HslToRgb(hsl);
        }
    }
}
