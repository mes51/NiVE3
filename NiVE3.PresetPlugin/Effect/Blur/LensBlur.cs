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
using NiVE3.Image.Color;
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
using NiVE3.PresetPlugin.Internal.ComputeShader;
using NiVE3.PresetPlugin.Internal.Drawing;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shape;
using NiVE3.Shared.Extension;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using Polygon = NiVE3.Shape.Polygon;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_LensBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_LensBlur_Description, ID, IsRenderEveryFrame = true, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class LensBlur : IEffect
    {
        internal const float GainMultiplyer = 3.0F;

        internal const float MaxSaturationSubtract = 0.9F;

        internal const float PowerBase = 1.2F;

        internal const float LogPowerBase = 0.1823215567939546F; // Math.Log(PowerBase)

        const int DepthMappedIrisCount = 256;

        const string ID = "5F36FE53-8F98-48F5-A3AC-95C6333D3A5F";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyIrisGroupId = nameof(PropertyIrisGroupId);

        const string PropertyIrisTypeId = nameof(PropertyIrisTypeId);

        const string PropertyIrisCornerRoundId = nameof(PropertyIrisCornerRoundId);

        const string PropertyIrisUseLayerMaskId = nameof(PropertyIrisUseLayerMaskId);

        const string PropertyIrisTargetLayerMaskId = nameof(PropertyIrisTargetLayerMaskId);

        const string PropertyIrisAngleId = nameof(PropertyIrisAngleId);

        const string PropertyDepthMapGroupId = nameof(PropertyDepthMapGroupId);

        const string PropertyDepthMapSourceLayerId = nameof(PropertyDepthMapSourceLayerId);

        const string PropertyDepthMapSourceChannelTypeId = nameof(PropertyDepthMapSourceChannelTypeId);

        const string PropertyDepthMapSourceLayerPositionId = nameof(PropertyDepthMapSourceLayerPositionId);

        const string PropertyDepthMapInvertId = nameof(PropertyDepthMapInvertId);

        const string PropertyDepthMapFocusId = nameof(PropertyDepthMapFocusId);

        const string PropertyHighlightGroupId = nameof(PropertyHighlightGroupId);

        const string PropertyHighlightGainId = nameof(PropertyHighlightGainId);

        const string PropertyHighlightThresholdId = nameof(PropertyHighlightThresholdId);

        const string PropertyHighlightSaturationId = nameof(PropertyHighlightSaturationId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        static readonly IrisMask FocusedIrisMask = new IrisMask(new ManagedRasterizedMaskImage(1, 1, 1.0F), 1, 1);

        IAcceleratorObject? AcceleratorObject { get; set; }

        IrisMask[] LastIrisMasks { get; set; } = [];

        IrisMaskParameters? LastIrisParameters { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);

                var expandX = (int)Math.Ceiling(amount / downSamplingRateX);
                var expandY = (int)Math.Ceiling(amount / downSamplingRateY);
                return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
            }
            else
            {
                return baseRoi;
            }
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Amount, 5.0, 0.0, 200.0, slideChangeValue: 0.1, digit: 2),
                new PropertyGroup(PropertyIrisGroupId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris,
                [
                    new EnumProperty(PropertyIrisTypeId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_Type, typeof(LensBlurIrisType), typeof(LanguageResourceDictionary), LensBlurIrisType.Hexagon, selectBoxWidth: 90.0),
                    new DoubleProperty(PropertyIrisCornerRoundId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_CornerRound, 0.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                    new AngleProperty(PropertyIrisAngleId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_Angle, 15.0, digit: 2),
                    new CheckBoxProperty(PropertyIrisUseLayerMaskId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_UseLayerMask, false),
                    new UseMaskPathProperty(PropertyIrisTargetLayerMaskId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Iris_TargetLayerMask, selectBoxWidth: 90.0)
                ]),
                new PropertyGroup(PropertyDepthMapGroupId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_DepthMap,
                [
                    new UseLayerImageProperty(PropertyDepthMapSourceLayerId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_DepthMap_SourceLayer, 90.0),
                    new EnumProperty(PropertyDepthMapSourceChannelTypeId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_DepthMap_SourceChannelType, typeof(LuminanceAndSingleChannelType), typeof(LanguageResourceDictionary), LuminanceAndSingleChannelType.Luminance, selectBoxWidth: 90.0),
                    new EnumProperty(PropertyDepthMapSourceLayerPositionId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_DepthMap_SourceLayerPosition, typeof(SourceLayerPositionType), typeof(LanguageResourceDictionary), SourceLayerPositionType.Center, selectBoxWidth: 90.0),
                    new CheckBoxProperty(PropertyDepthMapInvertId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_DepthMap_Invert, false),
                    new DoubleProperty(PropertyDepthMapFocusId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_DepthMap_Focus, 0.5, 0.0, 1.0, slideChangeValue: 0.01, digit: 2)
                ]),
                new PropertyGroup(PropertyHighlightGroupId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight,
                [
                    new DoubleProperty(PropertyHighlightGainId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight_Gain, 0.0, 0.0, 100.0, digit: 2),
                    new DoubleProperty(PropertyHighlightThresholdId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight_Threshold, 0.7, double.MinValue, double.MaxValue, slideChangeValue: 0.01, digit: 2),
                    new DoubleProperty(PropertyHighlightSaturationId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_Highlight_Saturation, 100.0, 0.0, 100.0, digit: 2)
                ]),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_LensBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var amount = (float)(properties.GetValue(PropertyAmountId, layerTime, 0.0) / downSamplingRateX);
            var irisGroup = properties.First(p => p.Id == PropertyIrisGroupId).GetChildren() ?? [];
            var irisType = irisGroup.GetValue(PropertyIrisTypeId, layerTime, LensBlurIrisType.Hexagon);
            var irisCornerRound = (float)(irisGroup.GetValue(PropertyIrisCornerRoundId, layerTime, 0.0) * 0.01);
            var irisUseLayerMask = irisGroup.GetValue(PropertyIrisUseLayerMaskId, layerTime, false);
            var irisTargetLayerMask = irisGroup.GetValue(PropertyIrisTargetLayerMaskId, layerTime, UseMaskPathTarget.Empty);
            var irisAngle = (float)irisGroup.GetValue(PropertyIrisAngleId, layerTime, 0.0);
            var depthMapGroup = properties.First(p => p.Id == PropertyDepthMapGroupId).GetChildren() ?? [];
            var depthMapSourceLayerId = depthMapGroup.GetValue(PropertyDepthMapSourceLayerId, layerTime, UseLayerImageTarget.Empty);
            var depthMapSourceChannelType = depthMapGroup.GetValue(PropertyDepthMapSourceChannelTypeId, layerTime, LuminanceAndSingleChannelType.Luminance);
            var depthMapSourceLayerPosition = depthMapGroup.GetValue(PropertyDepthMapSourceLayerPositionId, layerTime, SourceLayerPositionType.Center);
            var depthMapInvert = depthMapGroup.GetValue(PropertyDepthMapInvertId, layerTime, false);
            var depthMapFocus = (float)depthMapGroup.GetValue(PropertyDepthMapFocusId, layerTime, 0.0);
            var highlightGroup = properties.First(p => p.Id == PropertyHighlightGroupId).GetChildren() ?? [];
            var highlightGain = (float)(highlightGroup.GetValue(PropertyHighlightGainId, layerTime, 0.0) * 0.01);
            var highlightThreshold = (float)highlightGroup.GetValue(PropertyHighlightThresholdId, layerTime, 0.0);
            var highlightSaturationSubtract = 1.0F - (float)(highlightGroup.GetValue(PropertyHighlightSaturationId, layerTime, 0.0) * 0.01);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (amount <= 0.0F && highlightGain <= 0.0F)
            {
                return image;
            }
            else if (amount <= 0.0F)
            {
                if (useGpu && AcceleratorObject != null)
                {
                    return ProcessHighlightOnlyGpu(AcceleratorObject.CurrentDevice, image, roi, highlightGain, highlightThreshold, highlightSaturationSubtract);
                }
                else
                {
                    return ProcessHighlightOnlyCpu(image, roi, highlightGain, highlightThreshold, highlightSaturationSubtract);
                }
            }

            using var depthMapImage = depthMapSourceLayerId.GetImage(composition, layerTime + layer.SourceStartPoint, downSamplingRateX, useGpu);
            var irisMaskParameters = new IrisMaskParameters(amount, irisType, irisCornerRound, irisAngle, depthMapImage != null, depthMapFocus);

            var irisMasks = LastIrisMasks;
            var layerMaskPath = irisUseLayerMask ? irisTargetLayerMask.GetMask(layer, layerTime, downSamplingRateX) : null;
            if (layerMaskPath != null || irisMasks.Length < 1 || irisMaskParameters != LastIrisParameters)
            {
                foreach (var i in LastIrisMasks)
                {
                    if (i != FocusedIrisMask)
                    {
                        i.Dispose();
                    }
                }

                if (layerMaskPath != null && layerMaskPath.IsClosed && !layerMaskPath.IsEmpty())
                {
                    if (depthMapImage != null)
                    {
                        irisMasks = new IrisMask[DepthMappedIrisCount];
                        for (var i = 0; i < irisMasks.Length; i++)
                        {
                            var currentAmount = Math.Abs(1.0F / (DepthMappedIrisCount - 1) * i - depthMapFocus) * amount;
                            if (currentAmount <= 0.0F)
                            {
                                irisMasks[i] = FocusedIrisMask;
                            }
                            else
                            {
                                irisMasks[i] = GenerateMaskByLayerNonEmptyMask(layerMaskPath, currentAmount);
                            }
                        }
                    }
                    else
                    {
                        irisMasks = [GenerateMaskByLayerNonEmptyMask(layerMaskPath, amount)];
                    }
                    LastIrisMasks = [];
                    LastIrisParameters = null;
                }
                else
                {
                    if (depthMapImage != null)
                    {
                        irisMasks = new IrisMask[DepthMappedIrisCount];
                        for (var i = 0; i < irisMasks.Length; i++)
                        {
                            var currentAmount = Math.Abs(1.0F / (DepthMappedIrisCount - 1) * i - depthMapFocus) * amount;
                            if (currentAmount <= 0.0F)
                            {
                                irisMasks[i] = FocusedIrisMask;
                            }
                            else
                            {
                                irisMasks[i] = GenerateIrisMask(irisType, currentAmount, irisCornerRound, irisAngle);
                            }
                        }
                    }
                    else
                    {
                        irisMasks = [GenerateIrisMask(irisType, amount, irisCornerRound, irisAngle)];
                    }
                    LastIrisMasks = irisMasks;
                    LastIrisParameters = irisMaskParameters;
                }
            }

            if (depthMapImage != null)
            {
                if (useGpu && AcceleratorObject != null)
                {
                    return ProcessWithDepthMapGpu(AcceleratorObject.CurrentDevice, image, roi, amount, irisMasks, depthMapImage, depthMapSourceChannelType, depthMapSourceLayerPosition, depthMapInvert, highlightGain, highlightThreshold, highlightSaturationSubtract, edgeRepeatMode);
                }
                else
                {
                    return ProcessWithDepthMapCpu(image, roi, amount, irisMasks, depthMapImage, depthMapSourceChannelType, depthMapSourceLayerPosition, depthMapInvert, highlightGain, highlightThreshold, highlightSaturationSubtract, edgeRepeatMode);
                }
            }
            else
            {
                if (useGpu && AcceleratorObject != null)
                {
                    return ProcessNonDepthMapGpu(AcceleratorObject.CurrentDevice, image, roi, amount, irisMasks[0], highlightGain, highlightThreshold, highlightSaturationSubtract, edgeRepeatMode);
                }
                else
                {
                    return ProcessNonDepthMapCpu(image, roi, amount, irisMasks[0], highlightGain, highlightThreshold, highlightSaturationSubtract, edgeRepeatMode);
                }
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessHighlightOnlyCpu(NImage image, ROI roi, float highlightGain, float highlightThreshold, float highlightSaturationSubtract)
        {
            var managedImage = image.ToManaged();

            var imageWidth = managedImage.Width;
            var imageData = managedImage.Data;

            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = imageDataSpan[x];
                    if (Vector4.Dot(color, Const.ConvertToGrayScale) >= highlightThreshold)
                    {
                        var hsl = Hsl.FromRgb(color);
                        hsl.Saturation *= 1.0F - highlightSaturationSubtract * MaxSaturationSubtract;

                        var gainColor = hsl.ToRgb();
                        gainColor *= GainMultiplyer;
                        gainColor.W = color.W;
                        imageDataSpan[x] = Vector4.Lerp(color, gainColor, highlightGain);
                    }
                }
            });
            return managedImage;
        }

        static NManagedImage ProcessNonDepthMapCpu(NImage image, ROI roi, float amount, IrisMask irisMask, float highlightGain, float highlightThreshold, float highlightSaturationSubtract, EdgeRepeatMode edgeRepeatMode)
        {
            var managedImage = image.ToManaged();

            using var sourceImage = GenerateGainAndPowerSourceImageCpu(managedImage, roi, amount, highlightGain, highlightThreshold, highlightSaturationSubtract);

            var imageWidth = managedImage.Width;
            int imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;
            var count = irisMask.SumCount;
            var maskRadius = irisMask.MaskRadius;
            var maskSize = irisMask.MaskSize;
            var mask = irisMask.Mask.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var color = Vector4.Zero;
                    for (int t = y - maskRadius, my = 0, mpos = 0; my < maskSize; t++, my++)
                    {
                        for (int l = x - maskRadius, mx = 0; mx < maskSize; l++, mx++, mpos++)
                        {
                            color += BlurUtil.GetPixel(sourceImageData, imageWidth, imageHeight, l, t, edgeRepeatMode) * mask[mpos];
                        }
                    }

                    if (color.W > 0.0F)
                    {
                        var ta = color.W;
                        color /= ta;
                        color.W = ta / count;
                        imageDataSpan[x] = (color.AsVector128().Log() / LogPowerBase).AsVector4();
                    }
                    else
                    {
                        imageDataSpan[x] = Const.EmptyPixel;
                    }
                }
            });

            return managedImage;
        }

        static NManagedImage ProcessWithDepthMapCpu(NImage image, ROI roi, float amount, IrisMask[] irisMasks, NImage depthMapImage, LuminanceAndSingleChannelType singleChannelType, SourceLayerPositionType sourceLayerPosition, bool depthMapInvert, float highlightGain, float highlightThreshold, float highlightSaturationSubtract, EdgeRepeatMode edgeRepeatMode)
        {
            var managedImage = image.ToManaged();
            var managedDepthMapImage = depthMapImage.ToManaged();
            using var sourceImage = GenerateGainAndPowerSourceImageCpu(managedImage, roi, amount, highlightGain, highlightThreshold, highlightSaturationSubtract);

            var (sourceStartX, sourceStartY) = sourceLayerPosition switch
            {
                SourceLayerPositionType.Stretch => (0.0F, 0.0F),
                _ => ((managedDepthMapImage.Width - managedImage.Width) * 0.5F, (managedDepthMapImage.Height - managedImage.Height) * 0.5F)
            };
            var (sourceDiffX, sourceDiffY) = sourceLayerPosition switch
            {
                SourceLayerPositionType.Stretch => ((managedDepthMapImage.Width - 1) / (float)(managedImage.Width - 1), (managedDepthMapImage.Height - 1) / (float)(managedImage.Height - 1)),
                _ => (1.0F, 1.0F)
            };

            var imageWidth = managedImage.Width;
            int imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var sourceImageData = sourceImage.Data;
            Parallel.For(roi.Top, roi.Bottom, y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var sourceImageDataSpan = sourceImageData.AsSpan(y * imageWidth, imageWidth);
                var depthMapDataSpan = managedDepthMapImage.GetDataSpan();
                var sourceX = sourceStartX + sourceDiffX * roi.Left;
                var sourceY = sourceStartY + sourceDiffY * y;

                for (var x = roi.Left; x < roi.Right; x++)
                {
                    var mapColor = sourceLayerPosition == SourceLayerPositionType.Loop ?
                        ImageInterpolation.BilinearLoop(depthMapDataSpan, depthMapImage.Width, depthMapImage.Height, sourceX, sourceY) :
                        ImageInterpolation.BilinearEdgeRepeat(depthMapDataSpan, depthMapImage.Width, depthMapImage.Height, sourceX, sourceY);
                    var depth = Math.Clamp(GetDepthMapValue(mapColor, singleChannelType), 0.0F, 1.0F);
                    if (depthMapInvert)
                    {
                        depth = 1.0F - depth;
                    }
                    var irisMask = irisMasks[(int)Math.Round(depth * (DepthMappedIrisCount - 1))];
                    if (irisMask != FocusedIrisMask)
                    {
                        var count = irisMask.SumCount;
                        var maskRadius = irisMask.MaskRadius;
                        var maskSize = irisMask.MaskSize;
                        var mask = irisMask.Mask.Data;

                        var color = Vector4.Zero;
                        for (int t = y - maskRadius, my = 0, mpos = 0; my < maskSize; t++, my++)
                        {
                            for (int l = x - maskRadius, mx = 0; mx < maskSize; l++, mx++, mpos++)
                            {
                                color += BlurUtil.GetPixel(sourceImageData, imageWidth, imageHeight, l, t, edgeRepeatMode) * mask[mpos];
                            }
                        }

                        if (color.W > 0.0F)
                        {
                            var ta = color.W;
                            color /= ta;
                            color.W = ta / count;
                            imageDataSpan[x] = (color.AsVector128().Log() / LogPowerBase).AsVector4();
                        }
                        else
                        {
                            imageDataSpan[x] = Const.EmptyPixel;
                        }
                    }
                    else
                    {
                        var color = sourceImageDataSpan[x];
                        if (color.W > 0.0F)
                        {
                            var ta = color.W;
                            color /= ta;
                            color.W = ta;
                            imageDataSpan[x] = (color.AsVector128().Log() / LogPowerBase).AsVector4();
                        }
                        else
                        {
                            imageDataSpan[x] = Const.EmptyPixel;
                        }
                    }
                }
            });

            if (managedDepthMapImage != depthMapImage)
            {
                managedDepthMapImage.Dispose();
            }

            return managedImage;
        }

        static NGPUImage ProcessHighlightOnlyGpu(GraphicsDevice device, NImage image, ROI roi, float highlightGain, float highlightThreshold, float highlightSaturationSubtract)
        {
            var gpuImage = image.ToGpu(device);

            device.For(roi.Width, roi.Height, new LensBlurGainOnlyProcess(gpuImage.Data, gpuImage.Width, highlightGain, highlightThreshold, highlightSaturationSubtract, roi.Left, roi.Top));

            return gpuImage;
        }

        static NGPUImage ProcessNonDepthMapGpu(GraphicsDevice device, NImage image, ROI roi, float amount, IrisMask irisMask, float highlightGain, float highlightThreshold, float highlightSaturationSubtract, EdgeRepeatMode edgeRepeatMode)
        {
            var gpuImage = image.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            using var maskBuffer = device.AllocateReadOnlyBuffer<float>(irisMask.Mask.GetDataSpan());

            using var context = device.CreateComputeContext();

            var gainROI = roi.Expand((int)Math.Ceiling(amount)).Intersect(0, 0, gpuImage.Width, gpuImage.Height);
            context.For(gainROI.Width, gainROI.Height, new LensBlurGainAndPowerProcess(sourceImage.Data, gpuImage.Width, highlightGain, highlightThreshold, highlightSaturationSubtract, roi.Left, roi.Top));
            context.Barrier(sourceImage.Data);

            context.For(roi.Width, roi.Height, new LensBlurBlurNonDepthMapProcess(sourceImage.Data, gpuImage.Data, gpuImage.Width, gpuImage.Height, (int)edgeRepeatMode, maskBuffer, irisMask.MaskRadius, irisMask.MaskSize, irisMask.SumCount, roi.Left, roi.Top));

            return gpuImage;
        }

        static NGPUImage ProcessWithDepthMapGpu(GraphicsDevice device, NImage image, ROI roi, float amount, IrisMask[] irisMasks, NImage depthMapImage, LuminanceAndSingleChannelType sourceChannelType, SourceLayerPositionType sourceLayerPosition, bool depthMapInvert, float highlightGain, float highlightThreshold, float highlightSaturationSubtract, EdgeRepeatMode edgeRepeatMode)
        {
            var gpuImage = image.ToGpu(device);
            var gpuDepthMapImage = depthMapImage.ToGpu(device);

            using var sourceImage = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            gpuImage.CopyTo(sourceImage);

            var maskInfo = new GPUIrisMaskInfo[irisMasks.Length];
            var totalLength = 0;
            for (var i = 0; i < maskInfo.Length; i++)
            {
                maskInfo[i] = new GPUIrisMaskInfo(totalLength, irisMasks[i] == FocusedIrisMask, irisMasks[i].MaskRadius, irisMasks[i].MaskSize, irisMasks[i].SumCount);
                totalLength += irisMasks[i].Mask.DataLength;
            }
            using var maskInfoBuffer = device.AllocateReadOnlyBuffer(maskInfo);

            using var maskBuffer = device.AllocateReadOnlyBuffer<float>(totalLength);
            var currentPosition = 0;
            for (var i = 0; i < irisMasks.Length; i++)
            {
                maskBuffer.CopyFrom(irisMasks[i].Mask.GetDataSpan(), currentPosition);
                currentPosition += irisMasks[i].Mask.DataLength;
            }

            using (var context = device.CreateComputeContext())
            {
                var gainROI = roi.Expand((int)Math.Ceiling(amount)).Intersect(0, 0, gpuImage.Width, gpuImage.Height);
                context.For(gainROI.Width, gainROI.Height, new LensBlurGainAndPowerProcess(sourceImage.Data, gpuImage.Width, highlightGain, highlightThreshold, highlightSaturationSubtract, roi.Left, roi.Top));
                context.Barrier(sourceImage.Data);

                context.For(
                    roi.Width,
                    roi.Height,
                    new LensBlurBlurWithDepthMapProcess(
                        sourceImage.Data,
                        gpuImage.Data,
                        gpuImage.Width,
                        gpuImage.Height,
                        gpuDepthMapImage.Data,
                        gpuDepthMapImage.Width,
                        gpuDepthMapImage.Height,
                        depthMapInvert,
                        (int)sourceChannelType,
                        (int)sourceLayerPosition,
                        (int)edgeRepeatMode,
                        maskBuffer,
                        maskInfoBuffer,
                        roi.Left,
                        roi.Top
                    )
                );
            }

            if (depthMapImage != gpuDepthMapImage)
            {
                gpuDepthMapImage.Dispose();
            }

            return gpuImage;
        }

        static IrisMask GenerateMaskByLayerNonEmptyMask(BezierPath layerMaskPath, float size)
        {
            size += 0.5F;
            var path = layerMaskPath.BuildPath() ?? EmptyPath.ClosedPath;
            if (path == EmptyPath.ClosedPath)
            {
                throw new Exception(); // bug
            }

            var originalBounds = path.Bounds;
            var resizeRate = size / Math.Max(originalBounds.Width, originalBounds.Height);
            var transform = Matrix3x2.CreateTranslation(-originalBounds.X - originalBounds.Width * 0.5F, -originalBounds.Y - originalBounds.Height * 0.5F)
                * Matrix3x2.CreateScale(resizeRate, resizeRate)
                * Matrix3x2.CreateRotation(MathF.PI);
            path = path.Transform(transform);

            var maskRadius = (int)MathF.Ceiling(Math.Max(path.Bounds.Width, path.Bounds.Height) * 0.5F);
            var maskSize = maskRadius * 2 + 1;
            var maskImage = new ManagedRasterizedMaskImage(maskSize, maskSize);
            path = path.Transform(Matrix3x2.CreateTranslation(-path.Bounds.X + (maskSize - path.Bounds.Width) * 0.5F, -path.Bounds.Y + (maskSize - path.Bounds.Height) * 0.5F));
            var polygons = path.Flatten().Select(p => new Polygon(p.Points.Span)).ToArray();
            ShapeMaskRendererCPU.Fill(polygons, maskImage, 1.0F);

            return new IrisMask(maskImage, maskRadius, maskSize);
        }

        static IrisMask GenerateIrisMask(LensBlurIrisType irisType, float size, float cornerRound, float angle)
        {
            var maskRadius = (int)MathF.Ceiling(size - 0.5F);
            var maskSize = maskRadius * 2 + 1;
            var maskImageCenter = maskSize * 0.5F;

            size += 0.5F;
            var path = (IPath)EmptyPath.ClosedPath;
            if (irisType == LensBlurIrisType.Circle)
            {
                path = new EllipsePolygon(new PointF(maskImageCenter, maskImageCenter), size);
            }
            else
            {
                var count = (int)irisType;
                var pointRad = Math.PI * 2.0 / count;
                var pathBuilder = new PathBuilder();

                pathBuilder.StartFigure();
                pathBuilder.MoveTo(new PointF(0.0F, -size));
                if (cornerRound == 0.0F)
                {
                    for (var i = 1; i < count; i++)
                    {
                        var rad = pointRad * i;
                        pathBuilder.LineTo((float)Math.Sin(rad) * size, (float)-Math.Cos(rad) * size);
                    }
                }
                else
                {
                    var k = (4.0 / 3.0) * Math.Tan(Math.PI / (count * 2)) * cornerRound * size;
                    for (var i = 1; i <= count; i++)
                    {
                        var prevRad = pointRad * (i - 1);
                        var rad = pointRad * i;
                        var sp = new PointF((float)Math.Sin(prevRad) * size, (float)-Math.Cos(prevRad) * size);
                        var ep = new PointF((float)Math.Sin(rad) * size, (float)-Math.Cos(rad) * size);
                        var c1 = sp + new PointF((float)(Math.Sin(prevRad + Math.PI * 0.5) * k), (float)(-Math.Cos(prevRad + Math.PI * 0.5) * k));
                        var c2 = ep + new PointF((float)(Math.Sin(rad - Math.PI * 0.5) * k), (float)(-Math.Cos(rad - Math.PI * 0.5) * k));
                        pathBuilder.AddCubicBezier(sp, c1, c2, ep);
                    }
                }
                pathBuilder.CloseFigure();

                path = pathBuilder.Build().Transform(Matrix3x2.CreateRotation((angle / 180.0F + 1.0F) * MathF.PI) * Matrix3x2.CreateTranslation(maskImageCenter, maskImageCenter));
            }
            var maskImage = new ManagedRasterizedMaskImage(maskSize, maskSize);
            var polygons = path.Flatten().Select(p => new Polygon(p.Points.Span)).ToArray();
            ShapeMaskRendererCPU.Fill(polygons, maskImage, 1.0F);

            return new IrisMask(maskImage, maskRadius, maskSize);
        }

        static NManagedImage GenerateGainAndPowerSourceImageCpu(NManagedImage image, ROI roi, float amount, float highlightGain, float highlightThreshold, float highlightSaturationSubtract)
        {
            var imageWidth = image.Width;
            var gainImage = (NManagedImage)image.Copy();
            var gainROI = roi.Expand((int)Math.Ceiling(amount)).Intersect(0, 0, image.Width, image.Height);
            var sourceImageData = gainImage.Data;
            if (highlightGain > 0.0F)
            {
                Parallel.For(gainROI.Top, gainROI.Bottom, y =>
                {
                    const byte MmInsert0To3 = 3 << 4;
                    var sourceImageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(sourceImageData.AsSpan(y * imageWidth, imageWidth));

                    for (var x = gainROI.Left; x < gainROI.Right; x++)
                    {
                        var color = sourceImageDataSpan[x];
                        if (Vector128.Dot(color, Const.ConvertToGrayScale128) >= highlightThreshold)
                        {
                            var a = color.GetElement(3);
                            var hsl = Hsl.FromRgb(color);
                            hsl.Saturation *= 1.0F - highlightSaturationSubtract * MaxSaturationSubtract;
                            var gainColor = Sse41.Insert(hsl.ToRgbVector128() * GainMultiplyer, Vector128.Create(a), MmInsert0To3);
                            color = (gainColor - color) * highlightGain + color;
                        }

                        color = Vector128.Create(PowerBase).Pow(color);
                        var pa = color.GetElement(3);
                        color *= pa;
                        sourceImageDataSpan[x] = Sse41.Insert(color, Vector128.Create(pa), MmInsert0To3);
                    }
                });
            }
            else
            {
                Parallel.For(gainROI.Top, gainROI.Bottom, y =>
                {
                    const byte MmInsert0To3 = 3 << 4;
                    var sourceImageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(sourceImageData.AsSpan(y * imageWidth, imageWidth));

                    for (var x = gainROI.Left; x < gainROI.Right; x++)
                    {
                        var color = Vector128.Create(PowerBase).Pow(sourceImageDataSpan[x]);
                        var pa = color.GetElement(3);
                        color *= pa;
                        sourceImageDataSpan[x] = Sse41.Insert(color, Vector128.Create(pa), MmInsert0To3);
                    }
                });
            }

            return gainImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetDepthMapValue(in Vector4 color, LuminanceAndSingleChannelType channel)
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
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LensBlurGainOnlyProcess(ReadWriteBuffer<Float4> image, int width, float gain, float threshold, float saturationSubtract, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            if (Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) >= threshold)
            {
                var hsl = ColorSpaceConversion.RgbToHsl(color);
                hsl.Y *= 1.0F - saturationSubtract * LensBlur.MaxSaturationSubtract;

                var gainColor = ColorSpaceConversion.HslToRgb(hsl);
                gainColor *= LensBlur.GainMultiplyer;
                gainColor.W = color.W;
                image[pos] = Hlsl.Lerp(color, gainColor, gain);
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LensBlurGainAndPowerProcess(ReadWriteBuffer<Float4> image, int width, float gain, float threshold, float saturationSubtract, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var pos = (ThreadIds.Y + startY) * width + ThreadIds.X + startX;

            var color = image[pos];
            if (Hlsl.Dot(color.XYZ, Const.ConvertToGrayScaleFloat3) >= threshold)
            {
                var hsl = ColorSpaceConversion.RgbToHsl(color);
                hsl.Y *= 1.0F - saturationSubtract * LensBlur.MaxSaturationSubtract;

                var gainColor = ColorSpaceConversion.HslToRgb(hsl);
                gainColor *= LensBlur.GainMultiplyer;
                gainColor.W = color.W;
                color = Hlsl.Lerp(color, gainColor, gain);
            }

            color = Hlsl.Pow(LensBlur.PowerBase, color);
            image[pos] = new Float4(color.XYZ * color.W, color.W);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LensBlurBlurNonDepthMapProcess(ReadWriteBuffer<Float4> image, ReadWriteBuffer<Float4> result, int width, int height, int edgeRepeatMode, ReadOnlyBuffer<float> mask, int maskRadius, int maskSize, float sumCount, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;

            var color = Float4.Zero;
            for (int t = y - maskRadius, my = 0, mpos = 0; my < maskSize; t++, my++)
            {
                for (int l = x - maskRadius, mx = 0; mx < maskSize; l++, mx++, mpos++)
                {
                    color += GetPixel(l, t) * mask[mpos];
                }
            }

            if (color.W > 0.0F)
            {
                result[pos] = Hlsl.Log(new Float4(color.XYZ / color.W, color.W / sumCount)) / LensBlur.LogPowerBase;
            }
            else
            {
                image[pos] = Const.EmptyPixelFloat4;
            }
        }

        Float4 GetPixel(int l, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[CoordWrapGpu.Wrap(t, height) * width + CoordWrapGpu.Wrap(l, width)];
                case 2:
                    return image[CoordWrapGpu.Repeat(t, height) * width + CoordWrapGpu.Repeat(l, width)];
                case 3:
                    return image[CoordWrapGpu.Mirror(t, height) * width + CoordWrapGpu.Mirror(l, width)];
                default:
                    if (l > -1 && l < width && t > -1 && t < height)
                    {
                        return image[t * width + l];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct LensBlurBlurWithDepthMapProcess(
        ReadWriteBuffer<Float4> image,
        ReadWriteBuffer<Float4> result,
        int width,
        int height,
        ReadWriteBuffer<Float4> depthMapImage,
        int depthMapImageWidth,
        int depthMapImageHeight,
        bool depthMapInvert,
        int sourceChannel,
        int sourceLayerPosition,
        int edgeRepeatMode,
        ReadOnlyBuffer<float> mask,
        ReadOnlyBuffer<GPUIrisMaskInfo> maskInfo,
        int startX,
        int startY
    ) : IComputeShader
    {
        readonly float SourceStartX = sourceLayerPosition == 1 ? 0.0F : (depthMapImageWidth - width) * 0.5F;

        readonly float SourceStartY = sourceLayerPosition == 1 ? 0.0F : (depthMapImageHeight - height) * 0.5F;

        readonly float SourceDiffX = sourceLayerPosition == 1 ? (depthMapImageWidth - 1) / (float)(width - 1) : 1.0F;

        readonly float SourceDiffY = sourceLayerPosition == 1 ? (depthMapImageHeight - 1) / (float)(height - 1) : 1.0F;

        public void Execute()
        {
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;
            var pos = y * width + x;
            var sourceX = SourceStartX + SourceDiffX * x;
            var sourceY = SourceStartY + SourceDiffY * y;

            var mapColor = Float4.Zero;
            if (sourceLayerPosition == 2)
            {
                mapColor = DepthMapImageBilinearLoop(sourceX, sourceY);
            }
            else
            {
                mapColor = DepthMapImageBilinear(sourceX, sourceY);
            }

            var depth = Hlsl.Clamp(GetDepthMapValue(mapColor), 0.0F, 1.0F);
            if (depthMapInvert)
            {
                depth = 1.0F - depth;
            }
            var targetMskInfo = maskInfo[(int)Hlsl.Round(depth * (maskInfo.Length - 1))];
            if (!targetMskInfo.IsFocused)
            {
                var maskRadius = targetMskInfo.MaskRadius;
                var maskSize = targetMskInfo.MaskSize;
                var sumCount = targetMskInfo.SumCount;
                var color = Float4.Zero;
                for (int t = y - maskRadius, my = 0, mpos = targetMskInfo.MaskDataPosition; my < maskSize; t++, my++)
                {
                    for (int l = x - maskRadius, mx = 0; mx < maskSize; l++, mx++, mpos++)
                    {
                        color += GetPixel(l, t) * mask[mpos];
                    }
                }

                if (color.W > 0.0F)
                {
                    result[pos] = Hlsl.Log(new Float4(color.XYZ / color.W, color.W / sumCount)) / LensBlur.LogPowerBase;
                }
                else
                {
                    image[pos] = Const.EmptyPixelFloat4;
                }
            }
            else
            {
                var color = image[y * width + x];
                if (color.W > 0.0F)
                {
                    result[pos] = Hlsl.Log(new Float4(color.XYZ / color.W, color.W)) / LensBlur.LogPowerBase;
                }
                else
                {
                    image[pos] = Const.EmptyPixelFloat4;
                }
            }
        }

        Float4 DepthMapImageBilinear(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                if (ix > -1 && iy > -1 && ix < depthMapImageWidth && iy < depthMapImageHeight)
                {
                    return depthMapImage[iy * depthMapImageWidth + ix];
                }
                else
                {
                    return 0.0F;
                }
            }
            else if (ix < -1 || iy < -1 || ix >= depthMapImageWidth || iy >= depthMapImageHeight)
            {
                return 0.0F;
            }

            var pp = x - ix;
            var qq = y - iy;
            var ip = 1.0F - pp;
            var iq = 1.0F - qq;
            var mw = depthMapImageWidth - 1;
            var mh = depthMapImageHeight - 1;

            Float4 c1;
            Float4 c2;
            Float4 c3;
            Float4 c4;
            var pos = iy * depthMapImageWidth + ix;

            if (ix > -1)
            {
                if (ix < mw)
                {
                    if (iy > -1)
                    {
                        c1 = depthMapImage[pos];
                        c2 = depthMapImage[pos + 1];
                        if (iy < mh)
                        {
                            pos += depthMapImageWidth;
                            c3 = depthMapImage[pos];
                            c4 = depthMapImage[pos + 1];
                        }
                        else
                        {
                            c3 = c1;
                            c4 = c2;
                        }
                    }
                    else
                    {
                        pos += depthMapImageWidth;
                        c1 = depthMapImage[pos];
                        c2 = depthMapImage[pos + 1];
                        c3 = c1;
                        c4 = c2;
                    }
                }
                else
                {
                    if (iy > -1)
                    {
                        c1 = depthMapImage[pos];
                        c2 = c1;
                        if (iy < mh)
                        {
                            c3 = depthMapImage[pos + depthMapImageWidth];
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
                        c3 = depthMapImage[pos + depthMapImageWidth];
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
                    c2 = depthMapImage[pos];
                    c1 = c2;
                    if (iy < mh)
                    {
                        c4 = depthMapImage[pos + depthMapImageWidth];
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
                    c4 = depthMapImage[pos + depthMapImageWidth];
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

        Float4 DepthMapImageBilinearLoop(float x, float y)
        {
            var ix = (int)Hlsl.Floor(x);
            var iy = (int)Hlsl.Floor(y);

            if (ix == x && iy == y)
            {
                return depthMapImage[CoordWrapGpu.Repeat(iy, depthMapImageHeight) * depthMapImageWidth + CoordWrapGpu.Repeat(ix, depthMapImageWidth)];
            }

            var pp = x - ix;
            var qq = y - iy;

            var c1 = depthMapImage[CoordWrapGpu.Repeat(iy, depthMapImageHeight) * depthMapImageWidth + CoordWrapGpu.Repeat(ix, depthMapImageWidth)];
            var c2 = depthMapImage[CoordWrapGpu.Repeat(iy, depthMapImageHeight) * depthMapImageWidth + CoordWrapGpu.Repeat(ix + 1, depthMapImageWidth)];
            var c3 = depthMapImage[CoordWrapGpu.Repeat(iy + 1, depthMapImageHeight) * depthMapImageWidth + CoordWrapGpu.Repeat(ix, depthMapImageWidth)];
            var c4 = depthMapImage[CoordWrapGpu.Repeat(iy + 1, depthMapImageHeight) * depthMapImageWidth + CoordWrapGpu.Repeat(ix + 1, depthMapImageWidth)];

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

        float GetDepthMapValue(Float4 color)
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

        Float4 GetPixel(int l, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[CoordWrapGpu.Wrap(t, height) * width + CoordWrapGpu.Wrap(l, width)];
                case 2:
                    return image[CoordWrapGpu.Repeat(t, height) * width + CoordWrapGpu.Repeat(l, width)];
                case 3:
                    return image[CoordWrapGpu.Mirror(t, height) * width + CoordWrapGpu.Mirror(l, width)];
                default:
                    if (l > -1 && l < width && t > -1 && t < height)
                    {
                        return image[t * width + l];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }
    }

    enum LensBlurIrisType : int
    {
        Circle = 0,
        Triangle = 3,
        Rectangle,
        Pentagon,
        Hexagon,
        Heptagon,
        Octagon,
        Nonagon,
        Decagon
    }

    record IrisMask(ManagedRasterizedMaskImage Mask, int MaskRadius, int MaskSize) : IDisposable
    {
        public float SumCount { get; } = Mask.Data.Sum();

        public void Dispose()
        {
            Mask.Dispose();
        }
    }

    record IrisMaskParameters(float Amount, LensBlurIrisType IrisType, float CornerRound, float Angle, bool UseDepthMap, float DepthFocus);

    readonly struct GPUIrisMaskInfo
    {
        public readonly int MaskDataPosition;

        public readonly Bool IsFocused;

        public readonly int MaskRadius;

        public readonly int MaskSize;

        public readonly float SumCount;

        public GPUIrisMaskInfo(int maskDataPosition, bool isFocused, int maskRadius, int maskSize, float sumCount)
        {
            MaskDataPosition = maskDataPosition;
            IsFocused = isFocused;
            MaskRadius = maskRadius;
            MaskSize = maskSize;
            SumCount = sumCount;
        }
    }
}
