using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

namespace NiVE3.PresetPlugin.Effect.Noise
{
    [EffectMetadata(LanguageResourceDictionary.Noise_Median_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Noise, LanguageResourceDictionary.Noise_Median_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    [Export(typeof(IEffect))]
    public sealed class Median : IEffect
    {
        const string ID = "DD64884D-BAA1-40C2-AB60-96F19604655A";

        const string PropertyRadiusId = nameof(PropertyRadiusId);

        const string PropertyApplyToAlphaId = nameof(PropertyApplyToAlphaId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new DoubleProperty(PropertyRadiusId, LanguageResourceDictionary.ResourceKeys.Noise_Median_Radius, 0.0, 0.0, 300.0, digit: 0),
                new CheckBoxProperty(PropertyApplyToAlphaId, LanguageResourceDictionary.ResourceKeys.Noise_Median_ApplyToAlpha, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var radius = (int)properties.GetValue(PropertyRadiusId, layerTime, 0.0);
            var applyToAlpha = (bool)properties.GetValue(PropertyApplyToAlphaId, layerTime, false);
            if (radius < 1)
            {
                return image;
            }

            return ProcessCpu(image, roi, radius, applyToAlpha);
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, int radius, bool applyToAlpha)
        {
            var managedImage = image switch
            {
                NGPUImage gpuImage => gpuImage.CopyToCpu(),
                _ => (NManagedImage)image
            };

            var imageData = managedImage.Data;
            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var k = radius * 2 + 1;
            var temp = ArrayPool<Vector4>.Shared.Rent(managedImage.DataLength);
            temp.AsSpan(0, managedImage.DataLength).Fill(Vector4.UnitW);

            // NOTE: 本来medianはSeparableではないが、効果としては大体同じになるので縦横で分割する
            Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), y =>
            {
                Span<float> tempKeys = k < 256 ? stackalloc float[k] : new float[k];
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var sx = Math.Max(x - radius, 0);
                    var ex = Math.Min(x + radius + 1, imageWidth);
                    var length = ex - sx;
                    var targetSpan = MemoryMarshal.Cast<Vector4, float>(imageDataSpan[sx..ex]);
                    if (applyToAlpha)
                    {
                        for (var c = 0; c < 4; c++)
                        {
                            for (var tx = 0; tx < length; tx++)
                            {
                                tempKeys[tx] = targetSpan[tx * 4 + c];
                            }

                            tempKeys[..length].Sort();
                            ref var pixel = ref Unsafe.Add(ref temp[x * imageHeight + y].X, c);
                            pixel = tempKeys[length / 2 - (length % 2 == 1 ? 0 : 1)];
                        }
                    }
                    else
                    {
                        for (var c = 0; c < 3; c++)
                        {
                            for (var tx = 0; tx < length; tx++)
                            {
                                tempKeys[tx] = targetSpan[tx * 4 + c];
                            }

                            tempKeys[..length].Sort();
                            ref var pixel = ref Unsafe.Add(ref temp[x * imageHeight + y].X, c);
                            pixel = tempKeys[length / 2 - (length % 2 == 1 ? 0 : 1)];
                        }
                    }
                }
            });

            Parallel.For(roi.Left, roi.Right, x =>
            {
                Span<float> tempKeys = k < 256 ? stackalloc float[k] : new float[k];
                var tempSpan = temp.AsSpan(x * imageHeight, imageHeight);

                for (var y = roi.Top; y < roi.Bottom; y++)
                {
                    var sy = Math.Max(y - radius, 0);
                    var ey = Math.Min(y + radius + 1, imageHeight);
                    var length = ey - sy;
                    var targetSpan = MemoryMarshal.Cast<Vector4, float>(tempSpan[sy..ey]);
                    if (applyToAlpha)
                    {
                        for (var c = 0; c < 4; c++)
                        {
                            for (var ty = 0; ty < length; ty++)
                            {
                                tempKeys[ty] = targetSpan[ty * 4 + c];
                            }

                            tempKeys[..length].Sort();
                            ref var pixel = ref Unsafe.Add(ref imageData[y * imageWidth + x].X, c);
                            pixel = tempKeys[length / 2 - (length % 2 == 1 ? 0 : 1)];
                        }
                    }
                    else
                    {
                        for (var c = 0; c < 3; c++)
                        {
                            for (var ty = 0; ty < length; ty++)
                            {
                                tempKeys[ty] = targetSpan[ty * 4 + c];
                            }

                            tempKeys.Sort();
                            ref var pixel = ref Unsafe.Add(ref imageData[y * imageWidth + x].X, c);
                            pixel = tempKeys[length / 2 - (length % 2 == 1 ? 0 : 1)];
                        }
                    }
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp);

            return managedImage;
        }
    }
}
