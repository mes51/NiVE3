using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
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
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Property;
using NiVE3.PresetPlugin.Property.Properties;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_PathBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_PathBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class PathBlur : IEffect
    {
        const string ID = "520E08C2-95D2-42FC-AB49-E789F693E597";

        const string PropertyMaskId = nameof(PropertyMaskId);

        const string PropertyBeginOffsetId = nameof(PropertyBeginOffsetId);

        const string PropertyEndOffsetId = nameof(PropertyEndOffsetId);

        const string PropertyBlurCenterId = nameof(PropertyBlurCenterId);

        const string PropertyStrengthMapId = nameof(PropertyStrengthMapId);

        const string PropertySampleCountId = nameof(PropertySampleCountId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new UseMaskPathProperty(PropertyMaskId, LanguageResourceDictionary.ResourceKeys.Blur_PathBlur_Mask, 90.0),
                new DoubleProperty(PropertyBeginOffsetId, LanguageResourceDictionary.ResourceKeys.Blur_PathBlur_BeginOffset, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new DoubleProperty(PropertyEndOffsetId, LanguageResourceDictionary.ResourceKeys.Blur_PathBlur_EndOffset, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new DoubleProperty(PropertyBlurCenterId, LanguageResourceDictionary.ResourceKeys.Blur_PathBlur_BlurCenter, 50.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new GraphValueProperty(PropertyStrengthMapId, LanguageResourceDictionary.ResourceKeys.Blur_PathBlur_StrengthMap, true),
                new DoubleProperty(PropertySampleCountId, LanguageResourceDictionary.ResourceKeys.Blur_PathBlur_SampleCount, 16.0, 0.0, int.MaxValue, digit: 0),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_PathBlur_EdgeRepeatMode, typeof(BilinearEdgeMode), typeof(LanguageResourceDictionary), BilinearEdgeMode.None, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var beginOffset = (float)(properties.GetValue(PropertyBeginOffsetId, layerTime, 0.0) * 0.01);
            var endOffset = (float)(properties.GetValue(PropertyEndOffsetId, layerTime, 0.0) * 0.01);
            var strengthMap = properties.GetValue(PropertyStrengthMapId, layerTime, GraphValueParameter.Identity);
            var sampleCount = (int)properties.GetValue(PropertySampleCountId, layerTime, 0.0);
            if (beginOffset >= (1.0F - endOffset) || sampleCount < 1 || strengthMap.Values.All(v => v <= 0.0F))
            {
                return image;
            }

            var maskId = properties.GetValue(PropertyMaskId, layerTime, UseMaskPathTarget.Empty);
            var path = maskId.GetMask(layer, layerTime, downSamplingRateX)?.BuildPath()?.Transform(Matrix3x2.CreateRotation(MathF.PI))?.Flatten()?.FirstOrDefault();
            if (path == null)
            {
                return image;
            }

            var layoutPath = new LayoutPath(path, false, true, 0.0);
            var layoutPathOffset = beginOffset * layoutPath.TotalLength;
            var usePathLength = layoutPath.TotalLength - layoutPathOffset - endOffset * layoutPath.TotalLength;
            var blurCenter = (float)(properties.GetValue(PropertyBlurCenterId, layerTime, 0.0) * 0.01);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, BilinearEdgeMode.None);
            var blurCenterPoint = layoutPath.AlignToPath(blurCenter * usePathLength + layoutPathOffset, Vector2.Zero);

            var samplePoints = new BlurSamplePoint[sampleCount + 1];
            if (layoutPath.IsClosed && beginOffset <= 0.0F && endOffset <= 0.0F)
            {
                var diff = 1.0F / sampleCount;
                for (var i = 0; i < sampleCount; i++)
                {
                    var strength = strengthMap.Interpolation(0.0F, 1.0F, diff * i);
                    var point = layoutPath.AlignToPath(i * diff * layoutPath.TotalLength, Vector2.Zero) - blurCenterPoint;
                    samplePoints[i] = new BlurSamplePoint(point, strength);
                }
            }
            else
            {
                var diff = 1.0F / Math.Max(sampleCount - 1, 1);
                for (var i = 0; i < sampleCount; i++)
                {
                    var strength = strengthMap.Interpolation(0.0F, 1.0F, diff * i);
                    var point = layoutPath.AlignToPath(i * diff * usePathLength + layoutPathOffset, Vector2.Zero) - blurCenterPoint;
                    samplePoints[i] = new BlurSamplePoint(point, strength);
                }
            }
            var blurCenterStrength = strengthMap.Interpolation(0.0F, 1.0F, blurCenter);
            samplePoints[^1] = new BlurSamplePoint(Vector2.Zero, blurCenterStrength);
            var totalCount = samplePoints.Aggregate(0.0F, (r, t) => r + t.Strength);

            if (AcceleratorObject != null && useGpu)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, samplePoints, totalCount, edgeRepeatMode);
            }
            else
            {
                return ProcessCpu(image, roi, samplePoints, totalCount, edgeRepeatMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, BlurSamplePoint[] samplePoints, float totalCount, BilinearEdgeMode edgeRepeatMode)
        {
            var managedImage = image.ToManaged();
            using var sourceImage = (NManagedImage)managedImage.Copy();

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var a = 0.0F;
                    var rgb = Vector4.Zero;
                    for (var i = 0; i < samplePoints.Length; i++)
                    {
                        var (point, strength) = samplePoints[i];
                        var p = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, x + point.X, y + point.Y, Const.EmptyPixel, edgeRepeatMode);
                        var ta = p.W * strength;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / totalCount;
                        imageDataSpan[x] = result;
                    }
                    else
                    {
                        imageDataSpan[x] = Const.EmptyPixel;
                    }
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, BlurSamplePoint[] samplePoints, float totalCount, BilinearEdgeMode edgeRepeatMode)
        {
            var gpuImage = image.ToGpu(device);
            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            var temp = ArrayPool<GpuBlurSamplePoint>.Shared.Rent(samplePoints.Length);
            for (var i = 0; i < samplePoints.Length; i++)
            {
                var sp = samplePoints[i];
                temp[i] = new GpuBlurSamplePoint(sp.Point, sp.Strength);
            }
            using var samplePointBuffer = device.AllocateReadOnlyBuffer<GpuBlurSamplePoint>(samplePoints.Length);
            samplePointBuffer.CopyFrom(temp.AsSpan(0, samplePoints.Length));
            ArrayPool<GpuBlurSamplePoint>.Shared.Return(temp);

            device.For(roi.Width, roi.Height, new PathBlurProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, sourceImage.Data, samplePointBuffer, totalCount, (int)edgeRepeatMode, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    record BlurSamplePoint(Vector2 Point, float Strength);

    readonly record struct GpuBlurSamplePoint(Float2 Point, float Strength)
    {
        public readonly Float2 Point = Point;

        public readonly float Strength = Strength;
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PathBlurProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> sourceImage, ReadOnlyBuffer<GpuBlurSamplePoint> samplePoint, float totalCount, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var a = 0.0F;
            var rgb = Float3.Zero;
            for (var i = 0; i < samplePoint.Length; i++)
            {
                var sp = samplePoint[i];
                var p = SourceImageBilinear(x + sp.Point.X, y + sp.Point.Y);
                var ta = p.W * sp.Strength;
                rgb += p.XYZ * ta;
                a += ta;
            }

            var pos = y * width + x;
            if (a > 0.0F)
            {
                image[pos] = new Float4(rgb / a, a / totalCount);
            }
            else
            {
                image[pos] = Const.EmptyPixelFloat4;
            }
        }

        Float4 SourceImageBilinear(float x, float y)
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
                    return sourceImage[CoordWrapGpu.Wrap(y, height) * width + CoordWrapGpu.Wrap(x, width)];
                case 2:
                    return sourceImage[CoordWrapGpu.Repeat(y, height) * width + CoordWrapGpu.Repeat(x, width)];
                case 3:
                    return sourceImage[CoordWrapGpu.Mirror(y, height) * width + CoordWrapGpu.Mirror(x, width)];
                default:
                    if (x > -1 && x < width && y > -1 && y < height)
                    {
                        return sourceImage[y * width + x];
                    }
                    else
                    {
                        return Const.EmptyPixelFloat4;
                    }
            }
        }
    }
}
