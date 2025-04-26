using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;

namespace NiVE3.PresetPlugin.Effect.Util.Distortion
{
    static class PolarDistortionProcessor
    {
        const float SqrtHalf = 0.7071067811865476F; // Math.Sqrt(0.5);

        public static NManagedImage ProcessCpu(NImage image, ROI roi, float transformAmount, PolarDistortionMode mode, Vector2 imageOffset, Vector2 displayAreaOffset, bool forPreOrPostProcess)
        {
            var managedImage = image.ToManaged();

            using var sourceImage = (NManagedImage)managedImage.Copy();
            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;

            var imageSizeExpandRate = Math.Max(Math.Abs(imageOffset.X) / imageWidth, Math.Abs(imageOffset.Y) / imageHeight);
            var virtualWidth = imageWidth * imageSizeExpandRate + imageWidth;
            var virtualHeight = imageHeight * imageSizeExpandRate + imageHeight;
            var minSize = Math.Min(virtualWidth, virtualHeight);
            var maxSize = Math.Max(virtualWidth, virtualHeight);
            var imageSize = new Vector2(virtualWidth, virtualHeight);
            var positionOffset = (displayAreaOffset + new Vector2(imageWidth, imageHeight) * imageSizeExpandRate * 0.5F) / imageSize;
            var expandedImageOffset = imageOffset + new Vector2(imageWidth, imageHeight) * imageSizeExpandRate * 0.5F;
            switch ((mode, forPreOrPostProcess))
            {
                case (PolarDistortionMode.ToPolar, false):
                    {
                        var scale = imageSize / minSize;
                        var offset = (new Vector2(minSize) - imageSize) / imageSize * 0.5F;
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var pos = new Vector2(x, y) / imageSize + positionOffset;
                                var dir = ((pos + offset) * scale) - new Vector2(0.5F);
                                var radius = dir.Length() * 2.0F;
                                var rad = (MathF.Atan2(dir.X, dir.Y) + MathF.PI) / MathF.PI * 0.5F;
                                var transformed = Vector2.Lerp(pos, new Vector2(rad, radius), transformAmount) * imageSize - expandedImageOffset;

                                imageDataSpan[x] = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, transformed.X, transformed.Y);
                            }
                        });
                    }
                    break;
                case (PolarDistortionMode.ToPolar, true):
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var pos = new Vector2(x, y) / imageSize + positionOffset;
                            var dir = pos - new Vector2(0.5F);
                            var radius = dir.Length() / SqrtHalf;
                            var rad = (MathF.Atan2(dir.X, -dir.Y) + MathF.PI * 1.5F) / (MathF.PI * 3.0F);
                            var transformed = Vector2.Lerp(pos, new Vector2(rad, radius), transformAmount) * imageSize - expandedImageOffset;

                            imageDataSpan[x] = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, transformed.X, transformed.Y, Const.EmptyPixel, BilinearEdgeMode.Wrap);
                        }
                    });
                    break;
                case (PolarDistortionMode.ToRect, false):
                    {
                        var scale = imageSize / maxSize;
                        var offset = (new Vector2(virtualHeight, virtualWidth) - new Vector2(minSize)) / imageSize * 0.5F;
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var pos = new Vector2(x, y) / imageSize + positionOffset;
                                var t = (pos.X * MathF.PI * 2.0F) - MathF.PI;
                                var cos = MathF.Cos(t);
                                var sin = MathF.Sin(t);
                                var transformed = Vector2.Lerp(pos, (new Vector2(sin, cos) * SqrtHalf * pos.Y + new Vector2(0.5F)) / scale - offset, transformAmount) * imageSize - expandedImageOffset;

                                imageDataSpan[x] = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, transformed.X, transformed.Y);
                            }
                        });
                    }
                    break;
                case (PolarDistortionMode.ToRect, true):
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var pos = new Vector2(x, y) / imageSize + positionOffset;
                            var dir = pos.X - 0.5F;
                            var t = dir * MathF.PI * 3.0F - MathF.PI * 0.5F;
                            var cos = MathF.Cos(t);
                            var sin = MathF.Sin(t);
                            var transformed = Vector2.Lerp(pos, new Vector2(cos, sin) * SqrtHalf * pos.Y + new Vector2(0.5F), transformAmount) * imageSize - expandedImageOffset;

                            imageDataSpan[x] = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, transformed.X, transformed.Y, Const.EmptyPixel, BilinearEdgeMode.Wrap);
                        }
                    });
                    break;
            }

            return managedImage;
        }

        public static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float transformAmount, PolarDistortionMode mode, Vector2 imageOffset, Vector2 displayAreaOffset, bool forPreOrPostProcess)
        {
            var gpuImage = image.ToGpu(device);

            var imageWidth = gpuImage.Width;
            var imageHeight = gpuImage.Height;

            var imageSizeExpandRate = Math.Max(Math.Abs(imageOffset.X) / imageWidth, Math.Abs(imageOffset.Y) / imageHeight);
            var virtualWidth = imageWidth * imageSizeExpandRate + imageWidth;
            var virtualHeight = imageHeight * imageSizeExpandRate + imageHeight;
            var minSize = Math.Min(virtualWidth, virtualHeight);
            var maxSize = Math.Max(virtualWidth, virtualHeight);
            var imageSize = new Vector2(virtualWidth, virtualHeight);
            var positionOffset = (displayAreaOffset + new Vector2(imageWidth, imageHeight) * imageSizeExpandRate * 0.5F) / imageSize;
            var expandedImageOffset = imageOffset + new Vector2(imageWidth, imageHeight) * imageSizeExpandRate * 0.5F;
            using var sourceImage = new NGPUImage(imageWidth, imageHeight, device);
            gpuImage.CopyTo(sourceImage);

            using var context = device.CreateComputeContext();
            switch ((mode, forPreOrPostProcess))
            {
                case (PolarDistortionMode.ToPolar, false):
                    {
                        var scale = imageSize / minSize;
                        var offset = (new Vector2(minSize) - imageSize) / imageSize * 0.5F;
                        context.For(roi.Width, roi.Height, new PolarDistortionToPolarProcess(gpuImage.Data, sourceImage.Data, imageWidth, imageHeight, transformAmount, scale, offset, imageSize, expandedImageOffset, positionOffset, roi.Left, roi.Top));
                    }
                    break;
                case (PolarDistortionMode.ToPolar, true):
                    context.For(roi.Width, roi.Height, new PolarDistortionToPolarPreOrPostProcess(gpuImage.Data, sourceImage.Data, imageWidth, imageHeight, transformAmount, imageSize, expandedImageOffset, positionOffset, roi.Left, roi.Top));
                    break;
                case (PolarDistortionMode.ToRect, false):
                    {
                        var scale = imageSize / maxSize;
                        var offset = (new Vector2(virtualHeight, virtualWidth) - new Vector2(minSize)) / imageSize * 0.5F;
                        context.For(roi.Width, roi.Height, new PolarDistortionToRectProcess(gpuImage.Data, sourceImage.Data, imageWidth, imageHeight, transformAmount, scale, offset, imageSize, expandedImageOffset, positionOffset, roi.Left, roi.Top));
                    }
                    break;
                case (PolarDistortionMode.ToRect, true):
                    context.For(roi.Width, roi.Height, new PolarDistortionToRectPreOrPostProcess(gpuImage.Data, sourceImage.Data, imageWidth, imageHeight, transformAmount, imageSize, expandedImageOffset, positionOffset, roi.Left, roi.Top));
                    break;
            }

            return gpuImage;
        }
    }

    enum PolarDistortionMode
    {
        ToPolar,
        ToRect
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PolarDistortionToPolarProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, float transformAmount, Float2 scale, Float2 offset, Float2 imageSize, Float2 imageOffset, Float2 positionOffset, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = new Float2(x, y) / imageSize + positionOffset;
            var dir = ((pos + offset) * scale) - 0.5F;
            var radius = Hlsl.Length(dir) * 2.0F;
            var rad = (Hlsl.Atan2(dir.X, dir.Y) + MathF.PI) / MathF.PI * 0.5F;
            var transformed = Hlsl.Lerp(pos, new Float2(rad, radius), transformAmount) * imageSize - imageOffset;

            image[y * width + x] = OriginalImageBilinear(transformed.X, transformed.Y);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return Const.EmptyPixelFloat4;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return Const.EmptyPixelFloat4;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = Const.EmptyPixelFloat4;
            var c2 = Const.EmptyPixelFloat4;
            var c3 = Const.EmptyPixelFloat4;
            var c4 = Const.EmptyPixelFloat4;
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        c2 = originalImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = originalImage[pos];
                            c4 = originalImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = originalImage[pos];
                        c4 = originalImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        if (iy < mh)
                        {
                            c3 = originalImage[pos + width];
                        }
                    }
                    else
                    {
                        c3 = originalImage[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = originalImage[pos];
                    if (iy < mh)
                    {
                        c4 = originalImage[pos + width];
                    }
                }
                else
                {
                    c4 = originalImage[pos + width];
                }
            }

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
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PolarDistortionToPolarPreOrPostProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, float transformAmount, Float2 imageSize, Float2 imageOffset, Float2 positionOffset, int startX, int startY) : IComputeShader
    {
        const float SqrtHalf = 0.7071067811865476F; // Math.Sqrt(0.5);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = new Float2(x, y) / imageSize + positionOffset;
            var dir = pos - 0.5F;
            var radius = Hlsl.Length(dir) / SqrtHalf;
            var rad = (Hlsl.Atan2(dir.X, -dir.Y) + MathF.PI * 1.5F) / (MathF.PI * 3.0F);
            var transformed = Hlsl.Lerp(pos, new Float2(rad, radius), transformAmount) * imageSize - imageOffset;

            image[y * width + x] = OriginalImageBilinear(transformed.X, transformed.Y);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);
            if (ix == x && iy == y)
            {
                return originalImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix, width)];
            }

            var c1 = originalImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix, width)];
            var c2 = originalImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix + 1, width)];
            var c3 = originalImage[CoordWrapGpu.Wrap(iy + 1, height) * width + CoordWrapGpu.Wrap(ix, width)];
            var c4 = originalImage[CoordWrapGpu.Wrap(iy + 1, height) * width + CoordWrapGpu.Wrap(ix + 1, width)];

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
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PolarDistortionToRectProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, float transformAmount, Float2 scale, Float2 offset, Float2 imageSize, Float2 imageOffset, Float2 positionOffset, int startX, int startY) : IComputeShader
    {
        const float SqrtHalf = 0.7071067811865476F; // Math.Sqrt(0.5);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = new Float2(x, y) / imageSize + positionOffset;
            var invertTransform = (1.0F - transformAmount) * pos;
            var t = (pos.X * MathF.PI * 2.0F) - MathF.PI;
            var cos = Hlsl.Cos(t);
            var sin = Hlsl.Sin(t);
            var transformed = Hlsl.Lerp(pos, (new Float2(sin, cos) * SqrtHalf * pos.Y + 0.5F) / scale - offset, transformAmount) * imageSize - imageOffset;

            image[y * width + x] = OriginalImageBilinear(transformed.X, transformed.Y);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return Const.EmptyPixelFloat4;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return Const.EmptyPixelFloat4;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = Const.EmptyPixelFloat4;
            var c2 = Const.EmptyPixelFloat4;
            var c3 = Const.EmptyPixelFloat4;
            var c4 = Const.EmptyPixelFloat4;
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        c2 = originalImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = originalImage[pos];
                            c4 = originalImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = originalImage[pos];
                        c4 = originalImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        if (iy < mh)
                        {
                            c3 = originalImage[pos + width];
                        }
                    }
                    else
                    {
                        c3 = originalImage[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = originalImage[pos];
                    if (iy < mh)
                    {
                        c4 = originalImage[pos + width];
                    }
                }
                else
                {
                    c4 = originalImage[pos + width];
                }
            }

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
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PolarDistortionToRectPreOrPostProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, float transformAmount, Float2 imageSize, Float2 imageOffset, Float2 positionOffset, int startX, int startY) : IComputeShader
    {
        const float SqrtHalf = 0.7071067811865476F; // Math.Sqrt(0.5);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = new Float2(x, y) / imageSize + positionOffset;
            var dir = pos.X - 0.5F;
            var t = dir * MathF.PI * 3.0F - MathF.PI * 0.5F;
            var cos = Hlsl.Cos(t);
            var sin = Hlsl.Sin(t);
            var transformed = Hlsl.Lerp(pos, (new Float2(cos, sin) * SqrtHalf * pos.Y + 0.5F), transformAmount) * imageSize - imageOffset;

            image[y * width + x] = OriginalImageBilinear(transformed.X, transformed.Y);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);
            if (ix == x && iy == y)
            {
                return originalImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix, width)];
            }

            var c1 = originalImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix, width)];
            var c2 = originalImage[CoordWrapGpu.Wrap(iy, height) * width + CoordWrapGpu.Wrap(ix + 1, width)];
            var c3 = originalImage[CoordWrapGpu.Wrap(iy + 1, height) * width + CoordWrapGpu.Wrap(ix, width)];
            var c4 = originalImage[CoordWrapGpu.Wrap(iy + 1, height) * width + CoordWrapGpu.Wrap(ix + 1, width)];

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
    }
}
