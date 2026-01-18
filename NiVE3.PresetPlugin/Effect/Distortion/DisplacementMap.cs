using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Distortion
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Distortion_DisplacementMap_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Distortion, LanguageResourceDictionary.Distortion_DisplacementMap_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class DisplacementMap : IEffect
    {
        static readonly Vector4 Half = Vector4.One * 0.5F;

        const string ID = "9D4FCE3E-8261-41B5-9836-99BE227AE73C";

        const string PropertySourceLayerId = nameof(PropertySourceLayerId);

        const string PropertyHorizontalChannelId = nameof(PropertyHorizontalChannelId);

        const string PropertyHorizontalMaxMoveId = nameof(PropertyHorizontalMaxMoveId);

        const string PropertyVerticalChannelId = nameof(PropertyVerticalChannelId);

        const string PropertyVerticalMaxMoveId = nameof(PropertyVerticalMaxMoveId);

        const string PropertySourceLayerPositionId = nameof(PropertySourceLayerPositionId);

        const string PropertyIsLoopImageId = nameof(PropertyIsLoopImageId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new UseLayerImageProperty(PropertySourceLayerId, LanguageResourceDictionary.ResourceKeys.Distortion_DisplacementMap_SourceLayer),
                new EnumProperty(PropertyHorizontalChannelId, LanguageResourceDictionary.ResourceKeys.Distortion_DisplacementMap_HorizontalChannel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.R),
                new DoubleProperty(PropertyHorizontalMaxMoveId, LanguageResourceDictionary.ResourceKeys.Distortion_DisplacementMap_HorizontalMaxMove, 5.0, double.MinValue, double.MaxValue, digit: 2),
                new EnumProperty(PropertyVerticalChannelId, LanguageResourceDictionary.ResourceKeys.Distortion_DisplacementMap_VerticalChannel, typeof(WithHSLLOnOffChannelType), typeof(LanguageResourceDictionary), WithHSLLOnOffChannelType.G),
                new DoubleProperty(PropertyVerticalMaxMoveId, LanguageResourceDictionary.ResourceKeys.Distortion_DisplacementMap_VerticalMaxMove, 5.0, double.MinValue, double.MaxValue, digit: 2),
                new EnumProperty(PropertySourceLayerPositionId, LanguageResourceDictionary.ResourceKeys.Distortion_DisplacementMap_SourceLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center),
                new CheckBoxProperty(PropertyIsLoopImageId, LanguageResourceDictionary.ResourceKeys.Distortion_DisplacementMap_IsLoopImage, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var targetLayerId = properties.GetValue(PropertySourceLayerId, layerTime, UseLayerImageTarget.Empty);
            var horizontalChannel = properties.GetValue(PropertyHorizontalChannelId, layerTime, WithHSLLOnOffChannelType.R);
            var horizontalMaxMove = (float)properties.GetValue(PropertyHorizontalMaxMoveId, layerTime, 0.0);
            var verticalChannel = properties.GetValue(PropertyVerticalChannelId, layerTime, WithHSLLOnOffChannelType.R);
            var verticalMaxMove = (float)properties.GetValue(PropertyVerticalMaxMoveId, layerTime, 0.0);
            var sourceLayerPosition = properties.GetValue(PropertySourceLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var isLoopImage = properties.GetValue(PropertyIsLoopImageId, layerTime, false);

            var globalTime = layerTime + layer.SourceStartPoint;
            using var sourceImage = targetLayerId.GetImage(composition, globalTime, downSamplingRateX, useGpu);
            if (sourceImage == null)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceImage, horizontalChannel, horizontalMaxMove, verticalChannel, verticalMaxMove, sourceLayerPosition, isLoopImage);
            }
            else
            {
                return ProcessCpu(image, roi, sourceImage, horizontalChannel, horizontalMaxMove, verticalChannel, verticalMaxMove, sourceLayerPosition, isLoopImage);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, NImage sourceImage, WithHSLLOnOffChannelType horizontalChannel, float horizontalMaxMove, WithHSLLOnOffChannelType verticalChannel, float verticalMaxMove, SourceLayerPositionType position, bool isLoopImage)
        {
            var managedImage = image.ToManaged();
            var managedSourceImage = sourceImage.ToManaged();

            using var originalImage = (NManagedImage)managedImage.Copy();

            var (sourceStartX, sourceStartY) = position switch
            {
                SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                _ => ((managedSourceImage.Width - managedImage.Width) * 0.5F, (managedSourceImage.Height - managedImage.Height) * 0.5F)
            };
            var (sourceDiffX, sourceDiffY) = position switch
            {
                SourceLayerPositionType.Stretch => ((managedSourceImage.Width - 1) / (float)(managedImage.Width - 1), (managedSourceImage.Height - 1) / (float)(managedImage.Height - 1)),
                _ => (1.0F, 1.0F)
            };
            var bx = roi.Left;
            var ex = roi.Right;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var originalImageDataSpan = originalImage.GetDataSpan();
                var imageDataLineSpan = managedImage.GetDataSpan().Slice(y * managedImage.Width, managedImage.Width);
                var sourceDataSpan = managedSourceImage.GetDataSpan();
                var sourceX = sourceStartX + sourceDiffX * bx;
                var sourceY = sourceStartY + sourceDiffY * y;
                for (var x = bx; x < ex; x++, sourceX += sourceDiffX)
                {
                    var mapColor = position == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY) : EdgeRepeatBilinear(sourceDataSpan, managedSourceImage.Width, managedSourceImage.Height, sourceX, sourceY, Half);

                    var distortedX = x + CalcMoveRate(mapColor, horizontalChannel) * horizontalMaxMove;
                    var distortedY = y + CalcMoveRate(mapColor, verticalChannel) * verticalMaxMove;

                    imageDataLineSpan[x] = isLoopImage ? ImageInterpolation.BilinearLoop(originalImageDataSpan, managedImage.Width, managedImage.Height, distortedX, distortedY) : ImageInterpolation.Bilinear(originalImageDataSpan, managedImage.Width, managedImage.Height, distortedX, distortedY);
                }
            });

            if (managedSourceImage != sourceImage)
            {
                managedSourceImage.Dispose();
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, NImage sourceImage, WithHSLLOnOffChannelType horizontalChannel, float horizontalMaxMove, WithHSLLOnOffChannelType verticalChannel, float verticalMaxMove, SourceLayerPositionType position, bool isLoopImage)
        {
            var gpuImage = image.ToGpu(device);
            var gpuSourceImage = sourceImage.ToGpu(device);

            using var originalImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(originalImage);

            using (var context = device.CreateComputeContext())
            {
                context.For(
                    roi.Width,
                    roi.Height,
                    new DisplacementMapProcess(
                        gpuImage.Data,
                        gpuImage.Width,
                        gpuImage.Height,
                        originalImage.Data,
                        gpuSourceImage.Data,
                        gpuSourceImage.Width,
                        gpuSourceImage.Height,
                        (int)horizontalChannel,
                        horizontalMaxMove,
                        (int)verticalChannel,
                        verticalMaxMove,
                        (int)position,
                        isLoopImage,
                        roi.Left,
                        roi.Top
                    )
                );
            }

            if (gpuSourceImage != sourceImage)
            {
                gpuSourceImage.Dispose();
            }

            return gpuImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalcMoveRate(in Vector4 color, WithHSLLOnOffChannelType channelType)
        {
            switch (channelType)
            {
                case WithHSLLOnOffChannelType.R:
                    return (color.Z - 0.5F) * 2.0F;
                case WithHSLLOnOffChannelType.G:
                    return (color.Y - 0.5F) * 2.0F;
                case WithHSLLOnOffChannelType.B:
                    return (color.X - 0.5F) * 2.0F;
                case WithHSLLOnOffChannelType.A:
                    return (color.W - 0.5F) * 2.0F;
                case WithHSLLOnOffChannelType.Luminance:
                    return (Vector4.Dot(color, Const.ConvertToGrayScale) - 0.5F) * 2.0F;
                case WithHSLLOnOffChannelType.Hue:
                    {
                        var min = color.HorizontalMinBy3Element();
                        var max = color.HorizontalMaxBy3Element();
                        var diff = max - min;
                        var h = diff != 0.0F ? max switch
                        {
                            _ when max == color.X => (color.Z - color.Y) / diff * 60.0F + 240.0F,
                            _ when max == color.Y => (color.X - color.Z) / diff * 60.0F + 120.0F,
                            _ => (color.Y - color.X) / diff * 60.0F
                        } : 180.0F;

                        return (h - 180.0F) / 180.0F;
                    }
                case WithHSLLOnOffChannelType.Saturation:
                    {
                        var min = color.AsVector128().HorizontalMinBy3Element().GetElement(0);
                        var max = color.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                        if (max > 0.0F)
                        {
                            return ((max - min) / max - 0.5F) * 2.0F;
                        }
                        else
                        {
                            return 0.5F;
                        }
                    }
                case WithHSLLOnOffChannelType.Lightness:
                    {
                        var min = color.AsVector128().HorizontalMinBy3Element().GetElement(0);
                        var max = color.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                        return max + min - 1.0F; // ((max + min) * 0.5F - 0.5F) * 2.0F;
                    }
                case WithHSLLOnOffChannelType.On:
                    return 1.0F;
                case WithHSLLOnOffChannelType.Off:
                    return -1.0F;
                default:
                    return 0.0F;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 EdgeRepeatBilinear(Span<Vector4> texture, int width, int height, float x, float y, in Vector4 defaultColor)
        {
            var ix = (int)Math.Floor(x);
            var iy = (int)Math.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return texture[iy * width + ix];
                }
                else
                {
                    return defaultColor;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return defaultColor;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            Vector4 c1;
            Vector4 c2;
            Vector4 c3;
            Vector4 c4;
            var pos = iy * width + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        c2 = texture[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = texture[pos];
                            c4 = texture[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = texture[pos];
                        c4 = texture[pos + 1];
                        c1 = c3;
                        c2 = c4;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = texture[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = texture[pos + width];
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
                        c3 = texture[pos + width];
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
                    c2 = texture[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = texture[pos + width];
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
                    c4 = texture[pos + width];
                    c1 = c4;
                    c2 = c4;
                    c3 = c4;
                }
            }

            var ta = Vector4.Lerp(Vector4.Lerp(c1, c3, qq), Vector4.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return defaultColor;
            }
            var t = Vector4.Lerp(Vector4.Lerp(c1 * c1.W, c3 * c3.W, qq), Vector4.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
            t.W = ta;

            return t;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DisplacementMapProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> originalImage, ReadWriteBuffer<Float4> sourceImage, int sourceImageWidth, int sourceImageHeight, int horizontalChannel, float horizontalMaxMove, int verticalChannel, float verticalMaxMove, int position, Bool isLoopImage, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var sourceStartX = (sourceImageWidth - width) * 0.5F;
            var sourceStartY = (sourceImageHeight - height) * 0.5F;
            var sourceDiffX = 1.0F;
            var sourceDiffY = 1.0F;
            if (position == 1)
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
            if (position == 2)
            {
                mapColor = SourceImageBilinearLoop(sourceX, sourceY);
            }
            else
            {
                mapColor = SourceImageBilinear(sourceX, sourceY);
            }

            var distortedX = x + CalcMoveRate(mapColor, horizontalChannel) * horizontalMaxMove;
            var distortedY = y + CalcMoveRate(mapColor, verticalChannel) * verticalMaxMove;

            var pos = y * width + x;
            if (isLoopImage)
            {
                image[pos] = OriginalImageBilinearLoop(distortedX, distortedY);
            }
            else
            {
                image[pos] = OriginalImageBilinear(distortedX, distortedY);
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
                    return 0.5F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= sourceImageWidth || iy >= sourceImageHeight)
            {
                return 0.5F;
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
                return 0.5F;
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
                return sourceImage[RepeatCoord(iy, sourceImageHeight) * sourceImageWidth + RepeatCoord(ix, sourceImageWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = sourceImage[RepeatCoord(iy, sourceImageHeight) * sourceImageWidth + RepeatCoord(ix, sourceImageWidth)];
            var c2 = sourceImage[RepeatCoord(iy, sourceImageHeight) * sourceImageWidth + RepeatCoord(ix + 1, sourceImageWidth)];
            var c3 = sourceImage[RepeatCoord(iy + 1, sourceImageHeight) * sourceImageWidth + RepeatCoord(ix, sourceImageWidth)];
            var c4 = sourceImage[RepeatCoord(iy + 1, sourceImageHeight) * sourceImageWidth + RepeatCoord(ix + 1, sourceImageWidth)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return 0.5F;
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
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
                        c1 = originalImage[pos];
                        c2 = originalImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += width;
                            c3 = originalImage[pos];
                            c4 = originalImage[pos + 1];
                        }
                    }
                    else
                    {
                        pos += width;
                        c3 = originalImage[pos];
                        c4 = originalImage[pos + 1];
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = originalImage[pos];
                        if (iy < mh)
                        {
                            c3 = originalImage[pos + width];
                        }
                    }
                    else
                    {
                        c3 = originalImage[pos + width];
                    }
                }
            }
            else
            {
                pos++;
                if (iy > -1)
                {
                    c2 = originalImage[pos];
                    if (iy < mh)
                    {
                        c4 = originalImage[pos + width];
                    }
                }
                else
                {
                    c4 = originalImage[pos + width];
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

        Float4 OriginalImageBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return originalImage[RepeatCoord(iy, height) * width + RepeatCoord(ix, width)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = originalImage[RepeatCoord(iy, height) * width + RepeatCoord(ix, width)];
            var c2 = originalImage[RepeatCoord(iy, height) * width + RepeatCoord(ix + 1, width)];
            var c3 = originalImage[RepeatCoord(iy + 1, height) * width + RepeatCoord(ix, width)];
            var c4 = originalImage[RepeatCoord(iy + 1, height) * width + RepeatCoord(ix + 1, width)];

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

        static float CalcMoveRate(Float4 color, int channelType)
        {
            switch (channelType)
            {
                case 0:
                    return (color.Z - 0.5F) * 2.0F;
                case 1:
                    return (color.Y - 0.5F) * 2.0F;
                case 2:
                    return (color.X - 0.5F) * 2.0F;
                case 3:
                    return (color.W - 0.5F) * 2.0F;
                case 4:
                    return (Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) - 0.5F) * 2.0F;
                case 5:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        var diff = max - min;
                        var h = 180.0F;
                        if (diff != 0.0F)
                        {
                            if (max == color.X)
                            {
                                h = (color.Z - color.Y) / diff * 60.0F + 240.0F;
                            }
                            else if (max == color.Y)
                            {
                                h = (color.X - color.Z) / diff * 60.0F + 120.0F;
                            }
                            else
                            {
                                h = (color.Y - color.X) / diff * 60.0F;
                            }
                        }

                        return (h - 180.0F) / 180.0F;
                    }
                case 6:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        if (max > 0.0F)
                        {
                            return ((max - min) / max - 0.5F) * 2.0F;
                        }
                        else
                        {
                            return 0.5F;
                        }
                    }
                case 7:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        return max + min - 1.0F;
                    }
                case 8:
                    return 1.0F;
                case 10:
                    return -1.0F;
                default:
                    return 0.0F;
            }
        }

        static int RepeatCoord(int v, int max)
        {
            return ((v % max) + max) % max;
        }
    }
}
