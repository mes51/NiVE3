using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_ChannelShift_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_ChannelShift_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ChannelShift : IEffect
    {
        const string ID = "2B9A0C27-8BEF-46FB-803A-8BE922AA2EE6";

        const string PropertyRId = nameof(PropertyRId);

        const string PropertyGId = nameof(PropertyGId);

        const string PropertyBId = nameof(PropertyBId);

        const string PropertyAId = nameof(PropertyAId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyRId, LanguageResourceDictionary.ResourceKeys.Channel_ChannelShift_R, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.R, selectBoxWidth: 90.0),
                new EnumProperty(PropertyGId, LanguageResourceDictionary.ResourceKeys.Channel_ChannelShift_G, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.G, selectBoxWidth: 90.0),
                new EnumProperty(PropertyBId, LanguageResourceDictionary.ResourceKeys.Channel_ChannelShift_B, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.B, selectBoxWidth: 90.0),
                new EnumProperty(PropertyAId, LanguageResourceDictionary.ResourceKeys.Channel_ChannelShift_A, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.A, selectBoxWidth: 90.0),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var rType = properties.GetValue(PropertyRId, layerTime, WithHSLLOnOffChannelType.R);
            var gType = properties.GetValue(PropertyGId, layerTime, WithHSLLOnOffChannelType.G);
            var bType = properties.GetValue(PropertyBId, layerTime, WithHSLLOnOffChannelType.B);
            var aType = properties.GetValue(PropertyAId, layerTime, WithHSLLOnOffChannelType.A);

            if (rType == WithHSLLOnOffChannelType.R && gType == WithHSLLOnOffChannelType.G && bType == WithHSLLOnOffChannelType.B && aType == WithHSLLOnOffChannelType.A)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, rType, gType, bType, aType);
            }
            else
            {
                return ProcessCpu(image, roi, rType, gType, bType, aType);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, WithHSLLOnOffChannelType rType, WithHSLLOnOffChannelType gType, WithHSLLOnOffChannelType bType, WithHSLLOnOffChannelType aType)
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

                    var r = CalcShiftedValue(color, rType);
                    var g = CalcShiftedValue(color, gType);
                    var b = CalcShiftedValue(color, bType);
                    var a = CalcShiftedValue(color, aType);

                    imageDataSpan[x] = new Vector4(b, g, r, a);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, WithHSLLOnOffChannelType rType, WithHSLLOnOffChannelType gType, WithHSLLOnOffChannelType bType, WithHSLLOnOffChannelType aType)
        {
            var gpuImage = image.ToGpu(device);

            device.For(roi.Width, roi.Height, new ChannelShiftProcess(gpuImage.Data, gpuImage.Width, (int)rType, (int)gType, (int)bType, (int)aType, roi.Left, roi.Top));

            return gpuImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalcShiftedValue(in Vector4 color, WithHSLLOnOffChannelType channelType)
        {
            switch (channelType)
            {
                case WithHSLLOnOffChannelType.R:
                    return color.Z;
                case WithHSLLOnOffChannelType.G:
                    return color.Y;
                case WithHSLLOnOffChannelType.B:
                    return color.X;
                case WithHSLLOnOffChannelType.A:
                    return color.W;
                case WithHSLLOnOffChannelType.Luminance:
                    return Vector4.Dot(color, Const.ConvertToGrayScale);
                case WithHSLLOnOffChannelType.Hue:
                    {
                        var clamped = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                        var min = clamped.HorizontalMinBy3Element();
                        var max = clamped.HorizontalMaxBy3Element();
                        var diff = max - min;
                        var hue = diff != 0.0F ? max switch
                        {
                            _ when max == clamped.X => (clamped.Z - clamped.Y) / diff * 60.0F + 240.0F,
                            _ when max == clamped.Y => (clamped.X - clamped.Z) / diff * 60.0F + 120.0F,
                            _ => (clamped.Y - clamped.X) / diff * 60.0F
                        } : 0.0F;

                        return hue / 360.0F;
                    }
                case WithHSLLOnOffChannelType.Saturation:
                    {
                        var clamped = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                        var min = clamped.AsVector128().HorizontalMinBy3Element().GetElement(0);
                        var max = clamped.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                        if (max > 0.0F)
                        {
                            return (max - min) / max;
                        }
                        else
                        {
                            return 0.0F;
                        }
                    }
                case WithHSLLOnOffChannelType.Lightness:
                    {
                        var clamped = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                        var min = clamped.AsVector128().HorizontalMinBy3Element().GetElement(0);
                        var max = clamped.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                        return max + min; // ((max + min) * 0.5F - 0.5F) * 2.0F;
                    }
                case WithHSLLOnOffChannelType.On:
                    return 1.0F;
                case WithHSLLOnOffChannelType.Off:
                    return 0.0F;
                default:
                    return 0.5F;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ChannelShiftProcess(ReadWriteBuffer<Float4> image, int width, int rType, int gType, int bType, int aType, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];

            var r = CalcShiftedValue(color, rType);
            var g = CalcShiftedValue(color, gType);
            var b = CalcShiftedValue(color, bType);
            var a = CalcShiftedValue(color, aType);

            image[pos] = new Float4(b, g, r, a);
        }

        static float CalcShiftedValue(Float4 color, int channelType)
        {
            switch (channelType)
            {
                case 0:
                    return color.Z;
                case 1:
                    return color.Y;
                case 2:
                    return color.X;
                case 3:
                    return color.W;
                case 4:
                    return Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3);
                case 5:
                    {
                        var clamped = Hlsl.Clamp(color.XYZ, 0.0F, 1.0F);
                        var min = Hlsl.Min(Hlsl.Min(clamped.X, clamped.Y), clamped.Z);
                        var max = Hlsl.Max(Hlsl.Max(clamped.X, clamped.Y), clamped.Z);
                        var diff = max - min;
                        var h = 0.0F;
                        if (diff != 0.0F)
                        {
                            if (max == clamped.X)
                            {
                                h = (clamped.Z - clamped.Y) / diff * 60.0F + 240.0F;
                            }
                            else if (max == clamped.Y)
                            {
                                h = (clamped.X - clamped.Z) / diff * 60.0F + 120.0F;
                            }
                            else
                            {
                                h = (clamped.Y - clamped.X) / diff * 60.0F;
                            }
                        }

                        return h / 360.0F;
                    }
                case 6:
                    {
                        var clamped = Hlsl.Clamp(color.XYZ, 0.0F, 1.0F);
                        var min = Hlsl.Min(Hlsl.Min(clamped.X, clamped.Y), clamped.Z);
                        var max = Hlsl.Max(Hlsl.Max(clamped.X, clamped.Y), clamped.Z);
                        if (max > 0.0F)
                        {
                            return (max - min) / max;
                        }
                        else
                        {
                            return 0.0F;
                        }
                    }
                case 7:
                    {
                        var clamped = Hlsl.Clamp(color.XYZ, 0.0F, 1.0F);
                        var min = Hlsl.Min(Hlsl.Min(clamped.X, clamped.Y), clamped.Z);
                        var max = Hlsl.Max(Hlsl.Max(clamped.X, clamped.Y), clamped.Z);
                        return max + min;
                    }
                case 8:
                    return 1.0F;
                case 10:
                    return 0.0F;
                default:
                    return 0.5F;
            }
        }
    }
}
