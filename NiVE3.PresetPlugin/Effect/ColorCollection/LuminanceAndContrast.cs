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

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_LuminanceAndContrast_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_LuminanceAndContrast_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class LuminanceAndContrast : IEffect
    {
        const string ID = "EFC89AF0-8010-4776-91A4-4F2DF534068C";

        const string PropertyLuminanceId = nameof(PropertyLuminanceId);

        const string PropertyContrastId = nameof(PropertyContrastId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyLuminanceId, LanguageResourceDictionary.ResourceKeys.ColorCollection_LuminanceAndContrast_Luminance, 0.0, -100.0, 100.0, digit: 2),
                new DoubleProperty(PropertyContrastId, LanguageResourceDictionary.ResourceKeys.ColorCollection_LuminanceAndContrast_Contrast, 0.0, -100.0, 100.0, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var luminance = (float)properties.GetValue(PropertyLuminanceId, layerTime, 0.0) * 0.01F;
            var contrast = (float)(properties.GetValue(PropertyContrastId, layerTime, 0.0) + 100.0F) * 0.01F;

            if (luminance == 0.0F && contrast == 1.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, luminance, contrast);
            }
            else
            {
                return ProcessCpu(image, roi, luminance, contrast);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float luminance, float contrast)
        {
            var managedImage = image.ToManaged();

            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * managedImage.Width + roi.Left, roi.Width);
                for (var i = 0; i < imageDataSpan.Length; i++)
                {
                    var color = imageDataSpan[i];
                    var a = color.W;
                    color = (color - new Vector4(0.5F)) * contrast + new Vector4(0.5F + luminance);
                    color.W = a;
                    imageDataSpan[i] = color;
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float luminance, float contrast)
        {
            var gpuImage = image.ToGpu(device);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new LuminanceAndContrastProcess(gpuImage.Data, gpuImage.Width, roi.Left, roi.Top, luminance, contrast));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LuminanceAndContrastProcess(ReadWriteBuffer<Float4> image, int width, int startX, int startY, float luminance, float contrast) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            var color = image[pos];
            var a = color.W;
            image[pos] = new Float4(((color - 0.5F) * contrast + 0.5F + luminance).XYZ, a);
        }
    }
}
