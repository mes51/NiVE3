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
using NiVE3.Image.Drawing;
using NiVE3.Numerics;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;
using static Vanara.PInvoke.Kernel32;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_ChromaticAberration_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_ChromaticAberration_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class ChromaticAberration : IEffect
    {
        const string ID = "C18EE3FE-F8E2-41B7-9B63-D703DAEC4FDE";

        const string PropertyChannelId = nameof(PropertyChannelId);

        const string PropertyTransformGroupId = nameof(PropertyTransformGroupId);

        const string PropertyTransformAnchorPointId = nameof(PropertyTransformAnchorPointId);

        const string PropertyTransformPositionId = nameof(PropertyTransformPositionId);

        const string PropertyTransformScaleId = nameof(PropertyTransformScaleId);

        const string PropertyTransformAngleId = nameof(PropertyTransformAngleId);

        const string PropertyDistortionGroupId = nameof(PropertyDistortionGroupId);

        const string PropertyDistortionDistortionId = nameof(PropertyDistortionDistortionId);

        const string PropertyDistortionChromaDistortionId = nameof(PropertyDistortionChromaDistortionId);

        const string PropertyIsMirrorEdge = nameof(PropertyIsMirrorEdge);

        const float PowerRate = 0.01F;

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new EnumProperty(PropertyChannelId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Channel, typeof(ChromaticAberrationChannelType), typeof(LanguageResourceDictionary), ChromaticAberrationChannelType.RedAndBlue, selectBoxWidth: 90.0),
                new PropertyGroup(PropertyTransformGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Transform_Group,
                [
                    new Vector3dProperty(PropertyTransformAnchorPointId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Transform_AnchorPoint, new Vector3d(sourceSize.Width * 0.5, sourceSize.Height * 0.5, 0.0)),
                    new DoubleProperty(PropertyTransformPositionId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Transform_Position, 0.5, double.MinValue, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyTransformScaleId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Transform_Scale, 100.0, double.MinValue, double.MaxValue, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new AngleProperty(PropertyTransformAngleId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Transform_Angle, 0.0, digit: 2)
                ]),
                new PropertyGroup(PropertyDistortionGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Distortion_Group,
                [
                    new DoubleProperty(PropertyDistortionDistortionId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Distortion_Distortion, 0.0, double.MinValue, double.MaxValue, digit: 2),
                    new DoubleProperty(PropertyDistortionChromaDistortionId, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_Distortion_ChromaDistortion, 0.0, double.MinValue, double.MaxValue, digit: 2)
                ]),
                new CheckBoxProperty(PropertyIsMirrorEdge, LanguageResourceDictionary.ResourceKeys.Stylize_ChromaticAberration_IsMirrorEdge, true)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var channel = properties.GetValue(PropertyChannelId, layerTime, ChromaticAberrationChannelType.RedAndBlue);
            var isMirrorEdge = properties.GetValue(PropertyIsMirrorEdge, layerTime, false);

            var transformGroup = properties.FirstOrDefault(p => p.Id == PropertyTransformGroupId)?.GetChildren() ?? [];
            var anchorPoint = ((Vector3)transformGroup.GetValue(PropertyTransformAnchorPointId, layerTime, Vector3d.Zero)).AsVector2();
            var position = (float)transformGroup.GetValue(PropertyTransformPositionId, layerTime, 0.0) * 0.01F;
            var angle = (float)transformGroup.GetValue(PropertyTransformAngleId, layerTime, 0.0);
            var scale = 1.0F - (float)transformGroup.GetValue(PropertyTransformScaleId, layerTime, 0.0) * 0.01F;

            var distortionGroup = properties.FirstOrDefault(p => p.Id == PropertyDistortionGroupId)?.GetChildren() ?? [];
            var distortion = (float)distortionGroup.GetValue(PropertyDistortionDistortionId, layerTime, 0.0) * PowerRate;
            var chromaDistortion = (float)distortionGroup.GetValue(PropertyDistortionChromaDistortionId, layerTime, 0.0) * PowerRate;

            if (position == 0.0F && angle == 0.0F && scale == 0.0F && distortion == 0.0F && chromaDistortion == 0.0)
            {
                return image;
            }

            var frontTransform = Matrix3x3.AffineTransform(anchorPoint, new Vector2(1.0F - scale), -angle, anchorPoint + new Vector2(image.Width, image.Height) * position);
            var backTransform = Matrix3x3.AffineTransform(anchorPoint, new Vector2(1.0F + scale), angle, anchorPoint + new Vector2(image.Width, image.Height) * -position);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, channel, anchorPoint, frontTransform, backTransform, distortion, chromaDistortion, isMirrorEdge);
            }
            else
            {
                return ProcessCpu(image, roi, channel, anchorPoint, frontTransform, backTransform, distortion, chromaDistortion, isMirrorEdge);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, ChromaticAberrationChannelType channel, Vector2 anchorPoint, in Matrix3x3 frontTransform, in Matrix3x3 backTransform, float distortion, float chromaDistortion, bool isMirrorEdge)
        {
            var managedImage = image.ToManaged();

            using var sourceImage = (NManagedImage)managedImage.Copy();

            FillBaseColor(managedImage, sourceImage, roi, channel, anchorPoint, distortion, isMirrorEdge);

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;
            var imageSize = new Vector2(imageWidth, imageHeight);

            // back
            if (!Matrix3x3.Invert(backTransform, out var invertBackTransform) || invertBackTransform.IsIdentity)
            {
                if (channel == ChromaticAberrationChannelType.RedAndBlue || channel == ChromaticAberrationChannelType.RedAndGreen)
                {
                    DrawAberrationColor(managedImage, sourceImage, roi, 2, anchorPoint, distortion - chromaDistortion, isMirrorEdge);
                }
                else if (channel == ChromaticAberrationChannelType.GreenAndBlue)
                {
                    DrawAberrationColor(managedImage, sourceImage, roi, 1, anchorPoint, distortion - chromaDistortion, isMirrorEdge);
                }
            }
            else
            {
                if (channel == ChromaticAberrationChannelType.RedAndBlue || channel == ChromaticAberrationChannelType.RedAndGreen)
                {
                    DrawAberrationColorWithTransform(managedImage, sourceImage, roi, 2, anchorPoint, invertBackTransform, distortion - chromaDistortion, isMirrorEdge);
                }
                else if (channel == ChromaticAberrationChannelType.GreenAndBlue)
                {
                    DrawAberrationColorWithTransform(managedImage, sourceImage, roi, 1, anchorPoint, invertBackTransform, distortion - chromaDistortion, isMirrorEdge);
                }
            }

            // front
            if (!Matrix3x3.Invert(frontTransform, out var invertFrontTransform) || invertFrontTransform.IsIdentity)
            {
                if (channel == ChromaticAberrationChannelType.RedAndBlue || channel == ChromaticAberrationChannelType.GreenAndBlue)
                {
                    DrawAberrationColor(managedImage, sourceImage, roi, 0, anchorPoint, distortion + chromaDistortion, isMirrorEdge);
                }
                else if (channel == ChromaticAberrationChannelType.RedAndGreen)
                {
                    DrawAberrationColor(managedImage, sourceImage, roi, 1, anchorPoint, distortion + chromaDistortion, isMirrorEdge);
                }
            }
            else
            {
                if (channel == ChromaticAberrationChannelType.RedAndBlue || channel == ChromaticAberrationChannelType.GreenAndBlue)
                {
                    DrawAberrationColorWithTransform(managedImage, sourceImage, roi, 0, anchorPoint, invertFrontTransform, distortion + chromaDistortion, isMirrorEdge);
                }
                else if (channel == ChromaticAberrationChannelType.RedAndGreen)
                {
                    DrawAberrationColorWithTransform(managedImage, sourceImage, roi, 1, anchorPoint, invertFrontTransform, distortion + chromaDistortion, isMirrorEdge);
                }
            }

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, ChromaticAberrationChannelType channel, Vector2 anchorPoint, in Matrix3x3 frontTransform, in Matrix3x3 backTransform, float distortion, float chromaDistortion, bool isMirrorEdge)
        {
            var gpuImage = image.ToGpu(device);

            var center = anchorPoint / new Vector2(gpuImage.Width, gpuImage.Height);
            var (baseChannel, backChannel, frontChannel) = channel switch
            {
                ChromaticAberrationChannelType.RedAndGreen => (0, 2, 1),
                ChromaticAberrationChannelType.GreenAndBlue => (2, 1, 0),
                _ => (1, 2, 0)
            };

            if (!Matrix3x3.Invert(backTransform, out var invertBackTransform))
            {
                invertBackTransform = Matrix3x3.Identity;
            }
            if (!Matrix3x3.Invert(frontTransform, out var invertFrontTransform))
            {
                invertFrontTransform = Matrix3x3.Identity;
            }

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            using var context = device.CreateComputeContext();
            context.For(roi.Width, roi.Height, new ChromaticAberrationFillBaseColorProcess(gpuImage.Data, sourceImage.Data, gpuImage.Width, gpuImage.Height, baseChannel, center, distortion, isMirrorEdge, roi.Left, roi.Top));

            context.Barrier(gpuImage.Data);
            context.For(roi.Width, roi.Height, new ChromaticAberrationDrawAberrationColorProcess(gpuImage.Data, sourceImage.Data, gpuImage.Width, gpuImage.Height, backChannel, center, invertBackTransform.ToFloat3x3(), distortion - chromaDistortion, isMirrorEdge, roi.Left, roi.Top));

            context.Barrier(gpuImage.Data);
            context.For(roi.Width, roi.Height, new ChromaticAberrationDrawAberrationColorProcess(gpuImage.Data, sourceImage.Data, gpuImage.Width, gpuImage.Height, frontChannel, center, invertFrontTransform.ToFloat3x3(), distortion + chromaDistortion, isMirrorEdge, roi.Left, roi.Top));

            return gpuImage;
        }

        static void FillBaseColor(NManagedImage target, NManagedImage source, ROI roi, ChromaticAberrationChannelType channel, Vector2 anchorPoint, float distortion, bool isMirrorEdge)
        {
            var imageWidth = target.Width;
            var imageHeight = target.Height;
            var imageData = target.Data;
            var sourceImageData = source.Data;
            var imageSize = new Vector2(imageWidth, imageHeight);
            if (distortion == 0.0F)
            {
                switch (channel)
                {
                    case ChromaticAberrationChannelType.RedAndGreen:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var color = imageDataSpan[x];
                                color.Y = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case ChromaticAberrationChannelType.GreenAndBlue:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var color = imageDataSpan[x];
                                color.X = 0.0F;
                                color.Y = 0.0F;
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    default:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var color = imageDataSpan[x];
                                color.X = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                }
            }
            else
            {
                var center = anchorPoint / imageSize;
                var repeatMode = isMirrorEdge ? BilinearEdgeMode.Mirror : BilinearEdgeMode.None;
                switch (channel)
                {
                    case ChromaticAberrationChannelType.RedAndGreen:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var sourcePos = CalcFishEye(x, y, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.Y = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    case ChromaticAberrationChannelType.GreenAndBlue:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var sourcePos = CalcFishEye(x, y, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Y = 0.0F;
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                    default:
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var sourcePos = CalcFishEye(x, y, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = color;
                            }
                        });
                        break;
                }
            }
        }

        static void DrawAberrationColorWithTransform(NManagedImage target, NManagedImage source, ROI roi, int channel, Vector2 anchorPoint, Matrix3x3 invertedMatrix, float distortion, bool isMirrorEdge)
        {
            var imageWidth = target.Width;
            var imageHeight = target.Height;
            var imageData = target.Data;
            var sourceImageData = source.Data;
            var imageSize = new Vector2(imageWidth, imageHeight);
            var repeatMode = isMirrorEdge ? BilinearEdgeMode.Mirror : BilinearEdgeMode.None;
            if (distortion == 0.0F)
            {
                switch (channel)
                {
                    case 0: // B
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {   
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var (sx, sy) = invertedMatrix.Transform(x, y);
                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, repeatMode);
                                color.Y = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 1: // G
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var (sx, sy) = invertedMatrix.Transform(x, y);
                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 2: // R
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var (sx, sy) = invertedMatrix.Transform(x, y);
                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sx, sy, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Y = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                }
            }
            else
            {
                var center = anchorPoint / imageSize;
                switch (channel)
                {
                    case 0: // B
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var (sx, sy) = invertedMatrix.Transform(x, y);
                                var sourcePos = CalcFishEye(sx, sy, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.Y = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 1: // G
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var (sx, sy) = invertedMatrix.Transform(x, y);
                                var sourcePos = CalcFishEye(sx, sy, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 2: // R
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var (sx, sy) = invertedMatrix.Transform(x, y);
                                var sourcePos = CalcFishEye(sx, sy, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Y = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                }
            }
        }

        static void DrawAberrationColor(NManagedImage target, NManagedImage source, ROI roi, int channel, Vector2 anchorPoint, float distortion, bool isMirrorEdge)
        {
            var imageWidth = target.Width;
            var imageHeight = target.Height;
            var imageData = target.Data;
            var sourceImageData = source.Data;
            var imageSize = new Vector2(imageWidth, imageHeight);
            if (distortion == 0.0F)
            {
                switch (channel)
                {
                    case 0: // B
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceImageDataSpan = sourceImageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var color = sourceImageDataSpan[x];
                                color.Y = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 1: // G
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceImageDataSpan = sourceImageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var color = sourceImageDataSpan[x];
                                color.X = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 2: // R
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            var sourceImageDataSpan = sourceImageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var color = sourceImageDataSpan[x];
                                color.X = 0.0F;
                                color.Y = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                }
            }
            else
            {
                var center = anchorPoint / imageSize;
                var repeatMode = isMirrorEdge ? BilinearEdgeMode.Mirror : BilinearEdgeMode.None;
                switch (channel)
                {
                    case 0: // B
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var sourcePos = CalcFishEye(x, y, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.Y = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 1: // G
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var sourcePos = CalcFishEye(x, y, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Z = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                    case 2: // R
                        Parallel.For(roi.Top, roi.Bottom, y =>
                        {
                            var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                            for (var x = roi.Left; x < roi.Right; x++)
                            {
                                var sourcePos = CalcFishEye(x, y, center, imageSize, distortion, isMirrorEdge);

                                var color = ImageInterpolation.Bilinear(sourceImageData, imageWidth, imageHeight, sourcePos.X, sourcePos.Y, Const.EmptyPixel, repeatMode);
                                color.X = 0.0F;
                                color.Y = 0.0F;
                                imageDataSpan[x] = Blend.Process(BlendMode.Add, imageDataSpan[x], color);
                            }
                        });
                        break;
                }
            }
        }

        // SEE: https://www.shadertoy.com/view/MtcXDH
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2 CalcFishEye(float x, float y, Vector2 center, Vector2 imageSize, float distortion, bool isMirrorEdge)
        {
            var normalizedPos = new Vector2(x, y) / imageSize;
            var m = normalizedPos - center;
            var distortedPos = (center + m / MathF.Sqrt(1.0F - distortion * Vector2.Dot(m, m))) * imageSize;
            if (isMirrorEdge)
            {
                if (float.IsNaN(distortedPos.X) || float.IsInfinity(distortedPos.X))
                {
                    distortedPos.X = 0.0F;
                }
                if (float.IsNaN(distortedPos.Y) || float.IsInfinity(distortedPos.Y))
                {
                    distortedPos.Y = 0.0F;
                }
            }

            return distortedPos;
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ChromaticAberrationFillBaseColorProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, int channel, Float2 center, float distortion, bool isMirrorEdge, int startX, int startY) : IComputeShader
    {
        readonly Float2 ImageSize = new Float2(width, height);

        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = Float4.Zero;
            if (distortion == 0.0F)
            {
                color = originalImage[pos];
            }
            else
            {
                var normalizedPos = new Float2(ThreadIds.X + startX, ThreadIds.Y + startY) / ImageSize;
                var m = normalizedPos - center;
                var sourcePos = (center + m / Hlsl.Sqrt(1.0F - distortion * Hlsl.Dot(m, m))) * ImageSize;
                if (isMirrorEdge)
                {
                    var isNan = Hlsl.IsNaN(sourcePos);
                    var isInfinite = Hlsl.IsInfinite(sourcePos);
                    if (isNan.X || isInfinite.X)
                    {
                        sourcePos.X = 0.0F;
                    }
                    if (isNan.Y || isInfinite.Y)
                    {
                        sourcePos.Y = 0.0F;
                    }
                }

                if (isMirrorEdge)
                {
                    color = OriginalImageBilinearMirror(sourcePos.X, sourcePos.Y);
                }
                else
                {
                    color = OriginalImageBilinear(sourcePos.X, sourcePos.Y);
                }
            }

            for (var i = 0; i < 3; i++)
            {
                if (i != channel)
                {
                    color[i] = 0.0F;
                }
            }
            image[pos] = color;
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c2 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c3 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c4 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
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
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 OriginalImageBilinearMirror(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix == x && iy == y)
            {
                return originalImage[CoordWrapGpu.Mirror(iy, height) * width + CoordWrapGpu.Mirror(ix, width)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = originalImage[CoordWrapGpu.Mirror(iy, height) * width + CoordWrapGpu.Mirror(ix, width)];
            var c2 = originalImage[CoordWrapGpu.Mirror(iy, height) * width + CoordWrapGpu.Mirror(ix + 1, width)];
            var c3 = originalImage[CoordWrapGpu.Mirror(iy + 1, height) * width + CoordWrapGpu.Mirror(ix, width)];
            var c4 = originalImage[CoordWrapGpu.Mirror(iy + 1, height) * width + CoordWrapGpu.Mirror(ix + 1, width)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
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
    readonly partial struct ChromaticAberrationDrawAberrationColorProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> originalImage, int width, int height, int channel, Float2 center, Float3x3 matrix, float distortion, bool isMirrorEdge, int startX, int startY) : IComputeShader
    {
        readonly Float2 ImageSize = new Float2(width, height);

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var color = Float4.Zero;
            var sourcePos = Hlsl.Mul(matrix, new Float3(x, y, 1.0F)).XY;
            if (distortion != 0.0F)
            {
                var normalizedPos = sourcePos / ImageSize;
                var m = normalizedPos - center;
                sourcePos = (center + m / Hlsl.Sqrt(1.0F - distortion * Hlsl.Dot(m, m))) * ImageSize;
                if (isMirrorEdge)
                {
                    var isNan = Hlsl.IsNaN(sourcePos);
                    var isInfinite = Hlsl.IsInfinite(sourcePos);
                    if (isNan.X || isInfinite.X)
                    {
                        sourcePos.X = 0.0F;
                    }
                    if (isNan.Y || isInfinite.Y)
                    {
                        sourcePos.Y = 0.0F;
                    }
                }
            }

            if (isMirrorEdge)
            {
                color = OriginalImageBilinearMirror(sourcePos.X, sourcePos.Y);
            }
            else
            {
                color = OriginalImageBilinear(sourcePos.X, sourcePos.Y);
            }

            for (var i = 0; i < 3; i++)
            {
                if (i != channel)
                {
                    color[i] = 0.0F;
                }
            }
            image[pos] = BlendMethods.Process(2, image[pos], color);
        }

        Float4 OriginalImageBilinear(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < width && iy < height)
                {
                    return originalImage[iy * width + ix];
                }
                else
                {
                    return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
                }
            }
            else if (ix < -1 || iy < -1 || ix >= width || iy >= height)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = width - 1;
            var mh = height - 1;

            var c1 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c2 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c3 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            var c4 = new Float4(1.0F, 1.0F, 1.0F, 0.0F);
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
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }

        Float4 OriginalImageBilinearMirror(float x, float y)
        {
            var ix = (int)x;
            var iy = (int)y;

            if (ix == x && iy == y)
            {
                return originalImage[CoordWrapGpu.Mirror(iy, height) * width + CoordWrapGpu.Mirror(ix, width)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = originalImage[CoordWrapGpu.Mirror(iy, height) * width + CoordWrapGpu.Mirror(ix, width)];
            var c2 = originalImage[CoordWrapGpu.Mirror(iy, height) * width + CoordWrapGpu.Mirror(ix + 1, width)];
            var c3 = originalImage[CoordWrapGpu.Mirror(iy + 1, height) * width + CoordWrapGpu.Mirror(ix, width)];
            var c4 = originalImage[CoordWrapGpu.Mirror(iy + 1, height) * width + CoordWrapGpu.Mirror(ix + 1, width)];

            var ta = Hlsl.Lerp(Hlsl.Lerp(c1, c3, qq), Hlsl.Lerp(c2, c4, qq), pp).W;
            if (ta <= 0.0F)
            {
                return new Float4(1.0F, 1.0F, 1.0F, 0.0F);
            }
            else
            {
                var t = Hlsl.Lerp(Hlsl.Lerp(c1 * c1.W, c3 * c3.W, qq), Hlsl.Lerp(c2 * c2.W, c4 * c4.W, qq), pp) / ta;
                t.W = ta;
                return t;
            }
        }
    }

    enum ChromaticAberrationChannelType
    {
        RedAndBlue,
        RedAndGreen,
        GreenAndBlue
    }
}
