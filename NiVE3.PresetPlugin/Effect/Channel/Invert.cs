using System;
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
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing.ComputeShader;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_Invert_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_Invert_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Invert : IEffect
    {
        const string ID = "9A30E2F3-DCCA-4E93-AA29-ADCBEBBEFE99";

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertyBlendOrignalId = nameof(PropertyBlendOrignalId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.Channel_Invert_Channel, typeof(WithHSLChannelType), typeof(LanguageResourceDictionary), WithHSLChannelType.RGB, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyBlendOrignalId, LanguageResourceDictionary.ResourceKeys.Channel_Invert_BlendOriginal, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var channelType = properties.GetValue(PropertyChannelId, layerTime, WithHSLChannelType.RGB);
            var blendOriginal = (float)properties.GetValue(PropertyBlendOrignalId, layerTime, 0.0) * 0.01F;

            if (blendOriginal >= 1.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, channelType, blendOriginal);
            }
            else
            {
                return ProcessCpu(image, roi, channelType, blendOriginal);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, WithHSLChannelType channelType, float blendOriginal)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;

            switch (channelType)
            {
                case WithHSLChannelType.RGB:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            var a = c.W;
                            c = Vector4.One - c;
                            c.W = a;
                            imageDataSpan[x] = Vector4.Lerp(c, imageDataSpan[x], blendOriginal);
                        }
                    });
                    break;
                case WithHSLChannelType.R:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            c.Z = 1.0F - c.Z;
                            imageDataSpan[x] = Vector4.Lerp(c, imageDataSpan[x], blendOriginal);
                        }
                    });
                    break;
                case WithHSLChannelType.G:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            c.Y = 1.0F - c.Y;
                            imageDataSpan[x] = Vector4.Lerp(c, imageDataSpan[x], blendOriginal);
                        }
                    });
                    break;
                case WithHSLChannelType.B:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            c.X = 1.0F - c.X;
                            imageDataSpan[x] = Vector4.Lerp(c, imageDataSpan[x], blendOriginal);
                        }
                    });
                    break;
                case WithHSLChannelType.A:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            c.W = 1.0F - c.W;
                            imageDataSpan[x] = Vector4.Lerp(c, imageDataSpan[x], blendOriginal);
                        }
                    });
                    break;
                case WithHSLChannelType.Hue:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            var a = c.W;
                            var hsl = Hsl.FromRgb(c);
                            hsl = new Hsl(180.0F - hsl.Hue, hsl.Saturation, hsl.Lightness);
                            c = hsl.ToRgb();
                            c.W = a;
                            imageDataSpan[x] = Vector4.Lerp(c, imageDataSpan[x], blendOriginal);
                        }
                    });
                    break;
                case WithHSLChannelType.Saturation:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var c = imageDataSpan[x];
                            var a = c.W;
                            var hsl = Hsl.FromRgb(c);
                            hsl = new Hsl(hsl.Hue, 1.0F - hsl.Saturation, hsl.Lightness);
                            c = hsl.ToRgb();
                            c.W = a;
                            imageDataSpan[x] = Vector4.Lerp(c, imageDataSpan[x], blendOriginal);
                        }
                    });
                    break;
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, WithHSLChannelType channelType, float blendOriginal)
        {
            var gpuImage = image switch
            {
                NManagedImage managedImage => managedImage.CopyToGpu(device),
                _ => (NGPUImage)image
            };

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new InvertProcess(gpuImage.Data, gpuImage.Width, (int)channelType, blendOriginal, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct InvertProcess(ReadWriteBuffer<Float4> image, int width, int channelType, float blendOriginal, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var c = image[pos];
            switch (channelType)
            {
                case 0: // WithHSLChannelType.RGB
                    c = new Float4(1.0F - c.XYZ, c.W);
                    break;
                case 1: // WithHSLChannelType.R
                    c = new Float4(c.XY, 1.0F - c.Z, c.W);
                    break;
                case 2: // WithHSLChannelType.G
                    c = new Float4(c.X, 1.0F - c.Y, c.ZW);
                    break;
                case 3: // WithHSLChannelType.B
                    c = new Float4(1.0F - c.X, c.YZW);
                    break;
                case 4: // WithHSLChannelType.A
                    c = new Float4(c.XYZ, 1.0F - c.W);
                    break;
                case 5: // WithHSLChannelType.Hue
                    {
                        var hsl = ColorSpaceConversion.RgbToHsl(c);
                        c = ColorSpaceConversion.HslToRgb(new Float4(180.0F - hsl.X, hsl.YZW));
                    }
                    break;
                case 6: // WithHSLChannelType.Saturation
                    {
                        var hsl = ColorSpaceConversion.RgbToHsl(c);
                        c = ColorSpaceConversion.HslToRgb(new Float4(hsl.X, 1.0F - hsl.Y, hsl.ZW));
                    }
                    break;
            }

            image[pos] = Hlsl.Lerp(c, image[pos], blendOriginal);
        }
    }
}
