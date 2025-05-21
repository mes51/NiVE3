using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Color;
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
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shape;
using NiVE3.Shared.Extension;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using Polygon = NiVE3.Shape.Polygon;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_LensBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_LensBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class LensBlur : IEffect
    {
        internal const float GainMultiplyer = 3.0F;

        internal const float MaxSaturationSubtract = 0.9F;

        internal const float PowerBase = 1.2F;

        internal const float LogPowerBase = 0.1823215567939546F; // Math.Log(PowerBase)

        const string ID = "5F36FE53-8F98-48F5-A3AC-95C6333D3A5F";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyIrisGroupId = nameof(PropertyIrisGroupId);

        const string PropertyIrisTypeId = nameof(PropertyIrisTypeId);

        const string PropertyIrisCornerRoundId = nameof(PropertyIrisCornerRoundId);

        const string PropertyIrisUseLayerMaskId = nameof(PropertyIrisUseLayerMaskId);

        const string PropertyIrisTargetLayerMaskId = nameof(PropertyIrisTargetLayerMaskId);

        const string PropertyIrisAngleId = nameof(PropertyIrisAngleId);

        const string PropertyHighlightGroupId = nameof(PropertyHighlightGroupId);

        const string PropertyHighlightGainId = nameof(PropertyHighlightGainId);

        const string PropertyHighlightThresholdId = nameof(PropertyHighlightThresholdId);

        const string PropertyHighlightSaturationId = nameof(PropertyHighlightSaturationId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);

                var expandX = (int)Math.Ceiling(amount / downSamplingRateX);
                var expandY = (int)Math.Ceiling(amount / downSamplingRateY);
                return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
            }
            else
            {
                return baseRoi;
            }
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Amount, 5.0, 0.0, 200.0, slideChangeValue: 0.1, digit: 2),
                new PropertyGroup(PropertyIrisGroupId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris,
                [
                    new EnumProperty(PropertyIrisTypeId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_Type, typeof(LensBlurIrisType), typeof(LanguageResourceDictionary), LensBlurIrisType.Hexagon, selectBoxWidth: 90.0),
                    new DoubleProperty(PropertyIrisCornerRoundId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_CornerRound, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new AngleProperty(PropertyIrisAngleId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_Angle, 15.0, digit: 2),
                    new CheckBoxProperty(PropertyIrisUseLayerMaskId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_UseLayerMask, false),
                    new UseMaskPathProperty(PropertyIrisTargetLayerMaskId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_TargetLayerMask, selectBoxWidth: 90.0)
                ]),
                new PropertyGroup(PropertyHighlightGroupId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight,
                [
                    new DoubleProperty(PropertyHighlightGainId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight_Gain, 0.0, 0.0, 100.0, digit: 2),
                    new DoubleProperty(PropertyHighlightThresholdId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight_Threshold, 0.7, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                    new DoubleProperty(PropertyHighlightSaturationId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight_Saturation, 100.0, 0.0, 100.0, digit: 2)
                ]),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var amount = (float)(properties.GetValue(PropertyAmountId, layerTime, 0.0) / downSamplingRateX);
            var irisGroup = properties.First(p => p.Id == PropertyIrisGroupId).GetChildren() ?? [];
            var irisType = irisGroup.GetValue(PropertyIrisTypeId, layerTime, LensBlurIrisType.Hexagon);
            var irisCornerRound = (float)(irisGroup.GetValue(PropertyIrisCornerRoundId, layerTime, 0.0) * 0.01);
            var irisUseLayerMask = irisGroup.GetValue(PropertyIrisUseLayerMaskId, layerTime, false);
            var irisTargetLayerMask = irisGroup.GetValue(PropertyIrisTargetLayerMaskId, layerTime, UseMaskPathTarget.Empty);
            var irisAngle = (float)irisGroup.GetValue(PropertyIrisAngleId, layerTime, 0.0);
            var highlightGroup = properties.First(p => p.Id == PropertyHighlightGroupId).GetChildren() ?? [];
            var highlightGain = (float)(highlightGroup.GetValue(PropertyHighlightGainId, layerTime, 0.0) * 0.01);
            var highlightThreshold = (float)highlightGroup.GetValue(PropertyHighlightThresholdId, layerTime, 0.0);
            var highlightSaturationSubtract = 1.0F - (float)(highlightGroup.GetValue(PropertyHighlightSaturationId, layerTime, 0.0) * 0.01);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (amount <= 0.0F && highlightGain <= 0.0F)
            {
                return image;
            }

            var layerMaskPath = irisUseLayerMask ? layer.GetMask(irisTargetLayerMask.MaskId)?.GetPath(layerTime + layer.SourceStartPoint, downSamplingRateX) : null;
            using var irisMask = (layerMaskPath != null && layerMaskPath.IsClosed && !layerMaskPath.IsEmpty()) ?
                GenerateMaskByLayerMask(layerMaskPath, amount) :
                GenerateIrisMask(irisType, amount, irisCornerRound, irisAngle);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, amount, irisMask, highlightGain, highlightThreshold, highlightSaturationSubtract, edgeRepeatMode);
            }
            else
            {
                return ProcessCpu(image, roi, amount, irisMask, highlightGain, highlightThreshold, highlightSaturationSubtract, edgeRepeatMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float amount, IrisMask irisMask, float highlightGain, float highlightThreshold, float highlightSaturationSubtract, EdgeRepeatMode edgeRepeatMode)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            if (amount <= 0.0F)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var color = imageDataSpan[x];
                        if (Vector4.Dot(color, Const.ConvertToGrayScale) >= highlightThreshold)
                        {
                            var hsl = Hsl.FromRgb(color);
                            hsl.Saturation *= 1.0F - highlightSaturationSubtract * MaxSaturationSubtract;

                            var gainColor = hsl.ToRgb();
                            gainColor *= GainMultiplyer;
                            gainColor.W = color.W;
                            imageDataSpan[x] = Vector4.Lerp(color, gainColor, highlightGain);
                        }
                    }
                });
                return managedImage;
            }

            using var sourceImage = (NManagedImage)managedImage.Copy();
            var gainROI = roi.Expand((int)Math.Ceiling(amount)).Intersect(0, 0, managedImage.Width, managedImage.Height);
            var sourceImageData = sourceImage.Data;
            if (highlightGain > 0.0F)
            {
                Parallel.For(gainROI.Top, gainROI.Bottom, y =>
                {
                    const byte MmInsert0To3 = 3 << 4;
                    var sourceImageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(sourceImageData.AsSpan(y * imageWidth, imageWidth));

                    for (var x = gainROI.Left; x < gainROI.Right; x++)
                    {
                        var color = sourceImageDataSpan[x];
                        if (Vector128.Dot(color, Const.ConvertToGrayScale128) >= highlightThreshold)
                        {
                            var a = color.GetElement(3);
                            var hsl = Hsl.FromRgb(color);
                            hsl.Saturation *= 1.0F - highlightSaturationSubtract * MaxSaturationSubtract;
                            var gainColor = Sse41.Insert(hsl.ToRgbVector128() * GainMultiplyer, Vector128.Create(a), MmInsert0To3);
                            color = (gainColor - color) * highlightGain + color;
                        }

                        color = Vector128.Create(PowerBase).Pow(color);
                        var pa = color.GetElement(3);
                        color *= pa;
                        sourceImageDataSpan[x] = Sse41.Insert(color, Vector128.Create(pa), MmInsert0To3);
                    }
                });
            }
            else
            {
                Parallel.For(gainROI.Top, gainROI.Bottom, y =>
                {
                    const byte MmInsert0To3 = 3 << 4;
                    var sourceImageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(sourceImageData.AsSpan(y * imageWidth, imageWidth));

                    for (var x = gainROI.Left; x < gainROI.Right; x++)
                    {
                        var color = Vector128.Create(PowerBase).Pow(sourceImageDataSpan[x]);
                        var pa = color.GetElement(3);
                        color *= pa;
                        sourceImageDataSpan[x] = Sse41.Insert(color, Vector128.Create(pa), MmInsert0To3);
                    }
                });
            }

            int imageHeight = managedImage.Height;
            var count = irisMask.Mask.Data.Sum();
            var maskRadius = irisMask.MaskRadius;
            var maskSize = irisMask.MaskSize;
            var mask = irisMask.Mask.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = Vector4.Zero;
                    for (int t = y - maskRadius, my = 0, mpos = 0; my < maskSize; t++, my++)
                    {
                        for (int l = x - maskRadius, mx = 0; mx < maskSize; l++, mx++, mpos++)
                        {
                            color += BlurUtil.GetPixel(sourceImageData, imageWidth, imageHeight, l, t, edgeRepeatMode) * mask[mpos];
                        }
                    }

                    if (color.W > 0.0F)
                    {
                        var ta = color.W;
                        color /= ta;
                        color.W = ta / count;
                        imageDataSpan[x] = (color.AsVector128().Log() / LogPowerBase).AsVector4();
                    }
                    else
                    {
                        imageDataSpan[x] = Const.EmptyPixel;
                    }
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float amount, IrisMask irisMask, float highlightGain, float highlightThreshold, float highlightSaturationSubtract, EdgeRepeatMode edgeRepeatMode)
        {
            var gpuImage = image.ToGpu(device);

            if (amount <= 0.0F)
            {
                device.For(roi.Width, roi.Height, new LensBlurGainOnlyProcess(gpuImage.Data, gpuImage.Width, highlightGain, highlightThreshold, highlightSaturationSubtract, roi.Left, roi.Top));

                return gpuImage;
            }

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            var count = irisMask.Mask.Data.Sum();
            using var maskBuffer = device.AllocateReadOnlyBuffer<float>(irisMask.Mask.GetDataSpan());

            using var context = device.CreateComputeContext();

            var gainROI = roi.Expand((int)Math.Ceiling(amount)).Intersect(0, 0, gpuImage.Width, gpuImage.Height);
            context.For(gainROI.Width, gainROI.Height, new LensBlurGainAndPowerProcess(sourceImage.Data, gpuImage.Width, highlightGain, highlightThreshold, highlightSaturationSubtract, roi.Left, roi.Top));
            context.Barrier(sourceImage.Data);

            context.For(roi.Width, roi.Height, new LensBlurBlurProcess(sourceImage.Data, gpuImage.Data, gpuImage.Width, gpuImage.Height, (int)edgeRepeatMode, maskBuffer, irisMask.MaskRadius, irisMask.MaskSize, count, roi.Left, roi.Top));

            return gpuImage;
        }

        static IrisMask GenerateMaskByLayerMask(BezierPath layerMaskPath, float size)
        {
            size += 0.5F;
            var path = BuildNonEmptyClosedPath(layerMaskPath);

            var originalBounds = path.Bounds;
            var resizeRate = size / Math.Max(originalBounds.Width, originalBounds.Height);
            var transform = Matrix3x2.CreateTranslation(-originalBounds.X - originalBounds.Width * 0.5F, -originalBounds.Y - originalBounds.Height * 0.5F)
                * Matrix3x2.CreateScale(resizeRate, resizeRate)
                * Matrix3x2.CreateRotation(MathF.PI);
            path = path.Transform(transform);

            var maskRadius = (int)MathF.Ceiling(Math.Max(path.Bounds.Width, path.Bounds.Height) * 0.5F);
            var maskSize = maskRadius * 2 + 1;
            var maskImage = new ManagedRasterizedMaskImage(maskSize, maskSize);
            path = path.Transform(Matrix3x2.CreateTranslation(-path.Bounds.X + (maskSize - path.Bounds.Width) * 0.5F, -path.Bounds.Y + (maskSize - path.Bounds.Height) * 0.5F));
            var polygons = path.Flatten().Select(p => new Polygon(p.Points.Span)).ToArray();
            ShapeMaskRendererCPU.Fill(polygons, maskImage, 1.0F);

            return new IrisMask(maskImage, maskRadius, maskSize);
        }

        static IrisMask GenerateIrisMask(LensBlurIrisType irisType, float size, float cornerRound, float angle)
        {
            var maskRadius = (int)MathF.Ceiling(size - 0.5F);
            var maskSize = maskRadius * 2 + 1;
            var maskImageCenter = maskSize * 0.5F;

            size += 0.5F;
            var path = (IPath)EmptyPath.ClosedPath;
            if (irisType == LensBlurIrisType.Circle)
            {
                path = new EllipsePolygon(new PointF(maskImageCenter, maskImageCenter), size);
            }
            else
            {
                var count = (int)irisType;
                var pointRad = Math.PI * 2.0 / count;
                var pathBuilder = new PathBuilder();

                pathBuilder.StartFigure();
                pathBuilder.MoveTo(new PointF(0.0F, -size));
                if (cornerRound == 0.0F)
                {
                    for (var i = 1; i < count; i++)
                    {
                        var rad = pointRad * i;
                        pathBuilder.LineTo((float)Math.Sin(rad) * size, (float)-Math.Cos(rad) * size);
                    }
                }
                else
                {
                    var k = (4.0 / 3.0) * Math.Tan(Math.PI / (count * 2)) * cornerRound * size;
                    for (var i = 1; i <= count; i++)
                    {
                        var prevRad = pointRad * (i - 1);
                        var rad = pointRad * i;
                        var sp = new PointF((float)Math.Sin(prevRad) * size, (float)-Math.Cos(prevRad) * size);
                        var ep = new PointF((float)Math.Sin(rad) * size, (float)-Math.Cos(rad) * size);
                        var c1 = sp + new PointF((float)(Math.Sin(prevRad + Math.PI * 0.5) * k), (float)(-Math.Cos(prevRad + Math.PI * 0.5) * k));
                        var c2 = ep + new PointF((float)(Math.Sin(rad - Math.PI * 0.5) * k), (float)(-Math.Cos(rad - Math.PI * 0.5) * k));
                        pathBuilder.AddCubicBezier(sp, c1, c2, ep);
                    }
                }
                pathBuilder.CloseFigure();

                path = pathBuilder.Build().Transform(Matrix3x2.CreateRotation((angle / 180.0F + 1.0F) * MathF.PI) * Matrix3x2.CreateTranslation(maskImageCenter, maskImageCenter));
            }
            var maskImage = new ManagedRasterizedMaskImage(maskSize, maskSize);
            var polygons = path.Flatten().Select(p => new Polygon(p.Points.Span)).ToArray();
            ShapeMaskRendererCPU.Fill(polygons, maskImage, 1.0F);

            return new IrisMask(maskImage, maskRadius, maskSize);
        }

        static IPath BuildNonEmptyClosedPath(BezierPath path)
        {
            var pathBuilder = new PathBuilder();
            pathBuilder.StartFigure();
            pathBuilder.MoveTo((Vector2)path.BeginPoint);
            foreach (var p in path.Points)
            {
                if (p.IsLinear)
                {
                    pathBuilder.LineTo((Vector2)p.EndPoint);
                }
                else
                {
                    pathBuilder.CubicBezierTo((Vector2)p.ControlPoint1, (Vector2)p.ControlPoint2, (Vector2)p.EndPoint);
                }
            }

            if (path.IsClosed)
            {
                pathBuilder.CloseFigure();
            }

            return pathBuilder.Build();
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LensBlurGainOnlyProcess(ReadWriteBuffer<Float4> image, int width, float gain, float threshold, float saturationSubtract, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            if (Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) >= threshold)
            {
                var hsl = ColorSpaceConversion.RgbToHsl(color);
                hsl.Y *= 1.0F - saturationSubtract * LensBlur.MaxSaturationSubtract;

                var gainColor = ColorSpaceConversion.HslToRgb(hsl);
                gainColor *= LensBlur.GainMultiplyer;
                gainColor.W = color.W;
                image[pos] = Hlsl.Lerp(color, gainColor, gain);
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LensBlurGainAndPowerProcess(ReadWriteBuffer<Float4> image, int width, float gain, float threshold, float saturationSubtract, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            if (Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) >= threshold)
            {
                var hsl = ColorSpaceConversion.RgbToHsl(color);
                hsl.Y *= 1.0F - saturationSubtract * LensBlur.MaxSaturationSubtract;

                var gainColor = ColorSpaceConversion.HslToRgb(hsl);
                gainColor *= LensBlur.GainMultiplyer;
                gainColor.W = color.W;
                color = Hlsl.Lerp(color, gainColor, gain);
            }

            color = Hlsl.Pow(LensBlur.PowerBase, color);
            image[pos] = new Float4(color.XYZ * color.W, color.W);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LensBlurBlurProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> result, int width, int height, int edgeRepeatMode, ReadOnlyBuffer<float> mask, int maskRadius, int maskSize, float sumCount, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var color = Float4.Zero;
            for (int t = y - maskRadius, my = 0, mpos = 0; my < maskSize; t++, my++)
            {
                for (int l = x - maskRadius, mx = 0; mx < maskSize; l++, mx++, mpos++)
                {
                    color += GetPixel(l, t) * mask[mpos];
                }
            }

            if (color.W > 0.0F)
            {
                result[pos] = Hlsl.Log(new Float4(color.XYZ / color.W, color.W / sumCount)) / LensBlur.LogPowerBase;
            }
            else
            {
                image[pos] = Const.EmptyPixelFloat4;
            }
        }

        Float4 GetPixel(int l, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[CoordWrapGpu.Wrap(t, height) * width + CoordWrapGpu.Wrap(l, width)];
                case 2:
                    return image[CoordWrapGpu.Repeat(t, height) * width + CoordWrapGpu.Repeat(l, width)];
                case 3:
                    return image[CoordWrapGpu.Mirror(t, height) * width + CoordWrapGpu.Mirror(l, width)];
                default:
                    if (l > -1 && l < width && t > -1 && t < height)
                    {
                        return image[t * width + l];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }
    }

    enum LensBlurIrisType : int
    {
        Circle = 0,
        Triangle = 3,
        Rectangle,
        Pentagon,
        Hexagon,
        Heptagon,
        Octagon,
        Nonagon,
        Decagon
    }

    record IrisMask(ManagedRasterizedMaskImage Mask, int MaskRadius, int MaskSize) : IDisposable
    {
        public void Dispose()
        {
            Mask.Dispose();
        }
    }
}
