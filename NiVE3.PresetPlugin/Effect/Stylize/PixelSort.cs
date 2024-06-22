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
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Effect.Stylize
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Stylize_PixelSort_Name, "mes51", "スタイライズ", LanguageResourceDictionary.Stylize_PixelSort_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class PixelSort : IEffect
    {
        const string ID = "BF9E258A-FE18-4747-8799-FC92B9CA0907";

        const string PropertyThresholdId = nameof(PropertyThresholdId);

        const string PropertyThresholdModeId = nameof(PropertyThresholdModeId);

        const string PropertySortModeId = nameof(PropertySortModeId);

        const string PropertySortTargetChannelId = nameof(PropertySortTargetChannelId);

        static readonly Vector4 ConvertToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public PropertyBase[] GetProperties()
        {
            return [
                new DoubleProperty(PropertyThresholdId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Threshold, 0.5, float.MinValue, float.MaxValue, slideChangeValue: 0.01, digit: 2),
                new EnumProperty(PropertyThresholdModeId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Mode, typeof(ThresholdMode), typeof(LanguageResourceDictionary), ThresholdMode.Darkness, selectBoxWidth: 90),
                new EnumProperty(PropertySortModeId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Sort, typeof(SortMode), typeof(LanguageResourceDictionary), SortMode.Horizontal, selectBoxWidth: 90),
                new EnumProperty(PropertySortTargetChannelId, LanguageResourceDictionary.ResourceKeys.Stylize_PixelSort_Channel, typeof(SortTargetChannel), typeof(LanguageResourceDictionary), SortTargetChannel.RGB, selectBoxWidth: 90),
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, bool useGpu)
        {
            var threshold = (float)(double)(properties.First(p => p.Id == PropertyThresholdId).GetValue(layerTime) ?? 0.5);
            var mode = (ThresholdMode)(properties.First(p => p.Id == PropertyThresholdModeId).GetValue(layerTime) ?? ThresholdMode.Brightness);
            var sort = (SortMode)(properties.First(p => p.Id == PropertySortModeId).GetValue(layerTime) ?? SortMode.Horizontal);
            var channel = (SortTargetChannel)(properties.First(p => p.Id == PropertySortTargetChannelId).GetValue(layerTime) ?? SortTargetChannel.RGB);

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

            switch (sort)
            {
                case SortMode.Vertical:
                    SortVertical(managedImage, roi.Left, roi.Top, roi.Right, roi.Bottom, threshold, mode, channel);
                    break;
                default:
                    SortHorizontal(managedImage, roi.Left, roi.Top, roi.Right, roi.Bottom, threshold, mode, channel);
                    break;
            }

            return managedImage;
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static void SortVertical(NManagedImage image, int left, int top, int right, int bottom, float threshold, ThresholdMode mode, SortTargetChannel channel)
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

            SortHorizontal(temp, top, left, bottom, right, threshold, mode, channel);

            Parallel.For(0, imageWidth, x =>
            {
                var data = tempData.AsSpan(x * imageHeight, imageHeight);
                for (var y = 0; y < imageHeight; y++)
                {
                    imageData[y * imageWidth + x] = data[y];
                }
            });
        }

        static void SortHorizontal(NManagedImage image, int left, int top, int right, int bottom, float threshold, ThresholdMode mode, SortTargetChannel channel)
        {
            var imageData = image.Data;
            var imageWidth = image.Width;

            Parallel.For(top, bottom, y =>
            {
                var data = imageData.AsSpan(y * imageWidth + left, right - left);
                var x = 0;
                var next = 0;
                switch (channel)
                {
                    case SortTargetChannel.RGB:
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

                                data[x..next].Sort(CompareRGB);
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

                                data[x..next].Sort(CompareRGB);
                                x = next + 1;
                            }
                        }
                        break;
                    case SortTargetChannel.R:
                        if (mode == ThresholdMode.Brightness)
                        {
                            while (x < data.Length)
                            {
                                x = SearchXBrightnessR(data, x, threshold);
                                next = SearchXDarknessR(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareR);
                                x = next + 1;
                            }
                        }
                        else
                        {
                            while (x < data.Length)
                            {
                                x = SearchXDarknessR(data, x, threshold);
                                next = SearchXBrightnessR(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareR);
                                x = next + 1;
                            }
                        }
                        break;
                    case SortTargetChannel.G:
                        if (mode == ThresholdMode.Brightness)
                        {
                            while (x < data.Length)
                            {
                                x = SearchXBrightnessG(data, x, threshold);
                                next = SearchXDarknessG(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareG);
                                x = next + 1;
                            }
                        }
                        else
                        {
                            while (x < data.Length)
                            {
                                x = SearchXDarknessG(data, x, threshold);
                                next = SearchXBrightnessG(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareG);
                                x = next + 1;
                            }
                        }
                        break;
                    case SortTargetChannel.B:
                        if (mode == ThresholdMode.Brightness)
                        {
                            while (x < data.Length)
                            {
                                x = SearchXBrightnessB(data, x, threshold);
                                next = SearchXDarknessB(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareB);
                                x = next + 1;
                            }
                        }
                        else
                        {
                            while (x < data.Length)
                            {
                                x = SearchXDarknessB(data, x, threshold);
                                next = SearchXBrightnessB(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareB);
                                x = next + 1;
                            }
                        }
                        break;
                    case SortTargetChannel.A:
                        if (mode == ThresholdMode.Brightness)
                        {
                            while (x < data.Length)
                            {
                                x = SearchXBrightnessA(data, x, threshold);
                                next = SearchXDarknessA(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareB);
                                x = next + 1;
                            }
                        }
                        else
                        {
                            while (x < data.Length)
                            {
                                x = SearchXDarknessA(data, x, threshold);
                                next = SearchXBrightnessA(data, x + 1, threshold);

                                if (x < 0)
                                {
                                    break;
                                }
                                if (next < 0)
                                {
                                    next = data.Length;
                                }

                                data[x..next].Sort(CompareA);
                                x = next + 1;
                            }
                        }
                        break;
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SearchXBrightnessRGB(ReadOnlySpan<Vector4> data, int x, float threshold)
        {
            while (x < data.Length)
            {
                if ((data[x] * ConvertToGrayScale).HorizontalAdd() < threshold)
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
                if ((data[x] * ConvertToGrayScale).HorizontalAdd() > threshold)
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
        static int CompareRGB(Vector4 a, Vector4 b)
        {
            return (a * ConvertToGrayScale).HorizontalAdd().CompareTo((b * ConvertToGrayScale).HorizontalAdd());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareR(Vector4 a, Vector4 b)
        {
            return a.Z.CompareTo(b.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareG(Vector4 a, Vector4 b)
        {
            return a.Y.CompareTo(b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareB(Vector4 a, Vector4 b)
        {
            return a.X.CompareTo(b.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CompareA(Vector4 a, Vector4 b)
        {
            return a.W.CompareTo(b.W);
        }
    }

    public enum ThresholdMode
    {
        Brightness,
        Darkness
    }

    public enum SortMode
    {
        Vertical,
        Horizontal
    }

    public enum SortTargetChannel
    {
        RGB,
        R,
        G,
        B,
        A
    }
}
