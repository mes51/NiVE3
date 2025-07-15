using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Color;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Internal.Util;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_LongShadow_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_LongShadow_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class LongShadow : IEffect
    {
        const string ID = "9380500C-BC97-4E3C-8966-3EB4211CB186";

        const string PropertyLengthId = nameof(PropertyLengthId);

        const string PropertyShapeTypeId = nameof(PropertyShapeTypeId);

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyRadiateCenterId = nameof(PropertyRadiateCenterId);

        const string PropertyAlphaThresholdId = nameof(PropertyAlphaThresholdId);

        const string PropertyGradientId = nameof(PropertyGradientId);

        const string PropertyUseOkLabInterpolationId = nameof(PropertyUseOkLabInterpolationId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        internal const float OutsideThreshold = 0.01F;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var radiateCenter = new Vector3d(sourceSize.Width * 0.5, sourceSize.Height * 0.25, 0.0);
            return
            [
                new DoubleProperty(PropertyLengthId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_Length, 100.0, 0.0, double.MaxValue, digit: 2),
                new EnumProperty(PropertyShapeTypeId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_ShapeType, typeof(LongShadowShapeType), typeof(LanguageResourceDictionary), LongShadowShapeType.Parallel, selectBoxWidth: 90.0),
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_Angle, 45.0, digit: 2),
                new Vector3dProperty(PropertyRadiateCenterId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_RadiateCenter, radiateCenter, digit: 2),
                new DoubleProperty(PropertyAlphaThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_AlphaThreshold, 1.0, 0.0, 1.0, slideChangeValue: 0.01, digit: 2),
                new ColorGradientProperty(PropertyGradientId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_Gradient, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel),
                new CheckBoxProperty(PropertyUseOkLabInterpolationId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_UseOkLabInterpolation, false),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Stylize_LongShadow_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var length = (float)(properties.GetValue(PropertyLengthId, layerTime, 0.0) / downSamplingRateX);
            if (length <= 0.0F)
            {
                return baseRoi;
            }

            var type = properties.GetValue(PropertyShapeTypeId, layerTime, LongShadowShapeType.Parallel);
            switch (type)
            {
                case LongShadowShapeType.Radiate:
                case LongShadowShapeType.InvertRadiate:
                    {
                        var intLength = (int)MathF.Ceiling(length);
                        return baseRoi.Expand(-intLength, -intLength, intLength, intLength);
                    }
                default:
                    {
                        var radian = properties.GetValue(PropertyAngleId, layerTime, 0.0) / 180.0 * Math.PI;

                        var dx = (int)Math.Ceiling(Math.Cos(radian) * length);
                        var dy = (int)Math.Ceiling(Math.Sin(radian) * length);

                        return baseRoi.Expand(Math.Min(dx, 0), Math.Min(dy, 0), Math.Max(dx, 0), Math.Max(dy, 0));
                    }
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var length = (float)(properties.GetValue(PropertyLengthId, layerTime, 0.0) / downSamplingRateX);

            if (length <= 0.0)
            {
                return image;
            }

            var type = properties.GetValue(PropertyShapeTypeId, layerTime, LongShadowShapeType.Parallel);
            var radian = properties.GetValue(PropertyAngleId, layerTime, 0.0) / 180.0 * Math.PI;
            var radiateCenter = (Vector2)properties.GetValue(PropertyRadiateCenterId, layerTime, Vector3d.Zero) + new Vector2(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y);
            var gradient = properties.GetValue(PropertyGradientId, layerTime, ColorGradient.WhiteBlackGradient);
            var alphaThreshold = (float)properties.GetValue(PropertyAlphaThresholdId, layerTime, 0.0);
            var useOkLabInterpolation = properties.GetValue(PropertyUseOkLabInterpolationId, layerTime, false);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, length, type, radian, radiateCenter, alphaThreshold, gradient, useOkLabInterpolation, blendMode);
            }
            else
            {
                return ProcessCpu(image, roi, length, type, radian, radiateCenter, alphaThreshold, gradient, useOkLabInterpolation, blendMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float length, LongShadowShapeType type, double radian, Vector2 radiateCenter, float alphaThreshold, ColorGradient gradient, bool useOkLabInterpolation, BlendMode blendMode)
        {
            var managedImage = image.ToManaged();

            var shadowImage = new NManagedImage(managedImage.Width, managedImage.Height)
            {
                Origin = managedImage.Origin
            };

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var shadowImageData = shadowImage.Data;
            switch (type)
            {
                case LongShadowShapeType.Radiate:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        var shadowImageDataSpan = shadowImageData.AsSpan(y * imageWidth, imageWidth);

                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var lightPos = radiateCenter - new Vector2(x, y);
                            var (sin, cos) = MathF.SinCos(MathF.Atan2(lightPos.Y, lightPos.X));
                            var diff = new Vector2(cos, sin);
                            var a = 0.0F;
                            var hitLength = 0;
                            var rendered = false;
                            for (var i = 0; i < length; i++)
                            {
                                var p = new Vector2(x, y) + diff * i;
                                if (Vector2.Dot(radiateCenter - p, lightPos) < 0.0F)
                                {
                                    break;
                                }
                                var color = ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, p.X, p.Y);
                                var edge = Math.Min(length - i, 1.0F);
                                var currentA = color.W * edge;
                                if (a <= currentA)
                                {
                                    hitLength = a < alphaThreshold ? i : hitLength;
                                    a = currentA;
                                    rendered = false;
                                }
                                else if (Math.Abs(a - currentA) > OutsideThreshold)
                                {
                                    if (!rendered)
                                    {
                                        var shadowColor = gradient.GetrColor(hitLength / length, useOkLabInterpolation) * new Vector4(1.0F, 1.0F, 1.0F, a);
                                        shadowImageDataSpan[x] = Blend.Process(BlendMode.Normal, shadowColor, shadowImageDataSpan[x]);
                                        rendered = true;
                                    }

                                    a = currentA;
                                    hitLength = i;
                                }
                            }

                            if (!rendered && a > 0.0F)
                            {
                                var shadowColor = gradient.GetrColor(hitLength / length, useOkLabInterpolation) * new Vector4(1.0F, 1.0F, 1.0F, a);
                                shadowImageDataSpan[x] = Blend.Process(BlendMode.Normal, shadowColor, shadowImageDataSpan[x]);
                            }
                        }
                    });
                    break;
                case LongShadowShapeType.InvertRadiate:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        var shadowImageDataSpan = shadowImageData.AsSpan(y * imageWidth, imageWidth);

                        var diffY = radiateCenter.Y - y;
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var currentRad = MathF.Atan2(diffY, radiateCenter.X - x);
                            var dx = -MathF.Cos(currentRad);
                            var dy = -MathF.Sin(currentRad);
                            var a = 0.0F;
                            var hitLength = 0;
                            var rendered = false;
                            for (var i = 0; i < length; i++)
                            {
                                var color = ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x + dx * i, y + dy * i);
                                var edge = Math.Min(length - i, 1.0F);
                                var currentA = color.W * edge;
                                if (a <= currentA)
                                {
                                    hitLength = a < alphaThreshold ? i : hitLength;
                                    a = currentA;
                                    rendered = false;
                                }
                                else if (Math.Abs(a - currentA) > OutsideThreshold)
                                {
                                    if (!rendered)
                                    {
                                        var shadowColor = gradient.GetrColor(hitLength / length, useOkLabInterpolation) * new Vector4(1.0F, 1.0F, 1.0F, a);
                                        shadowImageDataSpan[x] = Blend.Process(BlendMode.Normal, shadowColor, shadowImageDataSpan[x]);
                                        rendered = true;
                                    }

                                    a = currentA;
                                    hitLength = i;
                                }
                            }

                            if (!rendered && a > 0.0F)
                            {
                                var shadowColor = gradient.GetrColor(hitLength / length, useOkLabInterpolation) * new Vector4(1.0F, 1.0F, 1.0F, a);
                                shadowImageDataSpan[x] = Blend.Process(BlendMode.Normal, shadowColor, shadowImageDataSpan[x]);
                            }
                        }
                    });
                    break;
                default:
                    {
                        var dx = (float)-Math.Cos(radian);
                        var dy = (float)-Math.Sin(radian);
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var shadowImageDataSpan = shadowImageData.AsSpan(y * imageWidth, imageWidth);

                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var a = 0.0F;
                                var hitLength = 0;
                                var rendered = false;
                                for (var i = 0; i < length; i++)
                                {
                                    var color = ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x + dx * i, y + dy * i);
                                    var edge = Math.Min(length - i, 1.0F);
                                    var currentA = color.W * edge;
                                    if (a <= currentA)
                                    {
                                        hitLength = a < alphaThreshold ? i : hitLength;
                                        a = currentA;
                                        rendered = false;
                                    }
                                    else if (Math.Abs(a - currentA) > OutsideThreshold)
                                    {
                                        if (!rendered)
                                        {
                                            var shadowColor = gradient.GetrColor(hitLength / length, useOkLabInterpolation) * new Vector4(1.0F, 1.0F, 1.0F, a);
                                            shadowImageDataSpan[x] = Blend.Process(BlendMode.Normal, shadowColor, shadowImageDataSpan[x]);
                                            rendered = true;
                                        }

                                        a = currentA;
                                        hitLength = i;
                                    }
                                }

                                if (!rendered && a > 0.0F)
                                {
                                    var shadowColor = gradient.GetrColor(hitLength / length, useOkLabInterpolation) * new Vector4(1.0F, 1.0F, 1.0F, a);
                                    shadowImageDataSpan[x] = Blend.Process(BlendMode.Normal, shadowColor, shadowImageDataSpan[x]);
                                }
                            }
                        });
                    }
                    break;
            }

            ImageBlendProcessor.SameSizeCpu(shadowImage, managedImage, new ROI(Int32Point.Zero, new Int32Size(managedImage.Width, managedImage.Height), 0, 0, managedImage.Width, managedImage.Height), blendMode);

            if (managedImage != image)
            {
                managedImage.Dispose();
            }

            return shadowImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float length, LongShadowShapeType type, double radian, Vector2 radiateCenter, float alphaThreshold, ColorGradient gradient, bool useOkLabInterpolation, BlendMode blendMode)
        {
            var gpuImage = image.ToGpu(device);

            var shadowImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device)
            {
                Origin = gpuImage.Origin
            };
            var (colorValues, colorPositions, opacityValues, opacityPositions) = GpuGradientColor.CopyToGPUGradientBuffers(device, gradient, useOkLabInterpolation);

            switch (type)
            {
                case LongShadowShapeType.Radiate:
                    device.For(
                        roi.Width,
                        roi.Height,
                        new LongShadowRadiateProcess(
                            gpuImage.Data,
                            shadowImage.Data,
                            gpuImage.Width,
                            gpuImage.Height,
                            length,
                            radiateCenter,
                            alphaThreshold,
                            colorValues,
                            colorPositions,
                            opacityValues,
                            opacityPositions,
                            useOkLabInterpolation,
                            roi.Left,
                            roi.Top
                        )
                    );
                    break;
                case LongShadowShapeType.InvertRadiate:
                    device.For(
                        roi.Width,
                        roi.Height,
                        new LongShadowInvertRadiateProcess(
                            gpuImage.Data,
                            shadowImage.Data,
                            gpuImage.Width,
                            gpuImage.Height,
                            length,
                            radiateCenter,
                            alphaThreshold,
                            colorValues,
                            colorPositions,
                            opacityValues,
                            opacityPositions,
                            useOkLabInterpolation,
                            roi.Left,
                            roi.Top
                        )
                    );
                    break;
                default:
                    {
                        var dx = (float)-Math.Cos(radian);
                        var dy = (float)-Math.Sin(radian);

                        device.For(
                            roi.Width,
                            roi.Height,
                            new LongShadowParallelProcess(
                                gpuImage.Data,
                                shadowImage.Data,
                                gpuImage.Width,
                                gpuImage.Height,
                                length,
                                dx,
                                dy,
                                alphaThreshold,
                                colorValues,
                                colorPositions,
                                opacityValues,
                                opacityPositions,
                                useOkLabInterpolation,
                                roi.Left,
                                roi.Top
                            )
                        );
                    }
                    break;
            }

            colorValues.Dispose();
            colorPositions.Dispose();
            opacityValues.Dispose();
            opacityPositions.Dispose();

            ImageBlendProcessor.SameSizeGpu(device, shadowImage, gpuImage, new ROI(Int32Point.Zero, new Int32Size(gpuImage.Width, gpuImage.Height), 0, 0, gpuImage.Width, gpuImage.Height), blendMode);

            if (gpuImage != image)
            {
                gpuImage.Dispose();
            }

            return shadowImage;
        }
    }

    enum LongShadowShapeType
    {
        Parallel,
        Radiate,
        InvertRadiate
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LongShadowParallelProcess(
        ReadWriteBuffer<Float4> image,
        ReadWriteBuffer<Float4> shadowImage,
        int width,
        int height,
        float length,
        float dx,
        float dy,
        float alphaThreshold,
        ReadOnlyBuffer<Float3> colorGradientValues,
        ReadOnlyBuffer<float> colorGradientPositions,
        ReadOnlyBuffer<float> opacityGradientValues,
        ReadOnlyBuffer<float> opacityGradientPositions,
        bool useOKLabInterpolation,
        int startX,
        int startY
    ) : IComputeShader
    {
        const float OutsideThreshold = LongShadow.OutsideThreshold;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var a = 0.0F;
            var hitLength = 0;
            var rendered = false;
            for (var i = 0; i < length; i++)
            {
                var color = Bilinear(x + dx * i, y + dy * i);
                var edge = Hlsl.Min(length - i, 1.0F);
                var currentA = color.W * edge;

                if (a <= currentA)
                {
                    hitLength = a < alphaThreshold ? i : hitLength;
                    a = currentA;
                    rendered = false;
                }
                else if (Hlsl.Abs(a - currentA) > OutsideThreshold)
                {
                    if (!rendered)
                    {
                        var colorPos = hitLength / length;
                        var shadowColor = new Float4(CalcColor(colorPos), CalcOpacity(colorPos)) * new Float4(1.0F, 1.0F, 1.0F, a);
                        shadowImage[pos] = BlendMethods.Process(0, shadowColor, shadowImage[pos]);
                        rendered = true;
                    }

                    a = currentA;
                    hitLength = i;
                }

                if (!rendered && a > 0.0F)
                {
                    var colorPos = hitLength / length;
                    var shadowColor = new Float4(CalcColor(colorPos), CalcOpacity(colorPos)) * new Float4(1.0F, 1.0F, 1.0F, a);
                    shadowImage[pos] = BlendMethods.Process(0, shadowColor, shadowImage[pos]);
                }
            }
        }

        Float4 Bilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return image[iy * width + ix];
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
                        c1 = image[pos];
                        c2 = image[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = image[pos];
                            c4 = image[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = image[pos];
                        c4 = image[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = image[pos];
                        if (iy < mh)
                        {
                            c3 = image[pos + width];
                        }
                    }
                    else
                    {
                        c3 = image[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = image[pos];
                    if (iy < mh)
                    {
                        c4 = image[pos + width];
                    }
                }
                else
                {
                    c4 = image[pos + width];
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

        Float3 CalcColor(float gradientPos)
        {
            if (colorGradientValues.Length < 2)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(colorGradientValues[0]);
                }
                else
                {
                    return colorGradientValues[0];
                }
            }

            var prevColor = colorGradientValues[0];
            var prevPosition = colorGradientPositions[0];
            for (var i = colorGradientPositions.Length - 1; i > -1; i--)
            {
                if (colorGradientPositions[i] <= gradientPos)
                {
                    prevColor = colorGradientValues[i];
                    prevPosition = colorGradientPositions[i];
                    break;
                }
            }

            var nextColor = colorGradientValues[colorGradientValues.Length - 1];
            var nextPosition = colorGradientPositions[colorGradientPositions.Length - 1];
            for (var i = 0; i < colorGradientPositions.Length; i++)
            {
                if (colorGradientPositions[i] > gradientPos)
                {
                    nextColor = colorGradientValues[i];
                    nextPosition = colorGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(prevColor);
                }
                else
                {
                    return prevColor;
                }
            }

            var result = Hlsl.Lerp(prevColor, nextColor, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            if (useOKLabInterpolation)
            {
                return ColorSpaceConversion.OkLabToRgb(result);
            }
            else
            {
                return result;
            }
        }

        float CalcOpacity(float gradientPos)
        {
            if (opacityGradientValues.Length < 2)
            {
                return opacityGradientValues[0];
            }

            var prevOpacity = opacityGradientValues[0];
            var prevPosition = opacityGradientPositions[0];
            for (var i = opacityGradientPositions.Length - 1; i > -1; i--)
            {
                if (opacityGradientPositions[i] <= gradientPos)
                {
                    prevOpacity = opacityGradientValues[i];
                    prevPosition = opacityGradientPositions[i];
                    break;
                }
            }

            var nextOpacity = opacityGradientValues[opacityGradientValues.Length - 1];
            var nextPosition = opacityGradientPositions[opacityGradientPositions.Length - 1];
            for (var i = 0; i < opacityGradientPositions.Length; i++)
            {
                if (opacityGradientPositions[i] > gradientPos)
                {
                    nextOpacity = opacityGradientValues[i];
                    nextPosition = opacityGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                return prevOpacity;
            }
            else
            {
                return Hlsl.Lerp(prevOpacity, nextOpacity, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LongShadowRadiateProcess(
        ReadWriteBuffer<Float4> image,
        ReadWriteBuffer<Float4> shadowImage,
        int width,
        int height,
        float length,
        Float2 radiateCenter,
        float alphaThreshold,
        ReadOnlyBuffer<Float3> colorGradientValues,
        ReadOnlyBuffer<float> colorGradientPositions,
        ReadOnlyBuffer<float> opacityGradientValues,
        ReadOnlyBuffer<float> opacityGradientPositions,
        bool useOKLabInterpolation,
        int startX,
        int startY
    ) : IComputeShader
    {
        const float OutsideThreshold = LongShadow.OutsideThreshold;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var lightPos = radiateCenter - new Float2(x, y);
            Hlsl.SinCos(Hlsl.Atan2(lightPos.Y, lightPos.X), out var sin, out var cos);
            var diff = new Float2(cos, sin);
            var a = 0.0F;
            var hitLength = 0;
            var rendered = false;
            for (var i = 0; i < length; i++)
            {
                var p = new Float2(x, y) + diff * i;
                if (Hlsl.Dot(radiateCenter - p, lightPos) < 0.0F)
                {
                    break;
                }
                var color = Bilinear(p.X, p.Y);
                var edge = Hlsl.Min(length - i, 1.0F);
                var currentA = color.W * edge;
                if (a <= currentA)
                {
                    hitLength = a < alphaThreshold ? i : hitLength;
                    a = currentA;
                    rendered = false;
                }
                else if (Hlsl.Abs(a - currentA) > OutsideThreshold)
                {
                    if (!rendered)
                    {
                        var colorPos = hitLength / length;
                        var shadowColor = new Float4(CalcColor(colorPos), CalcOpacity(colorPos)) * new Float4(1.0F, 1.0F, 1.0F, a);
                        shadowImage[pos] = BlendMethods.Process(0, shadowColor, shadowImage[pos]);
                        rendered = true;
                    }

                    a = currentA;
                    hitLength = i;
                }
            }

            if (!rendered && a > 0.0F)
            {
                var colorPos = hitLength / length;
                var shadowColor = new Float4(CalcColor(colorPos), CalcOpacity(colorPos)) * new Float4(1.0F, 1.0F, 1.0F, a);
                shadowImage[pos] = BlendMethods.Process(0, shadowColor, shadowImage[pos]);
            }
        }

        Float4 Bilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return image[iy * width + ix];
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
                        c1 = image[pos];
                        c2 = image[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = image[pos];
                            c4 = image[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = image[pos];
                        c4 = image[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = image[pos];
                        if (iy < mh)
                        {
                            c3 = image[pos + width];
                        }
                    }
                    else
                    {
                        c3 = image[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = image[pos];
                    if (iy < mh)
                    {
                        c4 = image[pos + width];
                    }
                }
                else
                {
                    c4 = image[pos + width];
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

        Float3 CalcColor(float gradientPos)
        {
            if (colorGradientValues.Length < 2)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(colorGradientValues[0]);
                }
                else
                {
                    return colorGradientValues[0];
                }
            }

            var prevColor = colorGradientValues[0];
            var prevPosition = colorGradientPositions[0];
            for (var i = colorGradientPositions.Length - 1; i > -1; i--)
            {
                if (colorGradientPositions[i] <= gradientPos)
                {
                    prevColor = colorGradientValues[i];
                    prevPosition = colorGradientPositions[i];
                    break;
                }
            }

            var nextColor = colorGradientValues[colorGradientValues.Length - 1];
            var nextPosition = colorGradientPositions[colorGradientPositions.Length - 1];
            for (var i = 0; i < colorGradientPositions.Length; i++)
            {
                if (colorGradientPositions[i] > gradientPos)
                {
                    nextColor = colorGradientValues[i];
                    nextPosition = colorGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(prevColor);
                }
                else
                {
                    return prevColor;
                }
            }

            var result = Hlsl.Lerp(prevColor, nextColor, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            if (useOKLabInterpolation)
            {
                return ColorSpaceConversion.OkLabToRgb(result);
            }
            else
            {
                return result;
            }
        }

        float CalcOpacity(float gradientPos)
        {
            if (opacityGradientValues.Length < 2)
            {
                return opacityGradientValues[0];
            }

            var prevOpacity = opacityGradientValues[0];
            var prevPosition = opacityGradientPositions[0];
            for (var i = opacityGradientPositions.Length - 1; i > -1; i--)
            {
                if (opacityGradientPositions[i] <= gradientPos)
                {
                    prevOpacity = opacityGradientValues[i];
                    prevPosition = opacityGradientPositions[i];
                    break;
                }
            }

            var nextOpacity = opacityGradientValues[opacityGradientValues.Length - 1];
            var nextPosition = opacityGradientPositions[opacityGradientPositions.Length - 1];
            for (var i = 0; i < opacityGradientPositions.Length; i++)
            {
                if (opacityGradientPositions[i] > gradientPos)
                {
                    nextOpacity = opacityGradientValues[i];
                    nextPosition = opacityGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                return prevOpacity;
            }
            else
            {
                return Hlsl.Lerp(prevOpacity, nextOpacity, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LongShadowInvertRadiateProcess(
        ReadWriteBuffer<Float4> image,
        ReadWriteBuffer<Float4> shadowImage,
        int width,
        int height,
        float length,
        Float2 radiateCenter,
        float alphaThreshold,
        ReadOnlyBuffer<Float3> colorGradientValues,
        ReadOnlyBuffer<float> colorGradientPositions,
        ReadOnlyBuffer<float> opacityGradientValues,
        ReadOnlyBuffer<float> opacityGradientPositions,
        bool useOKLabInterpolation,
        int startX,
        int startY
    ) : IComputeShader
    {
        const float OutsideThreshold = LongShadow.OutsideThreshold;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var lightPos = radiateCenter - new Float2(x, y);
            Hlsl.SinCos(Hlsl.Atan2(lightPos.Y, lightPos.X), out var sin, out var cos);
            var diff = -new Float2(cos, sin);
            var a = 0.0F;
            var hitLength = 0;
            var rendered = false;
            for (var i = 0; i < length; i++)
            {
                var p = new Float2(x, y) + diff * i;
                var color = Bilinear(p.X, p.Y);
                var edge = Hlsl.Min(length - i, 1.0F);
                var currentA = color.W * edge;
                if (a <= currentA)
                {
                    hitLength = a < alphaThreshold ? i : hitLength;
                    a = currentA;
                    rendered = false;
                }
                else if (Hlsl.Abs(a - currentA) > OutsideThreshold)
                {
                    if (!rendered)
                    {
                        var colorPos = hitLength / length;
                        var shadowColor = new Float4(CalcColor(colorPos), CalcOpacity(colorPos)) * new Float4(1.0F, 1.0F, 1.0F, a);
                        shadowImage[pos] = BlendMethods.Process(0, shadowColor, shadowImage[pos]);
                        rendered = true;
                    }

                    a = currentA;
                    hitLength = i;
                }
            }

            if (!rendered && a > 0.0F)
            {
                var colorPos = hitLength / length;
                var shadowColor = new Float4(CalcColor(colorPos), CalcOpacity(colorPos)) * new Float4(1.0F, 1.0F, 1.0F, a);
                shadowImage[pos] = BlendMethods.Process(0, shadowColor, shadowImage[pos]);
            }
        }

        Float4 Bilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return image[iy * width + ix];
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
                        c1 = image[pos];
                        c2 = image[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = image[pos];
                            c4 = image[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = image[pos];
                        c4 = image[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = image[pos];
                        if (iy < mh)
                        {
                            c3 = image[pos + width];
                        }
                    }
                    else
                    {
                        c3 = image[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = image[pos];
                    if (iy < mh)
                    {
                        c4 = image[pos + width];
                    }
                }
                else
                {
                    c4 = image[pos + width];
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

        Float3 CalcColor(float gradientPos)
        {
            if (colorGradientValues.Length < 2)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(colorGradientValues[0]);
                }
                else
                {
                    return colorGradientValues[0];
                }
            }

            var prevColor = colorGradientValues[0];
            var prevPosition = colorGradientPositions[0];
            for (var i = colorGradientPositions.Length - 1; i > -1; i--)
            {
                if (colorGradientPositions[i] <= gradientPos)
                {
                    prevColor = colorGradientValues[i];
                    prevPosition = colorGradientPositions[i];
                    break;
                }
            }

            var nextColor = colorGradientValues[colorGradientValues.Length - 1];
            var nextPosition = colorGradientPositions[colorGradientPositions.Length - 1];
            for (var i = 0; i < colorGradientPositions.Length; i++)
            {
                if (colorGradientPositions[i] > gradientPos)
                {
                    nextColor = colorGradientValues[i];
                    nextPosition = colorGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                if (useOKLabInterpolation)
                {
                    return ColorSpaceConversion.OkLabToRgb(prevColor);
                }
                else
                {
                    return prevColor;
                }
            }

            var result = Hlsl.Lerp(prevColor, nextColor, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            if (useOKLabInterpolation)
            {
                return ColorSpaceConversion.OkLabToRgb(result);
            }
            else
            {
                return result;
            }
        }

        float CalcOpacity(float gradientPos)
        {
            if (opacityGradientValues.Length < 2)
            {
                return opacityGradientValues[0];
            }

            var prevOpacity = opacityGradientValues[0];
            var prevPosition = opacityGradientPositions[0];
            for (var i = opacityGradientPositions.Length - 1; i > -1; i--)
            {
                if (opacityGradientPositions[i] <= gradientPos)
                {
                    prevOpacity = opacityGradientValues[i];
                    prevPosition = opacityGradientPositions[i];
                    break;
                }
            }

            var nextOpacity = opacityGradientValues[opacityGradientValues.Length - 1];
            var nextPosition = opacityGradientPositions[opacityGradientPositions.Length - 1];
            for (var i = 0; i < opacityGradientPositions.Length; i++)
            {
                if (opacityGradientPositions[i] > gradientPos)
                {
                    nextOpacity = opacityGradientValues[i];
                    nextPosition = opacityGradientPositions[i];
                    break;
                }
            }

            if (prevPosition >= nextPosition)
            {
                return prevOpacity;
            }
            else
            {
                return Hlsl.Lerp(prevOpacity, nextOpacity, (gradientPos - prevPosition) / (nextPosition - prevPosition));
            }
        }
    }
}
