using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_Halftone_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_Halftone_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class Halftone : IEffect
    {
        const float Epsilon = 1E-6F;

        const string ID = "94F70E60-6965-4206-A28E-74853AB9E4E5";

        const string PropertySourceChannelTypeId = nameof(PropertySourceChannelTypeId);

        const string PropertyInvertId = nameof(PropertyInvertId);

        const string PropertyDotSizeId = nameof(PropertyDotSizeId);

        const string PropertyOffsetId = nameof(PropertyOffsetId);

        const string PropertyAngleId = nameof(PropertyAngleId);

        const string PropertyDotColorId = nameof(PropertyDotColorId);

        const string PropertyBackgroundColorId = nameof(PropertyBackgroundColorId);

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
                new EnumProperty(PropertySourceChannelTypeId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_SourceChannelType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                new CheckBoxProperty(PropertyInvertId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_Invert, true),
                new DoubleProperty(PropertyDotSizeId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_DotSize, 20.0, 2.0, double.MaxValue, digit: 2),
                new Vector3dProperty(PropertyOffsetId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_AnchorPoint, new Vector3d(sourceSize.Width, sourceSize.Height, 0.0) * 0.5, digit: 2, useInteraction: true),
                new AngleProperty(PropertyAngleId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_Angle, 45.0, digit: 2),
                new ColorProperty(PropertyDotColorId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_DotColor, title, dialogOk, dialogCancel, Vector4.UnitW),
                new ColorProperty(PropertyBackgroundColorId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_BackgroundColor, title, dialogOk, dialogCancel, Vector4.One),
                new DoubleProperty(PropertyOpacityId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                new EnumProperty(PropertyBlendModeId, LanguageResourceDictionary.ResourceKeys.Stylize_Halftone_BlendMode, typeof(BlendMode), typeof(LanguageResourceDictionary), BlendMode.Normal, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var opacity = (float)(properties.GetValue(PropertyOpacityId, layerTime, 0.0) * 0.01);
            if (opacity <= 0.0F)
            {
                return image;
            }

            var originOffset = new Vector3d(roi.OriginalImagePosition.X, roi.OriginalImagePosition.Y, 0.0);
            var downSamplingRate = new Vector3d(downSamplingRateX, downSamplingRateY, 1.0);
            var sourceChannelType = properties.GetValue(PropertySourceChannelTypeId, layerTime, LuminanceAndSingleChannelType.Luminance);
            var invert = properties.GetValue(PropertyInvertId, layerTime, false);
            var dotSize = (float)properties.GetValue(PropertyDotSizeId, layerTime, 0.0);
            var offset = (Vector2)(properties.GetValue(PropertyOffsetId, layerTime, Vector3d.Zero) / downSamplingRate + originOffset);
            var angle = (float)properties.GetValue(PropertyAngleId, layerTime, 0.0);
            var dotColor = properties.GetValue(PropertyDotColorId, layerTime, Vector4.Zero);
            var backgroundColor = properties.GetValue(PropertyBackgroundColorId, layerTime, Vector4.Zero);
            var blendMode = properties.GetValue(PropertyBlendModeId, layerTime, BlendMode.Normal);
            dotColor.W = 1.0F;
            backgroundColor.W = 1.0F;

            var gridSize = dotSize / MathF.Sqrt(2.0F);

            var imageCenter = (Vector2)(new Vector3d(image.Width, image.Height, 0.0) * 0.5 + originOffset);
            var dotTransform = Matrix3x3.CreateScale((float)downSamplingRateX, (float)downSamplingRateY) * Matrix3x3.AffineTransform(imageCenter, Vector2.One, angle, offset);

            var leftTop = dotTransform.Transform(Vector2.Zero);
            var rightTop = dotTransform.Transform(new Vector2(image.Width, 0.0F));
            var rightBottom = dotTransform.Transform(new Vector2(image.Width, image.Height));
            var leftBottom = dotTransform.Transform(new Vector2(0.0F, image.Height));
            var min = Vector2.Min(Vector2.Min(Vector2.Min(leftTop, rightTop), rightBottom), leftBottom);

            var gridAlignOffsetX = MathF.Floor(min.X / gridSize) * gridSize;
            var gridAlignOffsetY = MathF.Floor(min.Y / gridSize) * gridSize;
            dotTransform *= Matrix3x3.CreateTranslate(-gridAlignOffsetX, -gridAlignOffsetY);

            leftTop = dotTransform.Transform(Vector2.Zero);
            rightTop = dotTransform.Transform(new Vector2(image.Width, 0.0F));
            rightBottom = dotTransform.Transform(new Vector2(image.Width, image.Height));
            leftBottom = dotTransform.Transform(new Vector2(0.0F, image.Height));
            var max = Vector2.Max(Vector2.Max(Vector2.Max(leftTop, rightTop), rightBottom), leftBottom);
            var colCount = (int)MathF.Ceiling(max.X / gridSize);
            var rowCount = (int)MathF.Ceiling(max.Y / gridSize);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, sourceChannelType, invert, dotSize, gridSize, rowCount, colCount, dotTransform, dotColor, backgroundColor, opacity, blendMode);
            }
            else
            {
                return ProcessCpu(image, roi, sourceChannelType, invert, dotSize, gridSize, rowCount, colCount, dotTransform, dotColor, backgroundColor, opacity, blendMode);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, LuminanceAndSingleChannelType sourceChannelType, bool invert, float dotSize, float gridSize, int rowCount, int colCount, Matrix3x3 dotTransform, Vector4 dotColor, Vector4 backgroundColor, float opacity, BlendMode blendMode)
        {
            var managedImage = image.ToManaged();

            var canvasWidth = (int)MathF.Ceiling(colCount * gridSize);
            var canvasHeight = (int)MathF.Ceiling(rowCount * gridSize);

            using var transformResult = GenerateGridImageCpu(managedImage, canvasWidth, canvasHeight, gridSize, dotTransform);

            var dotSizes = ArrayPool<float>.Shared.Rent(rowCount * colCount);
            var transformResultData = transformResult.Data;
            switch (sourceChannelType)
            {
                case LuminanceAndSingleChannelType.R:
                    if (invert)
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = (1.0F - Math.Clamp(transformResultDataSpan[x].Z, 0.0F, 1.0F)) * dotSize * 0.5F;
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = Math.Clamp(transformResultDataSpan[x].Z, 0.0F, 1.0F) * dotSize * 0.5F;
                            }
                        });
                    }
                    break;
                case LuminanceAndSingleChannelType.G:
                    if (invert)
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = (1.0F - Math.Clamp(transformResultDataSpan[x].Y, 0.0F, 1.0F)) * dotSize * 0.5F;
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = Math.Clamp(transformResultDataSpan[x].Y, 0.0F, 1.0F) * dotSize * 0.5F;
                            }
                        });
                    }
                    break;
                case LuminanceAndSingleChannelType.B:
                    if (invert)
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = (1.0F - Math.Clamp(transformResultDataSpan[x].X, 0.0F, 1.0F)) * dotSize * 0.5F;
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = Math.Clamp(transformResultDataSpan[x].X, 0.0F, 1.0F) * dotSize * 0.5F;
                            }
                        });
                    }
                    break;
                case LuminanceAndSingleChannelType.A:
                    if (invert)
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = (1.0F - Math.Clamp(transformResultDataSpan[x].W, 0.0F, 1.0F)) * dotSize * 0.5F;
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = Math.Clamp(transformResultDataSpan[x].W, 0.0F, 1.0F) * dotSize * 0.5F;
                            }
                        });
                    }
                    break;
                default:
                    if (invert)
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = (1.0F - Math.Clamp(Vector4.Dot(transformResultDataSpan[x], Const.ConvertToGrayScale), 0.0F, 1.0F)) * dotSize * 0.5F;
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, rowCount, y =>
                        {
                            var transformResultDataSpan = transformResultData.AsSpan(y * canvasWidth, colCount);
                            var dotSizesSpan = dotSizes.AsSpan(y * colCount, colCount);

                            for (var x = 0; x < colCount; x++)
                            {
                                dotSizesSpan[x] = Math.Clamp(Vector4.Dot(transformResultDataSpan[x], Const.ConvertToGrayScale), 0.0F, 1.0F) * dotSize * 0.5F;
                            }
                        });
                    }
                    break;
            }

            var tempData = transformResult.Data;
            var gridCenter = new Vector2(gridSize) * 0.5F;
            Parallel.For(0, canvasHeight, y =>
            {
                var tempDataSpan = tempData.AsSpan(y * canvasWidth, canvasWidth);

                var gridY = y / gridSize;
                var gridIndexY = (int)gridY;
                var gridLocalPosY = gridY - MathF.Truncate(gridY);
                var dotSizesSpan = dotSizes.AsSpan(gridIndexY * colCount, colCount);
                var prevDotSizesSpan = gridIndexY > 0 ? dotSizes.AsSpan((gridIndexY - 1) * colCount, colCount) : dotSizesSpan;
                var nextDotSizesSpan = gridIndexY < rowCount - 1 ? dotSizes.AsSpan((gridIndexY + 1) * colCount, colCount) : dotSizesSpan;
                for (var x = 0; x < canvasWidth; x++)
                {
                    var gridX = x / gridSize;
                    var gridIndexX = (int)gridX;
                    var gridLocalPosX = gridX - MathF.Truncate(gridX);
                    var dotPos = new Vector2(gridLocalPosX, gridLocalPosY) * gridSize - gridCenter;

                    var rate = Math.Clamp(dotSizesSpan[gridIndexX] - dotPos.Length(), 0.0F, 1.0F);
                    if (rate < 1.0F && gridIndexX > 0)
                    {
                        dotPos = new Vector2(gridLocalPosX + 1.0F, gridLocalPosY) * gridSize - gridCenter;
                        rate += Math.Clamp(dotSizesSpan[gridIndexX - 1] - dotPos.Length(), 0.0F, 1.0F);
                    }
                    if (rate < 1.0F && gridIndexX < colCount - 1)
                    {
                        dotPos = new Vector2(gridLocalPosX - 1.0F, gridLocalPosY) * gridSize - gridCenter;
                        rate += Math.Clamp(dotSizesSpan[gridIndexX + 1] - dotPos.Length(), 0.0F, 1.0F);
                    }
                    if (rate < 1.0F && gridIndexY > 0)
                    {
                        dotPos = new Vector2(gridLocalPosX, gridLocalPosY + 1.0F) * gridSize - gridCenter;
                        rate += Math.Clamp(prevDotSizesSpan[gridIndexX] - dotPos.Length(), 0.0F, 1.0F);
                    }
                    if (rate < 1.0F && gridIndexY < rowCount - 1)
                    {
                        dotPos = new Vector2(gridLocalPosX, gridLocalPosY - 1.0F) * gridSize - gridCenter;
                        rate += Math.Clamp(nextDotSizesSpan[gridIndexX] - dotPos.Length(), 0.0F, 1.0F);
                    }

                    tempDataSpan[x] = Vector4.Lerp(backgroundColor, dotColor, Math.Clamp(rate, 0.0F, 1.0F));
                }
            });

            ArrayPool<float>.Shared.Return(dotSizes);

            Matrix3x3.Invert(dotTransform, out var recoverTransform);
            new CPURenderer2D(managedImage)
            {
                Clip = new Int32Rect(roi.Left, roi.Top, roi.Width, roi.Height)
            }.DrawSingleImage(Int32Point.Zero, transformResult, opacity, recoverTransform, ImageInterpolationQuality.Level2, blendMode, null);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, LuminanceAndSingleChannelType sourceChannelType, bool invert, float dotSize, float gridSize, int rowCount, int colCount, Matrix3x3 dotTransform, Vector4 dotColor, Vector4 backgroundColor, float opacity, BlendMode blendMode)
        {
            var gpuImage = image.ToGpu(device);

            var canvasWidth = (int)MathF.Ceiling(colCount * gridSize);
            var canvasHeight = (int)MathF.Ceiling(rowCount * gridSize);

            using var transformResult = GenerateGridImageGpu(device, gpuImage, canvasWidth, canvasHeight, gridSize, dotTransform);

            using var dotSizes = device.AllocateReadWriteBuffer<float>(rowCount * colCount);
            device.For(colCount, rowCount, new HalftoneCalcDotSizeProcess(transformResult.Data, transformResult.Width, dotSizes, colCount, dotSize * 0.5F, (int)sourceChannelType, invert));

            device.For(canvasWidth, canvasHeight, new HalftoneDrawDotProcess(transformResult.Data, canvasWidth, dotSizes, colCount, rowCount, gridSize, backgroundColor, dotColor));

            Matrix3x3.Invert(dotTransform, out var recoverTransform);
            new GPURenderer2D(gpuImage, device)
            {
                Clip = new Int32Rect(roi.Left, roi.Top, roi.Width, roi.Height)
            }.DrawSingleImage(Int32Point.Zero, transformResult, opacity, recoverTransform, ImageInterpolationQuality.Level2, blendMode, null);

            return gpuImage;
        }

        static NManagedImage GenerateGridImageCpu(NManagedImage image, int canvasWidth, int canvasHeight, float gridSize, in Matrix3x3 dotTransform)
        {
            var canvas1 = new NManagedImage(canvasWidth, canvasHeight);
            var canvas2 = new NManagedImage(canvasWidth, canvasHeight);

            var temp = canvas2;
            var transformResult = canvas1;

            var totalScale = 1.0F;
            var scale = Math.Min(gridSize / totalScale, 2.0F);
            var scaledWidth = canvasWidth / scale;
            var scaledHeight = canvasHeight / scale;
            var isFirst = true;
            while ((scale - 1.0F) > Epsilon || isFirst)
            {
                (temp, transformResult) = (transformResult, temp);

                var transform = (isFirst ? dotTransform : Matrix3x3.Identity) * Matrix3x3.CreateScale(1.0F / scale, 1.0F / scale);

                var p1 = transform.Transform(new Vector2());
                var p2 = transform.Transform(new Vector2(image.Width, 0.0F));
                var p3 = transform.Transform(new Vector2(image.Width, image.Height));
                var p4 = transform.Transform(new Vector2(0.0F, image.Height));
                var minX = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)), 0);
                var minY = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)), 0);
                var maxX = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)), (int)Math.Min(MathF.Ceiling(scaledWidth), canvasWidth - 1));
                var maxY = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)), (int)Math.Min(MathF.Ceiling(scaledHeight), canvasHeight - 1));

                var targetImage = isFirst ? image : temp;
                var canvasData = transformResult.Data;
                var imageData = targetImage.Data;
                var imageWidth = targetImage.Width;
                var imageHeight = targetImage.Height;
                Matrix3x3.Invert(transform, out var inverted);
                Parallel.For(minY, maxY, y =>
                {
                    var canvasDataSpan = canvasData.AsSpan(y * canvasWidth, canvasWidth);

                    for (var x = minX; x < maxX; x++)
                    {
                        var (imageX, imageY) = inverted.Transform(x, y);
                        canvasDataSpan[x] = ImageInterpolation.BilinearEdgeRepeat(imageData, imageWidth, imageHeight, imageX, imageY);
                    }
                });

                totalScale *= scale;
                scale = Math.Min(gridSize / totalScale, 2.0F);
                scaledWidth = scaledWidth / scale;
                scaledHeight = scaledHeight / scale;
                isFirst = false;
            }

            temp.Dispose();

            return transformResult;
        }

        static NGPUImage GenerateGridImageGpu(GraphicsDevice device, NGPUImage image, int canvasWidth, int canvasHeight,  float gridSize, in Matrix3x3 dotTransform)
        {
            var canvas1 = new NGPUImage(canvasWidth, canvasHeight, device);
            var canvas2 = new NGPUImage(canvasWidth, canvasHeight, device);

            var temp = canvas2;
            var transformResult = canvas1;

            var totalScale = 1.0F;
            var scale = Math.Min(gridSize / totalScale, 2.0F);
            var scaledWidth = canvasWidth / scale;
            var scaledHeight = canvasHeight / scale;
            var isFirst = true;
            using (var context = device.CreateComputeContext())
            {
                while ((scale - 1.0F) > Epsilon || isFirst)
                {
                    if (!isFirst)
                    {
                        context.Barrier(transformResult.Data);
                    }
                    (temp, transformResult) = (transformResult, temp);
                    var transform = (isFirst ? dotTransform : Matrix3x3.Identity) * Matrix3x3.CreateScale(1.0F / scale, 1.0F / scale);

                    var p1 = transform.Transform(new Vector2());
                    var p2 = transform.Transform(new Vector2(image.Width, 0.0F));
                    var p3 = transform.Transform(new Vector2(image.Width, image.Height));
                    var p4 = transform.Transform(new Vector2(0.0F, image.Height));
                    var minX = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X)), 0);
                    var minY = Math.Max((int)Math.Floor(Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y)), 0);
                    var maxX = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X)), (int)Math.Min(MathF.Ceiling(scaledWidth), canvasWidth - 1));
                    var maxY = Math.Min((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y)), (int)Math.Min(MathF.Ceiling(scaledHeight), canvasHeight - 1));

                    Matrix3x3.Invert(transform, out var inverted);
                    var targetImage = isFirst ? image : temp;
                    context.For(maxX - minX, maxY - minY, new HalftoneDrawWrappedImageProcess(transformResult.Data, canvasWidth, targetImage.Data, targetImage.Width, targetImage.Height, inverted.ToFloat3x3(), minX, minY));

                    totalScale *= scale;
                    scale = Math.Min(gridSize / totalScale, 2.0F);
                    scaledWidth = scaledWidth / scale;
                    scaledHeight = scaledHeight / scale;
                    isFirst = false;
                }
            }

            temp.Dispose();

            return transformResult;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct HalftoneDrawWrappedImageProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<Float4> sourceImage, int sourceWidth, int sourceHeight, Float3x3 transform, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var imagePos = transform * new Float3(x, y, 1.0F);

            image[y * width + x] = SourceImageBilinear(imagePos.X, imagePos.Y);
        }

        Float4 SourceImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);
            if (ix == x && iy == y)
            {
                return sourceImage[CoordWrapGpu.Wrap(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Wrap(ix, sourceWidth)];
            }

            var c1 = sourceImage[CoordWrapGpu.Wrap(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Wrap(ix, sourceWidth)];
            var c2 = sourceImage[CoordWrapGpu.Wrap(iy, sourceHeight) * sourceWidth + CoordWrapGpu.Wrap(ix + 1, sourceWidth)];
            var c3 = sourceImage[CoordWrapGpu.Wrap(iy + 1, sourceHeight) * sourceWidth + CoordWrapGpu.Wrap(ix, sourceWidth)];
            var c4 = sourceImage[CoordWrapGpu.Wrap(iy + 1, sourceHeight) * sourceWidth + CoordWrapGpu.Wrap(ix + 1, sourceWidth)];

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
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct HalftoneCalcDotSizeProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> dotSizes, int colCount, float maxDotSize, int sourceChannelType, bool invert) : IComputeShader
    {
        public void Execute()
        {
            var pos = ThreadIds.Y * colCount + ThreadIds.X;
            var imagePos = ThreadIds.Y * width + ThreadIds.X;

            var value = 0.0F;
            switch (sourceChannelType)
            {
                case 0:
                    value = Hlsl.Clamp(image[imagePos].Z, 0.0F, 1.0F);
                    break;
                case 1:
                    value = Hlsl.Clamp(image[imagePos].Y, 0.0F, 1.0F);
                    break;
                case 2:
                    value = Hlsl.Clamp(image[imagePos].X, 0.0F, 1.0F);
                    break;
                case 3:
                    value = Hlsl.Clamp(image[imagePos].W, 0.0F, 1.0F);
                    break;
                default:
                    value = Hlsl.Clamp(Hlsl.Dot(image[imagePos].XYZ, Const.ConvertToGrayScaleFloat3), 0.0F, 1.0F);
                    break;
            }
            if (invert)
            {
                value = 1.0F - value;
            }

            dotSizes[pos] = value * maxDotSize;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct HalftoneDrawDotProcess(ReadWriteBuffer<Float4> image, int width, ReadWriteBuffer<float> dotSizes, int colCount, int rowCount, float gridSize, Float4 backgroundColor, Float4 dotColor) : IComputeShader
    {
        readonly Float2 GridCenter = new Float2(gridSize * 0.5F, gridSize * 0.5F);

        public void Execute()
        {
            var gridPos = (Float2)ThreadIds.XY / gridSize;
            var gridIndex = (Int2)gridPos;
            var gridLocalPos = gridPos - Hlsl.Trunc(gridPos);
            var dotPos = gridLocalPos * gridSize - GridCenter;

            var rate = Hlsl.Clamp(dotSizes[gridIndex.Y * colCount + gridIndex.X] - Hlsl.Length(dotPos), 0.0F, 1.0F);
            if (rate < 1.0F && gridIndex.X > 0)
            {
                dotPos = (gridLocalPos + new Float2(1.0F, 0.0F)) * gridSize - GridCenter;
                rate += Hlsl.Clamp(dotSizes[gridIndex.Y * colCount + gridIndex.X - 1] - Hlsl.Length(dotPos), 0.0F, 1.0F);
            }
            if (rate < 1.0F && gridIndex.X < colCount - 1)
            {
                dotPos = (gridLocalPos - new Float2(1.0F, 0.0F)) * gridSize - GridCenter;
                rate += Hlsl.Clamp(dotSizes[gridIndex.Y * colCount + gridIndex.X + 1] - Hlsl.Length(dotPos), 0.0F, 1.0F);
            }
            if (rate < 1.0F && gridIndex.Y > 0)
            {
                dotPos = (gridLocalPos + new Float2(0.0F, 1.0F)) * gridSize - GridCenter;
                rate += Hlsl.Clamp(dotSizes[(gridIndex.Y - 1) * colCount + gridIndex.X] - Hlsl.Length(dotPos), 0.0F, 1.0F);
            }
            if (rate < 1.0F && gridIndex.Y < rowCount - 1)
            {
                dotPos = (gridLocalPos - new Float2(0.0F, 1.0F)) * gridSize - GridCenter;
                rate += Hlsl.Clamp(dotSizes[(gridIndex.Y + 1) * colCount + gridIndex.X] - Hlsl.Length(dotPos), 0.0F, 1.0F);
            }

            image[ThreadIds.Y * width + ThreadIds.X] = Hlsl.Lerp(backgroundColor, dotColor, Hlsl.Clamp(rate, 0.0F, 1.0F));
        }
    }
}
