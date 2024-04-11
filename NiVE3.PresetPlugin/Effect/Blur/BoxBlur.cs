using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
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

        public PropertyBase[] GetProperties()
        {
            return
            [
                new DoubleProperty(PropertyAmountId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Amount, 0.0, 0.0, 10000.0, digit: 2),
                new DoubleProperty(PropertyRepeatId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Repeat, 3, 1, 50, digit: 0),
                new EnumProperty(PropertyDirectionId, LanguageResourceDictionary.ResourceKeys.Blur_BoxBlur_Direction, typeof(BlurDirection), typeof(LanguageResourceDictionary), BlurDirection.HorizontalAndVertical, selectBoxWidth: 90.0)
            ];
        }

        public NImage Process(NImage image, ROI roi, double layerTime, IPropertyObject[] properties, bool useGpu)
        {
            var amount = (float)(double)(properties.First(p => p.Id == PropertyAmountId).GetValue(layerTime) ?? 0.0);
            var repeat = (int)(double)(properties.First(p => p.Id == PropertyRepeatId).GetValue(layerTime) ?? 1.0);
            var direction  = (BlurDirection)(properties.First(p => p.Id == PropertyDirectionId).GetValue(layerTime) ?? BlurDirection.HorizontalAndVertical);

            if (amount <= 0.0F)
            {
                return image;
            }

            for (var i = 0; i < repeat; i++)
            {
                switch (direction)
                {
                    case BlurDirection.HorizontalAndVertical:
                        Blur(image, roi, amount);
                        break;
                    case BlurDirection.Horizontal:
                        BlurHorizontal(image, roi, amount);
                        break;
                    case BlurDirection.Vertical:
                        BlurVertical(image, roi, amount);
                        break;
                }
            }

            return image;
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties)
        {
            throw new NotImplementedException();
        }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public void Dispose() { }

        static void Blur(NImage image, ROI roi, float amount)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1 - fmz;
            var x = Math.Max(roi.Left - pz, 0);
            var y = roi.Top;
            var width = Math.Min(roi.Right + pz, image.Width);
            var height = roi.Bottom;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.GetData();
            var temp = ArrayPool<Vector4>.Shared.Rent(image.DataLength);
            Parallel.For(x, width, delegate (int w)
            {
                var count = 0.0F;
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);
                for (int h = y - pz; h < height; h++)
                {
                    int t = h - pz;
                    if (t >= y)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fz;
                    }
                    t++;
                    if (fmz > 0.0F && t >= y)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fmz;
                    }
                    t = h + pz;
                    if (t < height)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                        count += fz;
                    }
                    t--;
                    if (fmz > 0.0F && t >= y && t < height)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                        count += fmz;
                    }
                    if (h >= y && a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * imageWidth + w] = result;
                    }
                }
            });

            x = roi.Left;
            width = roi.Right;
            Parallel.For(y, height, delegate (int h)
            {
                var count = 0.0F;
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);
                for (int w = x - pz; w < width; w++)
                {
                    int l = w - pz;
                    if (l >= x)
                    {
                        var p = temp[h * imageWidth + l];
                        var ta = p.W * fz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fz;
                    }
                    l++;
                    if (fmz > 0.0F && l >= x)
                    {
                        var p = temp[h * imageWidth + l];
                        var ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fmz;
                    }
                    l = w + pz;
                    if (l < width)
                    {
                        var p = temp[h * imageWidth + l];
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                        count += fz;
                    }
                    l--;
                    if (fmz > 0.0F && l >= x && l < width)
                    {
                        var p = temp[h * imageWidth + l];
                        var ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                        count += fmz;
                    }
                    if (w >= x && a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        data[h * imageWidth + w] = result;
                    }
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp, true);
        }

        static void BlurHorizontal(NImage image, ROI roi, float amount)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1 - fmz;
            var x = roi.Left;
            var width = roi.Right;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.GetData();
            var temp = ArrayPool<Vector4>.Shared.Rent(image.DataLength);

            Parallel.For(roi.Top, roi.Bottom, delegate (int h)
            {
                var count = 0.0F;
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);
                for (int w = x - pz; w < width; w++)
                {
                    int l = w - pz;
                    if (l >= x)
                    {
                        var p = data[h * imageWidth + l];
                        var ta = p.W * fz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fz;
                    }
                    l++;
                    if (fmz > 0.0F && l >= x)
                    {
                        var p = data[h * imageWidth + l];
                        var ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fmz;
                    }
                    l = w + pz;
                    if (l < width)
                    {
                        var p = data[h * imageWidth + l];
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                        count += fz;
                    }
                    l--;
                    if (fmz > 0.0F && l >= x && l < width)
                    {
                        var p = data[h * imageWidth + l];
                        var ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                        count += fmz;
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

            ArrayPool<Vector4>.Shared.Return(temp, true);
        }

        static void BlurVertical(NImage image, ROI roi, float amount)
        {
            var pz = (int)Math.Ceiling(amount);
            var fmz = pz - amount;
            var fz = 1 - fmz;
            var y = roi.Top;
            var height = roi.Bottom;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.GetData();
            var temp = ArrayPool<Vector4>.Shared.Rent(image.DataLength);
            Parallel.For(roi.Left, roi.Right, delegate (int w)
            {
                var count = 0.0F;
                var rgb = Vector4.Zero;
                var a = 0.0F;
                var data = imageData.AsSpan(0, image.DataLength);
                for (int h = y - pz; h < height; h++)
                {
                    int t = h - pz;
                    if (t >= y)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fz;
                    }
                    t++;
                    if (fmz > 0.0F && t >= y)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fmz;
                        rgb -= p * ta;
                        a -= ta;
                        count -= fmz;
                    }
                    t = h + pz;
                    if (t < height)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                        count += fz;
                    }
                    t--;
                    if (fmz > 0.0F && t >= y && t < height)
                    {
                        var p = data[t * imageWidth + w];
                        var ta = p.W * fmz;
                        rgb += p * ta;
                        a += ta;
                        count += fmz;
                    }
                    if (h >= y && a > 0.0F)
                    {
                        var result = rgb / a;
                        result.W = a / count;
                        temp[h * imageWidth + w] = result;
                    }
                }
            });

            temp.AsSpan(0, image.DataLength).CopyTo(imageData.AsSpan(0, image.DataLength));

            ArrayPool<Vector4>.Shared.Return(temp, true);
        }
    }

    enum BlurDirection
    {
        HorizontalAndVertical,
        Horizontal,
        Vertical
    }
}
