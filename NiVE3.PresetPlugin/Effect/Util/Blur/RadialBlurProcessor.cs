using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Drawing;
using NiVE3.Image;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Distortion;
using NiVE3.PresetPlugin.Internal.Drawing;
using System.Windows;
using NiVE3.PresetPlugin.Internal;
using NiVE3.Numerics;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Effect.Util.Blur
{
    static class RadialBlurProcessor
    {
        public static NManagedImage ProcessCpu(NImage image, ROI roi, Vector2 center, float amount, bool fastMode)
        {
            if (fastMode)
            {
                return ProcessFastCpu(image, roi, center, amount);
            }
            else
            {
                return ProcessPrecisionCpu(image, roi, center, amount);
            }
        }

        public static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector2 center, float amount)
        {
            var gpuImage = image.ToGpu(device);
            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            device.For(roi.Width, roi.Height, new RadialBlurProcess(gpuImage.Data, sourceImage.Data, gpuImage.Width, gpuImage.Height, center + new Vector2(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y), amount, roi.Left, roi.Top));

            return gpuImage;
        }

        static NManagedImage ProcessPrecisionCpu(NImage image, ROI roi, Vector2 center, float amount)
        {
            var managedImage = image.ToManaged();
            using var sourceImage = (NManagedImage)managedImage.Copy();

            var cx = center.X + roi.OriginalImagePosition.X;
            var cy = center.Y + roi.OriginalImagePosition.Y;
            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageSize = new Vector2((float)imageWidth, imageHeight);
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                var dy = y - cy;
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var dx = x - cx;
                    var radius = new Vector2(MathF.Sqrt(dx * dx + dy * dy));
                    var rad = MathF.Atan2(dy, dx);
                    var (sin, cos) = MathF.SinCos(rad);
                    var cosSin = new Vector2(cos, sin);
                    var add = amount * radius * 0.1F;
                    var samplingCount = (int)MathF.Ceiling(add.X);
                    add /= samplingCount;

                    var color = Vector4.Zero;
                    var a = 0.0F;
                    var zpos = add * -samplingCount;
                    if (samplingCount < 1)
                    {
                        continue;
                    }
                    for (var i = 0; i < samplingCount; i++, zpos += add)
                    {
                        var tpos = (radius + zpos) * cosSin + center;
                        var tc = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, tpos.X, tpos.Y);
                        var ta = tc.W;
                        a += ta;
                        color += tc * ta;
                    }

                    if (a > 0.0F)
                    {
                        color /= a;
                        color.W = a / samplingCount;
                        imageDataSpan[x] = color;
                    }
                    else
                    {
                        imageDataSpan[x] = Const.EmptyPixel;
                    }
                }
            });

            return managedImage;
        }

        static NManagedImage ProcessFastCpu(NImage image, ROI roi, Vector2 center, float amount)
        {
            var managedImage = image.ToManaged();

            var imageSize = new Vector2(managedImage.Width, managedImage.Height);
            var originalImageSize = new Vector2(roi.OriginalImageSize.Width, roi.OriginalImageSize.Height);
            var originalImagePosition = new Vector2(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y);
            var polarImageAnchor = Vector2.Max(Vector2.Min(originalImageSize * 0.5F - center - originalImagePosition * 0.5F + Vector2.Max(imageSize - originalImagePosition - originalImageSize, Vector2.Zero) * 0.5F, imageSize * 0.5F), -imageSize * 0.5F);
            var polarImageMarginX = (int)Math.Abs(polarImageAnchor.X) * 2;
            var polarImageMarginY = (int)Math.Abs(polarImageAnchor.Y) * 2;
            var polarImageOffset = Vector2.Max(polarImageAnchor * 2.0F, Vector2.Zero);

            var polarImage = new NManagedImage(managedImage.Width + polarImageMarginX, managedImage.Height + polarImageMarginY);
            var polarRoi = new ROI(Int32Point.Zero, new Int32Size(polarImage.Width, polarImage.Height), 0, 0, polarImage.Width, polarImage.Height);

            new CPURenderer2D(polarImage).DrawSingleImage(Int32Point.Zero, managedImage, 1.0F, Matrix3x3.CreateTranslate(polarImageOffset.X, polarImageOffset.Y), ImageInterpolationQuality.Level2, BlendMode.Replace, null);

            polarImage = PolarDistortionProcessor.ProcessCpu(polarImage, polarRoi, 1.0F, PolarDistortionMode.ToRect, Vector2.Zero, Vector2.Zero, true);
            CumulativeVerticalBlur(polarImage, polarRoi, amount * 0.1F * managedImage.Height);
            polarImage = PolarDistortionProcessor.ProcessCpu(polarImage, polarRoi, 1.0F, PolarDistortionMode.ToPolar, Vector2.Zero, Vector2.Zero, true);

            new CPURenderer2D(managedImage)
            {
                Clip = new Int32Rect(roi.Left, roi.Top, roi.Width, roi.Height)
            }.DrawSingleImage(Int32Point.Zero, polarImage, 1.0F, Matrix3x3.CreateTranslate(-polarImageOffset.X, -polarImageOffset.Y), ImageInterpolationQuality.Level2, BlendMode.Replace, null);

            polarImage.Dispose();

            return managedImage;
        }

        static void CumulativeVerticalBlur(NManagedImage managedImage, ROI roi, float amount)
        {
            using var sourceImage = (NManagedImage)managedImage.Copy();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;
            var addz = 1.0F - Math.Min(amount, managedImage.Height) / managedImage.Height;
            Parallel.For(roi.Left, roi.Right, x =>
            {
                var color = Vector4.Zero;
                var a = 0.0F;
                var count = 0.0F;
                var zpos = -addz;
                for (var y = 0; y < roi.Top; y++, zpos += addz)
                {
                    var tc = sourceImageData[y * imageWidth + x];
                    var ta = tc.W;
                    a += ta;
                    color += tc * ta;
                    count += 1.0F;

                    if (zpos >= 0.0F)
                    {
                        var diff = (int)(zpos + addz) - (int)zpos;
                        if (diff == 1)
                        {
                            var pz = 1.0F - (zpos - (int)zpos);
                            tc = sourceImageData[(int)zpos * imageWidth + x];
                            ta = tc.W * pz;
                            a -= ta;
                            color -= tc * ta;

                            pz = addz - pz;
                            tc = sourceImageData[(int)(zpos + addz) * imageWidth + x];
                            ta = tc.W * pz;
                            a -= ta;
                            color -= tc * ta;
                        }
                        else
                        {
                            tc = sourceImageData[(int)zpos * imageWidth + x];
                            ta = tc.W * addz;
                            a -= ta;
                            color -= tc * ta;
                        }

                        count -= addz;
                    }
                }
                for (var y = roi.Top; y < roi.Bottom; y++, zpos += addz)
                {
                    var pos = y * imageWidth + x;
                    var tc = sourceImageData[pos];
                    var ta = tc.W;
                    a += ta;
                    color += tc * ta;
                    count += 1.0F;

                    if (zpos >= 0.0F)
                    {
                        var diff = (int)(zpos + addz) - (int)zpos;
                        if (diff == 1)
                        {
                            var pz = 1.0F - (zpos - (int)zpos);
                            tc = sourceImageData[(int)zpos * imageWidth + x];
                            ta = tc.W * pz;
                            color -= tc * ta;
                            a -= ta;

                            pz = addz - pz;
                            tc = sourceImageData[(int)(zpos + addz) * imageWidth + x];
                            ta = tc.W * pz;
                            a -= ta;
                            color -= tc * ta;
                        }
                        else
                        {
                            tc = sourceImageData[(int)zpos * imageWidth + x];
                            ta = tc.W * addz;
                            a -= ta;
                            color -= tc * ta;
                        }

                        count -= addz;
                    }

                    if (a > 0.0F)
                    {
                        var result = color / a;
                        result.W = a / count;
                        imageData[pos] = result;
                    }
                    else
                    {
                        imageData[pos] = Const.EmptyPixel;
                    }
                }
            });
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct RadialBlurProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, Float2 center, float amount, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var dx = x - center.X;
            var dy = y - center.Y;
            var radius = Hlsl.Sqrt(dx * dx + dy * dy);
            var rad = Hlsl.Atan2(dy, dx);
            Hlsl.SinCos(rad, out var sin, out var cos);
            var cosSin = new Float2(cos, sin);
            var add = amount * radius * 0.1F;
            var samplingCount = (int)Hlsl.Ceil(add);
            add /= samplingCount;

            if (samplingCount < 1)
            {
                return;
            }

            var color = Float4.Zero;
            var a = 0.0F;
            var zpos = add * -samplingCount;
            for (var i = 0; i < samplingCount; i++, zpos += add)
            {
                var tpos = (radius + zpos) * cosSin + center;
                var tc = OriginalImageBilinear(tpos.X, tpos.Y);
                var ta = tc.W;
                a += ta;
                color += tc * ta;
            }

            var pos = y * width + x;
            if (a > 0.0F)
            {
                color /= a;
                color.W = a / samplingCount;
                image[pos] = color;
            }
            else
            {
                image[pos] = Const.EmptyPixelFloat4;
            }
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
}
