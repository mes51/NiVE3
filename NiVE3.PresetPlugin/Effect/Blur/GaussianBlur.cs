using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
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
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_GaussianBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_GaussianBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class GaussianBlur : IEffect
    {
        const string ID = "EE6D548B-DF62-40CF-8ED8-B05430897A98";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyDirectionId = nameof(PropertyDirectionId);

        const string PropertyIsRepeatEdgeId = nameof(PropertyIsRepeatEdgeId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        const double InvertedSqrt2PI = 0.3989422804014327; // 1.0 / Math.Sqrt(Math.PI * 2.0)

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties()
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianBlur_Amount, 0.0, 0.0, 10000.0, digit: 2),
                new EnumProperty(PropertyDirectionId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianBlur_Direction, typeof(BlurDirection), typeof(LanguageResourceDictionary), BlurDirection.HorizontalAndVertical, selectBoxWidth: 90.0),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_GaussianBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);
                var direction = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);

                var expandX = (int)Math.Ceiling(amount / downSamplingRateX);
                var expandY = (int)Math.Ceiling(amount / downSamplingRateY);
                switch (direction)
                {
                    case BlurDirection.Horizontal:
                        return baseRoi.Expand(-expandX, 0, expandX, 0);
                    case BlurDirection.Vertical:
                        return baseRoi.Expand(0, -expandY, 0, expandY);
                    default:
                        return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
                }
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);
            var direction = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (amount <= 0.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, (float)downSamplingRateX, (float)downSamplingRateY, amount, direction, edgeRepeatMode);
            }
            else
            {
                return ProcessCpu(image, roi, (float)downSamplingRateX, (float)downSamplingRateY, amount, direction, edgeRepeatMode);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static NManagedImage ProcessCpu(NImage image, ROI roi, float downSamplingRateX, float downSamplingRateY, float amount, BlurDirection direction, EdgeRepeatMode edgeRepeatMode)
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
            switch (direction)
            {
                case BlurDirection.HorizontalAndVertical:
                    Blur(managedImage, roi, amount / downSamplingRateX, amount / downSamplingRateY, edgeRepeatMode);
                    break;
                case BlurDirection.Horizontal:
                    BlurHorizontal(managedImage, roi, amount / downSamplingRateX, edgeRepeatMode);
                    break;
                case BlurDirection.Vertical:
                    BlurVertical(managedImage, roi, amount / downSamplingRateY, edgeRepeatMode);
                    break;
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float downSamplingRateX, float downSamplingRateY, float amount, BlurDirection direction, EdgeRepeatMode edgeRepeatMode)
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

            {
                using var temp = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
                gpuImage.CopyTo(temp);
                switch (direction)
                {
                    case BlurDirection.HorizontalAndVertical:
                        {
                            var horizontalGaussian = GetGaussian(amount / downSamplingRateX);
                            var verticalGaussian = GetGaussian(amount / downSamplingRateY);
                            using var horizontalGaussianBuffer = device.AllocateReadOnlyBuffer(horizontalGaussian);
                            using var verticalGaussianBuffer = device.AllocateReadOnlyBuffer(verticalGaussian);
                            using var context = device.CreateComputeContext();
                            context.For(roi.Width, roi.Height, new GaussianBlurHorizontalProcess(temp.Data, gpuImage.Data, gpuImage.Width, horizontalGaussianBuffer, horizontalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, roi.Top));
                            context.Barrier(temp.Data);
                            context.Barrier(gpuImage.Data);
                            context.For(roi.Width, roi.Height, new GaussianBlurVerticalProcess(gpuImage.Data, temp.Data, gpuImage.Width, gpuImage.Height, verticalGaussianBuffer, verticalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, roi.Top));
                            context.Barrier(temp.Data);
                            context.Barrier(gpuImage.Data);
                        }
                        break;
                    case BlurDirection.Horizontal:
                        {
                            var horizontalGaussian = GetGaussian(amount / downSamplingRateX);
                            using var horizontalGaussianBuffer = device.AllocateReadOnlyBuffer(horizontalGaussian);
                            using var context = device.CreateComputeContext();
                            context.For(roi.Width, roi.Height, new GaussianBlurHorizontalProcess(gpuImage.Data, temp.Data, temp.Width, horizontalGaussianBuffer, horizontalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, roi.Top));
                            context.Barrier(gpuImage.Data);
                        }
                        break;
                    case BlurDirection.Vertical:
                        {
                            var verticalGaussian = GetGaussian(amount / downSamplingRateY);
                            using var verticalGaussianBuffer = device.AllocateReadOnlyBuffer(verticalGaussian);
                            using var context = device.CreateComputeContext();
                            context.For(roi.Width, roi.Height, new GaussianBlurVerticalProcess(gpuImage.Data, temp.Data, temp.Width, temp.Height, verticalGaussianBuffer, verticalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, roi.Top));
                            context.Barrier(gpuImage.Data);
                        }
                        break;
                }
            }

            return gpuImage;
        }

        static void Blur(NManagedImage image, ROI roi, float horizontal, float vertical, EdgeRepeatMode edgeRepeatMode)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            var temp = ArrayPool<Vector4>.Shared.Rent(imageData.Length);

            Parallel.For(0, imageHeight, y =>
            {
                var data = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = 0; x < imageWidth; x++)
                {
                    temp[x * imageHeight + y] = data[x];
                }
            });

            var horizontalGaussian = GetGaussian(horizontal);
            var verticalGaussian = GetGaussian(vertical);
            var horizontalRange = horizontalGaussian.Length / 2;
            var verticalRange = verticalGaussian.Length / 2;

            var count = horizontalGaussian.Sum();
            var width = Math.Min(roi.Right + horizontalRange, imageWidth);
            var x = Math.Max(roi.Left - horizontalRange, 0);
            Parallel.For(Math.Max(roi.Top - verticalRange, 0), Math.Min(roi.Bottom + verticalRange, imageHeight), h =>
            {
                var data = imageData.AsSpan(0, image.DataLength);

                for (var w = x; w < width; w++)
                {
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    for (int l = w - horizontalRange, limit = w + horizontalRange + 1, c = 0; l < limit; l++, c++)
                    {
                        var p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        var ta = p.W * horizontalGaussian[c];
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[w * imageHeight + h] = result;
                    }
                }
            });

            var y = roi.Top;
            var height = roi.Bottom;
            count = horizontalGaussian.Sum();
            Parallel.For(roi.Left, roi.Right, w =>
            {
                var data = temp.AsSpan(0, image.DataLength);

                for (var h = y; h < height; h++)
                {
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    for (int t = h - verticalRange, limit = h + verticalRange + 1, c = 0; t < limit; t++, c++)
                    {
                        var p = BlurUtil.GetPixelForX(data, imageHeight, t, w, edgeRepeatMode);
                        var ta = p.W * verticalGaussian[c];
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        imageData[h * imageWidth + w] = result;
                    }
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp);
        }

        static void BlurHorizontal(NManagedImage image, ROI roi, float horizontal, EdgeRepeatMode edgeRepeatMode)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            using var tempImage = (NManagedImage)image.Copy();
            var tempImageData = tempImage.Data;

            var horizontalGaussian = GetGaussian(horizontal);
            var horizontalRange = horizontalGaussian.Length / 2;

            var count = horizontalGaussian.Sum();
            var x = roi.Left;
            var width = roi.Right;
            Parallel.For(roi.Top, roi.Bottom, h =>
            {
                var data = imageData.AsSpan(0, image.DataLength);
                var tempData = tempImageData.AsSpan(0, tempImage.DataLength);

                for (var w = x; w < width; w++)
                {
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    for (int l = w - horizontalRange, limit = w + horizontalRange + 1, c = 0; l < limit; l++, c++)
                    {
                        var p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        var ta = p.W * horizontalGaussian[c];
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        tempData[h * imageWidth + w] = result;
                    }
                }
            });

            tempImageData.AsSpan(0, tempImage.DataLength).CopyTo(imageData);
        }

        static void BlurVertical(NManagedImage image, ROI roi,  float vertical, EdgeRepeatMode edgeRepeatMode)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            using var tempImage = (NManagedImage)image.Copy();
            var tempImageData = tempImage.Data;

            var verticalGaussian = GetGaussian(vertical);
            var verticalRange = verticalGaussian.Length / 2;

            var count = verticalGaussian.Sum();
            var y = roi.Top;
            var height = roi.Bottom;
            Parallel.For(roi.Left, roi.Right, w =>
            {
                var data = imageData.AsSpan(0, image.DataLength);
                var tempData = tempImageData.AsSpan(0, tempImage.DataLength);

                for (var h = y; h < height; h++)
                {
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    for (int t = h - verticalRange, limit = h + verticalRange + 1, c = 0; t < limit; t++, c++)
                    {
                        var p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                        var ta = p.W * verticalGaussian[c];
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        tempData[h * imageWidth + w] = result;
                    }
                }
            });

            tempImageData.AsSpan(0, tempImage.DataLength).CopyTo(imageData);
        }

        static float[] GetGaussian(float range)
        {
            var fz = (int)MathF.Ceiling(range) - 1;
            var gaussian = new float[fz * 2 + 1];
            var denom = 2.0 * range * range;
            for (var i = 0; i < gaussian.Length; i++)
            {
                var x = Math.Abs(fz - i);
                gaussian[i] = (float)(InvertedSqrt2PI * Math.Exp(-x * x / denom));
            }

            return gaussian;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GaussianBlurHorizontalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, ReadOnlyBuffer<float> gaussian, float totalGaussian, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = gaussian.Length / 2;
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var c = new Float4();
            var a = 0.0F;
            for (int i = -range, g = 0; i <= range; i++, g++)
            {
                var tc = GetPixel(x + i, y);
                var ta = tc.W * gaussian[g];
                c += tc * ta;
                a += ta;
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / totalGaussian;
                result[y * width + x] = rc;
            }
            else
            {
                result[y * width + x] = 0.0F;
            }
        }

        Float4 GetPixel(int l, int y)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[y * width + Hlsl.Clamp(l, 0, width - 1)];
                case 2:
                    return image[y * width + (((l % width) + width) % width)];
                case 3:
                    {
                        var lw = width - 1;
                        var a = Hlsl.Abs(l);
                        var b = a % (lw * 2);
                        var c = b - Hlsl.Max(b - lw, 0) * 2;
                        return image[y * width + c];
                    }
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
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GaussianBlurVerticalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, ReadOnlyBuffer<float> gaussian, float totalGaussian, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = gaussian.Length / 2;
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var c = new Float4();
            var a = 0.0F;
            for (int i = -range, g = 0; i <= range; i++, g++)
            {
                var tc = GetPixel(x, y + i);
                var ta = tc.W * gaussian[g];
                c += tc * ta;
                a += ta;
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / totalGaussian;
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
                    return image[Hlsl.Clamp(t, 0, height - 1) * width + x];
                case 2:
                    return image[(((t % height) + height) % height) * width + x];
                case 3:
                    {
                        var lh = height - 1;
                        var a = Hlsl.Abs(t);
                        var b = a % (lh * 2);
                        var c = b - Hlsl.Max(b - lh, 0) * 2;
                        return image[c * width + x];
                    }
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
    }
}
