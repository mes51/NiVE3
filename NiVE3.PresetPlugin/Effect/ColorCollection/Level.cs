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
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.ColorCollection
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.ColorCollection_Level_Name, "mes51", "色調補正", LanguageResourceDictionary.ColorCollection_Level_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class Level : IEffect
    {
        const string ID = "9EE3E1A0-476B-488B-A3CE-17422D0B6C75";

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertyBlackInLevelId = nameof(PropertyBlackInLevelId);

        const string PropertyWhiteInLevelId = nameof(PropertyWhiteInLevelId);

        const string PropertyBlackOutLevelId = nameof(PropertyBlackOutLevelId);

        const string PropertyWhiteOutLevelId = nameof(PropertyWhiteOutLevelId);

        const string PropertyGammaId = nameof(PropertyGammaId);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_Channel, typeof(ChannelType), typeof(LanguageResourceDictionary), ChannelType.RGB, selectBoxWidth: 90),
                new DoubleProperty(PropertyBlackInLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_BlackInLevel, 0.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyWhiteInLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_WhiteInLevel, 1.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyBlackOutLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_BlackOutLevel, 0.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyWhiteOutLevelId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_WhiteOutLevel, 1.0, -10.0, 10.0, slideChangeValue: 0.01, digit: 2),
                new DoubleProperty(PropertyGammaId, LanguageResourceDictionary.ResourceKeys.ColorCollection_Level_Gamma, 1.0, 0.0, 10.0, slideChangeValue: 0.01, digit: 2),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, bool useGpu)
        {
            var channel = (ChannelType)(properties.First(p => p.Id == PropertyChannelId).GetValue(layerTime) ?? ChannelType.RGB);
            var blackIn = (float)(double)(properties.First(p => p.Id == PropertyBlackInLevelId).GetValue(layerTime) ?? 0.0);
            var whiteIn = (float)(double)(properties.First(p => p.Id == PropertyWhiteInLevelId).GetValue(layerTime) ?? 0.0);
            var blackOut = (float)(double)(properties.First(p => p.Id == PropertyBlackOutLevelId).GetValue(layerTime) ?? 1.0);
            var whiteOut = (float)(double)(properties.First(p => p.Id == PropertyWhiteOutLevelId).GetValue(layerTime) ?? 1.0);
            var gamma = (float)(double)(properties.First(p => p.Id == PropertyGammaId).GetValue(layerTime) ?? 1.0);

            if (useGpu)
            {
                return image;
            }
            else
            {
                return ProcessCpu(image, roi, channel, blackIn, whiteIn, blackOut, whiteOut, gamma);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NImage ProcessCpu(NImage image, ROI roi, ChannelType channel, float blackIn, float whiteIn, float blackOut, float whiteOut, float gamma)
        {
            NManagedImage managedImage;
            if (image is NGPUImage gpuImage)
            {
                managedImage = gpuImage.CopyToCpu();
                image.Dispose();
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
                            c.Z = MathF.Pow((c.Z - blackIn) * inAdd, gamma) * outAdd + blackOut;
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
                            c.Y = MathF.Pow((c.Y - blackIn) * inAdd, gamma) * outAdd + blackOut;
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
                            c.X = MathF.Pow((c.X - blackIn) * inAdd, gamma) * outAdd + blackOut;
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
                            c.W = MathF.Pow((c.W - blackIn) * inAdd, gamma) * outAdd + blackOut;
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
                        var vectorGamma = Vector128.Create(gamma);
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
    }
}
