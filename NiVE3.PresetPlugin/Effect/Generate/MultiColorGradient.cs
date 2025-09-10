using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
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
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Generate
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Generate_MultiColorGradient_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Generate, LanguageResourceDictionary.Generate_MultiColorGradient_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class MultiColorGradient : IEffect
    {
        const string ID = "85650661-C66F-42EC-886B-0C7AF2EDF0B8";

        const string PropertyPoint1ColorId = nameof(PropertyPoint1ColorId);

        const string PropertyPoint1PointId = nameof(PropertyPoint1PointId);

        const string PropertyPoint2ColorId = nameof(PropertyPoint2ColorId);

        const string PropertyPoint2PointId = nameof(PropertyPoint2PointId);

        const string PropertyColorPointsId = nameof(PropertyColorPointsId);

        const string PropertyColorPointsColorPointGroupId = nameof(PropertyColorPointsColorPointGroupId);

        const string PropertyColorPointsColorId = nameof(PropertyColorPointsColorId);

        const string PropertyColorPointsPointId = nameof(PropertyColorPointsPointId);

        const string PropertyUseOkLabInterpolationId = nameof(PropertyUseOkLabInterpolationId);

        const string PropertyBlendId = nameof(PropertyBlendId);

        const string PropertyOpacityId = nameof(PropertyOpacityId);

        const string PropertyBlendModeId = nameof(PropertyBlendModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var title = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            var dialogOk = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var dialogCancel = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            return
            [
                new Vector3dProperty(PropertyPoint1PointId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_Point1_Point, new Vector3d(sourceSize.Width * 0.25, sourceSize.Height * 0.5, 0.0), digit: 2, useInteraction: true),
                new ColorProperty(PropertyPoint1ColorId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_Point1_Color, title, dialogOk, dialogCancel, new Vector4(0.0F, 0.0F, 1.0F, 1.0F)),
                new Vector3dProperty(PropertyPoint2PointId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_Point2_Point, new Vector3d(sourceSize.Width * 0.75, sourceSize.Height * 0.5, 0.0), digit: 2, useInteraction: true),
                new ColorProperty(PropertyPoint2ColorId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_Point2_Color, title, dialogOk, dialogCancel, new Vector4(1.0F, 0.0F, 0.0F, 1.0F)),
                new AppendableProperty(PropertyColorPointsId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_ColorPoints,
                [
                    new AppendablePropertyItem(PropertyColorPointsColorPointGroupId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_ColorPoints_ColorPoint, () =>
                        new PropertyGroup(PropertyColorPointsColorPointGroupId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_ColorPoints_ColorPoint,
                        [
                            new Vector3dProperty(PropertyColorPointsPointId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_ColorPoints_ColorPoint_Point, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, useInteraction: true),
                            new ColorProperty(PropertyColorPointsColorId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_ColorPoints_ColorPoint_Color, title, dialogOk, dialogCancel, new Vector4(0.0F, 1.0F, 0.0F, 1.0F))
                        ]))
                ], 0, true),
                new CheckBoxProperty(PropertyUseOkLabInterpolationId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_UseOkLabInterpolation, false),
                new DoubleProperty(PropertyBlendId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_Blend, 100.0, 5.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Generate_MultiColorGradient_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var opacity = (float)(properties.GetValue(PropertyOpacityId, layerTime, 0.0) * 0.01);
            if (opacity <= 0.0F)
            {
                return image;
            }

            var useOkLabInterpolation = properties.GetValue(PropertyUseOkLabInterpolationId, layerTime, false);

            var downSample = new Vector3d(downSamplingRateX, downSamplingRateY, 0.0);
            var offset = new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0);

            var colorPoints = new List<ColorPoint>
            {
                new ColorPoint(
                    (Vector2)(properties.GetValue(PropertyPoint1PointId, layerTime, Vector3d.Zero) / downSample + offset),
                    properties.GetValue(PropertyPoint1ColorId, layerTime, Vector4.Zero)
                ),
                new ColorPoint(
                    (Vector2)(properties.GetValue(PropertyPoint2PointId, layerTime, Vector3d.Zero) / downSample + offset),
                    properties.GetValue(PropertyPoint2ColorId, layerTime, Vector4.Zero)
                )
            };
            foreach (var cp in (properties.First(p => p.Id == PropertyColorPointsId).GetChildren() ?? []).Where(p => p.IsEnable))
            {
                var colorPointProperties = cp.GetChildren() ?? [];
                colorPoints.Add(new ColorPoint(
                    (Vector2)(colorPointProperties.GetValue(PropertyColorPointsPointId, layerTime, Vector3d.Zero) / downSample + offset),
                    colorPointProperties.GetValue(PropertyColorPointsColorId, layerTime, Vector4.Zero)
                ));
            }

            if (useOkLabInterpolation)
            {
                for (var i = 0; i < colorPoints.Count; i++)
                {
                    colorPoints[i] = new ColorPoint(colorPoints[i].Point, OkLab.FromRgb(colorPoints[i].Color).AsVector4());
                }
            }

            var blend = (float)properties.GetValue(PropertyBlendId, layerTime, 0.0);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, [..colorPoints], useOkLabInterpolation, blend, opacity, blendMode);
            }
            else
            {
                return ProcessCpu(image, roi, [..colorPoints], useOkLabInterpolation, blend, opacity, blendMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, ColorPoint[] colorPoints, bool useOkLabInterpolation, float blend, float opacity, BlendMode blendMode)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var blendRate = 1.0F / (MathF.Sqrt(blend) * 0.2F);
            var max = float.MaxValue / (blend * 1E35F);
            if (useOkLabInterpolation)
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var lh = ArrayPool<float>.Shared.Rent(colorPoints.Length);
                    for (var i = 0; i < colorPoints.Length; i++)
                    {
                        var p = colorPoints[i].Point;
                        lh[i] = (y - p.Y) * (y - p.Y);
                    }

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var total = 0.0F;
                        var okLabColor = Vector4.Zero;
                        for (var i = 0; i < colorPoints.Length; i++)
                        {
                            var (p, c) = colorPoints[i];
                            var l = Math.Min(max, MathF.Pow(1.0F / ((x - p.X) * (x - p.X) + lh[i]), blendRate));
                            total += l;
                            okLabColor += c * l;
                        }

                        okLabColor /= Math.Max(total, float.Epsilon);
                        var color = okLabColor.AsOkLab().ToRgb();
                        color.W = opacity;
                        imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], color);
                    }

                    ArrayPool<float>.Shared.Return(lh);
                });
            }
            else
            {
                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                    var lh = ArrayPool<float>.Shared.Rent(colorPoints.Length);
                    for (var i = 0; i < colorPoints.Length; i++)
                    {
                        var p = colorPoints[i].Point;
                        lh[i] = (y - p.Y) * (y - p.Y);
                    }

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var total = 0.0F;
                        var color = Vector4.Zero;
                        for (var i = 0; i < colorPoints.Length; i++)
                        {
                            var (p, c) = colorPoints[i];
                            var l = Math.Min(max, MathF.Pow(1.0F / ((x - p.X) * (x - p.X) + lh[i]), blendRate));
                            total += l;
                            color += c * l;
                        }

                        color /= Math.Max(total, float.Epsilon);
                        color.W = opacity;
                        imageDataSpan[x] = Blend.Process(blendMode, imageDataSpan[x], color);
                    }

                    ArrayPool<float>.Shared.Return(lh);
                });
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, ColorPoint[] colorPoints, bool useOkLabInterpolation, float blend, float opacity, BlendMode blendMode)
        {
            var gpuImage = image.ToGpu(device);

            var gpuColorPoints = new GpuColorPoint[colorPoints.Length];
            for (var i = 0; i < colorPoints.Length; i++)
            {
                gpuColorPoints[i] = new GpuColorPoint(colorPoints[i].Point, colorPoints[i].Color);
            }

            using var gpuColorPointBuffer = device.AllocateReadOnlyBuffer(gpuColorPoints);

            var blendRate = 1.0F / (MathF.Sqrt(blend) * 0.2F);
            var max = float.MaxValue / (blend * 1E35F);
            device.For(roi.Width, roi.Height, new MultiColorGradientProcess(gpuImage.Data, gpuImage.Width, gpuColorPointBuffer, useOkLabInterpolation, blendRate, max, opacity, (int)blendMode, roi.Left, roi.Top));

            return gpuImage;
        }
    }

    record class ColorPoint(Vector2 Point, Vector4 Color);

    readonly struct GpuColorPoint
    {
        public readonly Float2 Point;

        public readonly Float4 Color;

        public GpuColorPoint(Vector2 point, Vector4 color)
        {
            Point = point;
            Color = color;
        }
    }

    file static class OkLabExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 AsVector4(this in OkLab color)
        {
            return Unsafe.BitCast<OkLab, Vector4>(color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OkLab AsOkLab(this in Vector4 color)
        {
            return Unsafe.BitCast<Vector4, OkLab>(color);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MultiColorGradientProcess(ReadWriteBuffer<Float4> image, int width, ReadOnlyBuffer<GpuColorPoint> colorPoints, bool useOkLabInterpolation, float blendRate, float maxBlendRate, float opacity, int blendMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var imagePos = y * width + x;

            var total = 0.0F;
            var color = Float4.Zero;
            var pos = new Float2(x, y);
            for (var i = 0; i < colorPoints.Length; i++)
            {
                var diff = pos - colorPoints[i].Point;
                var l = Hlsl.Min(maxBlendRate, Hlsl.Pow(1.0F / Hlsl.Dot(diff, diff), blendRate));
                total += l;
                color += colorPoints[i].Color * l;
            }

            color /= Hlsl.Max(total, float.Epsilon);
            if (useOkLabInterpolation)
            {
                color = ColorSpaceConversion.OkLabToRgb(color);
            }
            color.W = opacity;

            image[imagePos] = BlendMethods.Process(blendMode, image[imagePos], color);
        }
    }
}
