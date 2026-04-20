using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    // SEE: https://github.com/scriptituk/xfade-easing/blob/main/glsl/SimplePageCurl.glsl

    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_PageCurl_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_PageCurl_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class PageCurl : IEffect
    {
        const string ID = "EB4DEAE2-BC77-42A4-80F4-557C910A62B6";

        const string PropertyProgress = nameof(PropertyProgress);

        const string PropertyAngle = nameof(PropertyAngle);

        const string PropertyRadius = nameof(PropertyRadius);

        const string PropertyIsRoll = nameof(PropertyIsRoll);

        const string PropertyBackColor = nameof(PropertyBackColor);

        const string PropertyBackOpacity = nameof(PropertyBackOpacity);

        const string PropertyShadowStrength = nameof(PropertyShadowStrength);

        const string PropertyAntiAlias = nameof(PropertyAntiAlias);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyProgress, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_Progress, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new AngleProperty(PropertyAngle, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_Angle, 0.0, digit: 2),
                new DoubleProperty(PropertyRadius, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_Radius, 15, 0.0, double.MaxValue, digit: 2),
                new CheckBoxProperty(PropertyIsRoll, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_IsRoll, false),
                new ColorProperty(PropertyBackColor, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_BackColor, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.One),
                new DoubleProperty(PropertyBackOpacity, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_BackOpacity, 80.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new DoubleProperty(PropertyShadowStrength, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_ShadowStrength, 10.0, 0.0, double.MaxValue, digit: 2),
                new CheckBoxProperty(PropertyAntiAlias, LanguageResourceDictionary.ResourceKeys.Distortion_PageCurl_AntiAlias, true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var progress = (float)(properties.GetValue(PropertyProgress, layerTime, 0.0) * 0.01);
            if (progress <= 0.0F)
            {
                return image;
            }

            var radian = (float)(properties.GetValue(PropertyAngle, layerTime, 0.0) / 180.0 * Math.PI);
            var radius = (float)(properties.GetValue(PropertyRadius, layerTime, 0.0) * 0.01);
            var isRoll = properties.GetValue(PropertyIsRoll, layerTime, false);
            var backColor = properties.GetValue(PropertyBackColor, layerTime, Vector4.Zero);
            var backOpacity = (float)(properties.GetValue(PropertyBackOpacity, layerTime, 0.0) * 0.01);
            var shadowStrength = (float)(properties.GetValue(PropertyShadowStrength, layerTime, 0.0) * 0.01);
            var antiAlias = properties.GetValue(PropertyAntiAlias, layerTime, false);

            backColor.W = backOpacity;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, progress, radian, radius, isRoll, backColor, shadowStrength, antiAlias);
            }
            else
            {
                return ProcessCpu(image, roi, progress, radian, radius, isRoll, backColor, shadowStrength, antiAlias);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float progress, float radian, float radius, bool isRoll, Vector4 backColor, float shadowStrength, bool antiAlias)
        {
            const int SuperSamplingCount = 2;
            const float SuperSamplingAddRate = 1.0F / SuperSamplingCount;
            const float SuperSamplingRate = 1.0F / (SuperSamplingCount * SuperSamplingCount);

            var managedImage = image.ToManaged();

            using var sourceImage = (NManagedImage)managedImage.Copy();

            var ratio = managedImage.Width / (float)managedImage.Height;
            var dir = Vector2.Normalize(new Vector2(MathF.Cos(radian) * ratio, MathF.Sin(radian)));
            var quadrantCorner = new Vector2(dir.X >= 0.0F ? 0.5F : -0.5F, dir.Y >= 0.0F ? 0.5F : -0.5F);
            var initialPosition = dir * Vector2.Dot(quadrantCorner, dir);
            var finalPosition = -(initialPosition + dir * radius * 2.0F);
            var pp = initialPosition + (finalPosition - initialPosition) * progress;

            var imageData = managedImage.Data;
            var sourceData = sourceImage.Data;
            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var uRate = (float)(imageWidth - 1);
            var vRate = (float)(imageHeight - 1);
            if (antiAlias)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var sourceDataSpan = sourceData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var total = Vector4.Zero;
                        for (var sy = 0; sy < SuperSamplingCount; sy++)
                        {
                            for (var sx = 0; sx < SuperSamplingCount; sx++)
                            {
                                var q = (new Vector2(x, y) + new Vector2(sx, sy) * SuperSamplingAddRate) / new Vector2(uRate, vRate) - new Vector2(0.5F);
                                var distance = Vector2.Dot(q - pp, dir);
                                var p = q - dir * distance;

                                var isRolling = false;
                                var isBack = false;
                                var onShadow = false;
                                var reverseShadow = false;
                                var result = Const.EmptyPixel;
                                if (distance < 0.0F)
                                {
                                    if (!isRoll)
                                    {
                                        p += dir * (MathF.PI * radius - distance) + new Vector2(0.5F);
                                        isRolling = true;
                                        onShadow = true;
                                    }
                                    else if (-distance < radius)
                                    {
                                        var phi = MathF.Asin(-distance / radius);
                                        p += dir * (MathF.PI + phi) * radius + new Vector2(0.5F);
                                        isRolling = true;
                                        onShadow = true;
                                        reverseShadow = true;
                                    }

                                    if (isRolling && Vector2.GreaterThanOrEqualAll(p, Vector2.Zero) && Vector2.LessThanOrEqualAll(p, Vector2.One))
                                    {
                                        isBack = true;
                                    }
                                    else
                                    {
                                        result = sourceDataSpan[x];
                                        isRolling = false;
                                    }
                                }
                                else if (radius > 0.0F)
                                {
                                    var phi = MathF.Asin(distance / radius);
                                    var p2 = p + dir * (MathF.PI - phi) * radius + new Vector2(0.5F);
                                    var p1 = p + dir * phi * radius + new Vector2(0.5F);
                                    if (Vector2.GreaterThanOrEqualAll(p2, Vector2.Zero) && Vector2.LessThanOrEqualAll(p2, Vector2.One))
                                    {
                                        p = p2;
                                        isRolling = true;
                                        isBack = true;
                                        onShadow = true;
                                    }
                                    else if (Vector2.GreaterThanOrEqualAll(p1, Vector2.Zero) && Vector2.LessThanOrEqualAll(p1, Vector2.One))
                                    {
                                        p = p1;
                                        isRolling = true;
                                    }
                                    else
                                    {
                                        onShadow = true;
                                    }
                                }

                                if (isRolling)
                                {
                                    result = ImageInterpolation.Bilinear(sourceData, imageWidth, imageHeight, p.X * uRate, p.Y * vRate);
                                }
                                if (isBack)
                                {
                                    result = Blend.Process(BlendMode.Normal, result, backColor);
                                }
                                if (onShadow && isBack && radius > 0.0F)
                                {
                                    var shadowOpacity = 1.0F - MathF.Pow(Math.Clamp(Math.Abs(distance + (isRolling && reverseShadow ? radius : -radius)) / radius, 0.0F, 1.0F), shadowStrength);
                                    result = Blend.Process(BlendMode.Multiply, result, new Vector4(0.0F, 0.0F, 0.0F, shadowOpacity));
                                }

                                total += result;
                            }
                        }

                        imageDataSpan[x] = total * SuperSamplingRate;
                    }
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var sourceDataSpan = sourceData.AsSpan(y * imageWidth, imageWidth);
                    var v = y / vRate;
                    var qv = v - 0.5F;

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var u = x / uRate;
                        var q = new Vector2(u - 0.5F, qv);
                        var distance = Vector2.Dot(q - pp, dir);
                        var p = q - dir * distance;

                        var isRolling = false;
                        var isBack = false;
                        var onShadow = false;
                        var reverseShadow = false;
                        var result = Const.EmptyPixel;
                        if (distance < 0.0F)
                        {
                            if (!isRoll)
                            {
                                p += dir * (MathF.PI * radius - distance) + new Vector2(0.5F);
                                isRolling = true;
                                onShadow = true;
                            }
                            else if (-distance < radius)
                            {
                                var phi = MathF.Asin(-distance / radius);
                                p += dir * (MathF.PI + phi) * radius + new Vector2(0.5F);
                                isRolling = true;
                                onShadow = true;
                                reverseShadow = true;
                            }

                            if (isRolling && Vector2.GreaterThanOrEqualAll(p, Vector2.Zero) && Vector2.LessThanOrEqualAll(p, Vector2.One))
                            {
                                isBack = true;
                            }
                            else
                            {
                                result = sourceDataSpan[x];
                                isRolling = false;
                            }
                        }
                        else if (radius > 0.0F)
                        {
                            var phi = MathF.Asin(distance / radius);
                            var p2 = p + dir * (MathF.PI - phi) * radius + new Vector2(0.5F);
                            var p1 = p + dir * phi * radius + new Vector2(0.5F);
                            if (Vector2.GreaterThanOrEqualAll(p2, Vector2.Zero) && Vector2.LessThanOrEqualAll(p2, Vector2.One))
                            {
                                p = p2;
                                isRolling = true;
                                isBack = true;
                                onShadow = true;
                            }
                            else if (Vector2.GreaterThanOrEqualAll(p1, Vector2.Zero) && Vector2.LessThanOrEqualAll(p1, Vector2.One))
                            {
                                p = p1;
                                isRolling = true;
                            }
                            else
                            {
                                onShadow = true;
                            }
                        }

                        if (isRolling)
                        {
                            result = ImageInterpolation.Bilinear(sourceData, imageWidth, imageHeight, p.X * uRate, p.Y * vRate);
                        }
                        if (isBack)
                        {
                            result = Blend.Process(BlendMode.Normal, result, backColor);
                        }
                        if (onShadow && isBack && radius > 0.0F)
                        {
                            var shadowOpacity = 1.0F - MathF.Pow(Math.Clamp(Math.Abs(distance + (isRolling && reverseShadow ? radius : -radius)) / radius, 0.0F, 1.0F), shadowStrength);
                            result = Blend.Process(BlendMode.Multiply, result, new Vector4(0.0F, 0.0F, 0.0F, shadowOpacity));
                        }

                        imageDataSpan[x] = result;
                    }
                });
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float progress, float radian, float radius, bool isRoll, Vector4 backColor, float shadowStrength, bool antiAlias)
        {
            var gpuImage = image.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            var ratio = gpuImage.Width / (float)gpuImage.Height;
            var dir = Vector2.Normalize(new Vector2(MathF.Cos(radian) * ratio, MathF.Sin(radian)));
            var quadrantCorner = new Vector2(dir.X >= 0.0F ? 0.5F : -0.5F, dir.Y >= 0.0F ? 0.5F : -0.5F);
            var initialPosition = dir * Vector2.Dot(quadrantCorner, dir);
            var finalPosition = -(initialPosition + dir * radius * 2.0F);
            var pp = initialPosition + (finalPosition - initialPosition) * progress;

            device.For(roi.Width, roi.Height, new PageCurlProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, sourceImage.Data, radius, isRoll, backColor, shadowStrength, antiAlias, dir, pp, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PageCurlProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> sourceImage, float radius, bool isRoll, Float4 backColor, float shadowStrength, bool antiAlias, Float2 dir, Float2 pp, int startX, int startY) : IComputeShader
    {
        const int SuperSamplingCount = 2;
        const float SuperSamplingAddRate = 1.0F / SuperSamplingCount;
        const float SuperSamplingRate = 1.0F / (SuperSamplingCount * SuperSamplingCount);

        readonly Float2 UVRate = new Float2(width - 1.0F, height - 1.0F);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var originalPixel = sourceImage[pos];

            if (antiAlias)
            {
                var total = Float4.Zero;
                for (var sy = 0; sy < SuperSamplingCount; sy++)
                {
                    for (var sx = 0; sx < SuperSamplingCount; sx++)
                    {
                        total += Process(x + sx * SuperSamplingAddRate, y + sy * SuperSamplingAddRate, originalPixel);
                    }
                }
                image[pos] = total * SuperSamplingRate;
            }
            else
            {
                image[pos] = Process(x, y, originalPixel);
            }
        }

        Float4 Process(float x, float y, Float4 originalPixel)
        {
            var q = new Float2(x, y) / UVRate - 0.5F;
            var distance = Hlsl.Dot(q - pp, dir);
            var p = q - dir * distance;

            var isRolling = false;
            var isBack = false;
            var onShadow = false;
            var reverseShadow = false;
            var result = Const.EmptyPixelFloat4;
            if (distance < 0.0F)
            {
                if (!isRoll)
                {
                    p += dir * (MathF.PI * radius - distance) + 0.5F;
                    isRolling = true;
                    onShadow = true;
                }
                else if (-distance < radius)
                {
                    var phi = Hlsl.Asin(-distance / radius);
                    p += dir * (MathF.PI + phi) * radius + 0.5F;
                    isRolling = true;
                    onShadow = true;
                    reverseShadow = true;
                }

                if (isRolling && Hlsl.All(p >= 0.0F) && Hlsl.All(p <= 1.0F))
                {
                    isBack = true;
                }
                else
                {
                    result = originalPixel;
                    isRolling = false;
                }
            }
            else if (radius > 0.0F)
            {
                var phi = Hlsl.Asin(distance / radius);
                var p2 = p + dir * (MathF.PI - phi) * radius + 0.5F;
                var p1 = p + dir * phi * radius + 0.5F;
                if (Hlsl.All(p2 >= 0.0F) && Hlsl.All(p2 <= 1.0F))
                {
                    p = p2;
                    isRolling = true;
                    isBack = true;
                    onShadow = true;
                }
                else if (Hlsl.All(p1 >= 0.0F) && Hlsl.All(p1 <= 1.0F))
                {
                    p = p1;
                    isRolling = true;
                }
                else
                {
                    onShadow = true;
                }
            }

            if (isRolling)
            {
                var pos = p * UVRate;
                result = SourceImageBilinear(pos.X, pos.Y);
            }
            if (isBack)
            {
                result = BlendMethods.Process(0, result, backColor);
            }
            if (onShadow && isBack && radius > 0.0F)
            {
                var shadowOpacity = 1.0F - Hlsl.Pow(Hlsl.Clamp(Hlsl.Abs(distance + (isRolling && reverseShadow ? radius : -radius)) / radius, 0.0F, 1.0F), shadowStrength);
                result = BlendMethods.Process(4, result, new Float4(0.0F, 0.0F, 0.0F, shadowOpacity));
            }

            return result;
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return sourceImage[iy * width + ix];
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
                        c1 = sourceImage[pos];
                        c2 = sourceImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = sourceImage[pos];
                            c4 = sourceImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = sourceImage[pos];
                        c4 = sourceImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = sourceImage[pos];
                        if (iy < mh)
                        {
                            c3 = sourceImage[pos + width];
                        }
                    }
                    else
                    {
                        c3 = sourceImage[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = sourceImage[pos];
                    if (iy < mh)
                    {
                        c4 = sourceImage[pos + width];
                    }
                }
                else
                {
                    c4 = sourceImage[pos + width];
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
