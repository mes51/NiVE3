using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_Posterization_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Posterization_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Posterization : IEffect
    {
        const string ID = "56538C9D-5C3D-4E77-8CCC-8B1BF7715AB0";

        const string PropertyGradationId = nameof(PropertyGradationId);

        const string PropertyChangeByLuminanceId = nameof(PropertyChangeByLuminanceId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyGradationId, LanguageResourceDictionary.ResourceKeys.Stylize_Posterization_Gradation, 7.0, 2.0, 255.0, digit: 2),
                new CheckBoxProperty(PropertyChangeByLuminanceId, LanguageResourceDictionary.ResourceKeys.Stylize_Posterization_ChangeByLuminance, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var gradation = (float)properties.GetValue(PropertyGradationId, layerTime, 0.0);
            var changeByLuminance = properties.GetValue(PropertyChangeByLuminanceId, layerTime, false);

            var table = new float[256];
            var count = 0.0F;
            var tc = 0;
            var t = 0;
            for (var i = 0; i < table.Length; i++)
            {
                count += gradation;
                if (count >= 256.0F)
                {
                    count -= 256.0F;
                    tc++;
                    t = (int)((255 * tc) / (gradation - 1.0F));
                }
                table[i] = t / 255.0F;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, table, changeByLuminance);
            }
            else
            {
                return ProcessCpu(image, roi, table, changeByLuminance);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float[] table, bool changeByLuminance)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            if (changeByLuminance)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var color = imageDataSpan[x];
                        var gray = table[(int)(Vector4.Dot(color, Const.ConvertToGrayScale) * 255.0F)];
                        imageDataSpan[x] = Blend.Process(BlendMode.Luminance, color, new Vector4(gray, gray, gray, 1.0F));
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var color = imageDataSpan[x];
                        var a = color.W;
                        color *= 255.0F;
                        color.X = table[(int)color.X];
                        color.Y = table[(int)color.Y];
                        color.Z = table[(int)color.Z];
                        color.W = a;
                        imageDataSpan[x] = color;
                    }
                });
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float[] table, bool changeByLuminance)
        {
            var gpuImage = image.ToGpu(device);

            using var tableBuffer = device.AllocateReadOnlyBuffer(table);

            if (changeByLuminance)
            {
                device.For(roi.Width, roi.Height, new PosterizationChangeByLuminanceProcess(gpuImage.Data, gpuImage.Width, tableBuffer, roi.Left, roi.Top));
            }
            else
            {
                device.For(roi.Width, roi.Height, new PosterizationProcess(gpuImage.Data, gpuImage.Width, tableBuffer, roi.Left, roi.Top));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PosterizationProcess(ReadWriteBuffer<Float4> image, int width, ReadOnlyBuffer<float> table, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            var index = (Int4)(color * 255.0F);
            color.X = table[index.X];
            color.Y = table[index.Y];
            color.Z = table[index.Z];
            image[pos] = color;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PosterizationChangeByLuminanceProcess(ReadWriteBuffer<Float4> image, int width, ReadOnlyBuffer<float> table, int startX, int startY) : IComputeShader
    {
        const int Luminance = (int)BlendMode.Luminance;

        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            var gray = table[(int)(Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) * 255.0F)];
            image[pos] = BlendMethods.Process(Luminance, color, new Float4(gray, gray, gray, 1.0F));
        }
    }
}
