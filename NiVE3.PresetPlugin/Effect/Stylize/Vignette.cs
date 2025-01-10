using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_Vignette_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Vignette_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Vignette : IEffect
    {
        const string ID = "5627E8EC-D128-4F0B-8A53-FB21845674A6";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyRadiusId = nameof(PropertyRadiusId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Stylize_Vignette_Amount, 100.0, 0.0, 1000.0, digit: 2),
                new DoubleProperty(PropertyRadiusId, LanguageResourceDictionary.ResourceKeys.Stylize_Vignette_Radius, sourceSize.Width * 0.25, 0.0, double.MaxValue, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0) * 0.00001F;
            var radius = (float)properties.GetValue(PropertyRadiusId, layerTime, 0.0);

            if (amount <= 0.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, amount, radius);
            }
            else
            {
                return ProcessCpu(image, roi, amount, radius);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float amount, float radius)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var center = new Vector2(imageWidth, managedImage.Height) * 0.5F;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var distance = Vector2.Distance(center, new Vector2(x, y)) - radius;
                    if (distance > 0.0F)
                    {
                        var falloff = 1.0F -  1.0F / (MathF.Pow(MathF.Pow(distance * amount, 2.0F) + 1, 2.0F));
                        imageDataSpan[x] = Blend.Process(BlendMode.Normal, imageDataSpan[x], new Vector4(0.0F, 0.0F, 0.0F, falloff));
                    }
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float amount, float radius)
        {
            var gpuImage = image.ToGpu(device);

            var center = new Vector2(gpuImage.Width, gpuImage.Height) * 0.5F;
            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new VignetteProcess(gpuImage.Data, gpuImage.Width, amount, radius, center, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct VignetteProcess(ReadWriteBuffer<Float4> image, int width, float amount, float radius, Float2 center, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var distance = Hlsl.Distance(new Float2(x, y), center) - radius;

            if (distance > 0.0F)
            {
                var pos = y * width + x;
                var falloff = 1.0F - 1.0F / (Hlsl.Pow(Hlsl.Pow(distance * amount, 2.0F) + 1, 2.0F));
                image[pos] = BlendMethods.Process(0, image[pos], new Float4(0.0F, 0.0F, 0.0F, falloff));
            }
        }
    }
}
