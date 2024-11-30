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

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_Clamp_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_Clamp_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Clamp : IEffect
    {
        const string ID = "B87741E2-8D07-4BE6-834A-D77C9C3CEA6F";

        const string PropertyMaxId = nameof(PropertyMaxId);

        const string PropertyMinId = nameof(PropertyMinId);

        const string PropertyIsClampAlphaId = nameof(PropertyIsClampAlphaId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyMaxId, LanguageResourceDictionary.ResourceKeys.Channel_Clamp_Max, 1.0, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyMinId, LanguageResourceDictionary.ResourceKeys.Channel_Clamp_Min, 0.0, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                new CheckBoxProperty(PropertyIsClampAlphaId, LanguageResourceDictionary.ResourceKeys.Channel_Clamp_IsClampAlpha, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var max = (float)properties.GetValue(PropertyMaxId, layerTime, 1.0);
            var min = (float)properties.GetValue(PropertyMinId, layerTime, 0.0);
            var isClampAlpha = properties.GetValue(PropertyIsClampAlphaId, layerTime, false);

            if (max < min)
            {
                (min, max) = (max, min);
            }

            var minVec = new Vector4(min);
            var maxVec = new Vector4(max);
            if (!isClampAlpha)
            {
                minVec.W = float.MinValue;
                maxVec.W = float.MaxValue;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, minVec, maxVec);
            }
            else
            {
                return ProcessCpu(image, roi, minVec, maxVec);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector4 min, Vector4 max)
        {
            var managedImage = image.ToManaged();

            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * managedImage.Width, managedImage.Width);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    imageDataSpan[x] = Vector4.Clamp(imageDataSpan[x], min, max);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector4 min, Vector4 max)
        {
            var gpuImage = image.ToGpu(device);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new ClampProcess(gpuImage.Data, gpuImage.Width, min, max, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ClampProcess(ReadWriteBuffer<Float4> image, int width, Float4 min, Float4 max, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;
            image[pos] = Hlsl.Clamp(image[pos], min, max);
        }
    }
}
