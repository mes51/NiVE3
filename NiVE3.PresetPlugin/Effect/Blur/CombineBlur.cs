using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
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
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_CombineBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_CombineBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class CombineBlur : IEffect
    {
        const string ID = "0294992A-875A-4561-B0D7-4D95D1A0EF22";

        const string PropertySourceLayerId = nameof(PropertySourceLayerId);

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertySourceLayerPositionId = nameof(PropertySourceLayerPositionId);

        const string PropertyHorizontalAmountId = nameof(PropertyHorizontalAmountId);

        const string PropertyVerticalAmountId = nameof(PropertyVerticalAmountId);

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
                new UseLayerImageProperty(PropertySourceLayerId, LanguageResourceDictionary.ResourceKeys.Blur_CombineBlur_SourceLayer, selectBoxWidth: 90.0),
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.Blur_CombineBlur_Channel, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                new EnumProperty(PropertySourceLayerPositionId, LanguageResourceDictionary.ResourceKeys.Blur_CombineBlur_SourceLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                new DoubleProperty(PropertyHorizontalAmountId, LanguageResourceDictionary.ResourceKeys.Blur_CombineBlur_HorizontalAmount, 0.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyVerticalAmountId, LanguageResourceDictionary.ResourceKeys.Blur_CombineBlur_VerticalAmount, 0.0, 0.0, 10000.0, digit: 2),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_CombineBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var horizontalAmount = (float)(properties.GetValue(PropertyHorizontalAmountId, layerTime, 0.0) / downSamplingRateX);
                var verticalAmount = (float)(properties.GetValue(PropertyVerticalAmountId, layerTime, 0.0) / downSamplingRateY);

                var expandX = (int)MathF.Ceiling(horizontalAmount);
                var expandY = (int)MathF.Ceiling(verticalAmount);
                return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var targetLayerId = properties.GetValue(PropertySourceLayerId, layerTime, UseLayerImageTarget.Empty);
            var channel = properties.GetValue(PropertyChannelId, layerTime, LuminanceAndSingleChannelType.Luminance);
            var sourceLayerPosition = properties.GetValue(PropertySourceLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var horizontalAmount = (float)(properties.GetValue(PropertyHorizontalAmountId, layerTime, 0.0) / downSamplingRateX);
            var verticalAmount = (float)(properties.GetValue(PropertyVerticalAmountId, layerTime, 0.0) / downSamplingRateY);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            var globalTime = layerTime + layer.SourceStartPoint;
            using var sourceImage = targetLayerId.GetImage(composition, globalTime, downSamplingRateX, useGpu);
            if (sourceImage == null)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpuNaive(AcceleratorObject.CurrentDevice, roi, image, sourceImage, channel, sourceLayerPosition, horizontalAmount, verticalAmount, edgeRepeatMode);
            }
            else
            {
                return ProcessCpuDouble(roi, image, sourceImage, channel, sourceLayerPosition, horizontalAmount, verticalAmount, edgeRepeatMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        /*
        static NManagedImage ProcessCpu(ROI roi, NImage image, NImage sourceImage, LuminanceAndSingleChannelType channel, SourceLayerPositionType sourceLayerPosition, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            var managedImage = image.ToManaged();
            var managedSourceImage = sourceImage.ToManaged();

            var horizontalMargin = (int)MathF.Ceiling(horizontalAmount);
            var verticalMargin = (int)MathF.Ceiling(verticalAmount);
            using var satImage = SatProcessor.ProcessCpu(managedImage, horizontalMargin, verticalMargin, edgeRepeatMode);

            var (sourceStartX, sourceStartY) = sourceLayerPosition switch
            {
                SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                _ => ((managedSourceImage.Width - managedImage.Width) * 0.5F, (managedSourceImage.Height - managedImage.Height) * 0.5F)
            };
            var (sourceDiffX, sourceDiffY) = sourceLayerPosition switch
            {
                SourceLayerPositionType.Stretch => ((managedSourceImage.Width - 1) / (float)(managedImage.Width - 1), (managedSourceImage.Height - 1) / (float)(managedImage.Height - 1)),
                _ => (1.0F, 1.0F)
            };

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var satWidth = satImage.Width;
            var satHeight = satImage.Height;
            var satImageData = satImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var sourceDataSpan = managedSourceImage.GetDataSpan();
                var sourceX = sourceStartX + sourceDiffX * roi.Left;
                var sourceY = sourceStartY + sourceDiffY * y;

                for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                {
                    var mapColor = sourceLayerPosition == SourceLayerPositionType.Loop ?
                        ImageInterpolation.BilinearLoop(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY) :
                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY);

                    var rate = CalcBlurRate(mapColor, channel);
                    var currentHorizontalAmount = horizontalAmount * rate;
                    var currentVerticalAmount = verticalAmount * rate;

                    var satX = x + horizontalMargin - 1.0F;
                    var satY = y + verticalMargin - 1.0F;
                    var satLeftTop = ImageInterpolation.BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX - currentHorizontalAmount, satY - currentVerticalAmount);
                    var satLeftBottom = ImageInterpolation.BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX - currentHorizontalAmount, satY + currentVerticalAmount + 1.0F);
                    var satRightTop = ImageInterpolation.BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX + currentHorizontalAmount + 1.0F, satY - currentVerticalAmount);
                    var satRightBottom = ImageInterpolation.BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX + currentHorizontalAmount + 1.0F, satY + currentVerticalAmount + 1.0F);

                    var color = satRightBottom - satLeftBottom - satRightTop + satLeftTop;

                    var a = color.W;
                    if (a > 0.0F)
                    {
                        color /= a;
                        color.W = a / ((currentHorizontalAmount * 2.0F + 1.0F) * (currentVerticalAmount * 2.0F + 1.0F));
                        imageDataSpan[x] = color;
                    }
                }
            });

            if (managedSourceImage != sourceImage)
            {
                managedSourceImage.Dispose();
            }

            return managedImage;
        }
        */

        static NManagedImage ProcessCpuDouble(ROI roi, NImage image, NImage sourceImage, LuminanceAndSingleChannelType channel, SourceLayerPositionType sourceLayerPosition, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            var managedImage = image.ToManaged();
            var managedSourceImage = sourceImage.ToManaged();

            var horizontalMargin = (int)MathF.Ceiling(horizontalAmount);
            var verticalMargin = (int)MathF.Ceiling(verticalAmount);
            var (satWidth, satHeight, satImageData) = SatProcessor.ProcessCpuDouble(managedImage, horizontalMargin, verticalMargin, edgeRepeatMode);

            var (sourceStartX, sourceStartY) = sourceLayerPosition switch
            {
                SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                _ => ((managedSourceImage.Width - managedImage.Width) * 0.5F, (managedSourceImage.Height - managedImage.Height) * 0.5F)
            };
            var (sourceDiffX, sourceDiffY) = sourceLayerPosition switch
            {
                SourceLayerPositionType.Stretch => ((managedSourceImage.Width - 1) / (float)(managedImage.Width - 1), (managedSourceImage.Height - 1) / (float)(managedImage.Height - 1)),
                _ => (1.0F, 1.0F)
            };

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(imageData.AsSpan(y * imageWidth, imageWidth));
                var sourceDataSpan = managedSourceImage.GetDataSpan();
                var sourceX = sourceStartX + sourceDiffX * roi.Left;
                var sourceY = sourceStartY + sourceDiffY * y;

                for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                {
                    var mapColor = sourceLayerPosition == SourceLayerPositionType.Loop ?
                        ImageInterpolation.BilinearLoop(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY) :
                        ImageInterpolation.BilinearEdgeRepeat(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY);

                    var rate = CalcBlurRate(mapColor, channel);
                    var currentHorizontalAmount = horizontalAmount * rate;
                    var currentVerticalAmount = verticalAmount * rate;

                    var satX = x + horizontalMargin - 1.0;
                    var satY = y + verticalMargin - 1.0;
                    var satLeftTop = BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX - currentHorizontalAmount, satY - currentVerticalAmount);
                    var satLeftBottom = BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX - currentHorizontalAmount, satY + currentVerticalAmount + 1.0F);
                    var satRightTop = BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX + currentHorizontalAmount + 1.0F, satY - currentVerticalAmount);
                    var satRightBottom = BilinearEdgeRepeat(satImageData, satWidth, satHeight, satX + currentHorizontalAmount + 1.0F, satY + currentVerticalAmount + 1.0F);

                    var color = satRightBottom - satLeftBottom - satRightTop + satLeftTop;

                    var a = color.GetElement(3);
                    if (a > 0.0F)
                    {
                        color /= a;
                        var ta = (float)(a / ((currentHorizontalAmount * 2.0 + 1.0) * (currentVerticalAmount * 2.0 + 1.0)));
                        imageDataSpan[x] = Avx.ConvertToVector128Single(color & Const.WithoutWMask256) | (Vector128.Create(ta) & Const.WMask128);
                    }
                }
            });

            if (managedSourceImage != sourceImage)
            {
                managedSourceImage.Dispose();
            }

            ArrayPool<Vector256<double>>.Shared.Return(satImageData);

            return managedImage;
        }

        // TODO: double非対応GPUでも総和テーブルで誤差が出ない方法が見つかったら切り替える
        /*
        static NGPUImage ProcessGpu(GraphicsDevice device, ROI roi, NImage image, NImage sourceImage, LuminanceAndSingleChannelType channel, SourceLayerPositionType sourceLayerPosition, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            var gpuImage = image.ToGpu(device);
            var gpuSourceImage = sourceImage.ToGpu(device);

            var horizontalMargin = (int)MathF.Ceiling(horizontalAmount);
            var verticalMargin = (int)MathF.Ceiling(verticalAmount);
            using var satImage = SatProcessor.ProcessGpu(device, gpuImage, horizontalMargin, verticalMargin, edgeRepeatMode);

            device.For(
                roi.Width,
                roi.Height,
                new CombineBlurProcess(
                    gpuImage.Data,
                    gpuImage.Width,
                    gpuImage.Height,
                    gpuSourceImage.Data,
                    gpuSourceImage.Width,
                    gpuSourceImage.Height,
                    (int)channel,
                    (int)sourceLayerPosition,
                    satImage.Data,
                    satImage.Width,
                    satImage.Height,
                    horizontalAmount,
                    verticalAmount,
                    horizontalMargin,
                    verticalMargin,
                    roi.Left,
                    roi.Top
                )
            );

            if (gpuSourceImage != sourceImage)
            {
                gpuSourceImage.Dispose();
            }

            return gpuImage;
        }
        */

        static NGPUImage ProcessGpuNaive(GraphicsDevice device, ROI roi, NImage image, NImage sourceImage, LuminanceAndSingleChannelType channel, SourceLayerPositionType sourceLayerPosition, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            var gpuImage = image.ToGpu(device);
            var gpuSourceImage = sourceImage.ToGpu(device);

            using var originalImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(originalImage);

            device.For(
                roi.Width,
                roi.Height,
                new CombineBlurNaiveProcess(
                    gpuImage.Data,
                    originalImage.Data,
                    gpuImage.Width,
                    gpuImage.Height,
                    (int)edgeRepeatMode,
                    gpuSourceImage.Data,
                    gpuSourceImage.Width,
                    gpuSourceImage.Height,
                    (int)channel,
                    (int)sourceLayerPosition,
                    horizontalAmount,
                    verticalAmount,
                    roi.Left,
                    roi.Top
                )
            );

            if (gpuSourceImage != sourceImage)
            {
                gpuSourceImage.Dispose();
            }

            return gpuImage;
        }

        static float CalcBlurRate(in Vector4 color, LuminanceAndSingleChannelType channel)
        {
            return channel switch
            {
                LuminanceAndSingleChannelType.R => color.Z,
                LuminanceAndSingleChannelType.G => color.Y,
                LuminanceAndSingleChannelType.B => color.X,
                LuminanceAndSingleChannelType.A => color.W,
                LuminanceAndSingleChannelType.Luminance => Vector4.Dot(color, Const.ConvertToGrayScale),
                _ => 0.0F
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> BilinearEdgeRepeat(ReadOnlySpan<Vector256<double>> texture, int width, int height, double x, double y)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                return texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Wrap(ix, width)];
            }

            var c1 = texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Wrap(ix, width)];
            var c2 = texture[CoordWrap.Wrap(iy, height) * width + CoordWrap.Wrap(ix + 1, width)];
            var c3 = texture[CoordWrap.Wrap(iy + 1, height) * width + CoordWrap.Wrap(ix, width)];
            var c4 = texture[CoordWrap.Wrap(iy + 1, height) * width + CoordWrap.Wrap(ix + 1, width)];

            var pp = x - ix;
            var qq = y - iy;
            var ta = Lerp(Lerp(c1, c3, qq), Lerp(c2, c4, qq), pp).GetElement(3);
            if (ta <= 0.0F)
            {
                return Vector256.Create(1.0, 1.0, 1.0, 0.0);
            }
            var t = Lerp(Lerp(c1 * c1.GetElement(3), c3 * c3.GetElement(3), qq), Lerp(c2 * c2.GetElement(3), c4 * c4.GetElement(3), qq), pp) / ta;
            t = (t & Const.WithoutWMask256) | (Vector256.Create(ta) & Const.WMask256);

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<double> Lerp(in Vector256<double> v1, in Vector256<double> v2, double t)
        {
            return (v1 * (1.0f - t)) + (v2 * t);
        }
    }

    /*
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct CombineBlurProcess(
        ReadWriteBuffer<Float4> image,
        int width,
        int height,
        ReadWriteBuffer<Float4> sourceImage,
        int sourceImageWidth,
        int sourceImageHeight,
        int sourceChannel,
        int sourceLayerPosition,
        ReadWriteBuffer<Float4> satImage,
        int satWidth,
        int satHeight,
        float horizontalAmount,
        float verticalAmount,
        int horizontalMargin,
        int verticalMargin,
        int startX,
        int startY
    ) : IComputeShader
    {
        public void Execute()
        {
            var sourceStartX = (sourceImageWidth - width) * 0.5F;
            var sourceStartY = (sourceImageHeight - height) * 0.5F;
            var sourceDiffX = 1.0F;
            var sourceDiffY = 1.0F;
            if (sourceLayerPosition == 1)
            {
                sourceStartX = 0.0F;
                sourceStartY = 0.0F;
                sourceDiffX = (sourceImageWidth - 1) / (float)(width - 1);
                sourceDiffY = (sourceImageHeight - 1) / (float)(height - 1);
            }

            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var sourceX = sourceStartX + sourceDiffX * x;
            var sourceY = sourceStartY + sourceDiffY * y;

            var mapColor = Float4.Zero;
            if (sourceLayerPosition == 2)
            {
                mapColor = SourceImageBilinearLoop(sourceX, sourceY);
            }
            else
            {
                mapColor = SourceImageBilinear(sourceX, sourceY);
            }

            var rate = CalcBlurRate(mapColor);
            var currentHorizontalAmount = horizontalAmount * rate;
            var currentVerticalAmount = verticalAmount * rate;

            var satX = x + horizontalMargin - 1.0F;
            var satY = y + verticalMargin - 1.0F;
            var satLeftTop = SamplingSatImage(satX - currentHorizontalAmount, satY - currentVerticalAmount);
            var satLeftBottom = SamplingSatImage(satX - currentHorizontalAmount, satY + currentVerticalAmount + 1.0F);
            var satRightTop = SamplingSatImage(satX + currentHorizontalAmount + 1.0F, satY - currentVerticalAmount);
            var satRightBottom = SamplingSatImage(satX + currentHorizontalAmount + 1.0F, satY + currentVerticalAmount + 1.0F);

            var pos = y * width + x;
            var color = satRightBottom - satLeftBottom - satRightTop + satLeftTop;
            if (color.W > 0.0F)
            {
                image[pos] = new Float4(color.XYZ / color.W, color.W / ((currentHorizontalAmount * 2.0F + 1.0F) * (currentVerticalAmount * 2.0F + 1.0F)));
            }
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < sourceImageWidth && iy < sourceImageHeight)
                {
                    return sourceImage[iy * sourceImageWidth + ix];
                }
                else
                {
                    return 0.0F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= sourceImageWidth || iy >= sourceImageHeight)
            {
                return 0.0F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = sourceImageWidth - 1;
            var mh = sourceImageHeight - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
            var pos = iy * sourceImageWidth + ix;

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
                            pos += sourceImageWidth;
                            c3 = sourceImage[pos];
                            c4 = sourceImage[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += sourceImageWidth;
                        c1 = sourceImage[pos];
                        c2 = sourceImage[pos + 1];
                        c3 = c1;
                        c4 = c2;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = sourceImage[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = sourceImage[pos + sourceImageWidth];
                            c4 = c3;
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c1;
                        }
                    }
                    else
                    {
                        c3 = sourceImage[pos + sourceImageWidth];
                        c1 = c3;
                        c2 = c3;
                        c4 = c3;
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = sourceImage[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = sourceImage[pos + sourceImageWidth];
                        c3 = c4;
                    }
                    else
                    {
                        c3 = c2;
                        c4 = c2;
                    }
                }
                else
                {
                    c4 = sourceImage[pos + sourceImageWidth];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.0F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 SourceImageBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            var c2 = sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix + 1, sourceImageWidth)];
            var c3 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            var c4 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix + 1, sourceImageWidth)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.0F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        float CalcBlurRate(Float4 color)
        {
            switch (sourceChannel)
            {
                case 0: // R
                    return color.Z;
                case 1: // G
                    return color.Y;
                case 2: // B
                    return color.X;
                case 3: // A
                    return color.W;
                case 4: // Luminance
                    return Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3);
                default:
                    return 0.0F;
            }
        }

        Float4 SamplingSatImage(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);
            if (ix == x && iy == y)
            {
                return GetSatPixel(ix, iy);
            }

            var c1 = GetSatPixel(ix, iy);
            var c2 = GetSatPixel(ix + 1, iy);
            var c3 = GetSatPixel(ix, iy + 1);
            var c4 = GetSatPixel(ix + 1, iy + 1);

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

        Float4 GetSatPixel(int x, int y)
        {
            return satImage[CoordWrapGpu.Wrap(y, satHeight) * satWidth + CoordWrapGpu.Wrap(x, satWidth)];
        }
    }
    */

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct CombineBlurNaiveProcess(
        ReadWriteBuffer<Float4> image,
        ReadWriteBuffer<Float4> originalImage,
        int width,
        int height,
        int edgeRepeatMode,
        ReadWriteBuffer<Float4> sourceImage,
        int sourceImageWidth,
        int sourceImageHeight,
        int sourceChannel,
        int sourceLayerPosition,
        float horizontalAmount,
        float verticalAmount,
        int startX,
        int startY
    ) : IComputeShader
    {
        public void Execute()
        {
            var sourceStartX = (sourceImageWidth - width) * 0.5F;
            var sourceStartY = (sourceImageHeight - height) * 0.5F;
            var sourceDiffX = 1.0F;
            var sourceDiffY = 1.0F;
            if (sourceLayerPosition == 1)
            {
                sourceStartX = 0.0F;
                sourceStartY = 0.0F;
                sourceDiffX = (sourceImageWidth - 1) / (float)(width - 1);
                sourceDiffY = (sourceImageHeight - 1) / (float)(height - 1);
            }

            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var sourceX = sourceStartX + sourceDiffX * x;
            var sourceY = sourceStartY + sourceDiffY * y;

            var mapColor = Float4.Zero;
            if (sourceLayerPosition == 2)
            {
                mapColor = SourceImageBilinearLoop(sourceX, sourceY);
            }
            else
            {
                mapColor = SourceImageBilinear(sourceX, sourceY);
            }

            var rate = CalcBlurRate(mapColor);
            var currentHorizontalAmount = horizontalAmount * rate;
            var currentVerticalAmount = verticalAmount * rate;
            var currentHorizontalRange = (int)Hlsl.Ceil(currentHorizontalAmount);
            var currentVerticalRange = (int)Hlsl.Ceil(currentVerticalAmount);
            var currentHorizontalSize = currentHorizontalRange * 2;
            var currentVerticalSize = currentVerticalRange * 2;
            var horizontalEdgeAmount = currentHorizontalAmount - (int)currentHorizontalAmount;
            var verticalEdgeAmount = currentVerticalAmount - (int)currentVerticalAmount;
            if (horizontalEdgeAmount <= 0.0F)
            {
                horizontalEdgeAmount = 1.0F;
            }
            if (verticalEdgeAmount <= 0.0F)
            {
                verticalEdgeAmount = 1.0F;
            }

            var color = Float4.Zero;
            for (int t = y - currentVerticalRange, my = 0; my <= currentVerticalSize; t++, my++)
            {
                var tz = (my == 0 || my == currentVerticalSize) ? verticalEdgeAmount : 1.0F;
                for (int l = x - currentHorizontalRange, mx = 0; mx <= currentHorizontalSize; l++, mx++)
                {
                    var lz = (mx == 0 || mx == currentHorizontalSize) ? horizontalEdgeAmount : 1.0F;

                    var c = GetImagePixel(l, t);
                    color += new Float4(c.XYZ * c.W, c.W) * tz * lz;
                }
            }

            if (color.W > 0.0F)
            {
                image[y * width + x] = new Float4(color.XYZ / color.W, color.W / ((currentHorizontalAmount * 2.0F + 1.0F) * (currentVerticalAmount * 2.0F + 1.0F)));
            }
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < sourceImageWidth && iy < sourceImageHeight)
                {
                    return sourceImage[iy * sourceImageWidth + ix];
                }
                else
                {
                    return 0.0F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= sourceImageWidth || iy >= sourceImageHeight)
            {
                return 0.0F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = sourceImageWidth - 1;
            var mh = sourceImageHeight - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
            var pos = iy * sourceImageWidth + ix;

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
                            pos += sourceImageWidth;
                            c3 = sourceImage[pos];
                            c4 = sourceImage[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += sourceImageWidth;
                        c1 = sourceImage[pos];
                        c2 = sourceImage[pos + 1];
                        c3 = c1;
                        c4 = c2;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = sourceImage[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = sourceImage[pos + sourceImageWidth];
                            c4 = c3;
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c1;
                        }
                    }
                    else
                    {
                        c3 = sourceImage[pos + sourceImageWidth];
                        c1 = c3;
                        c2 = c3;
                        c4 = c3;
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = sourceImage[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = sourceImage[pos + sourceImageWidth];
                        c3 = c4;
                    }
                    else
                    {
                        c3 = c2;
                        c4 = c2;
                    }
                }
                else
                {
                    c4 = sourceImage[pos + sourceImageWidth];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.0F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 SourceImageBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            var c2 = sourceImage[CoordWrapGpu.Repeat(iy, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix + 1, sourceImageWidth)];
            var c3 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix, sourceImageWidth)];
            var c4 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceImageHeight) * sourceImageWidth + CoordWrapGpu.Repeat(ix + 1, sourceImageWidth)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.0F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        float CalcBlurRate(Float4 color)
        {
            switch (sourceChannel)
            {
                case 0: // R
                    return color.Z;
                case 1: // G
                    return color.Y;
                case 2: // B
                    return color.X;
                case 3: // A
                    return color.W;
                case 4: // Luminance
                    return Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3);
                default:
                    return 0.0F;
            }
        }

        Float4 GetImagePixel(int l, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return originalImage[CoordWrapGpu.Wrap(t, height) * width + CoordWrapGpu.Wrap(l, width)];
                case 2:
                    return originalImage[CoordWrapGpu.Repeat(t, height) * width + CoordWrapGpu.Repeat(l, width)];
                case 3:
                    return originalImage[CoordWrapGpu.Mirror(t, height) * width + CoordWrapGpu.Mirror(l, width)];
                default:
                    if (l > -1 && l < width && t > -1 && t < height)
                    {
                        return originalImage[t * width + l];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }
    }
}
