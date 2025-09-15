using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.PresetPlugin.Effect.Util.Blur
{
    static class MaskBoxBlurProcessor
    {
        public static void ProcessCpu(ManagedRasterizedMaskImage mask, ROI roi, float horizontalAmount, float verticalAmount, int repeat, EdgeRepeatMode edgeRepeatMode)
        {
            if (horizontalAmount > 0.0F && verticalAmount > 0.0)
            {
                for (var i = 0; i < repeat; i++)
                {
                    HorizontalAndVertical(mask, roi, horizontalAmount, verticalAmount, edgeRepeatMode);
                }
            }
            else if (horizontalAmount > 0.0F)
            {
                for (var i = 0; i < repeat; i++)
                {
                    Horizontal(mask, roi, horizontalAmount, edgeRepeatMode);
                }
            }
            else
            {
                for (var i = 0; i < repeat; i++)
                {
                    Vertical(mask, roi, verticalAmount, edgeRepeatMode);
                }
            }
        }

        public static void ProcessGpu(GraphicsDevice device, GPURasterizedMaskImage mask, ROI roi, float horizontalAmount, float verticalAmount, int repeat, EdgeRepeatMode edgeRepeatMode)
        {
            using var temp = new GPURasterizedMaskImage(mask.Width, mask.Height, device);
            mask.CopyTo(temp);
            var verticalMargin = (int)MathF.Ceiling(horizontalAmount);
            if (horizontalAmount > 0.0F && verticalAmount > 0.0F)
            {
                using var context = device.CreateComputeContext();
                for (var i = 0; i < repeat; i++)
                {
                    context.For(roi.Width, Math.Min(roi.Height + verticalMargin * 2, mask.Height), new MaskBoxBlurHorizontalProcess(temp.Data, mask.Data, mask.Width, horizontalAmount, (int)edgeRepeatMode, roi.Left, Math.Max(roi.Top - verticalMargin, 0)));
                    context.Barrier(temp.Data);
                    context.Barrier(mask.Data);
                    context.For(roi.Width, roi.Height, new MaskBoxBlurVerticalProcess(mask.Data, temp.Data, mask.Width, mask.Height, verticalAmount, (int)edgeRepeatMode, roi.Left, roi.Top));
                    context.Barrier(temp.Data);
                    context.Barrier(mask.Data);
                }
            }
            else if (horizontalAmount > 0.0F)
            {
                var src = temp;
                var dst = mask;

                using (var context = device.CreateComputeContext())
                {
                    for (var i = 0; i < repeat; i++)
                    {
                        (src, dst) = (dst, src);
                        context.For(roi.Width, roi.Height, new MaskBoxBlurHorizontalProcess(dst.Data, src.Data, src.Width, horizontalAmount, (int)edgeRepeatMode, roi.Left, roi.Top));
                        context.Barrier(dst.Data);
                        context.Barrier(src.Data);
                    }
                }

                if (dst == temp)
                {
                    temp.CopyTo(mask);
                }
            }
            else
            {
                var src = temp;
                var dst = mask;

                using (var context = device.CreateComputeContext())
                {
                    for (var i = 0; i < repeat; i++)
                    {
                        (src, dst) = (dst, src);
                        context.For(roi.Width, roi.Height, new MaskBoxBlurVerticalProcess(dst.Data, src.Data, src.Width, src.Height, verticalAmount, (int)edgeRepeatMode, roi.Left, roi.Top));
                        context.Barrier(dst.Data);
                        context.Barrier(src.Data);
                    }
                }

                if (dst == temp)
                {
                    temp.CopyTo(mask);
                }
            }
        }

        static void HorizontalAndVertical(ManagedRasterizedMaskImage mask, ROI roi, float horizontal, float vertical, EdgeRepeatMode edgeRepeatMode)
        {
            var maskWidth = mask.Width;
            var maskHeight = mask.Height;
            var maskData = mask.Data;
            var temp = ArrayPool<float>.Shared.Rent(maskData.Length);

            Parallel.For(0, maskHeight, y =>
            {
                var data = maskData.AsSpan(y * maskWidth, maskWidth);
                for (var x = 0; x < maskWidth; x++)
                {
                    temp[x * maskHeight + y] = data[x];
                }
            });

            var pz = (int)Math.Ceiling(horizontal);
            var fmz = pz - horizontal;
            var fz = 1 - fmz;
            var count = horizontal * 2.0F + 1.0F;
            var width = Math.Min(roi.Right + pz, maskWidth);
            var x = Math.Max(roi.Left - pz, 0);

            Parallel.For(Math.Max(roi.Top - pz, 0), Math.Min(roi.Bottom + pz, maskHeight), h =>
            {
                var a = 0.0F;
                var data = maskData.AsSpan(0, mask.DataLength);

                {
                    var w = x - pz - 1;
                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, w, h, edgeRepeatMode) * fz;
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, x + pz - 1, h, edgeRepeatMode) * fz;
                    }
                    for (var limit = x + pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                    {
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, w, h, edgeRepeatMode);
                    }
                }

                for (var w = x; w < width; w++)
                {
                    var l = w - pz - 1;
                    a -= BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fz;
                    l++;

                    if (fmz > 0.0F)
                    {
                        a -= BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fmz;
                    }

                    l = w + pz;
                    a += BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fz;
                    l--;

                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fmz;
                    }

                    if (w >= x && a > 0.0F)
                    {
                        temp[w * maskHeight + h] = a / count;
                    }
                }
            });

            pz = (int)Math.Ceiling(vertical);
            fmz = pz - vertical;
            fz = 1 - fmz;
            count = vertical * 2.0F + 1.0F;
            var y = roi.Top;
            var height = roi.Bottom;

            Parallel.For(roi.Left, roi.Right, w =>
            {
                var a = 0.0F;
                var data = temp.AsSpan(0, mask.DataLength);

                {
                    var h = y - pz - 1;
                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForX<float>(temp, maskHeight, h, w, edgeRepeatMode) * fz;
                        a += BlurUtil.GetPixelForX<float>(temp, maskHeight, y + pz - 1, w, edgeRepeatMode) * fz;
                        h++;
                    }
                    for (var limit = y + pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                    {
                        a += BlurUtil.GetPixelForX<float>(temp, maskHeight, h, w, edgeRepeatMode);
                    }
                }

                for (var h = y; h < height; h++)
                {
                    var t = h - pz - 1;
                    a -= BlurUtil.GetPixelForX<float>(temp, maskHeight, t, w, edgeRepeatMode) * fz;
                    t++;

                    if (fmz > 0.0F)
                    {
                        a -= BlurUtil.GetPixelForX<float>(temp, maskHeight, t, w, edgeRepeatMode) * fmz;
                    }

                    t = h + pz;
                    a += BlurUtil.GetPixelForX<float>(temp, maskHeight, t, w, edgeRepeatMode) * fz;
                    t--;

                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForX<float>(temp, maskHeight, t, w, edgeRepeatMode) * fmz;
                    }

                    if (a > 0.0F)
                    {
                        maskData[h * maskWidth + w] = a/ count;
                    }
                }
            });

            ArrayPool<float>.Shared.Return(temp);
        }

        static void Horizontal(ManagedRasterizedMaskImage mask, ROI roi, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1.0F - fmz;
            var x = roi.Left;
            var width = roi.Right;
            var maskWidth = mask.Width;
            var maskHeight = mask.Height;
            var maskData = mask.Data;
            using var tempImage = (ManagedRasterizedMaskImage)mask.Copy();
            var temp = tempImage.Data;
            var count = amount * 2.0F + 1.0F;

            Parallel.For(roi.Top, roi.Bottom, h =>
            {
                var a = 0.0F;
                var data = maskData.AsSpan(0, mask.DataLength);

                {
                    var w = x - pz - 1;
                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, w, h, edgeRepeatMode) * fz;
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, x + pz - 1, h, edgeRepeatMode) * fz;
                        w++;
                    }
                    for (var limit = x + pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                    {
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, w, h, edgeRepeatMode);
                    }
                }

                for (var w = x; w < width; w++)
                {
                    var l = w - pz - 1;
                    a -= BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fz;
                    l++;

                    if (fmz > 0.0F)
                    {
                        a -= BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fmz;
                    }

                    l = w + pz;
                    a += BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fz;
                    l--;

                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForX<float>(data, maskWidth, l, h, edgeRepeatMode) * fmz;
                    }

                    if (w >= x && a > 0.0F)
                    {
                        temp[h * maskWidth + w] = a / count;
                    }
                }
            });

            temp.AsSpan(0, mask.DataLength).CopyTo(maskData.AsSpan(0, mask.DataLength));
        }

        static void Vertical(ManagedRasterizedMaskImage mask, ROI roi, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1.0F - fmz;
            var y = roi.Top;
            var height = roi.Bottom;
            var maskWidth = mask.Width;
            var maskHeight = mask.Height;
            var maskData = mask.Data;
            using var tempImage = (ManagedRasterizedMaskImage)mask.Copy();
            var temp = tempImage.Data;
            var count = amount * 2.0F + 1.0F;

            Parallel.For(roi.Left, roi.Right, w =>
            {
                var a = 0.0F;
                var data = maskData.AsSpan(0, mask.DataLength);

                {
                    var h = y - pz - 1;
                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForY<float>(data, maskWidth, maskHeight, h, w, edgeRepeatMode) * fz;
                        a += BlurUtil.GetPixelForY<float>(data, maskWidth, maskHeight, y + pz - 1, w, edgeRepeatMode) * fz;
                        h++;
                    }
                    for (var limit = y + pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                    {
                        a += BlurUtil.GetPixelForY<float>(data, maskWidth, maskHeight, h, w, edgeRepeatMode);
                    }
                }

                for (var h = y; h < height; h++)
                {
                    var t = h - pz - 1;
                    a -= BlurUtil.GetPixelForY<float>(data, maskWidth, maskHeight, t, w, edgeRepeatMode) * fz;
                    t++;

                    if (fmz > 0.0F)
                    {
                        a -= BlurUtil.GetPixelForY<float>(data, maskWidth, maskHeight, t, w, edgeRepeatMode) * fmz;
                    }

                    t = h + pz;
                    a += BlurUtil.GetPixelForY<float>(data, maskWidth, maskHeight, t, w, edgeRepeatMode) * fz;
                    t--;

                    if (fmz > 0.0F)
                    {
                        a += BlurUtil.GetPixelForY<float>(data, maskWidth, maskHeight, t, w, edgeRepeatMode) * fmz;
                    }

                    if (a > 0.0F)
                    {
                        temp[h * maskWidth + w] = a / count;
                    }
                }
            });

            temp.AsSpan(0, mask.DataLength).CopyTo(maskData.AsSpan(0, mask.DataLength));
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MaskBoxBlurHorizontalProcess(ReadWriteBuffer<float> result, ReadWriteBuffer<float> mask, int width, float amount, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                a += GetPixel(x + i, y);
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                a += GetPixel(x - edge, y) * fz;
                a += GetPixel(x + edge, y) * fz;
            }

            if (a > 0.0F)
            {
                result[y * width + x] = a / (amount * 2.0F + 1.0F);
            }
            else
            {
                result[y * width + x] = 0.0F;
            }
        }

        float GetPixel(int l, int y)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return mask[y * width + CoordWrapGpu.Wrap(l, width)];
                case 2:
                    return mask[y * width + CoordWrapGpu.Repeat(l, width)];
                case 3:
                    return mask[y * width + CoordWrapGpu.Mirror(l, width)];
                default:
                    if (l > -1 && l < width)
                    {
                        return mask[y * width + l];
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
    readonly partial struct MaskBoxBlurVerticalProcess(ReadWriteBuffer<float> result, ReadWriteBuffer<float> mask, int width, int height, float amount, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                a += GetPixel(x, y + i);
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                a += GetPixel(x, y - edge) * fz;
                a += GetPixel(x, y + edge) * fz;
            }

            if (a > 0.0F)
            {
                result[y * width + x] = a / (amount * 2.0F + 1.0F);
            }
            else
            {
                result[y * width + x] = 0.0F;
            }
        }

        float GetPixel(int x, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return mask[CoordWrapGpu.Wrap(t, height) * width + x];
                case 2:
                    return mask[CoordWrapGpu.Repeat(t, height) * width + x];
                case 3:
                    return mask[CoordWrapGpu.Mirror(t, height) * width + x];
                default:
                    if (t > -1 && t < height)
                    {
                        return mask[t * width + x];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }
    }
}
