using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util.Blur;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_DropShadow_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_DropShadow_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class DropShadow : IEffect
    {
        const string ID = "91E54C39-69D0-45BF-9B10-3B983D5FF273";

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyDistanceId = nameof(PropertyDistanceId);

        const string PropertyShadowColorId = nameof(PropertyShadowColorId);

        const string PropertyShadowOpacityId = nameof(PropertyShadowOpacityId);

        const string PropertyShadowBlurId = nameof(PropertyShadowBlurId);

        const string PropertyDrawShadowOnlyId = nameof(PropertyDrawShadowOnlyId);

        const int ShadowBlurRepeatCount = 3;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Stylize_DropShadow_Angle, 135.0, digit: 2),
                new DoubleProperty(PropertyDistanceId, LanguageResourceDictionary.ResourceKeys.Stylize_DropShadow_Distance, 30.0, 0.0, double.MaxValue, digit: 2),
                new ColorProperty(PropertyShadowColorId, LanguageResourceDictionary.ResourceKeys.Stylize_DropShadow_ShadowColor, LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_Color, LanguageResourceDictionary.ResourceKeys.Dialog_OK, LanguageResourceDictionary.ResourceKeys.Dialog_Cancel, Vector4.UnitW),
                new DoubleProperty(PropertyShadowOpacityId, LanguageResourceDictionary.ResourceKeys.Stylize_DropShadow_ShadowOpacity, 50.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new DoubleProperty(PropertyShadowBlurId, LanguageResourceDictionary.ResourceKeys.Stylize_DropShadow_ShadowBlur, 10.0, 0.0, 10000.0, digit: 2),
                new CheckBoxProperty(PropertyDrawShadowOnlyId, LanguageResourceDictionary.ResourceKeys.Stylize_DropShadow_DrawShadowOnly, false)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var rad = (properties.GetValue(PropertyAngleId, layerTime, 0.0) + 90.0) / 180.0 * Math.PI;
            var distance = (float)(properties.GetValue(PropertyDistanceId, layerTime, 0.0) / downSamplingRateY);
            var shadowBlur = properties.GetValue(PropertyShadowBlurId, layerTime, 0.0);

            var sin = (float)Math.Sin(rad);
            var cos = (float)Math.Cos(rad);

            var expandX = (int)MathF.Ceiling(Math.Abs(distance * cos));
            var expandY = (int)MathF.Ceiling(Math.Abs(distance * sin));
            var shadowHorizontalBlurRange = (int)MathF.Ceiling((float)(shadowBlur / downSamplingRateX));
            var shadowVerticalBlurRange = (int)MathF.Ceiling((float)(shadowBlur / downSamplingRateY));
            var signX = -Math.Sign(cos);
            var signY = -Math.Sign(sin);

            return baseRoi.Expand(
                Math.Min(expandX * signX - shadowHorizontalBlurRange, 0),
                Math.Min(expandY * signY - shadowVerticalBlurRange, 0),
                Math.Max(expandX * signX + shadowHorizontalBlurRange, 0),
                Math.Max(expandY * signY + shadowVerticalBlurRange, 0)
            );
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var rad = (properties.GetValue(PropertyAngleId, layerTime, 0.0) + 90.0) / 180.0 * Math.PI;
            var distance = (float)(properties.GetValue(PropertyDistanceId, layerTime, 0.0) / downSamplingRateY);
            var shadowColor = properties.GetValue(PropertyShadowColorId, layerTime, Vector4.UnitW);
            var shadowOpacity = (float)properties.GetValue(PropertyShadowOpacityId, layerTime, 0.0) * 0.01F;
            var shadowBlur = properties.GetValue(PropertyShadowBlurId, layerTime, 0.0);
            var drawShadowOnly = properties.GetValue(PropertyDrawShadowOnlyId, layerTime, false);

            if (shadowOpacity <= 0.0F && !drawShadowOnly)
            {
                return image;
            }

            var sin = (float)Math.Sin(rad);
            var cos = (float)Math.Cos(rad);

            shadowColor.W = shadowOpacity;
            var shadowHorizontalBlur = (float)(shadowBlur / downSamplingRateX);
            var shadowVerticalBlur = (float)(shadowBlur / downSamplingRateY);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sin, cos, distance, shadowColor, shadowHorizontalBlur, shadowVerticalBlur, drawShadowOnly);
            }
            else
            {
                return ProcessCpu(image, roi, sin, cos, distance, shadowColor, shadowHorizontalBlur, shadowVerticalBlur, drawShadowOnly);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float sin, float cos, float distance, Vector4 shadowColor, float shadowHorizontalBlur, float shadowVerticalBlur, bool drawShadowOnly)
        {
            var managedImage = image.ToManaged();

            var originalWidth = roi.OriginalImageSize.Width;
            var originalHeight = roi.OriginalImageSize.Height;
            var shadowHorizontalBlurRange = (int)MathF.Ceiling(shadowHorizontalBlur);
            var shadowVerticalBlurRange = (int)MathF.Ceiling(shadowVerticalBlur);
            var shadowImageWidth = originalWidth + shadowHorizontalBlurRange * 2;
            var shadowImageHeight = originalHeight + shadowVerticalBlurRange * 2;
            using var shadowImage = new NManagedImage(shadowImageWidth, shadowImageHeight);

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var shadowImageData = shadowImage.Data;
            var originalX = roi.OriginalImagePosition.X;
            var originalY = roi.OriginalImagePosition.Y;
            Parallel.For(0, originalHeight, y =>
            {
                var imageDataSpan = imageData.AsSpan((y + originalY) * imageWidth, imageWidth);
                var shadowImageDataSpan = shadowImageData.AsSpan((y + shadowVerticalBlurRange) * shadowImageWidth, shadowImageWidth);
                for (var x = 0; x < originalWidth; x++)
                {
                    var newColor = shadowColor;
                    newColor.W *= imageDataSpan[x + originalX].W;
                    shadowImageDataSpan[x + shadowHorizontalBlurRange] = Vector4.Clamp(newColor, Vector4.Zero, Vector4.One);
                }
            });

            if (shadowHorizontalBlurRange > 0 || shadowVerticalBlurRange > 0)
            {
                BoxBlurProcess.ProcessCpu(shadowImage, new ROI(new Int32Point(shadowHorizontalBlurRange, shadowHorizontalBlurRange), roi.OriginalImageSize, 0, 0, shadowImageWidth, shadowImageHeight), shadowHorizontalBlur / ShadowBlurRepeatCount, shadowVerticalBlur / ShadowBlurRepeatCount, ShadowBlurRepeatCount, EdgeRepeatMode.None);
            }

            var shadowTransformedX = distance * cos - originalX + shadowHorizontalBlurRange;
            var shadowTransformedY = distance * sin - originalY + shadowVerticalBlurRange;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var sx = x + shadowTransformedX;
                    var sy = y + shadowTransformedY;
                    imageDataSpan[x] = Blend.Process(BlendMode.Normal, ImageInterpolation.Bilinear(shadowImageData, shadowImageWidth, shadowImageHeight, sx, sy), imageDataSpan[x]);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float sin, float cos, float distance, Vector4 shadowColor, float shadowHorizontalBlur, float shadowVerticalBlur, bool drawShadowOnly)
        {
            var gpuImage = image.ToGpu(device);

            var originalWidth = roi.OriginalImageSize.Width;
            var originalHeight = roi.OriginalImageSize.Height;
            var shadowHorizontalBlurRange = (int)MathF.Ceiling(shadowHorizontalBlur);
            var shadowVerticalBlurRange = (int)MathF.Ceiling(shadowVerticalBlur);
            var shadowImageWidth = originalWidth + shadowHorizontalBlurRange * 2;
            var shadowImageHeight = originalHeight + shadowVerticalBlurRange * 2;
            var originalX = roi.OriginalImagePosition.X;
            var originalY = roi.OriginalImagePosition.Y;

            using var shadowImage = new NGPUImage(shadowImageWidth, shadowImageHeight, device);
            using (var context = device.CreateComputeContext())
            {
                context.For(originalWidth, originalHeight, new DropShadowGenerateShadowProcess(
                    shadowImage.Data, shadowImageWidth, gpuImage.Data, gpuImage.Width, shadowHorizontalBlurRange, shadowVerticalBlurRange, originalX, originalY, shadowColor
                ));
            }

            if (shadowHorizontalBlur > 0.0F || shadowVerticalBlur > 0.0F)
            {
                BoxBlurProcess.ProcessGpu(device, shadowImage, new ROI(new Int32Point(shadowHorizontalBlurRange, shadowHorizontalBlurRange), roi.OriginalImageSize, 0, 0, shadowImageWidth, shadowImageHeight), shadowHorizontalBlur / ShadowBlurRepeatCount, shadowVerticalBlur / ShadowBlurRepeatCount, ShadowBlurRepeatCount, EdgeRepeatMode.None);
            }

            var shadowTransformedX = distance * cos - originalX + shadowHorizontalBlurRange;
            var shadowTransformedY = distance * sin - originalY + shadowVerticalBlurRange;
            using (var context = device.CreateComputeContext())
            {
                context.For(roi.Width, roi.Height, new DropShadowBlendProcess(gpuImage.Data, gpuImage.Width, shadowImage.Data, shadowImageWidth, shadowImageHeight, shadowTransformedX, shadowTransformedY, roi.Left, roi.Top));
            }

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DropShadowGenerateShadowProcess(ReadWriteBuffer<Float4> shadowImage, int shadowWidth, ReadWriteBuffer<Float4> sourceImage, int sourceWidth, int shadowOffsetX, int shadowOffsetY, int originalX, int originalY, Float4 shadowColor) : IComputeShader
    {
        public void Execute()
        {
            var shadowPos = (ThreadIds.Y + shadowOffsetY) * shadowWidth + ThreadIds.X + shadowOffsetX;
            var sourcePos = (ThreadIds.Y + originalY) * sourceWidth + ThreadIds.X + originalX;

            var a = shadowColor.W * sourceImage[sourcePos].W;
            if (a > 0.0F)
            {
                shadowImage[shadowPos] = Hlsl.Saturate(new Float4(shadowColor.XYZ, a));
            }
            else
            {
                shadowImage[shadowPos] = Const.EmptyPixelFloat4;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DropShadowBlendProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<Float4> shadowImage, int shadowWidth, int shadowHeight, float shadowTransformedX, float shadowTransformedY, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var sx = x + shadowTransformedX;
            var sy = y + shadowTransformedY;
            var shadowColor = ShadowImageBilinear(sx, sy);
            image[pos] = BlendMethods.Process(0, shadowColor, image[pos]);
        }

        Float4 ShadowImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < shadowWidth && iy < shadowHeight)
                {
                    return shadowImage[iy * shadowWidth + ix];
                }
                else
                {
                    return Const.EmptyPixelFloat4;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= shadowWidth || iy >= shadowHeight)
            {
                return Const.EmptyPixelFloat4;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = shadowWidth - 1;
            var mh = shadowHeight - 1;

            var c1 = Const.EmptyPixelFloat4;
            var c2 = Const.EmptyPixelFloat4;
            var c3 = Const.EmptyPixelFloat4;
            var c4 = Const.EmptyPixelFloat4;
            var pos = iy * shadowWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = shadowImage[pos];
                        c2 = shadowImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += shadowWidth;
                            c3 = shadowImage[pos];
                            c4 = shadowImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += shadowWidth;
                        c3 = shadowImage[pos];
                        c4 = shadowImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = shadowImage[pos];
                        if (iy < mh)
                        {
                            c3 = shadowImage[pos + shadowWidth];
                        }
                    }
                    else
                    {
                        c3 = shadowImage[pos + shadowWidth];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = shadowImage[pos];
                    if (iy < mh)
                    {
                        c4 = shadowImage[pos + shadowWidth];
                    }
                }
                else
                {
                    c4 = shadowImage[pos + shadowWidth];
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
