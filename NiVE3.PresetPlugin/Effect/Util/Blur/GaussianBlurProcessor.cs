using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Effect.Util.Blur
{
    static class GaussianBlurProcessor
    {
        const double Sigma = 0.1;

        const double InvertedSqrt2PI = 1.2615662610100802; // 1.0 / Math.Sqrt(Math.PI * 2.0 * 0.1)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessCpu(NManagedImage image, ROI roi, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            if (horizontalAmount > 0.0F && verticalAmount > 0.0F)
            {
                HorizontalAndVertical(image, roi, horizontalAmount, verticalAmount, edgeRepeatMode);
            }
            else if (horizontalAmount > 0.0F)
            {
                Horizontal(image, roi, horizontalAmount, edgeRepeatMode);
            }
            else
            {
                Vertical(image, roi, verticalAmount, edgeRepeatMode);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessGpu(GraphicsDevice device, NGPUImage image, ROI roi, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            using var temp = new NGPUImage(image.Width, image.Height, device);
            image.CopyTo(temp);
            if (horizontalAmount > 0.0F && verticalAmount > 0.0F)
            {
                var horizontalGaussian = GetGaussian(horizontalAmount);
                var verticalGaussian = GetGaussian(verticalAmount);
                using var horizontalGaussianBuffer = device.AllocateReadOnlyBuffer(horizontalGaussian);
                using var verticalGaussianBuffer = device.AllocateReadOnlyBuffer(verticalGaussian);
                using var context = device.CreateComputeContext();
                var verticalRange = horizontalGaussian.Length / 2;
                context.For(roi.Width, Math.Min(roi.Height + verticalRange * 2, image.Height), new GaussianBlurHorizontalProcess(temp.Data, image.Data, image.Width, horizontalGaussianBuffer, horizontalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, Math.Max(roi.Top - verticalRange, 0)));
                context.Barrier(temp.Data);
                context.Barrier(image.Data);
                context.For(roi.Width, roi.Height, new GaussianBlurVerticalProcess(image.Data, temp.Data, image.Width, image.Height, verticalGaussianBuffer, verticalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, roi.Top));
                context.Barrier(temp.Data);
                context.Barrier(image.Data);
            }
            else if (horizontalAmount > 0.0F)
            {
                var horizontalGaussian = GetGaussian(horizontalAmount);
                using var horizontalGaussianBuffer = device.AllocateReadOnlyBuffer(horizontalGaussian);
                using var context = device.CreateComputeContext();
                context.For(roi.Width, roi.Height, new GaussianBlurHorizontalProcess(image.Data, temp.Data, temp.Width, horizontalGaussianBuffer, horizontalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, roi.Top));
                context.Barrier(image.Data);
            }
            else
            {
                var verticalGaussian = GetGaussian(verticalAmount);
                using var verticalGaussianBuffer = device.AllocateReadOnlyBuffer(verticalGaussian);
                using var context = device.CreateComputeContext();
                context.For(roi.Width, roi.Height, new GaussianBlurVerticalProcess(image.Data, temp.Data, temp.Width, temp.Height, verticalGaussianBuffer, verticalGaussian.Sum(), (int)edgeRepeatMode, roi.Left, roi.Top));
                context.Barrier(image.Data);
            }
        }

        static void HorizontalAndVertical(NManagedImage image, ROI roi, float horizontal, float vertical, EdgeRepeatMode edgeRepeatMode)
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

        static void Horizontal(NManagedImage image, ROI roi, float horizontal, EdgeRepeatMode edgeRepeatMode)
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

        static void Vertical(NManagedImage image, ROI roi, float vertical, EdgeRepeatMode edgeRepeatMode)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float[] GetGaussian(float range)
        {
            var fz = (int)MathF.Ceiling(range) - 1;
            var gaussian = new float[fz * 2 + 1];
            var denom = 2.0 * range * range * Sigma;
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
