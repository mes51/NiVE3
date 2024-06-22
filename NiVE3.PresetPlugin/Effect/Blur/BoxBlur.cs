using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_BoxBlur_Name, "mes51", "ブラー", LanguageResourceDictionary.Blur_BoxBlur_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public class BoxBlur : IEffect
    {
        const string ID = "6DC081A1-4748-45ED-95BB-3E48AA74FD48";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyRepeatId = nameof(PropertyRepeatId);

        const string PropertyDirectionId = nameof(PropertyDirectionId);

        const string PropertyIsRepeatEdgeId = nameof(PropertyIsRepeatEdgeId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        public PropertyBase[] GetProperties()
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Amount, 0.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyRepeatId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Repeat, 3, 1, 50, digit: 0),
                new EnumProperty(PropertyDirectionId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Direction, typeof(BlurDirection), typeof(LanguageResourceDictionary), BlurDirection.HorizontalAndVertical, selectBoxWidth: 90.0),
                new EnumProperty(PropertyEdgeRepeatModeId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_EdgeRepeatMode, typeof(EdgeRepeatMode), typeof(LanguageResourceDictionary), EdgeRepeatMode.None, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, bool useGpu)
        {
            var amount = (float)(double)(properties.First(p => p.Id == PropertyAmountId).GetValue(layerTime) ?? 0.0);
            var repeat = (int)(double)(properties.First(p => p.Id == PropertyRepeatId).GetValue(layerTime) ?? 1.0);
            var direction  = (BlurDirection)(properties.First(p => p.Id == PropertyDirectionId).GetValue(layerTime) ?? BlurDirection.HorizontalAndVertical);
            var edgeRepeatMode = (EdgeRepeatMode)(properties.First(p => p.Id == PropertyEdgeRepeatModeId).GetValue(layerTime) ?? EdgeRepeatMode.None);

            if (amount <= 0.0F)
            {
                return image;
            }

            if (useGpu)
            {

            }
            else
            {
                return ProcessCpu(image, roi, (float)downSamplingRateX, (float)downSamplingRateY, amount, repeat, direction, edgeRepeatMode);
            }

            return image;
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties)
        {
            throw new NotImplementedException();
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static NImage ProcessCpu(NImage image, ROI roi, float downSamplingRateX, float downSamplingRateY, float amount, int repeat, BlurDirection direction, EdgeRepeatMode edgeRepeatMode)
        {
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

            for (var i = 0; i < repeat; i++)
            {
                switch (direction)
                {
                    case BlurDirection.HorizontalAndVertical:
                        Blur(managedImage, roi, amount / downSamplingRateX, amount / downSamplingRateY, edgeRepeatMode);
                        break;
                    case BlurDirection.Horizontal:
                        BlurHorizontal(managedImage, roi, amount / downSamplingRateX, edgeRepeatMode);
                        break;
                    case BlurDirection.Vertical:
                        BlurVertical(managedImage, roi, amount / downSamplingRateY, edgeRepeatMode);
                        break;
                }
            }

            return managedImage;
        }

        static void Blur(NManagedImage image, ROI roi, float horizontal, float vertical, EdgeRepeatMode edgeRepeatMode)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            var temp = ArrayPool<Vector4>.Shared.Rent(imageData.Length);

            Parallel.For(0, imageHeight, y =>
            {
                var data = imageData.AsSpan(y * imageWidth, imageWidth);
                for (var x = 0; x < imageWidth; x++)
                {
                    temp[x * imageHeight + y] = data[x];
                }
            });

            var pz = (int)Math.Ceiling(horizontal);
            var fmz = pz - horizontal;
            var fz = 1 - fmz;
            var count = horizontal * 2.0F + 1.0F;
            var width = Math.Min(roi.Right + pz, imageWidth);
            var x = Math.Max(roi.Left - pz, 0);

            Parallel.For(Math.Max(roi.Top - pz, 0), Math.Min(roi.Bottom + pz, imageHeight), h =>
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);

                {
                    var w = x - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;

                        p = GetPixelForX(data, imageWidth, x + pz, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }
                    for (var limit = x + pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                    {
                        var p = GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var w = x; w < width; w++)
                {
                    var l = w - pz - 1;
                    var p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    l++;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    l = w + pz;

                    p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    l--;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (w >= x && a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[w * imageHeight + h] = result;
                    }
                }
            });

            pz = (int)Math.Ceiling(vertical);
            fmz = pz - vertical;
            fz = 1 - fmz;
            count = vertical * 2.0F + 1.0F;
            var y = roi.Top;
            var height = roi.Bottom;

            Parallel.For(roi.Left, roi.Right, w =>
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = temp.AsSpan(0, image.DataLength);

                {
                    var h = y - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = GetPixelForX(temp, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        p = GetPixelForX(temp, imageHeight, y + pz, w, edgeRepeatMode);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        h++;
                    }
                    for (var limit = y + pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                    {
                        var p = GetPixelForX(temp, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var h = y; h < height; h++)
                {
                    var t = h - pz - 1;
                    var p = GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    t++;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    t = h + pz;

                    p = GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    t--;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForX(temp, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        imageData[h * imageWidth + w] = result;
                    }
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp);
        }

        static void BlurHorizontal(NManagedImage image, ROI roi, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1 - fmz;
            var x = roi.Left;
            var width = roi.Right;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            using var tempImage = (NManagedImage)image.Copy();
            var temp = tempImage.Data;
            var count = amount * 2.0F + 1.0F;

            Parallel.For(roi.Top, roi.Bottom, delegate (int h)
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);

                {
                    var w = x - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;

                        p = GetPixelForX(data, imageWidth, x + pz, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }
                    for (var limit = x + pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                    {
                        var p = GetPixelForX(data, imageWidth, w, h, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var w = x; w < width; w++)
                {
                    var l = w - pz - 1;
                    var p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    l++;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    l = w + pz;

                    p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    l--;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForX(data, imageWidth, l, h, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (w >= x && a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * imageWidth + w] = result;
                    }
                }
            });

            temp.AsSpan(0, image.DataLength).CopyTo(imageData.AsSpan(0, image.DataLength));
        }

        static void BlurVertical(NManagedImage image, ROI roi, float amount, EdgeRepeatMode edgeRepeatMode)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1.0F - fmz;
            var y = roi.Top;
            var height = roi.Bottom;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            using var tempImage = (NManagedImage)image.Copy();
            var temp = tempImage.Data;
            var count = amount * 2.0F + 1.0F;

            Parallel.For(roi.Left, roi.Right, w =>
            {
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);

                {
                    var h = y - pz - 1;
                    if (fmz > 0.0F)
                    {
                        var p = GetPixelForY(data, imageWidth, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        p = GetPixelForY(data, imageWidth, imageHeight, y + pz, w, edgeRepeatMode);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;

                        h++;
                    }
                    for (var limit = y + pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                    {
                        var p = GetPixelForY(data, imageWidth, imageHeight, h, w, edgeRepeatMode);
                        var ta = p.W;
                        rgb += p * ta;
                        a += ta;
                    }
                }

                for (var h = y; h < height; h++)
                {
                    var t = h - pz - 1;
                    var p = GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                    var ta = p.W * fz;
                    rgb -= p * ta;
                    a -= ta;
                    t++;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                    }
                    t = h + pz;

                    p = GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                    ta = p.W * fz;
                    rgb += p * ta;
                    a += ta;
                    t--;

                    if (fmz > 0.0F)
                    {
                        p = GetPixelForY(data, imageWidth, imageHeight, t, w, edgeRepeatMode);
                        ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                    }

                    if (a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * imageWidth + w] = result;
                    }
                }
            });

            temp.AsSpan(0, image.DataLength).CopyTo(imageData.AsSpan(0, image.DataLength));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 GetPixelForY(Span<Vector4> data, int width, int height, int t, int w, EdgeRepeatMode edgeRepeatMode)
        {
            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    return data[Math.Clamp(t, 0, height - 1) * width + w];
                case EdgeRepeatMode.Repeat:
                    return data[(((t % height) + height) % height) * width + w];
                case EdgeRepeatMode.Mirror:
                    {
                        var lh = height - 1;
                        var a = Math.Abs(t);
                        var b = a % (lh * 2);
                        var c = b - Math.Max(b - lh, 0) * 2;
                        return data[c * width + w];
                    }
                default:
                    if (t > -1 && t < height)
                    {
                        return data[t * width + w];
                    }
                    else
                    {
                        return Vector4.Zero;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 GetPixelForX(Span<Vector4> data, int width, int l, int h, EdgeRepeatMode edgeRepeatMode)
        {
            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    return data[h * width + Math.Clamp(l, 0, width - 1)];
                case EdgeRepeatMode.Repeat:
                    return data[h * width + (((l % width) + width) % width)];
                case EdgeRepeatMode.Mirror:
                    {
                        var lw = width - 1;
                        var a = Math.Abs(l);
                        var b = a % (lw * 2);
                        var c = b - Math.Max(b - lw, 0) * 2;
                        return data[h * width + c];
                    }
                default:
                    if (l > -1 && l < width)
                    {
                        return data[h * width + l];
                    }
                    else
                    {
                        return Vector4.Zero;
                    }
            }
        }
    }
}
