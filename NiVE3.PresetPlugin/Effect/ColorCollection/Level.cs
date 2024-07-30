using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_Level_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_ColorCollection, LanguageResourceDictionary.ColorCollection_Level_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    sealed public class Level : IEffect
    {
        const string ID = "9EE3E1A0-476B-488B-A3CE-17422D0B6C75";

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertyBlackInLevelId = nameof(PropertyBlackInLevelId);

        const string PropertyWhiteInLevelId = nameof(PropertyWhiteInLevelId);

        const string PropertyBlackOutLevelId = nameof(PropertyBlackOutLevelId);

        const string PropertyWhiteOutLevelId = nameof(PropertyWhiteOutLevelId);

        const string PropertyGammaId = nameof(PropertyGammaId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_Channel, typeof(ChannelType), typeof(LanguageResourceDictionary), ChannelType.RGB, selectBoxWidth: 90),
                new DoubleProperty(PropertyBlackInLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_BlackInLevel, 0.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyWhiteInLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_WhiteInLevel, 1.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyBlackOutLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_BlackOutLevel, 0.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyWhiteOutLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_WhiteOutLevel, 1.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyGammaId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_Gamma, 1.0, 0.0001, 10.0, slideChangeValue: 0.01, digit: 2),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var channel = properties.GetValue(PropertyChannelId, layerTime, ChannelType.RGB);
            var blackIn = (float)properties.GetValue(PropertyBlackInLevelId, layerTime, 0.0);
            var whiteIn = (float)properties.GetValue(PropertyWhiteInLevelId, layerTime, 1.0);
            var blackOut = (float)properties.GetValue(PropertyBlackOutLevelId, layerTime, 0.0);
            var whiteOut = (float)properties.GetValue(PropertyWhiteOutLevelId, layerTime, 1.0);
            var invertGamma = 1.0F / (float)properties.GetValue(PropertyGammaId, layerTime, 1.0);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, channel, blackIn, whiteIn, blackOut, whiteOut, invertGamma);
            }
            else
            {
                return ProcessCpu(image, roi, channel, blackIn, whiteIn, blackOut, whiteOut, invertGamma);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, ChannelType channel, float blackIn, float whiteIn, float blackOut, float whiteOut, float invertGamma)
        {
            NManagedImage managedImage;
            if (image is NGPUImage gpuImage)
            {
                managedImage = gpuImage.CopyToCpu();
            }
            else
            {
                managedImage = (NManagedImage)image;
            }

            var left = roi.Left;
            var targetLength = roi.Right - left;
            var imageData = managedImage.Data;
            var imageWidth = image.Width;
            var inAdd = blackIn >= whiteIn ? 0.0F : (1.0F / (whiteIn - blackIn));
            var outAdd = whiteOut - blackOut;
            switch (channel)
            {
                case ChannelType.R:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var data = imageData.AsSpan(y * imageWidth + left, targetLength);
                        for (var i = 0; i < data.Length; i++)
                        {
                            var c = data[i];
                            c.Z = MathF.Pow((c.Z - blackIn) * inAdd, invertGamma) * outAdd + blackOut;
                            data[i] = c;
                        }
                    });
                    break;
                case ChannelType.G:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var data = imageData.AsSpan(y * imageWidth + left, targetLength);
                        for (var i = 0; i < data.Length; i++)
                        {
                            var c = data[i];
                            c.Y = MathF.Pow((c.Y - blackIn) * inAdd, invertGamma) * outAdd + blackOut;
                            data[i] = c;
                        }
                    });
                    break;
                case ChannelType.B:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var data = imageData.AsSpan(y * imageWidth + left, targetLength);
                        for (var i = 0; i < data.Length; i++)
                        {
                            var c = data[i];
                            c.X = MathF.Pow((c.X - blackIn) * inAdd, invertGamma) * outAdd + blackOut;
                            data[i] = c;
                        }
                    });
                    break;
                case ChannelType.A:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var data = imageData.AsSpan(y * imageWidth + left, targetLength);
                        for (var i = 0; i < data.Length; i++)
                        {
                            var c = data[i];
                            c.W = MathF.Pow((c.W - blackIn) * inAdd, invertGamma) * outAdd + blackOut;
                            data[i] = c;
                        }
                    });
                    break;
                default:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var data = imageData.AsSpan(y * imageWidth + left, targetLength);
                        var vectorBlackIn = Vector128.Create(blackIn);
                        var vectorBlackOut = Vector128.Create(blackOut);
                        var vectorGamma = Vector128.Create(invertGamma);
                        for (var i = 0; i < data.Length; i++)
                        {
                            var c = (((data[i].AsVector128() - vectorBlackIn) * inAdd).Pow(vectorGamma) * outAdd + vectorBlackOut).AsVector4();
                            c.W = data[i].W;
                            data[i] = c;
                        }
                    });
                    break;
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, ChannelType channel, float blackIn, float whiteIn, float blackOut, float whiteOut, float invertGamma)
        {
            NGPUImage gpuImage;
            if (image is NManagedImage managedImage)
            {
                gpuImage = managedImage.CopyToGpu(device);
            }
            else
            {
                gpuImage = (NGPUImage)image;
            }

            var inAdd = blackIn >= whiteIn ? 0.0F : (1.0F / (whiteIn - blackIn));
            var outAdd = whiteOut - blackOut;
            using (var context = device.CreateComputeContext())
            {
                context.For(roi.Width, roi.Height, new LevelProcess(gpuImage.Data, gpuImage.Width, roi.Left, roi.Top, (int)channel, blackIn, blackOut, inAdd, outAdd, invertGamma));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LevelProcess(ReadWriteBuffer<Float4> image, int width, int startX, int startY, int channel, float blackIn, float blackOut, float inAdd, float outAdd, float gamma) : IComputeShader
    {
        public void Execute()
        {
            var p = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            switch (channel)
            {
                case 1:
                    {
                        var c = image[p];
                        c.Z = ShaderMath.PowRetainSign((c.Z - blackIn) * inAdd, gamma) * outAdd + blackOut;
                        image[p] = c;
                    }
                    break;
                case 2:
                    {
                        var c = image[p];
                        c.Y = ShaderMath.PowRetainSign((c.Y - blackIn) * inAdd, gamma) * outAdd + blackOut;
                        image[p] = c;
                    }
                    break;
                case 3:
                    {
                        var c = image[p];
                        c.X = ShaderMath.PowRetainSign((c.X - blackIn) * inAdd, gamma) * outAdd + blackOut;
                        image[p] = c;
                    }
                    break;
                case 4:
                    {
                        var c = image[p];
                        c.W = ShaderMath.PowRetainSign((c.W - blackIn) * inAdd, gamma) * outAdd + blackOut;
                        image[p] = c;
                    }
                    break;
                default:
                    {
                        var c = ShaderMath.PowRetainSign((image[p] - blackIn) * inAdd, gamma) * outAdd + blackOut;
                        if (Hlsl.Any(Hlsl.IsNaN(c.XYZ)))
                        {
                            c = new Float4();
                        }
                        c.W = image[p].W;
                        image[p] = c;
                    }
                    break;
            }
        }
    }
}
