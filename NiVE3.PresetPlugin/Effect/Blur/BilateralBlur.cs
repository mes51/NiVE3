using System;
using System.Buffers;
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
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using static Vanara.PInvoke.User32.RAWINPUT;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_BilateralBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_BilateralBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class BilateralBlur : IEffect
    {
        public const int ContourQuantizeMax = 1023;

        const string ID = "A5360A2E-B74C-4C88-B8DD-C83425101C9F";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyContourId = nameof(PropertyContourId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        const double InvertedSqrt2PI = 0.3989422804014327; // 1.0 / Math.Sqrt(Math.PI * 2.0)

        IAcceleratorObject? AcceleratorObject { get; set; }

        // TODO: マルチフレームレンダリング時に競合しないようにThreadLocalに入れる等何かしらの対策をする
        static float[] ContourSigmas = [];

        static float LastContourValue = -1.0F;

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_BilateralBlur_Amount, 0.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyContourId, LanguageResourceDictionary.ResourceKeys.Blur_BilateralBlur_Contour, 20.0, 0.0, 300.0, digit: 2),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_BilateralBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);
            var contour = (float)properties.GetValue(PropertyContourId, layerTime, 0.0);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (amount <= 0.0F || contour <= 0.0F)
            {
                return image;
            }

            if (LastContourValue != contour)
            {
                ContourSigmas = new float[ContourQuantizeMax * 3 + 1];
                LastContourValue = contour;

                var sigma = Math.Sqrt(ContourQuantizeMax * 3.0) * 2.0 * contour;
                for (var i = 0; i < ContourSigmas.Length; i++)
                {
                    ContourSigmas[i] = (float)(InvertedSqrt2PI * Math.Exp(-i / sigma));
                }
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, amount, ContourSigmas, edgeRepeatMode);
            }
            else
            {
                return ProcessCpu(image, roi, amount, ContourSigmas, edgeRepeatMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float amount, float[] contourSigmas, EdgeRepeatMode edgeRepeatMode)
        {
            var managedImage = image.ToManaged();

            var lengthMap = ArrayPool<int>.Shared.Rent(managedImage.DataLength);
            lengthMap.AsSpan(managedImage.DataLength).Clear();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(0, managedImage.Height, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var lengthMapSpan = lengthMap.AsSpan(y * imageWidth, imageWidth);

                for (var x = 0; x < imageDataSpan.Length; x++)
                {
                    var color = imageDataSpan[x];
                    var rgb = color.AsVector3() * color.W;
                    lengthMapSpan[x] = (int)MathF.Round(rgb.LengthSquared() * ContourQuantizeMax);
                }
            });

            var temp = ArrayPool<Vector4>.Shared.Rent(managedImage.DataLength);
            temp.AsSpan(managedImage.DataLength).Clear();
            var gaussian = GaussianBlurProcessor.GetGaussian(amount);
            var imageHeight = managedImage.Height;
            var range = gaussian.Length / 2;
            var x = Math.Max(roi.Left - range, 0);
            var width = Math.Min(roi.Right + range, imageWidth);
            Parallel.For(Math.Max(roi.Top - range, 0), Math.Min(roi.Bottom + range, managedImage.Height), h =>
            {
                var tempSpan = temp.AsSpan(h * imageWidth, imageWidth);
                var lengthMapSpan = lengthMap.AsSpan(h * imageWidth, imageWidth);

                for (var w = x; w < width; w++)
                {
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    var count = 0.0F;
                    var currentLength = lengthMapSpan[w];
                    for (int l = w - range, limit = w + range + 1, c = 0; l < limit; l++, c++)
                    {
                        var p = BlurUtil.GetPixelForX(imageData, imageWidth, l, h, edgeRepeatMode);
                        var pLength = BlurUtil.GetPixelForX<int>(lengthMap, imageWidth, l, h, edgeRepeatMode);
                        var contourSigmasDiffIndex = Math.Clamp(Math.Abs(currentLength - pLength), 0, contourSigmas.Length - 1);
                        var g = gaussian[c] * contourSigmas[contourSigmasDiffIndex];
                        var ta = p.W * g;
                        rgb += p * ta;
                        a += ta;
                        count += g;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        tempSpan[w] = result;
                    }
                }
            });

            Parallel.For(x, width, w =>
            {
                for (var h = roi.Top; h < roi.Bottom; h++)
                {
                    var pos = h * imageWidth + w;
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    var count = 0.0F;
                    var currentLength = lengthMap[pos];
                    for (int t = h - range, limit = h + range + 1, c = 0; t < limit; t++, c++)
                    {
                        var p = BlurUtil.GetPixelForY(temp, imageWidth, imageHeight, t, w, edgeRepeatMode);
                        var pLength = BlurUtil.GetPixelForY<int>(lengthMap, imageWidth, imageHeight, t, w, edgeRepeatMode);
                        var contourSigmasDiffIndex = Math.Clamp(Math.Abs(currentLength - pLength), 0, contourSigmas.Length - 1);
                        var g = gaussian[c] * contourSigmas[contourSigmasDiffIndex];
                        var ta = p.W * g;
                        rgb += p * ta;
                        a += ta;
                        count += g;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        imageData[pos] = result;
                    }
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp);
            ArrayPool<int>.Shared.Return(lengthMap);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float amount, float[] contourSigmas, EdgeRepeatMode edgeRepeatMode)
        {
            var gpuImage = image.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            using var lengthMapBuffer = device.AllocateReadWriteBuffer<int>(gpuImage.DataLength);
            using var gaussianBuffer = device.AllocateReadOnlyBuffer(GaussianBlurProcessor.GetGaussian(amount));
            using var contourSigmasBuffer = device.AllocateReadOnlyBuffer(contourSigmas);

            using var context = device.CreateComputeContext();

            var range = gaussianBuffer.Length / 2;
            var left = Math.Max(roi.Left - range, 0);
            var top = Math.Max(roi.Top - range, 0);
            var right = Math.Min(roi.Right + range, gpuImage.Width);
            var bottom = Math.Min(roi.Bottom + range, gpuImage.Height);

            context.For(gpuImage.Width, gpuImage.Height, new BilateralBlurCalcLengthProcess(gpuImage.Data, gpuImage.Width, lengthMapBuffer));
            context.Barrier(lengthMapBuffer);

            context.For(right - left, bottom - top, new BilateralBlurHorizontalProcess(sourceImage.Data, gpuImage.Data, gpuImage.Width, gaussianBuffer, lengthMapBuffer, contourSigmasBuffer, (int)edgeRepeatMode, left, top));
            context.Barrier(sourceImage.Data);

            context.For(roi.Width, roi.Height, new BilateralBlurVerticalProcess(gpuImage.Data, sourceImage.Data, gpuImage.Width, gpuImage.Height, gaussianBuffer, lengthMapBuffer, contourSigmasBuffer, (int)edgeRepeatMode, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BilateralBlurCalcLengthProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<int> lengthMap) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * width + ThreadIds.X;

            var color = image[pos];
            var rgb = color.XYZ * color.W;
            lengthMap[pos] = (int)Hlsl.Round(Hlsl.Dot(rgb, rgb) * BilateralBlur.ContourQuantizeMax);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BilateralBlurHorizontalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, ReadOnlyBuffer<float> gaussian, ReadWriteBuffer<int> lengthMap, ReadOnlyBuffer<float> contourSigmas, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = gaussian.Length / 2;
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var color = new Float4();
            var a = 0.0F;
            var count = 0.0F;
            var currentLength = GetLength(x, y);
            for (int i = -range, c = 0; i <= range; i++, c++)
            {
                var tc = GetPixel(x + i, y);
                var tl = GetLength(x + i, y);
                var contourSigmasDiffIndex = Hlsl.Clamp(Hlsl.Abs(currentLength - tl), 0, contourSigmas.Length - 1);
                var g = gaussian[c] * contourSigmas[contourSigmasDiffIndex];
                var ta = tc.W * g;
                color += tc * ta;
                a += ta;
                count += g;
            }

            if (a > 0.0F)
            {
                var rc = color / a;
                rc.W = a / count;
                result[y * width + x] = rc;
            }
            else
            {
                result[y * width + x] = Const.EmptyPixelFloat4;
            }
        }

        Float4 GetPixel(int l, int y)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[y * width + CoordWrapGpu.Wrap(l, width)];
                case 2:
                    return image[y * width + CoordWrapGpu.Repeat(l, width)];
                case 3:
                    return image[y * width + CoordWrapGpu.Mirror(l, width)];
                default:
                    if (l > -1 && l < width)
                    {
                        return image[y * width + l];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }

        int GetLength(int l, int y)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return lengthMap[y * width + CoordWrapGpu.Wrap(l, width)];
                case 2:
                    return lengthMap[y * width + CoordWrapGpu.Repeat(l, width)];
                case 3:
                    return lengthMap[y * width + CoordWrapGpu.Mirror(l, width)];
                default:
                    if (l > -1 && l < width)
                    {
                        return lengthMap[y * width + l];
                    }
                    else
                    {
                        return 0;
                    }
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BilateralBlurVerticalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, ReadOnlyBuffer<float> gaussian, ReadWriteBuffer<int> lengthMap, ReadOnlyBuffer<float> contourSigmas, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = gaussian.Length / 2;
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var color = new Float4();
            var a = 0.0F;
            var count = 0.0F;
            var currentLength = GetLength(x, y);
            for (int i = -range, c = 0; i <= range; i++, c++)
            {
                var tc = GetPixel(x, y + i);
                var tl = GetLength(x, y + i);
                var contourSigmasDiffIndex = Hlsl.Clamp(Hlsl.Abs(currentLength - tl), 0, contourSigmas.Length - 1);
                var g = gaussian[c] * contourSigmas[contourSigmasDiffIndex];
                var ta = tc.W * g;
                color += tc * ta;
                a += ta;
                count += g;
            }

            if (a > 0.0F)
            {
                var rc = color / a;
                rc.W = a / count;
                result[y * width + x] = rc;
            }
            else
            {
                result[y * width + x] = 0.0F;
            }
        }

        Float4 GetPixel(int x, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[CoordWrapGpu.Wrap(t, height) * width + x];
                case 2:
                    return image[CoordWrapGpu.Repeat(t, height) * width + x];
                case 3:
                    return image[CoordWrapGpu.Mirror(t, height) * width + x];
                default:
                    if (t > -1 && t < height)
                    {
                        return image[t * width + x];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }

        int GetLength(int x, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return lengthMap[CoordWrapGpu.Wrap(t, height) * width + x];
                case 2:
                    return lengthMap[CoordWrapGpu.Repeat(t, height) * width + x];
                case 3:
                    return lengthMap[CoordWrapGpu.Mirror(t, height) * width + x];
                default:
                    if (t > -1 && t < height)
                    {
                        return lengthMap[t * width + x];
                    }
                    else
                    {
                        return 0;
                    }
            }
        }
    }
}
