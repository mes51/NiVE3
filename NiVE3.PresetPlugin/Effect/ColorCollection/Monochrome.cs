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

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_Monochrome_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_Monochrome_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Monochrome : IEffect
    {
        const string ID = "219601F7-4CED-4CEE-8F05-D6FC91FDF655";

        const string PropertyBlendOriginalId = nameof(PropertyBlendOriginalId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyBlendOriginalId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Monochrome_BlendOriginal, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var blendOriginal = (float)(properties.GetValue(PropertyBlendOriginalId, layerTime, 0.0) * 0.01);

            if (blendOriginal >= 1.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, blendOriginal);
            }
            else
            {
                return ProcessCpu(image, roi, blendOriginal);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float blendOriginal)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = imageDataSpan[x];
                    var mono = new Vector4(Vector4.Dot(color, Const.ConvertToGrayScale));
                    mono.W = color.W;

                    imageDataSpan[x] = Vector4.Lerp(mono, color, blendOriginal);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float blendOriginal)
        {
            var gpuImage = image.ToGpu(device);

            device.For(roi.Width, roi.Height, new MonochromeProcess(gpuImage.Data, gpuImage.Width, blendOriginal, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MonochromeProcess(ReadWriteBuffer<Float4> image, int width, float blendOriginal, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            var mono = new Float4((Float3)Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3), color.W);
            image[pos] = Hlsl.Lerp(mono, color, blendOriginal);
        }
    }
}
