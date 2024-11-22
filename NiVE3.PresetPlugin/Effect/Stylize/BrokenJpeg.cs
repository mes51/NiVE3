using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Effect.Util.Jpeg;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_BrokenJpeg_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_BrokenJpeg_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary), IsRenderEveryFrame = true)]
    public sealed class BrokenJpeg : IEffect
    {
        const string ID = "F70C3855-E1D7-4727-B7AF-2ADEB607FBE5";

        const string PropertyCompressQualityId = nameof(PropertyCompressQualityId);

        const string PropertyColorSpaceId = nameof(PropertyColorSpaceId);

        const string PropertyBackgroundColorId = nameof(PropertyBackgroundColorId);

        const string PropertyBrokenScanGroupId = nameof(PropertyBrokenScanGroupId);

        const string PropertyBrokenScanBrokenCountId = nameof(PropertyBrokenScanBrokenCountId);

        const string PropertyBrokenScanBrokenRangeBeginId = nameof(PropertyBrokenScanBrokenRangeBeginId);

        const string PropertyBrokenScanBrokenRangeEndId = nameof(PropertyBrokenScanBrokenRangeEndId);

        const string PropertyBrokenScanRandomSeedId = nameof(PropertyBrokenScanRandomSeedId);

        const string PropertyBrokenLuminanceQuantizeTableGroupId = nameof(PropertyBrokenLuminanceQuantizeTableGroupId);

        const string PropertyBrokenLuminanceQuantizeTableEnabledId = nameof(PropertyBrokenLuminanceQuantizeTableEnabledId);

        const string PropertyBrokenLuminanceQuantizeTableBrokenPositionId = nameof(PropertyBrokenLuminanceQuantizeTableBrokenPositionId);

        const string PropertyBrokenLuminanceQuantizeTableReplaceValueId = nameof(PropertyBrokenLuminanceQuantizeTableReplaceValueId);

        const string PropertyBrokenLuminanceQuantizeTableBrokenCountId = nameof(PropertyBrokenLuminanceQuantizeTableBrokenCountId);

        const string PropertyBrokenLuminanceQuantizeTableMaxValueId = nameof(PropertyBrokenLuminanceQuantizeTableMaxValueId);

        const string PropertyBrokenLuminanceQuantizeTableRandomSeedId = nameof(PropertyBrokenLuminanceQuantizeTableRandomSeedId);

        const string PropertyBrokenChrominanceQuantizeTableGroupId = nameof(PropertyBrokenChrominanceQuantizeTableGroupId);

        const string PropertyBrokenChrominanceQuantizeTableEnabledId = nameof(PropertyBrokenChrominanceQuantizeTableEnabledId);

        const string PropertyBrokenChrominanceQuantizeTableBrokenPositionId = nameof(PropertyBrokenChrominanceQuantizeTableBrokenPositionId);

        const string PropertyBrokenChrominanceQuantizeTableReplaceValueId = nameof(PropertyBrokenChrominanceQuantizeTableReplaceValueId);

        const string PropertyBrokenChrominanceQuantizeTableBrokenCountId = nameof(PropertyBrokenChrominanceQuantizeTableBrokenCountId);

        const string PropertyBrokenChrominanceQuantizeTableMaxValueId = nameof(PropertyBrokenChrominanceQuantizeTableMaxValueId);

        const string PropertyBrokenChrominanceQuantizeTableRandomSeedId = nameof(PropertyBrokenChrominanceQuantizeTableRandomSeedId);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyCompressQualityId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_CompressQuality, 100.0, 0.0, 100.0, digit: 0),
                new EnumProperty(PropertyColorSpaceId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_ColorSpace, typeof(JpegColorSpace), typeof(LanguageResourceDictionary), JpegColorSpace.YCbCr444, selectBoxWidth: 100.0),
                new ColorProperty(
                    PropertyBackgroundColorId,
                    LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BackgroundColor,
                    LanguageResourceDictionary.ResourceKeys.Dialog_ColorDialog_Title_BackgroundColor,
                    LanguageResourceDictionary.ResourceKeys.Dialog_OK,
                    LanguageResourceDictionary.ResourceKeys.Dialog_Cancel,
                    Vector4.UnitW
                ),
                new PropertyGroup(PropertyBrokenScanGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BokenScan,
                [
                    new DoubleProperty(PropertyBrokenScanBrokenCountId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BokenScan_BrokenCount, 10.0, 0.0, 100000.0, digit: 0),
                    new DoubleProperty(PropertyBrokenScanBrokenRangeBeginId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BokenScan_BrokenRangeBegin, 0.0, 0.0, 100.0, digit: 2),
                    new DoubleProperty(PropertyBrokenScanBrokenRangeEndId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BokenScan_BrokenRangeEnd, 100.0, 0.0, 100.0, digit: 2),
                    new DoubleProperty(PropertyBrokenScanRandomSeedId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BokenScan_RandomSeed, 0.0, 0.0, int.MaxValue, digit: 0)
                ]),
                new PropertyGroup(PropertyBrokenLuminanceQuantizeTableGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_Luminance,
                [
                    new CheckBoxProperty(PropertyBrokenLuminanceQuantizeTableEnabledId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_Enabled, false),
                    new DoubleProperty(PropertyBrokenLuminanceQuantizeTableBrokenPositionId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenPosition, 3.0, 1.0, 64.0, digit: 0),
                    new DoubleProperty(PropertyBrokenLuminanceQuantizeTableReplaceValueId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_ReplaceValue, 100.0, 1.0, 255.0, digit: 0),
                    new DoubleProperty(PropertyBrokenLuminanceQuantizeTableBrokenCountId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenCount, 0.0, 0.0, 63.0, digit: 0),
                    new DoubleProperty(PropertyBrokenLuminanceQuantizeTableMaxValueId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_MaxValue, 100.0, 1.0, 255.0, digit: 0),
                    new DoubleProperty(PropertyBrokenLuminanceQuantizeTableRandomSeedId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_RandomSeed, 0.0, 0.0, int.MaxValue, digit: 0)
                ]),
                new PropertyGroup(PropertyBrokenChrominanceQuantizeTableGroupId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_Chrominance,
                [
                    new CheckBoxProperty(PropertyBrokenChrominanceQuantizeTableEnabledId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_Enabled, false),
                    new DoubleProperty(PropertyBrokenChrominanceQuantizeTableBrokenPositionId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenPosition, 3.0, 1.0, 64.0, digit: 0),
                    new DoubleProperty(PropertyBrokenChrominanceQuantizeTableReplaceValueId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_ReplaceValue, 100.0, 1.0, 255.0, digit: 0),
                    new DoubleProperty(PropertyBrokenChrominanceQuantizeTableBrokenCountId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenCount, 0.0, 0.0, 63.0, digit: 0),
                    new DoubleProperty(PropertyBrokenChrominanceQuantizeTableMaxValueId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_MaxValue, 100.0, 1.0, 255.0, digit: 0),
                    new DoubleProperty(PropertyBrokenChrominanceQuantizeTableRandomSeedId, LanguageResourceDictionary.ResourceKeys.Stylize_BrokenJpeg_BrokenQuantizeTable_RandomSeed, 0.0, 0.0, int.MaxValue, digit: 0)
                ])
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var compressQuality = (float)properties.GetValue(PropertyCompressQualityId, layerTime, 100.0);
            var colorSpace = properties.GetValue(PropertyColorSpaceId, layerTime, JpegColorSpace.YCbCr444);
            var backgroundColor = properties.GetValue(PropertyBackgroundColorId, layerTime, Vector4.UnitW);

            var scanGroup = properties.First(p => p.Id == PropertyBrokenScanGroupId).GetChildren() ?? [];
            var scanBrokenCount = (int)scanGroup.GetValue(PropertyBrokenScanBrokenCountId, layerTime, 0.0);
            var scanBrokenRangeBegin = scanGroup.GetValue(PropertyBrokenScanBrokenRangeBeginId, layerTime, 0.0) * 0.01;
            var scanBrokenRangeEnd = scanGroup.GetValue(PropertyBrokenScanBrokenRangeEndId, layerTime, 0.0) * 0.01;
            var scanRandomSeed = (int)scanGroup.GetValue(PropertyBrokenScanRandomSeedId, layerTime, 0.0);

            var luminanceQTGroup = properties.First(p => p.Id == PropertyBrokenLuminanceQuantizeTableGroupId).GetChildren() ?? [];
            var luminanceQTEnableBroken = luminanceQTGroup.GetValue(PropertyBrokenLuminanceQuantizeTableEnabledId, layerTime, false);
            var luminanceQTBrokenPosition = (int)luminanceQTGroup.GetValue(PropertyBrokenLuminanceQuantizeTableBrokenPositionId, layerTime, 0.0);
            var luminanceQRReplaceValue = (int)luminanceQTGroup.GetValue(PropertyBrokenLuminanceQuantizeTableReplaceValueId, layerTime, 0.0);
            var luminanceQTBrokenCount = (int)luminanceQTGroup.GetValue(PropertyBrokenLuminanceQuantizeTableBrokenCountId, layerTime, 0.0);
            var luminanceQTMaxValue = (int)luminanceQTGroup.GetValue(PropertyBrokenLuminanceQuantizeTableMaxValueId, layerTime, 0.0);
            var luminanceQTRandomSeed = (int)luminanceQTGroup.GetValue(PropertyBrokenLuminanceQuantizeTableRandomSeedId, layerTime, 0.0);

            var chrominanceQTGroup = properties.First(p => p.Id == PropertyBrokenChrominanceQuantizeTableGroupId).GetChildren() ?? [];
            var chrominanceQTEnableBroken = chrominanceQTGroup.GetValue(PropertyBrokenChrominanceQuantizeTableEnabledId, layerTime, false);
            var chrominanceQTBrokenPosition = (int)chrominanceQTGroup.GetValue(PropertyBrokenChrominanceQuantizeTableBrokenPositionId, layerTime, 0.0);
            var chrominanceQRReplaceValue = (int)chrominanceQTGroup.GetValue(PropertyBrokenChrominanceQuantizeTableReplaceValueId, layerTime, 0.0);
            var chrominanceQTBrokenCount = (int)chrominanceQTGroup.GetValue(PropertyBrokenChrominanceQuantizeTableBrokenCountId, layerTime, 0.0);
            var chrominanceQTMaxValue = (int)chrominanceQTGroup.GetValue(PropertyBrokenChrominanceQuantizeTableMaxValueId, layerTime, 0.0);
            var chrominanceQTRandomSeed = (int)chrominanceQTGroup.GetValue(PropertyBrokenChrominanceQuantizeTableRandomSeedId, layerTime, 0.0);

            var (resultImage, compressImage) = image switch
            {
                NGPUImage gpuImage => (gpuImage.CopyToCpu(), gpuImage.CopyToCpu()),
                _ => ((NManagedImage)image, (NManagedImage)image.Copy())
            };
            PreProcess(compressImage, backgroundColor);

            var encoder = colorSpace switch
            {
                JpegColorSpace.Rgb => new EncodeImageRgb(compressImage.Width, compressImage.Height, compressQuality),
                JpegColorSpace.YCbCr422 => new EncodeImageYCbCr4XX(compressImage.Width, compressImage.Height, compressQuality, 2, 1),
                JpegColorSpace.YCbCr420 => new EncodeImageYCbCr4XX(compressImage.Width, compressImage.Height, compressQuality, 2, 2),
                JpegColorSpace.YCbCr411 => new EncodeImageYCbCr4XX(compressImage.Width, compressImage.Height, compressQuality, 4, 1),
                JpegColorSpace.YCbCr410 => new EncodeImageYCbCr4XX(compressImage.Width, compressImage.Height, compressQuality, 4, 4),
                _ => (EncodeImageBase)new EncodeImageYCbCr4XX(compressImage.Width, compressImage.Height, compressQuality, 1, 1)
            };
            encoder.Compress(compressImage.Data);

            if (scanBrokenCount > 1)
            {
                BreakScan(encoder, scanBrokenCount, scanBrokenRangeBegin, scanBrokenRangeEnd, scanRandomSeed, layerTime);
            }
            if (luminanceQTEnableBroken)
            {
                BreakQuantizeTable(encoder.QuantizeTables[0], 0, luminanceQTBrokenPosition, luminanceQRReplaceValue, luminanceQTBrokenCount, luminanceQTMaxValue, luminanceQTRandomSeed, layerTime);
            }
            if (chrominanceQTEnableBroken && encoder is not EncodeImageRgb)
            {
                BreakQuantizeTable(encoder.QuantizeTables[1], 1, chrominanceQTBrokenPosition, chrominanceQRReplaceValue, chrominanceQTBrokenCount, chrominanceQTMaxValue, chrominanceQTRandomSeed, layerTime);
            }

            try
            {
                encoder.Decompress(compressImage.Data);

                Parallel.For(roi.Top, roi.Bottom, y =>
                {
                    var pos = y * resultImage.Width + roi.Left;
                    compressImage.Data.AsSpan(pos, roi.Width).CopyTo(resultImage.Data.AsSpan(pos));
                });

                return resultImage;
            }
            catch
            {
                resultImage.Dispose();
                return image;
            }
            finally
            {
                compressImage.Dispose();
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static void PreProcess(NManagedImage image, Vector4 backgroundColor)
        {
            var imageData = image.Data;
            Parallel.For(0, image.Height, y =>
            {
                for (int x = 0, pos = y * image.Width; x < image.Width; x++, pos++)
                {
                    imageData[pos] = Blend.Process(BlendMode.Normal, backgroundColor, imageData[pos]);
                }
            });
        }

        static void BreakScan(EncodeImageBase encoder, int brokenCount, double brokenRangeBegin, double brokenRangeEnd, int randomSeed, double time)
        {
            if (brokenRangeBegin > brokenRangeEnd)
            {
                (brokenRangeBegin, brokenRangeEnd) = (brokenRangeEnd, brokenRangeBegin);
            }

            var brokenBeginPos = (int)(encoder.EncodedDataLength * brokenRangeBegin) * 4;
            var brokenEndPos = (int)(encoder.EncodedDataLength * brokenRangeEnd) * 4;
            if (brokenBeginPos == brokenEndPos)
            {
                return;
            }

            brokenCount = (int)Math.Min(brokenCount, brokenEndPos - brokenBeginPos);
            var rangePerBreak = ((brokenEndPos - brokenBeginPos) / brokenCount);
            for (var i = 0; i < brokenCount; i++)
            {
                var random = NoiseFunction.Pcg3DUIntCpu((uint)i, unchecked((uint)time.GetHashCode()), 0, (uint)randomSeed);
                var pos = brokenBeginPos + rangePerBreak * i + (int)((random.GetElement(0) / (double)uint.MaxValue) * rangePerBreak);
                var slice = MemoryMarshal.Cast<uint, byte>(encoder.EncodedData.AsSpan(pos / 4, 1))[(pos % 4)..];
                slice[0] = (byte)random.GetElement(1);
            }
        }

        static void BreakQuantizeTable(QuantizeTable table, int tableIndex, int brokenPosition, int replaceValue, int brokenCount, int maxValue, int randomSeed, double time)
        {
            table.Table[brokenPosition] = replaceValue;

            for (var i = 0; i < brokenCount; i++)
            {
                var random = NoiseFunction.Pcg3DUIntCpu((uint)i, unchecked((uint)time.GetHashCode()), (uint)tableIndex + 1, (uint)randomSeed);
                table.Table[random.GetElement(0) % 64] = (byte)((random.GetElement(1) / (float)uint.MaxValue) * maxValue);
            }
        }
    }

    enum JpegColorSpace
    {
        Rgb,
        YCbCr444,
        YCbCr422,
        YCbCr420,
        YCbCr411,
        YCbCr410,
    }
}
