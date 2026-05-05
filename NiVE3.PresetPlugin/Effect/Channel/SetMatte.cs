using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Channel
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Channel_SetMatte_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Channel, LanguageResourceDictionary.Channel_SetMatte_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class SetMatte : IEffect
    {
        const string ID = "8123B2CE-919F-4574-AF94-899D296AAC0E";

        const string PropertySourceLayerId = nameof(PropertySourceLayerId);

        const string PropertySourceLayerPositionId = nameof(PropertySourceLayerPositionId);

        const string PropertySourceChannelId = nameof(PropertySourceChannelId);

        const string PropertyIsInvertId = nameof(PropertyIsInvertId);

        const string PropertyModeId = nameof(PropertyModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new UseLayerImageProperty(PropertySourceLayerId, LanguageResourceDictionary.ResourceKeys.Channel_SetMatte_SourceLayer, 90.0),
                new EnumProperty(PropertySourceLayerPositionId, LanguageResourceDictionary.ResourceKeys.Channel_SetMatte_SourceLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                new EnumProperty(PropertySourceChannelId, LanguageResourceDictionary.ResourceKeys.Channel_SetMatte_SourceChannel, typeof(WithHSLChannelType), typeof(LanguageResourceDictionary), WithHSLChannelType.A, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyIsInvertId, LanguageResourceDictionary.ResourceKeys.Channel_SetMatte_IsInvert, false),
                new EnumProperty(PropertyModeId, LanguageResourceDictionary.ResourceKeys.Channel_MinMax_Mode, typeof(SetMatteMode), typeof(LanguageResourceDictionary), SetMatteMode.Overwrite, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var sourceLayerId = properties.GetValue(PropertySourceLayerId, layerTime, UseLayerImageTarget.Empty);
            if (sourceLayerId == UseLayerImageTarget.Empty)
            {
                return image;
            }

            var mode = properties.GetValue(PropertyModeId, layerTime, SetMatteMode.None);
            if (mode == SetMatteMode.None)
            {
                return image;
            }

            using var sourceImage = sourceLayerId.GetImage(composition, layerTime + layer.SourceStartPoint, downSamplingRateX, useGpu);
            if (sourceImage == null)
            {
                return image;
            }

            var sourceLayerPosition = properties.GetValue(PropertySourceLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var sourceChannel = properties.GetValue(PropertySourceChannelId, layerTime, WithHSLChannelType.A);
            var isInvert = properties.GetValue(PropertyIsInvertId, layerTime, false);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceImage, sourceLayerPosition, sourceChannel, isInvert, mode);
            }
            else
            {
                return ProcessCpu(image, roi, sourceImage, sourceLayerPosition, sourceChannel, isInvert, mode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, NImage sourceImage, SourceLayerPositionType sourceLayerPosition, WithHSLChannelType channelType, bool isInvert, SetMatteMode mode)
        {
            var managedImage = image.ToManaged();

            var managedSourceImage = sourceImage.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;
            var sourceWidth = managedSourceImage.Width;
            var sourceHeight = managedSourceImage.Height;
            var sourceData = managedSourceImage.Data;

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

            if (isInvert)
            {
                switch (mode)
                {
                    case SetMatteMode.Overwrite:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W = 1.0F - CalcAlpha(source, channelType);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case SetMatteMode.Add:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W = Math.Clamp(color.W + (1.0F - CalcAlpha(source, channelType)), 0.0F, 1.0F);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case SetMatteMode.Multiply:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W *= 1.0F - CalcAlpha(source, channelType);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case SetMatteMode.Subtract:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W = Math.Clamp(color.W - (1.0F - CalcAlpha(source, channelType)), 0.0F, 1.0F);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                }
            }
            else
            {
                switch (mode)
                {
                    case SetMatteMode.Overwrite:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W = CalcAlpha(source, channelType);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case SetMatteMode.Add:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W = Math.Clamp(color.W + CalcAlpha(source, channelType), 0.0F, 1.0F);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case SetMatteMode.Multiply:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W *= CalcAlpha(source, channelType);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case SetMatteMode.Subtract:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceX = sourceStartX + sourceDiffX * roi.Left;
                            var sourceY = sourceStartY + sourceDiffY * y;
                            for (var x = roi.Left; x < roi.Right; x++, sourceX += sourceDiffX)
                            {
                                var source = (sourceLayerPosition == SourceLayerPositionType.Loop ? ImageInterpolation.BilinearLoop(sourceData, sourceWidth, sourceHeight, sourceX, sourceY) : ImageInterpolation.BilinearEdgeRepeat(sourceData, sourceWidth, sourceHeight, sourceX, sourceY));
                                var color = imageDataSpan[x];
                                color.W = Math.Clamp(color.W - CalcAlpha(source, channelType), 0.0F, 1.0F);
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                }
            }

            if (managedSourceImage != sourceImage)
            {
                managedSourceImage.Dispose();
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, NImage sourceImage, SourceLayerPositionType sourceLayerPosition, WithHSLChannelType channelType, bool isInvert, SetMatteMode mode)
        {
            var gpuImage = image.ToGpu(device);

            var gpuSourceImage = sourceImage.ToGpu(device);

            device.For(roi.Width, roi.Height, new SetMatteProcess(gpuImage.Data, gpuImage.Width, gpuImage.Height, gpuSourceImage.Data, gpuSourceImage.Width, gpuSourceImage.Height, (int)sourceLayerPosition, (int)channelType, isInvert, (int)mode, roi.Left, roi.Top));

            if (gpuSourceImage != sourceImage)
            {
                gpuSourceImage.Dispose();
            }
;
            return gpuImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalcAlpha(in Vector4 color, WithHSLChannelType channelType)
        {
            switch (channelType)
            {
                case WithHSLChannelType.RGB:
                    return Vector4.Dot(color, Const.ConvertToGrayScale);
                case WithHSLChannelType.R:
                    return Math.Clamp(color.Z, 0.0F, 1.0F);
                case WithHSLChannelType.G:
                    return Math.Clamp(color.Y, 0.0F, 1.0F);
                case WithHSLChannelType.B:
                    return Math.Clamp(color.X, 0.0F, 1.0F);
                case WithHSLChannelType.A:
                    return Math.Clamp(color.W, 0.0F, 1.0F);
                case WithHSLChannelType.Hue:
                    {
                        var min = color.HorizontalMinBy3Element();
                        var max = color.HorizontalMaxBy3Element();
                        var diff = max - min;
                        var h = diff != 0.0F ? max switch
                        {
                            _ when max == color.X => (color.Z - color.Y) / diff * 60.0F + 240.0F,
                            _ when max == color.Y => (color.X - color.Z) / diff * 60.0F + 120.0F,
                            _ => (color.Y - color.X) / diff * 60.0F
                        } : 0.0F;

                        return h / 360.0F;
                    }
                case WithHSLChannelType.Saturation:
                    {
                        var min = color.AsVector128().HorizontalMinBy3Element().GetElement(0);
                        var max = color.AsVector128().HorizontalMaxBy3Element().GetElement(0);
                        if (max > 0.0F)
                        {
                            return (max - min) / max;
                        }
                        else
                        {
                            return 0.0F;
                        }
                    }
                default:
                    return 0.0F;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SetMatteProcess(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> sourceImage, int sourceWidth, int sourceHeight, int sourcePosition, int channelType, bool isInvert, int mode, int startX, int startY) : IComputeShader
    {
        readonly float SourceStartX = sourcePosition == 1 ? 0.0F : (sourceWidth - width) * 0.5F;

        readonly float SourceStartY = sourcePosition == 1 ? 0.0F : (sourceHeight - height) * 0.5F;

        readonly float SourceDiffX = sourcePosition == 1 ? (sourceWidth - 1.0F) / (width - 1.0F) : 1.0F;

        readonly float SourceDiffY = sourcePosition == 1 ? (sourceHeight - 1.0F) / (height - 1.0F) : 1.0F;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var color = image[pos];
            var sourceX = x * SourceDiffX + SourceStartX;
            var sourceY = y * SourceDiffY + SourceStartY;
            var source = (sourcePosition == 2 ? NormalMapBilinearLoop(sourceX, sourceY) : NormalMapBilinear(sourceX, sourceY));

            var alpha = CalcAlpha(source);
            if (isInvert)
            {
                alpha = 1.0F - alpha;
            }

            switch (mode)
            {
                case 1:
                    color.W = alpha;
                    break;
                case 2:
                    color.W = Hlsl.Clamp(color.W + alpha, 0.0F, 1.0F);
                    break;
                case 3:
                    color.W *= alpha;
                    break;
                case 4:
                    color.W = Hlsl.Clamp(color.W - alpha, 0.0F, 1.0F);
                    break;
            }

            image[pos] = color;
        }

        Float4 NormalMapBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < sourceWidth && iy < sourceHeight)
                {
                    return sourceImage[iy * sourceWidth + ix];
                }
                else
                {
                    return 0.5F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= sourceWidth || iy >= sourceHeight)
            {
                return 0.5F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = sourceWidth - 1;
            var mh = sourceHeight - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
            var pos = iy * sourceWidth + ix;

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
                            pos += sourceWidth;
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
                        pos += sourceWidth;
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
                            c3 = sourceImage[pos + sourceWidth];
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
                        c3 = sourceImage[pos + sourceWidth];
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
                        c4 = sourceImage[pos + sourceWidth];
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
                    c4 = sourceImage[pos + sourceWidth];
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

        Float4 NormalMapBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return sourceImage[CoordWrapGpu.Repeat(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix, sourceWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = sourceImage[CoordWrapGpu.Repeat(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix, sourceWidth)];
            var c2 = sourceImage[CoordWrapGpu.Repeat(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix + 1, sourceWidth)];
            var c3 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix, sourceWidth)];
            var c4 = sourceImage[CoordWrapGpu.Repeat(iy + 1, sourceHeight) * sourceWidth + CoordWrapGpu.Repeat(ix + 1, sourceWidth)];

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

        float CalcAlpha(Float4 color)
        {
            switch (channelType)
            {
                case 0:
                    return Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3);
                case 1:
                    return Hlsl.Clamp(color.Z, 0.0F, 1.0F);
                case 2:
                    return Hlsl.Clamp(color.Y, 0.0F, 1.0F);
                case 3:
                    return Hlsl.Clamp(color.X, 0.0F, 1.0F);
                case 4:
                    return Hlsl.Clamp(color.W, 0.0F, 1.0F);
                case 5:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        var diff = max - min;
                        var h = 0.0F;
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

                        return h / 360.0F;
                    }
                case 6:
                    {
                        var min = Hlsl.Min(Hlsl.Min(color.X, color.Y), color.Z);
                        var max = Hlsl.Max(Hlsl.Max(color.X, color.Y), color.Z);
                        if (max > 0.0F)
                        {
                            return (max - min) / max;
                        }
                        else
                        {
                            return 0.0F;
                        }
                    }
                default:
                    return 0.0F;
            }
        }
    }

    enum SetMatteMode
    {
        None,
        Overwrite,
        Add,
        Multiply,
        Subtract
    }
}
