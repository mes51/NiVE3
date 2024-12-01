using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;

namespace NiVE3.PresetPlugin.Effect.Util.Blur
{
    static class DirectionalBlurProcess
    {
        public static void UnidirectionalCpu(NManagedImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode, bool fastMode)
        {
            if (fastMode)
            {
                UnidirectionalCpuFast(image, roi, radian, amount, edgeRepeatMode);
            }
            else
            {
                UnidirectionalCpuPrecision(image, roi, radian, amount, edgeRepeatMode);
            }
        }

        public static void BidirectionalCpu(NManagedImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode, bool fastMode)
        {
            if (fastMode)
            {
                BidirectionalCpuFast(image, roi, radian, amount, edgeRepeatMode);
            }
            else
            {
                BidirectionalCpuPrecision(image, roi, radian, amount, edgeRepeatMode);
            }
        }

        public static void UnidirectionalGpu(GraphicsDevice device, NGPUImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var cos = (float)Math.Cos(radian);
            var sin = (float)Math.Sin(radian);

            using var sourceImage = new NGPUImage(image.Width, image.Height, device);
            image.CopyTo(sourceImage);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new DirectionalBlurUnidirectionalProcess(image.Data, sourceImage.Data, image.Width, image.Height, (int)edgeRepeatMode, sin, cos, amount, roi.Left, roi.Top));
        }

