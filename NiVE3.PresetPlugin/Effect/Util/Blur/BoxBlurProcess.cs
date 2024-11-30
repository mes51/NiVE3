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
    static class BoxBlurProcess
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessCpu(NManagedImage image, ROI roi, float horizontalAmount, float verticalAmount, int repeat, EdgeRepeatMode edgeRepeatMode)
        {
            if (horizontalAmount > 0.0F && verticalAmount > 0.0)
            {
                for (var i = 0; i < repeat; i++)
                {
                    HorizontalAndVertical(image, roi, horizontalAmount, verticalAmount, edgeRepeatMode);
                }
            }
            else if (horizontalAmount > 0.0F)
            {
                for (var i = 0; i < repeat; i++)
                {
                    Horizontal(image, roi, horizontalAmount, edgeRepeatMode);
                }
            }
            else
            {
                for (var i = 0; i < repeat; i++)
                {
                    Vertical(image, roi, verticalAmount, edgeRepeatMode);
                }
            }
        }

        public static void ProcessGpu(GraphicsDevice device, NGPUImage image, ROI roi, float horizontalAmount, float verticalAmount, int repeat, EdgeRepeatMode edgeRepeatMode)
        {
            using var temp = new NGPUImage(image.Width, image.Height, device);
            image.CopyTo(temp);
            if (horizontalAmount > 0.0F && verticalAmount > 0.0F)
            {
                using var context = device.CreateComputeContext();
                for (var i = 0; i < repeat; i++)
                {
                    context.For(roi.Width, roi.Height, new BoxBlurHorizontalProcess(temp.Data, image.Data, image.Width, horizontalAmount, (int)edgeRepeatMode, roi.Left, roi.Top));
                    context.Barrier(temp.Data);
                    context.Barrier(image.Data);
                    context.For(roi.Width, roi.Height, new BoxBlurVerticalProcess(image.Data, temp.Data, image.Width, image.Height, verticalAmount, (int)edgeRepeatMode, roi.Left, roi.Top));
                    context.Barrier(temp.Data);
                    context.Barrier(image.Data);
                }
            }
            else if (horizontalAmount > 0.0F)
            {
                var src = temp;
                var dst = image;

                using (var context = device.CreateComputeContext())
                {
                    for (var i = 0; i < repeat; i++)
                    {
                        (src, dst) = (dst, src);
                        context.For(roi.Width, roi.Height, new BoxBlurHorizontalProcess(dst.Data, src.Data, src.Width, horizontalAmount, (int)edgeRepeatMode, roi.Left, roi.Top));
                        context.Barrier(dst.Data);
                        context.Barrier(src.Data);
                    }
                }

                if (dst == temp)
                {
                    temp.CopyTo(image);
                }
            }
            else
            {
                var src = temp;
                var dst = image;

                using (var context = device.CreateComputeContext())
                {
                    for (var i = 0; i < repeat; i++)
                    {
                        (src, dst) = (dst, src);
                        context.For(roi.Width, roi.Height, new BoxBlurVerticalProcess(dst.Data, src.Data, src.Width, src.Height, verticalAmount, (int)edgeRepeatMode, roi.Left, roi.Top));
                        context.Barrier(dst.Data);
                        context.Barrier(src.Data);
                    }
                }

                if (dst == temp)
                {
                    temp.CopyTo(image);
                }
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

            var pz = (int)Math.Ceiling(horizontal);
            var fmz = pz - horizontal;
            var fz = 1 - fmz;
            var count = horizontal * 2.0F + 1.0F;
            var width = Math.Min(roi.Right + pz, imageWidth);
            var x = Math.Max(roi.Left - pz, 0);

            Parallel.For(Math.Max(roi.Top - pz, 0), Math.Min(roi.Bottom + pz, imageHeight), h =>
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);

                {
                    var w = x - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = BlurUtil.GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        p = BlurUtil.GetPixelForX(data, imageWidth, x + pz - 1, h, edgeRepeatMode);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                    }
                    for (var limit = x + pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                    {
                        var p = BlurUtil.GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var w = x; w < width; w++)
                {
                    var l = w - pz - 1;
                    var p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    l++;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    l = w + pz;

                    p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    l--;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (w >= x && a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[w * imageHeight + h] = result;
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
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = temp.AsSpan(0, image.DataLength);

                {
                    var h = y - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = BlurUtil.GetPixelForX(temp, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        p = BlurUtil.GetPixelForX(temp, imageHeight, y + pz - 1 , w, edgeRepeatMode);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        h++;
                    }
                    for (var limit = y + pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                    {
                        var p = BlurUtil.GetPixelForX(temp, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var h = y; h < height; h++)
                {
                    var t = h - pz - 1;
                    var p = BlurUtil.GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    t++;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    t = h + pz;

                    p = BlurUtil.GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    t--;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
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

        static void Horizontal(NManagedImage image, ROI roi, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1.0F - fmz;
            var x = roi.Left;
            var width = roi.Right;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            using var tempImage = (NManagedImage)image.Copy();
            var temp = tempImage.Data;
            var count = amount * 2.0F + 1.0F;

            Parallel.For(roi.Top, roi.Bottom, delegate (int h)
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);

                {
                    var w = x - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = BlurUtil.GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        p = BlurUtil.GetPixelForX(data, imageWidth, x + pz - 1, h, edgeRepeatMode);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        w++;
                    }
                    for (var limit = x + pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                    {
                        var p = BlurUtil.GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var w = x; w < width; w++)
                {
                    var l = w - pz - 1;
                    var p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    l++;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    l = w + pz;

                    p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    l--;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (w >= x && a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * imageWidth + w] = result;
                    }
                }
            });

            temp.AsSpan(0, image.DataLength).CopyTo(imageData.AsSpan(0, image.DataLength));
        }

        static void Vertical(NManagedImage image, ROI roi, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1.0F - fmz;
            var y = roi.Top;
            var height = roi.Bottom;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            using var tempImage = (NManagedImage)image.Copy();
            var temp = tempImage.Data;
            var count = amount * 2.0F + 1.0F;

            Parallel.For(roi.Left, roi.Right, w =>
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);

                {
                    var h = y - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, y + pz - 1, w, edgeRepeatMode);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        h++;
                    }
                    for (var limit = y + pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                    {
                        var p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var h = y; h < height; h++)
                {
                    var t = h - pz - 1;
                    var p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    t++;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    t = h + pz;

                    p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    t--;

                    if (fmz > 0.0F)
                    {
                        p = BlurUtil.GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * imageWidth + w] = result;
                    }
                }
            });

            temp.AsSpan(0, image.DataLength).CopyTo(imageData.AsSpan(0, image.DataLength));
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BoxBlurHorizontalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, float amount, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var c = new Float4();
            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                var tc = GetPixel(x + i, y);
                c += tc * tc.W;
                a += tc.W;
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                var tc = GetPixel(x - edge, y);
                c += tc * tc.W * fz;
                a += tc.W * fz;

                tc = GetPixel(x + edge, y);
                c += tc * tc.W * fz;
                a += tc.W * fz;
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / (amount * 2.0F + 1.0F);
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
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BoxBlurVerticalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, float amount, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var c = new Float4();
            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                var tc = GetPixel(x, y + i);
                c += tc * tc.W;
                a += tc.W;
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                var tc = GetPixel(x, y - edge);
                c += tc * tc.W * fz;
                a += tc.W * fz;

                tc = GetPixel(x, y + edge);
                c += tc * tc.W * fz;
                a += tc.W * fz;
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / (amount * 2.0F + 1.0F);
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
    }
}
