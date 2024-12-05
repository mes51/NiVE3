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
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_Sepia_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_Sepia_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Sepia : IEffect
    {
        const string ID = "4C9B42BD-4028-4CED-8A8C-63E15AA715C7";

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
                new DoubleProperty(PropertyBlendOriginalId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Sepia_BlendOriginal, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var blendOriginal = (float)properties.GetValue(PropertyBlendOriginalId, layerTime, 0.0) * 0.01F;

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

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
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

                    // SEE: https://learn.microsoft.com/en-us/archive/msdn-magazine/2005/january/net-matters-sepia-tone-stringlogicalcomparer-and-more
                    var sepiaColor = new Vector4(
                        Vector4.Dot(color, new Vector4(0.131F, 0.534F, 0.272F, 0.0F)),
                        Vector4.Dot(color, new Vector4(0.168F, 0.686F, 0.349F, 0.0F)),
                        Vector4.Dot(color, new Vector4(0.189F, 0.769F, 0.393F, 0.0F)),
                        color.W
                    );
                    imageDataSpan[x] = Vector4.Lerp(sepiaColor, color, blendOriginal);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float blendOriginal)
        {
            var gpuImage = image.ToGpu(device);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new SepiaProcess(gpuImage.Data, gpuImage.Width, blendOriginal, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SepiaProcess(ReadWriteBuffer<Float4> image, int width, float blendOriginal, int startX, int startY) : IComputeShader
    {
        // SEE: https://learn.microsoft.com/en-us/archive/msdn-magazine/2005/january/net-matters-sepia-tone-stringlogicalcomparer-and-more
        static readonly Float3x3 SepiaMatrix = new Float3x3(
            0.131F, 0.534F, 0.272F,
            0.168F, 0.686F, 0.349F,
            0.189F, 0.769F, 0.393F
        );

        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            var sepiaColor = Hlsl.Mul(SepiaMatrix, color.XYZ);

            image[pos] = new Float4(Hlsl.Lerp(sepiaColor, color.XYZ, blendOriginal), color.W);
        }
    }
}
