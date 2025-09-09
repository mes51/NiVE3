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
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_ExtractEdge_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_ExtractEdge_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ExtractEdge : IEffect
    {
        const string ID = "08BCDB68-005F-4FC3-8C50-10909341DDB6";

        const string PropertyWidthId = nameof(PropertyWidthId);

        const string PropertyInvertId = nameof(PropertyInvertId);

        const string PropertyMonochromeId = nameof(PropertyMonochromeId);

        const string PropertyBlendOriginalId = nameof(PropertyBlendOriginalId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyWidthId, LanguageResourceDictionary.ResourceKeys.Stylize_ExtractEdge_Width, 1.0, 0.0, double.MaxValue, digit: 2),
                new CheckBoxProperty(PropertyInvertId, LanguageResourceDictionary.ResourceKeys.Stylize_ExtractEdge_Invert, false),
                new CheckBoxProperty(PropertyMonochromeId, LanguageResourceDictionary.ResourceKeys.Stylize_ExtractEdge_Monochrome, false),
                new DoubleProperty(PropertyBlendOriginalId, LanguageResourceDictionary.ResourceKeys.Stylize_ExtractEdge_BlendOriginal, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var width = properties.GetValue(PropertyWidthId, layerTime, 0.0);
            var invert = properties.GetValue(PropertyInvertId, layerTime, false);
            var monochrome = properties.GetValue(PropertyMonochromeId, layerTime, false);
            var blendOriginal = (float)(properties.GetValue(PropertyBlendOriginalId, layerTime, 0.0) * 0.01);

            if (blendOriginal >= 100.0)
            {
                return image;
            }

            var edgeWidth = (float)(width / downSamplingRateX);
            var edgeHeight = (float)(width / downSamplingRateY);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, edgeWidth, edgeHeight, invert, monochrome, blendOriginal);
            }
            else
            {
                return ProcessCpu(image, roi, edgeWidth, edgeHeight, invert, monochrome, blendOriginal);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, float edgeWidth, float edgeHeight, bool invert, bool monochrome, float blendOriginal)
        {
            var managedImage = image.ToManaged();

            if (edgeWidth <= 0.0 && edgeHeight <= 0.0)
            {
                var color = invert ? Vector4.One : Vector4.UnitW;
                using var filledImage = new NManagedImage(managedImage.Width, managedImage.Height, color);
                ImageBlendProcessor.MixSameSizeCpu(managedImage, filledImage, blendOriginal, roi);
                return managedImage;
            }

            using var edgeImage = new NManagedImage(managedImage.Width, managedImage.Height);
            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var edgeImageData = edgeImage.Data;
            var maxEdgeWidth = imageWidth - 1.0F;
            var maxEdgeHeight = imageHeight - 1.0F;

            switch ((invert, monochrome))
            {
                case (true, true):
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        var edgeImageDataSpan = edgeImageData.AsSpan(y * imageWidth, imageWidth);

                        var py = Math.Max(y - edgeHeight, 0.0F);
                        var ny = Math.Min(y + edgeHeight, maxEdgeHeight);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var back = imageDataSpan[x];
                            var color = Vector4.Zero;
                            var px = Math.Max(x - edgeWidth, 0.0F);
                            var nx = Math.Min(x + edgeWidth, maxEdgeWidth);

                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, ny));
                            color = Vector4.One - Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                            var gray = Vector4.Dot(color, Const.ConvertToGrayScale);
                            edgeImageDataSpan[x] = new Vector4(gray, gray, gray, 1.0F);
                        }
                    });
                    break;
                case (true, false):
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        var edgeImageDataSpan = edgeImageData.AsSpan(y * imageWidth, imageWidth);

                        var py = Math.Max(y - edgeHeight, 0.0F);
                        var ny = Math.Min(y + edgeHeight, maxEdgeHeight);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var back = imageDataSpan[x];
                            var color = Vector4.Zero;
                            var px = Math.Max(x - edgeWidth, 0.0F);
                            var nx = Math.Min(x + edgeWidth, maxEdgeWidth);

                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, ny));
                            color = Vector4.One - Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                            color.W = 1.0F;
                            edgeImageDataSpan[x] = color;
                        }
                    });
                    break;
                case (false, true):
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        var edgeImageDataSpan = edgeImageData.AsSpan(y * imageWidth, imageWidth);

                        var py = Math.Max(y - edgeHeight, 0.0F);
                        var ny = Math.Min(y + edgeHeight, maxEdgeHeight);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var back = imageDataSpan[x];
                            var color = Vector4.Zero;
                            var px = Math.Max(x - edgeWidth, 0.0F);
                            var nx = Math.Min(x + edgeWidth, maxEdgeWidth);

                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, ny));
                            color = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                            var gray = Vector4.Dot(color, Const.ConvertToGrayScale);
                            edgeImageDataSpan[x] = new Vector4(gray, gray, gray, 1.0F);
                        }
                    });
                    break;
                default:
                    Parallel.For(roi.Top, roi.Bottom, y =>
                    {
                        var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                        var edgeImageDataSpan = edgeImageData.AsSpan(y * imageWidth, imageWidth);

                        var py = Math.Max(y - edgeHeight, 0.0F);
                        var ny = Math.Min(y + edgeHeight, maxEdgeHeight);
                        for (var x = roi.Left; x < roi.Right; x++)
                        {
                            var back = imageDataSpan[x];
                            var color = Vector4.Zero;
                            var px = Math.Max(x - edgeWidth, 0.0F);
                            var nx = Math.Min(x + edgeWidth, maxEdgeWidth);

                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, py));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, y));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, px, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, x, ny));
                            color += Vector4.Abs(back - ImageInterpolation.Bilinear(imageData, imageWidth, imageHeight, nx, ny));
                            color = Vector4.Clamp(color, Vector4.UnitW, Vector4.One);
                            edgeImageDataSpan[x] = color;
                        }
                    });
                    break;
            }
            ImageBlendProcessor.MixSameSizeCpu(managedImage, edgeImage, blendOriginal, roi);
            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float edgeWidth, float edgeHeight, bool invert, bool monochrome, float blendOriginal)
        {
            var gpuImage = image.ToGpu(device);

            if (edgeWidth <= 0.0 && edgeHeight <= 0.0)
            {
                var color = invert ? Vector4.One : Vector4.UnitW;
                using var filledImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device, color);
                ImageBlendProcessor.MixSameSizeGpu(device, gpuImage, filledImage, blendOriginal, roi);
                return gpuImage;
            }

            using var edgeImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            device.For(roi.Width, roi.Height, new ExtractEdgeProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, edgeImage.Data, edgeWidth, edgeHeight, invert, monochrome, roi.Left, roi.Top));

            ImageBlendProcessor.MixSameSizeGpu(device, gpuImage, edgeImage, blendOriginal, roi);

            return gpuImage;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ExtractEdgeProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> edgeImage, float edgeWidth, float edgeHeight, bool invert, bool monochrome, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var back = image[pos];

            var py = y - edgeHeight;
            var ny = y + edgeHeight;
            var px = x - edgeWidth;
            var nx = x + edgeWidth;

            var color = Float4.Zero;
            color += Hlsl.Abs(back - Bilinear(px, py));
            color += Hlsl.Abs(back - Bilinear(x, py));
            color += Hlsl.Abs(back - Bilinear(nx, py));
            color += Hlsl.Abs(back - Bilinear(px, y));
            color += Hlsl.Abs(back - Bilinear(nx, y));
            color += Hlsl.Abs(back - Bilinear(px, ny));
            color += Hlsl.Abs(back - Bilinear(x, ny));
            color += Hlsl.Abs(back - Bilinear(nx, ny));
            color = Hlsl.Clamp(color, 0.0F, 1.0F);

            if (invert)
            {
                color = 1.0F - color;
            }
            if (monochrome)
            {
                color = Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3);
            }
            color.W = 1.0F;

            edgeImage[pos] = color;
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
            return image[CoordWrapGpu.Wrap(y, height) * width + CoordWrapGpu.Wrap(x, width)];
        }
    }
}