        public static void BidirectionalGpu(GraphicsDevice device, NGPUImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var cos = (float)Math.Cos(radian);
            var sin = (float)Math.Sin(radian);

            using var sourceImage = new NGPUImage(image.Width, image.Height, device);
            image.CopyTo(sourceImage);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new DirectionalBlurBidirectionalProcess(image.Data, sourceImage.Data, image.Width, image.Height, (int)edgeRepeatMode, sin, cos, amount, roi.Left, roi.Top));
        }

        static void UnidirectionalCpuPrecision(NManagedImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            using var sourceImage = (NManagedImage)image.Copy();

            var cos = (float)Math.Cos(radian);
            var sin = (float)Math.Sin(radian);
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            var count = amount + 1.0F;
            var edge = amount - (int)amount;
            var edgeRange = (int)MathF.Ceiling(amount);
            var range = (int)MathF.Floor(amount);

            var bilinearEdgeMode = edgeRepeatMode switch
            {
                EdgeRepeatMode.Wrap => BilinearEdgeMode.Wrap,
                EdgeRepeatMode.Repeat => BilinearEdgeMode.Repeat,
                EdgeRepeatMode.Mirror => BilinearEdgeMode.Mirror,
                _ => BilinearEdgeMode.None
            };

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var sourceDataSpan = sourceImage.GetDataSpan();

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = Vector4.Zero;
                    var a = 0.0F;
                    if (edge > 0.0F)
                    {
                        var tc = ImageInterpolation.Bilinear(sourceDataSpan, imageWidth, imageHeight, x + edgeRange * cos, y + edgeRange * sin, Const.EmptyPixel, bilinearEdgeMode);
                        var ta = tc.W * edge;
                        a += ta;
                        color += tc * ta;
                    }
                    for (var r = 0; r <= range; r++)
                    {
                        var tc = ImageInterpolation.Bilinear(sourceDataSpan, imageWidth, imageHeight, x + r * cos, y + r * sin, Const.EmptyPixel, bilinearEdgeMode);
                        var ta = tc.W;
                        a += ta;
                        color += tc * ta;
                    }

                    if (a > 0.0F)
                    {
                        color /= a;
                        a /= count;
                        color.W = a;
                    }
                    else
                    {
                        color = new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
                    }
                    imageDataSpan[x] = color;
                }
            });
        }

        static void BidirectionalCpuPrecision(NManagedImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            using var sourceImage = (NManagedImage)image.Copy();

            var cos = (float)Math.Cos(radian);
            var sin = (float)Math.Sin(radian);
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            var count = amount * 2.0F + 1.0F;
            var edge = amount - (int)amount;
            var edgeRange = (int)MathF.Ceiling(amount);
            var range = (int)MathF.Floor(amount);

            var bilinearEdgeMode = edgeRepeatMode switch
            {
                EdgeRepeatMode.Wrap => BilinearEdgeMode.Wrap,
                EdgeRepeatMode.Repeat => BilinearEdgeMode.Repeat,
                EdgeRepeatMode.Mirror => BilinearEdgeMode.Mirror,
                _ => BilinearEdgeMode.None
            };

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var sourceDataSpan = sourceImage.GetDataSpan();

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = Vector4.Zero;
                    var a = 0.0F;
                    if (edge > 0.0F)
                    {
                        var tc = ImageInterpolation.Bilinear(sourceDataSpan, imageWidth, imageHeight, x + edgeRange * cos, y + edgeRange * sin, Const.EmptyPixel, bilinearEdgeMode);
                        var ta = tc.W * edge;
                        a += ta;
                        color += tc * ta;

                        tc = ImageInterpolation.Bilinear(sourceDataSpan, imageWidth, imageHeight, x - edgeRange * cos, y - edgeRange * sin, Const.EmptyPixel, bilinearEdgeMode);
                        ta = tc.W * edge;
                        a += ta;
                        color += tc * ta;
                    }
                    for (var r = -range; r <= range; r++)
                    {
                        var tc = ImageInterpolation.Bilinear(sourceDataSpan, imageWidth, imageHeight, x + r * cos, y + r * sin, Const.EmptyPixel, bilinearEdgeMode);
                        var ta = tc.W;
                        a += ta;
                        color += tc * ta;
                    }

                    if (a > 0.0F)
                    {
                        color /= a;
                        a /= count;
                        color.W = a;
                    }
                    else
                    {
                        color = new Vector4(1.0F, 1.0F, 1.0F, 0.0F);
                    }
                    imageDataSpan[x] = color;
                }
            });
        }

        static void UnidirectionalCpuFast(NManagedImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1.0F - fmz;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            var count = amount + 1.0F;
            var totalPixelCount = pz + 1;
            var angle = (float)(-radian / Math.PI * 180.0);

            var cos = (float)Math.Cos(radian);
            var sin = (float)Math.Sin(radian);
            var matrix = Matrix3x3.CreateTranslate(-imageWidth * 0.5F, -imageHeight * 0.5F)
                .Rotate(angle)
                .Translate(imageWidth * 0.5F, imageHeight * 0.5F);
            var p1 = matrix.Transform(new Vector2());
            var p2 = matrix.Transform(new Vector2(image.Width, 0.0F));
            var p3 = matrix.Transform(new Vector2(image.Width, image.Height));
            var p4 = matrix.Transform(new Vector2(0.0F, image.Height));
            var tempWidth = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)) - (int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)) + (int)Math.Ceiling(Math.Abs(pz * cos)) + 1;
            var tempHeight = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)) - (int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)) + (int)Math.Ceiling(Math.Abs(pz * sin)) + 1;
            var temp = ArrayPool<Vector4>.Shared.Rent(tempWidth * tempHeight);
            temp.AsSpan().Clear();

            matrix = Matrix3x3.CreateTranslate(-imageWidth * 0.5F, -imageHeight * 0.5F)
                .Rotate(angle)
                .Translate(tempWidth * 0.5F, tempHeight * 0.5F);
            if (!Matrix3x3.Invert(matrix, out var sourceMatrix))
            {
                BoxBlurProcess.ProcessCpu(image, roi, 0.0F, amount, 1, edgeRepeatMode);
            }

            var bilinearEdgeMode = edgeRepeatMode switch
            {
                EdgeRepeatMode.Wrap => BilinearEdgeMode.Wrap,
                EdgeRepeatMode.Repeat => BilinearEdgeMode.Repeat,
                EdgeRepeatMode.Mirror => BilinearEdgeMode.Mirror,
                _ => BilinearEdgeMode.None
            };

            Parallel.For(0, tempHeight, delegate (int h)
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);
                var cache = ArrayPool<Vector4>.Shared.Rent(totalPixelCount);

                {
                    if (fmz > 0.0F)
                    {
                        var (sx, sy) = sourceMatrix.Transform(pz - 1, h);
                        var p = ImageInterpolation.Bilinear(data, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, bilinearEdgeMode);
                        cache[totalPixelCount - 1] = p;
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                    }

                    for (int w = 0, limit = pz - (fmz > 0.0F ? 2 : 1), ci = fmz > 0.0F ? 1 : 0; w <= limit; w++, ci++)
                    {
                        var (sx, sy) = sourceMatrix.Transform(w, h);
                        var p = ImageInterpolation.Bilinear(data, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, bilinearEdgeMode);
                        cache[ci] = p;
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                var cacheIndex = totalPixelCount;
                for (var w = 0; w < tempWidth; w++, cacheIndex++)
                {
                    var p = cache[(cacheIndex - totalPixelCount) % totalPixelCount];
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;

                    if (fmz > 0.0F)
                    {
                        p = cache[(cacheIndex - totalPixelCount + 1) % totalPixelCount];
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }

                    var l = w + pz;
                    var (sx, sy) = sourceMatrix.Transform(l, h);
                    p = ImageInterpolation.Bilinear(data, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, bilinearEdgeMode);
                    cache[cacheIndex % totalPixelCount] = p;
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;

                    if (fmz > 0.0F)
                    {
                        p = cache[(cacheIndex - 1) % totalPixelCount];
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * tempWidth + w] = result;
                    }
                }

                ArrayPool<Vector4>.Shared.Return(cache);
            });

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var (tx, ty) = matrix.Transform(x, y);
                    imageDataSpan[x] = ImageInterpolation.Bilinear(temp, tempWidth, tempHeight, tx, ty);
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp);
        }

        static void BidirectionalCpuFast(NManagedImage image, ROI roi, double radian, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1.0F - fmz;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            var count = amount * 2.0F + 1.0F;
            var totalPixelCount = pz * 2 + 1;
            var angle = (float)(-radian / Math.PI * 180.0);

            var cos = (float)Math.Cos(radian);
            var sin = (float)Math.Sin(radian);
            var matrix = Matrix3x3.CreateTranslate(-imageWidth * 0.5F, -imageHeight * 0.5F)
                .Rotate(angle)
                .Translate(imageWidth * 0.5F, imageHeight * 0.5F);
            var p1 = matrix.Transform(new Vector2());
            var p2 = matrix.Transform(new Vector2(image.Width, 0.0F));
            var p3 = matrix.Transform(new Vector2(image.Width, image.Height));
            var p4 = matrix.Transform(new Vector2(0.0F, image.Height));
            var tempWidth = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)) - (int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)) + (int)Math.Ceiling(Math.Abs(pz * cos)) + 1;
            var tempHeight = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)) - (int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)) + (int)Math.Ceiling(Math.Abs(pz * sin)) + 1;
            var temp = ArrayPool<Vector4>.Shared.Rent(tempWidth * tempHeight);
            temp.AsSpan().Clear();

            matrix = Matrix3x3.CreateTranslate(-imageWidth * 0.5F, -imageHeight * 0.5F)
                .Rotate(angle)
                .Translate(tempWidth * 0.5F, tempHeight * 0.5F);
            if (!Matrix3x3.Invert(matrix, out var sourceMatrix))
            {
                BoxBlurProcess.ProcessCpu(image, roi, 0.0F, amount, 1, edgeRepeatMode);
            }

            var bilinearEdgeMode = edgeRepeatMode switch
            {
                EdgeRepeatMode.Wrap => BilinearEdgeMode.Wrap,
                EdgeRepeatMode.Repeat => BilinearEdgeMode.Repeat,
                EdgeRepeatMode.Mirror => BilinearEdgeMode.Mirror,
                _ => BilinearEdgeMode.None
            };

            Parallel.For(0, tempHeight, delegate (int h)
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);
                var cache = ArrayPool<Vector4>.Shared.Rent(totalPixelCount);

                {
                    var w = -pz - 1;
                    if (fmz > 0.0F)
                    {
                        var (sx, sy) = sourceMatrix.Transform(w, h);
                        var p = ImageInterpolation.Bilinear(data, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, bilinearEdgeMode);
                        cache[0] = p;
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        (sx, sy) = sourceMatrix.Transform(pz - 1, h);
                        p = ImageInterpolation.Bilinear(data, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, bilinearEdgeMode);
                        cache[totalPixelCount - 1] = p;
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        w++;
                    }

                    for (int limit = pz - (fmz > 0.0F ? 2 : 1), ci = fmz > 0.0F ? 1 : 0; w <= limit; w++, ci++)
                    {
                        var (sx, sy) = sourceMatrix.Transform(w, h);
                        var p = ImageInterpolation.Bilinear(data, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, bilinearEdgeMode);
                        cache[ci] = p;
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                var cacheIndex = totalPixelCount;
                for (var w = 0; w < tempWidth; w++, cacheIndex++)
                {
                    var p = cache[(cacheIndex - totalPixelCount) % totalPixelCount];
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;

                    if (fmz > 0.0F)
                    {
                        p = cache[(cacheIndex - totalPixelCount + 1) % totalPixelCount];
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }

                    var l = w + pz;
                    var (sx, sy) = sourceMatrix.Transform(l, h);
                    p = ImageInterpolation.Bilinear(data, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, bilinearEdgeMode);
                    cache[cacheIndex % totalPixelCount] = p;
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;

                    if (fmz > 0.0F)
                    {
                        p = cache[(cacheIndex - 1) % totalPixelCount];
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * tempWidth + w] = result;
                    }
                }

                ArrayPool<Vector4>.Shared.Return(cache);
            });

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var (tx, ty) = matrix.Transform(x, y);
                    imageDataSpan[x] = ImageInterpolation.Bilinear(temp, tempWidth, tempHeight, tx, ty);
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DirectionalBlurUnidirectionalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, int edgeRepeatMode, float sin, float cos, float amount, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var pos = y * width + x;

            var c = new Float4();
            var a = 0.0F;
            for (var i = 0; i <= range; i++)
            {
                var tc = Bilinear(x + i * cos, y + i * sin);
                c += tc * tc.W;
                a += tc.W;
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                var tc = Bilinear(x + edge * cos, y + edge * sin);
                c += tc * tc.W * fz;
                a += tc.W * fz;
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / (amount + 1.0F);
                result[pos] = rc;
            }
            else
            {
                result[pos] = Const.EmptyPixelFloat4;
            }
        }

        Float4 Bilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);
            if (ix == x && iy == y)
            {
                return GetPixel(ix, iy);
            }

            var c1 = GetPixel(ix, iy);
            var c2 = GetPixel(ix + 1, iy);
            var c3 = GetPixel(ix, iy + 1);
            var c4 = GetPixel(ix + 1, iy + 1);

            var pp = x - ix;
            var qq = y - iy;
            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return Const.EmptyPixelFloat4;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 GetPixel(int x, int y)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[CoordWrapGpu.Wrap(y, height) * width + CoordWrapGpu.Wrap(x, width)];
                case 2:
                    return image[CoordWrapGpu.Repeat(y, height) * width + CoordWrapGpu.Repeat(x, width)];
                case 3:
                    return image[CoordWrapGpu.Mirror(y, height) * width + CoordWrapGpu.Mirror(x, width)];
                default:
                    if (x > -1 && x < width && y > -1 && y < height)
                    {
                        return image[y * width + x];
                    }
                    else
                    {
                        return Const.EmptyPixelFloat4;
                    }
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DirectionalBlurBidirectionalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, int edgeRepeatMode, float sin, float cos, float amount, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var pos = y * width + x;

            var c = new Float4();
            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                var tc = Bilinear(x + i * cos, y + i * sin);
                c += tc * tc.W;
                a += tc.W;
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var fz = amount - range;
                var tc = Bilinear(x - edge * cos, y - edge * sin);
                c += tc * tc.W * fz;
                a += tc.W * fz;

                tc = Bilinear(x + edge * cos, y + edge * sin);
                c += tc * tc.W * fz;
                a += tc.W * fz;
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / (amount * 2.0F + 1.0F);
                result[pos] = rc;
            }
            else
            {
                result[pos] = Const.EmptyPixelFloat4;
            }
        }

        Float4 Bilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);
            if (ix == x && iy == y)
            {
                return GetPixel(ix, iy);
            }

            var c1 = GetPixel(ix, iy);
            var c2 = GetPixel(ix + 1, iy);
            var c3 = GetPixel(ix, iy + 1);
            var c4 = GetPixel(ix + 1, iy + 1);

            var pp = x - ix;
            var qq = y - iy;
            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return Const.EmptyPixelFloat4;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 GetPixel(int x, int y)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[CoordWrapGpu.Wrap(y, height) * width + CoordWrapGpu.Wrap(x, width)];
                case 2:
                    return image[CoordWrapGpu.Repeat(y, height) * width + CoordWrapGpu.Repeat(x, width)];
                case 3:
                    return image[CoordWrapGpu.Mirror(y, height) * width + CoordWrapGpu.Mirror(x, width)];
                default:
                    if (x > -1 && x < width && y > -1 && y < height)
                    {
                        return image[y * width + x];
                    }
                    else
                    {
                        return Const.EmptyPixelFloat4;
                    }
            }
        }
    }
}
