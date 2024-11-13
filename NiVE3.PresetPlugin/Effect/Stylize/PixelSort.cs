using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_PixelSort_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Stylize, LanguageResourceDictionary.Stylize_PixelSort_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class PixelSort : IEffect
    {
        const string ID = "BF9E258A-FE18-4747-8799-FC92B9CA0907";

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyThresholdModeId = nameof(PropertyThresholdModeId);

        const string PropertySortModeId = nameof(PropertySortModeId);

        const string PropertySortTargetChannelId = nameof(PropertySortTargetChannelId);

        const string PropertySortOrderId = nameof(PropertySortOrderId);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return [
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Threshold, 0.5, float.MinValue, float.MaxValue, slideChangeValue: 0.01, digit: 2),
                new EnumProperty(PropertyThresholdModeId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Mode, typeof(ThresholdMode), typeof(LanguageResourceDictionary), ThresholdMode.Darkness, selectBoxWidth: 90),
                new EnumProperty(PropertySortModeId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Sort, typeof(SortMode), typeof(LanguageResourceDictionary), SortMode.Horizontal, selectBoxWidth: 90),
                new EnumProperty(PropertySortOrderId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_SortOrder, typeof(SortOrder), typeof(LanguageResourceDictionary), SortOrder.Ascending, selectBoxWidth: 90),
                new EnumProperty(PropertySortTargetChannelId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Channel, typeof(ChannelType), typeof(LanguageResourceDictionary), ChannelType.RGB, selectBoxWidth: 90),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var threshold = (float)properties.GetValue(PropertyThresholdId, layerTime, 0.5);
            var mode = properties.GetValue(PropertyThresholdModeId, layerTime, ThresholdMode.Brightness);
            var sort = properties.GetValue(PropertySortModeId, layerTime, SortMode.Horizontal);
            var order = properties.GetValue(PropertySortOrderId, layerTime, SortOrder.Ascending);
            var channel = properties.GetValue(PropertySortTargetChannelId, layerTime, ChannelType.RGB);

            NManagedImage managedImage;
            if (image is NGPUImage gpuImage)
            {
                managedImage = gpuImage.CopyToCpu();
                image.Dispose();
            }
            else
            {
                managedImage = (NManagedImage)image;
            }

            var comparison = (channel, order) switch
            {
                (ChannelType.RGB, SortOrder.Descending) => CompareRGBDescending,
                (ChannelType.R, SortOrder.Ascending) => CompareRAscending,
                (ChannelType.R, SortOrder.Descending) => CompareRDescending,
                (ChannelType.G, SortOrder.Ascending) => CompareGAscending,
                (ChannelType.G, SortOrder.Descending) => CompareGDescending,
                (ChannelType.B, SortOrder.Ascending) => CompareBAscending,
                (ChannelType.B, SortOrder.Descending) => CompareBDescending,
                (ChannelType.A, SortOrder.Ascending) => CompareAAscending,
                (ChannelType.A, SortOrder.Descending) => CompareADescending,
                _ => (Comparison<Vector4>)CompareRGBAscending
            };

            switch (sort)
            {
                case SortMode.Vertical:
                    SortVertical(managedImage, roi.Left, roi.Top, roi.Right, roi.Bottom, threshold, mode, channel, comparison);
                    break;
                default:
                    SortHorizontal(managedImage, roi.Left, roi.Top, roi.Right, roi.Bottom, threshold, mode, channel, comparison);
                    break;
            }

            return managedImage;
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static void SortVertical(NManagedImage image, int left, int top, int right, int bottom, float threshold, ThresholdMode mode, ChannelType channel, Comparison<Vector4> comparison)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            using var temp = new NManagedImage(imageHeight, imageWidth);
            var imageData = image.Data;
            var tempData = temp.Data;

            Parallel.For(0, imageHeight, y =>
            {
                var data = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = 0; x < imageWidth; x++)
                {
                    tempData[x * imageHeight + y] = data[x];
                }
            });

            SortHorizontal(temp, top, left, bottom, right, threshold, mode, channel, comparison);

            Parallel.For(0, imageWidth, x =>
            {
                var data = tempData.AsSpan(x * imageHeight, imageHeight);
                for (var y = 0; y < imageHeight; y++)
                {
                    imageData[y * imageWidth + x] = data[y];
                }
            });
        }

        static void SortHorizontal(NManagedImage image, int left, int top, int right, int bottom, float threshold, ThresholdMode mode, ChannelType channel, Comparison<Vector4> comparison)
        {
            var imageData = image.Data;
            var imageWidth = image.Width;

            Parallel.For(top, bottom, y =>
            {
                var data = imageData.AsSpan(y * imageWidth + left, right - left);
                var x = 0;
                var next = 0;

                if (mode == ThresholdMode.Brightness)
                {
                    while (x < data.Length)
                    {
                        x = SearchXBrightnessRGB(data, x, threshold);
                        next = SearchXDarknessRGB(data, x + 1, threshold);

                        if (x < 0)
                        {
                            break;
                        }
                        if (next < 0)
                        {
                            next = data.Length;
                        }

                        data[x..next].Sort(comparison);
                        x = next + 1;
                    }
                }
                else
                {
                    while (x < data.Length)
                    {
                        x = SearchXDarknessRGB(data, x, threshold);
                        next = SearchXBrightnessRGB(data, x + 1, threshold);

                        if (x < 0)
                        {
                            break;
                        }
                        if (next < 0)
                        {
                            next = data.Length;
                        }

                        data[x..next].Sort(comparison);
                        x = next + 1;
                    }
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXBrightnessRGB(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if ((data[x] * Const.ConvertToGrayScale).HorizontalAdd() < threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXDarknessRGB(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if ((data[x] * Const.ConvertToGrayScale).HorizontalAdd() > threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXBrightnessR(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].Z < threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXDarknessR(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].Z > threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXBrightnessG(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].Y < threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXDarknessG(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].Y > threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXBrightnessB(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].X < threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXDarknessB(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].X > threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXBrightnessA(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].W < threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXDarknessA(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if (data[x].W > threshold)
                {
                    x++;
                }
                else
                {
                    return x;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareRGBAscending(Vector4 a, Vector4 b)
        {
            return (a * Const.ConvertToGrayScale).HorizontalAdd().CompareTo((b * Const.ConvertToGrayScale).HorizontalAdd());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareRGBDescending(Vector4 a, Vector4 b)
        {
            return (b * Const.ConvertToGrayScale).HorizontalAdd().CompareTo((a * Const.ConvertToGrayScale).HorizontalAdd());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareRAscending(Vector4 a, Vector4 b)
        {
            return a.Z.CompareTo(b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareRDescending(Vector4 a, Vector4 b)
        {
            return b.Z.CompareTo(a.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareGAscending(Vector4 a, Vector4 b)
        {
            return a.Y.CompareTo(b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareGDescending(Vector4 a, Vector4 b)
        {
            return b.Y.CompareTo(a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareBAscending(Vector4 a, Vector4 b)
        {
            return a.X.CompareTo(b.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareBDescending(Vector4 a, Vector4 b)
        {
            return b.X.CompareTo(a.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareAAscending(Vector4 a, Vector4 b)
        {
            return a.W.CompareTo(b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareADescending(Vector4 a, Vector4 b)
        {
            return b.W.CompareTo(b.W);
        }
    }

    enum ThresholdMode
    {
        Brightness,
        Darkness
    }

    enum SortMode
    {
        Vertical,
        Horizontal
    }

    enum SortOrder
    {
        Ascending,
        Descending
    }
}
