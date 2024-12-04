using System;
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
using NiVE3.Shared.Extension;
using static Vanara.PInvoke.Gdi32;

namespace NiVE3.PresetPlugin.Effect.Generate
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Generate_Gradient_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Generate, LanguageResourceDictionary.Generate_Gradient_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Gradient : IEffect
    {
        const string ID = "9DD026DB-89D1-451E-B087-EDCB4C8842CC";

        const string PropertyBeginPointId = nameof(PropertyBeginPointId);

        const string PropertyBeginColorId = nameof(PropertyBeginColorId);

        const string PropertyBeginOpacityId = nameof(PropertyBeginOpacityId);

        const string PropertyEndPointId = nameof(PropertyEndPointId);

        const string PropertyEndColorId = nameof(PropertyEndColorId);

        const string PropertyEndOpacityId = nameof(PropertyEndOpacityId);

        const string PropertyTypeId = nameof(PropertyTypeId);

        const string PropertyUseOkLabInterpolationId = nameof(PropertyUseOkLabInterpolationId);

        const string PropertyBlendOriginalId = nameof(PropertyBlendOriginalId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            var dialogOK = LanguageResourceDictionary.ResourceKeys.Dialog_OK;
            var dialogCancel = LanguageResourceDictionary.ResourceKeys.Dialog_Cancel;
            var colorDialogTitle = LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color;
            var centerX = sourceSize.Width * 0.5;
            var centerY = sourceSize.Height * 0.5;
            return
            [
                new Vector3dProperty(PropertyBeginPointId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_BeginPoint, new Vector3d(centerX - 100.0, centerY - 100.0, 0.0), digit: 2),
                new ColorProperty(PropertyBeginColorId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_BeginColor, colorDialogTitle, dialogOK, dialogCancel, Vector4.One),
                new DoubleProperty(PropertyBeginOpacityId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_BeginOpacity, 100.0, 0.0, 100.0, digit: 2),
                new Vector3dProperty(PropertyEndPointId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_EndPoint, new Vector3d(centerX + 100.0, centerY + 100.0, 0.0), digit: 2),
                new ColorProperty(PropertyEndColorId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_EndColor, colorDialogTitle, dialogOK, dialogCancel, Vector4.UnitW),
                new DoubleProperty(PropertyEndOpacityId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_EndOpacity, 100.0, 0.0, 100.0, digit: 2),
                new EnumProperty(PropertyTypeId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_Type, typeof(GradientShapeType), typeof(LanguageResourceDictionary), GradientShapeType.Linear, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyUseOkLabInterpolationId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_UseOkLabInterpolation, false),
                new DoubleProperty(PropertyBlendOriginalId, LanguageResourceDictionary.ResourceKeys.Generate_Gradient_BlendOriginal, 0.0, 0.0, 100.0, digit: 2)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var downSamplingRate3d = new Vector3d(downSamplingRateX, downSamplingRateY, 1.0);
            var beginPoint = ((Vector3)(properties.GetValue(PropertyBeginPointId, layerTime, Vector3d.Zero) / downSamplingRate3d)).AsVector2();
            var beginColor = properties.GetValue(PropertyBeginColorId, layerTime, Vector4.One);
            var beginOpacity = (float)properties.GetValue(PropertyBeginOpacityId, layerTime, 0.0) * 0.01F;
            var endPoint = ((Vector3)(properties.GetValue(PropertyEndPointId, layerTime, Vector3d.Zero) / downSamplingRate3d)).AsVector2();
            var endColor = properties.GetValue(PropertyEndColorId, layerTime, Vector4.UnitW);
            var endOpacity = (float)properties.GetValue(PropertyEndOpacityId, layerTime, 0.0) * 0.01F;
            var type = properties.GetValue(PropertyTypeId, layerTime, GradientShapeType.Linear);
            var useOkLabInterpolation = properties.GetValue(PropertyUseOkLabInterpolationId, layerTime, false);
            var blendOriginal = (float)properties.GetValue(PropertyBlendOriginalId, layerTime, 0.0) * 0.01F;

            if (blendOriginal >= 1.0F)
            {
                return image;
            }

            beginColor.W = beginOpacity;
            endColor.W = endOpacity;

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, beginPoint, beginColor, endPoint, endColor, type, useOkLabInterpolation, blendOriginal);
            }
            else
            {
                return ProcessCpu(image, roi, beginPoint, beginColor, endPoint, endColor, type, useOkLabInterpolation, blendOriginal);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Vector2 beginPoint, Vector4 beginColor, Vector2 endPoint, Vector4 endColor, GradientShapeType type, bool useOkLabInterpolation, float blendOriginal)
        {
            var managedImage = image.ToManaged();

            var imageOrigin = new Vector2((float)(roi.OriginalImagePosition.X + managedImage.Origin.X), (float)(roi.OriginalImagePosition.Y + managedImage.Origin.Y));
            beginPoint += imageOrigin;
            endPoint += imageOrigin;

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            switch (type)
            {
                case GradientShapeType.Radial:
                    {
                        var distance = Vector2.Distance(beginPoint, endPoint);
                        if (useOkLabInterpolation)
                        {
                            var beginOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(beginColor));
                            var endOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(endColor));
                            var beginOpacity = beginColor.W;
                            var endOpacity = beginColor.W;
                            Parallel.For(roi.Top, roi.Bottom, y =>
                            {
                                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                                for (var x = roi.Left; x < roi.Right; x++)
                                {
                                    var diff = Vector2.Distance(new Vector2(x, y), beginPoint) / distance;
                                    var color = diff switch
                                    {
                                        _ when 0.0F >= diff => beginColor,
                                        _ when 1.0F <= diff => endColor,
                                        _ => Unsafe.BitCast<Vector4, OkLab>(Vector4.Lerp(beginOkLabColor, endOkLabColor, diff)).ToRgb()
                                    };
                                    color.W = float.Lerp(beginOpacity, endOpacity, diff);
                                    imageDataSpan[x] = Vector4.Lerp(color, imageDataSpan[x], blendOriginal);
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(roi.Top, roi.Bottom, y =>
                            {
                                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                                for (var x = roi.Left; x < roi.Right; x++)
                                {
                                    var diff = Vector2.Distance(new Vector2(x, y), beginPoint) / distance;
                                    var color = diff switch
                                    {
                                        _ when 0.0F >= diff => beginColor,
                                        _ when 1.0F <= diff => endColor,
                                        _ => Vector4.Lerp(beginColor, endColor, diff)
                                    };
                                    imageDataSpan[x] = Vector4.Lerp(color, imageDataSpan[x], blendOriginal);
                                }
                            });
                        }
                    }
                    break;
                default:
                    {
                        var center = imageOrigin + new Vector2(managedImage.Width, managedImage.Height) * 0.5F;
                        var angle = MathF.Atan2(endPoint.X - beginPoint.X, endPoint.Y - beginPoint.Y) * (180.0F / MathF.PI);
                        var matrix = Matrix3x3.CreateRotateAt(angle, center.X, center.Y);
                        var sourceBeginY = matrix.Transform(beginPoint).Y;
                        var sourceEndY = matrix.Transform(endPoint).Y;
                        if (sourceBeginY > sourceEndY)
                        {
                            (sourceBeginY, sourceEndY) = (sourceEndY, sourceBeginY);
                            (beginColor, endColor) = (endColor, beginColor);
                        }
                        var diff = sourceEndY - sourceBeginY;

                        if (useOkLabInterpolation)
                        {
                            var beginOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(beginColor));
                            var endOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(endColor));
                            var beginOpacity = beginColor.W;
                            var endOpacity = beginColor.W;
                            Parallel.For(roi.Top, roi.Bottom, y =>
                            {
                                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                                for (var x = roi.Left; x < roi.Right; x++)
                                {
                                    var (_, ty) = matrix.Transform(x, y);
                                    var t = (ty - sourceBeginY) / diff;
                                    var color = ty switch
                                    {
                                        _ when sourceBeginY > ty => beginColor,
                                        _ when sourceEndY < ty => endColor,
                                        _ => Unsafe.BitCast<Vector4, OkLab>(Vector4.Lerp(beginOkLabColor, endOkLabColor, t)).ToRgb()
                                    };
                                    color.W = float.Lerp(beginOpacity, endOpacity, t);
                                    imageDataSpan[x] = Vector4.Lerp(color, imageDataSpan[x], blendOriginal);
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(roi.Top, roi.Bottom, y =>
                            {
                                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                                for (var x = roi.Left; x < roi.Right; x++)
                                {
                                    var (_, ty) = matrix.Transform(x, y);
                                    var color = ty switch
                                    {
                                        _ when sourceBeginY > ty => beginColor,
                                        _ when sourceEndY < ty => endColor,
                                        _ => Vector4.Lerp(beginColor, endColor, (ty - sourceBeginY) / diff)
                                    };
                                    imageDataSpan[x] = Vector4.Lerp(color, imageDataSpan[x], blendOriginal);
                                }
                            });
                        }
                    }
                    break;
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Vector2 beginPoint, Vector4 beginColor, Vector2 endPoint, Vector4 endColor, GradientShapeType type, bool useOkLabInterpolation, float blendOriginal)
        {
            var gpuImage = image.ToGpu(device);

            var imageOrigin = new Vector2((float)(roi.OriginalImagePosition.X + gpuImage.Origin.X), (float)(roi.OriginalImagePosition.Y + gpuImage.Origin.Y));
            beginPoint += imageOrigin;
            endPoint += imageOrigin;

            using var context = device.CreateComputeContext();

            switch (type)
            {
                case GradientShapeType.Radial:
                    {
                        var distance = Vector2.Distance(beginPoint, endPoint);
                        if (useOkLabInterpolation)
                        {
                            var beginOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(beginColor));
                            var endOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(endColor));
                            context.For(roi.Width, roi.Height, new GradientRadialOkLabProcess(gpuImage.Data, gpuImage.Width, distance, beginPoint, beginColor, beginOkLabColor, endColor, endOkLabColor, blendOriginal, roi.Left, roi.Top));
                        }
                        else
                        {
                            context.For(roi.Width, roi.Height, new GradientRadialRgbProcess(gpuImage.Data, gpuImage.Width, distance, beginPoint, beginColor, endColor, blendOriginal, roi.Left, roi.Top));
                        }
                    }
                    break;
                default:
                    {
                        var center = imageOrigin + new Vector2(gpuImage.Width, gpuImage.Height) * 0.5F;
                        var angle = MathF.Atan2(endPoint.X - beginPoint.X, endPoint.Y - beginPoint.Y) * (180.0F / MathF.PI);
                        var matrix = Matrix3x3.CreateRotateAt(angle, center.X, center.Y);
                        var sourceBeginY = matrix.Transform(beginPoint).Y;
                        var sourceEndY = matrix.Transform(endPoint).Y;
                        if (sourceBeginY > sourceEndY)
                        {
                            (sourceBeginY, sourceEndY) = (sourceEndY, sourceBeginY);
                            (beginColor, endColor) = (endColor, beginColor);
                        }

                        if (useOkLabInterpolation)
                        {
                            var beginOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(beginColor));
                            var endOkLabColor = Unsafe.BitCast<OkLab, Vector4>(OkLab.FromRgb(endColor));
                            context.For(roi.Width, roi.Height, new GradientLinearOkLabProcess(gpuImage.Data, gpuImage.Width, sourceBeginY, beginColor, beginOkLabColor, sourceEndY, endColor, endOkLabColor, blendOriginal, matrix.ToFloat3x3(), roi.Left, roi.Top));
                        }
                        else
                        {
                            context.For(roi.Width, roi.Height, new GradientLinearRgbProcess(gpuImage.Data, gpuImage.Width, sourceBeginY, beginColor, sourceEndY, endColor, blendOriginal, matrix.ToFloat3x3(), roi.Left, roi.Top));
                        }
                    }
                    break;
            }

            return gpuImage;
        }
    }

    enum GradientShapeType
    {
        Linear,
        Radial
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GradientLinearRgbProcess(ReadWriteBuffer<Float4> image, int width, float sourceBegintY, Float4 beginColor, float sourceEndY, Float4 endColor, float blendOriginal, Float3x3 matrix, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var ty = Hlsl.Mul(matrix, new Float3(x, y, 1.0F)).Y;
            var color = beginColor;
            if (sourceEndY < ty)
            {
                color = endColor;
            }
            else if (sourceBegintY < ty)
            {
                color = Hlsl.Lerp(beginColor, endColor, (ty - sourceBegintY) / (sourceEndY - sourceBegintY));
            }

            image[pos] = Hlsl.Lerp(color, image[pos], blendOriginal);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GradientLinearOkLabProcess(ReadWriteBuffer<Float4> image, int width, float sourceBegintY, Float4 beginColor, Float4 beginOkLabColor, float sourceEndY, Float4 endColor, Float4 endOkLabColor, float blendOriginal, Float3x3 matrix, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var ty = Hlsl.Mul(matrix, new Float3(x, y, 1.0F)).Y;
            var color = beginColor;
            if (sourceEndY < ty)
            {
                color = endColor;
            }
            else if (sourceBegintY < ty)
            {
                var t = (ty - sourceBegintY) / (sourceEndY - sourceBegintY);
                var okLabInterpolated = Hlsl.Lerp(beginOkLabColor, endOkLabColor, t);
                color = new Float4(ColorSpaceConversion.OkLabToRgb(okLabInterpolated).XYZ, Hlsl.Lerp(beginColor.W, endColor.W, t));
            }

            image[pos] = Hlsl.Lerp(color, image[pos], blendOriginal);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GradientRadialRgbProcess(ReadWriteBuffer<Float4> image, int width, float distance, Float2 beginPoint, Float4 beginColor, Float4 endColor, float blendOriginal, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var diff = Hlsl.Distance(new Float2(x, y), beginPoint) / distance;
            var color = beginColor;
            if (diff > 1.0F)
            {
                color = endColor;
            }
            else
            {
                color = Hlsl.Lerp(beginColor, endColor, diff);
            }

            image[pos] = Hlsl.Lerp(color, image[pos], blendOriginal);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct GradientRadialOkLabProcess(ReadWriteBuffer<Float4> image, int width, float distance, Float2 beginPoint, Float4 beginColor, Float4 beginOkLabColor, Float4 endColor, Float4 endOkLabColor, float blendOriginal, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var diff = Hlsl.Distance(new Float2(x, y), beginPoint) / distance;
            var color = beginColor;
            if (diff > 1.0F)
            {
                color = endColor;
            }
            else
            {
                var okLabInterpolated = Hlsl.Lerp(beginOkLabColor, endOkLabColor, diff);
                color = new Float4(ColorSpaceConversion.OkLabToRgb(okLabInterpolated).XYZ, Hlsl.Lerp(beginColor.W, endColor.W, diff));
            }

            image[pos] = Hlsl.Lerp(color, image[pos], blendOriginal);
        }
    }
}
