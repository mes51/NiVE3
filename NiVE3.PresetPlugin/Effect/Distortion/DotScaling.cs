using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using System.Windows.Media.Media3D;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;
using NWaves.Utils;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_DotScaling_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_DotScaling_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class DotScaling : IEffect
    {
        const string ID = "54D1A190-55A7-4BB4-BD62-F1BCFA931934";

        const string PropertyScaleId = nameof(PropertyScaleId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new Vector3dProperty(PropertyScaleId, LanguageResourceDictionary.ResourceKeys.Distortion_DotScaling_Scale, new Vector3d(100.0, 100.0, 0.0), new Vector3d(100.0, 100.0, 0.0), new Vector3d(double.MaxValue), digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent, useLinkRatio: true)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var scale = (Vector2)(properties.GetValue(PropertyScaleId, layerTime, Vector3d.Zero) * 0.01);

            if (scale == Vector2.One)
            {
                return baseRoi;
            }

            var paddingWidth = (int)MathF.Ceiling((scale.X - 1.0F) * baseRoi.OriginalImageSize.Width * 0.5F);
            var paddingHeight = (int)MathF.Ceiling((scale.Y - 1.0F) * baseRoi.OriginalImageSize.Height * 0.5F);

            return baseRoi.Expand(-paddingWidth, -paddingHeight, paddingWidth, paddingHeight);
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var scale = (Vector2)(properties.GetValue(PropertyScaleId, layerTime, Vector3d.Zero) * 0.01);

            if (scale == Vector2.One)
            {
                return image;
            }

            var paddingWidth = (scale.X - 1.0F) * roi.OriginalImageSize.Width * 0.5F;
            var paddingHeight = (scale.Y - 1.0F) * roi.OriginalImageSize.Height * 0.5F;
            var paddingOffsetX = (int)MathF.Ceiling(paddingWidth) - paddingWidth;
            var paddingOffsetY = (int)MathF.Ceiling(paddingHeight) - paddingHeight;

            var intScaleX = (int)MathF.Ceiling(scale.X);
            var intScaleY = (int)MathF.Ceiling(scale.Y);
            var downScale = new Vector2(scale.X / intScaleX, scale.Y / intScaleY);
            var origin = new Vector2(roi.OriginalImagePosition.X * intScaleX - paddingOffsetX, roi.OriginalImagePosition.Y * intScaleY - paddingOffsetY);
            var matrix = Matrix3x3.AffineTransform(origin, downScale, 0.0F, Vector2.Zero);
            if (!Matrix3x3.Invert(matrix, out var inverted))
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, inverted, intScaleX, intScaleY);
            }
            else
            {
                return ProcessCpu(image, roi, inverted, intScaleX, intScaleY);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, Matrix3x3 downScaleMatrix, int intScaleX, int intScaleY)
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
                imageDataSpan.Slice(roi.Left, roi.Width).Fill(Const.EmptyPixel);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var (px, py) = downScaleMatrix.Transform(x, y);

                    imageDataSpan[x] = BilinearIntScaled(sourceImageData, imageWidth, imageHeight, px, py, intScaleX, intScaleY);
                }
            });

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, Matrix3x3 downScaleMatrix, int intScaleX, int intScaleY)
        {
            var gpuImage = image.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            device.For(roi.Width, roi.Height, new DotScalingProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, sourceImage.Data, downScaleMatrix.ToFloat3x3(), intScaleX, intScaleY, roi.Left, roi.Top));

            return gpuImage;
        }

        static Vector4 BilinearIntScaled(ReadOnlySpan<Vector4> image, int width, int height, float x, float y, int scaleX, int scaleY)
        {
            var ipx = (int)x / scaleX;
            var ipy = (int)y / scaleY;
            var inx = (int)(x + 1.0F) / scaleX;
            var iny = (int)(y + 1.0F) / scaleY;

            var pp = x - (int)x;
            var qq = y - (int)y;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;

            var c1 = Const.EmptyPixel;
            var c2 = Const.EmptyPixel;
            var c3 = Const.EmptyPixel;
            var c4 = Const.EmptyPixel;

            if (ipy > -1 && ipy < height)
            {
                var line = ipy * width;
                if (ipx > -1 && ipx < width)
                {
                    c1 = image[line + ipx];
                }
                if (inx > -1 && inx < width)
                {
                    c2 = image[line + inx];
                }
            }
            if (iny > -1 && iny < height)
            {
                var line = iny * width;
                if (ipx > -1 && ipx < width)
                {
                    c3 = image[line + ipx];
                }
                if (inx > -1 && inx < width)
                {
                    c4 = image[line + inx];
                }
            }

            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return Const.EmptyPixel;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DotScalingProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> sourceImage, Float3x3 downScaleMatrix, int intScaleX, int intScaleY, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var imagePos = downScaleMatrix * new Float3(x, y, 1.0F);
            image[y * width + x] = BilinearIntScaled(imagePos.X, imagePos.Y);
        }

        Float4 BilinearIntScaled(float x, float y)
        {
            var ipx = (int)x / intScaleX;
            var ipy = (int)y / intScaleY;
            var inx = (int)(x + 1.0F) / intScaleX;
            var iny = (int)(y + 1.0F) / intScaleY;

            var pp = x - (int)x;
            var qq = y - (int)y;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;

            var c1 = Const.EmptyPixelFloat4;
            var c2 = Const.EmptyPixelFloat4;
            var c3 = Const.EmptyPixelFloat4;
            var c4 = Const.EmptyPixelFloat4;

            if (ipy > -1 && ipy < height)
            {
                var line = ipy * width;
                if (ipx > -1 && ipx < width)
                {
                    c1 = sourceImage[line + ipx];
                }
                if (inx > -1 && inx < width)
                {
                    c2 = sourceImage[line + inx];
                }
            }
            if (iny > -1 && iny < height)
            {
                var line = iny * width;
                if (ipx > -1 && ipx < width)
                {
                    c3 = sourceImage[line + ipx];
                }
                if (inx > -1 && inx < width)
                {
                    c4 = sourceImage[line + inx];
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
