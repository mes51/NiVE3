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
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Blur
{
    [Export(typeof(IEffect))]
    [EffectMetadata(LanguageResourceDictionary.Blur_BoxBlur_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Blur, LanguageResourceDictionary.Blur_BoxBlur_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    public sealed class BoxBlur : IEffect
    {
        const string ID = "6DC081A1-4748-45ED-95BB-3E48AA74FD48";

        const string PropertyAmountId = nameof(PropertyAmountId);

        const string PropertyRepeatId = nameof(PropertyRepeatId);

        const string PropertyDirectionId = nameof(PropertyDirectionId);

        const string PropertyIsRepeatEdgeId = nameof(PropertyIsRepeatEdgeId);

        const string PropertyEdgeRepeatModeId = nameof(PropertyEdgeRepeatModeId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

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

        public ROI CalcRoi(ROI baseRoi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (edgeRepeatMode == EdgeRepeatMode.AddAmount)
            {
                var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);
                var repeat = (int)properties.GetValue(PropertyRepeatId, layerTime, 1.0);
                var direction = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);

                var expandX = (int)Math.Ceiling(amount * repeat / downSamplingRateX);
                var expandY = (int)Math.Ceiling(amount * repeat / downSamplingRateY);
                switch (direction)
                {
                    case BlurDirection.Horizontal:
                        return baseRoi.Expand(-expandX, 0, expandX, 0);
                    case BlurDirection.Vertical:
                        return baseRoi.Expand(0, -expandY, 0, expandY);
                    default:
                        return baseRoi.Expand(-expandX, -expandY, expandX, expandY);
                }
            }
            else
            {
                return baseRoi;
            }
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, double layerTime, IPropertyObject[] properties, ICompositionObject composition, bool useGpu)
        {
            var amount = (float)properties.GetValue(PropertyAmountId, layerTime, 0.0);
            var repeat = (int)properties.GetValue(PropertyRepeatId, layerTime, 1.0);
            var direction  = properties.GetValue(PropertyDirectionId, layerTime, BlurDirection.HorizontalAndVertical);
            var edgeRepeatMode = properties.GetValue(PropertyEdgeRepeatModeId, layerTime, EdgeRepeatMode.None);

            if (amount <= 0.0F)
            {
                return image;
            }

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, (float)downSamplingRateX, (float)downSamplingRateY, amount, repeat, direction, edgeRepeatMode);
            }
            else
            {
                return ProcessCpu(image, roi, (float)downSamplingRateX, (float)downSamplingRateY, amount, repeat, direction, edgeRepeatMode);
            }
        }

        public float[] Process(float[] audio, double startTime, IPropertyObject[] properties, ICompositionObject composition)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static NManagedImage ProcessCpu(NImage image, ROI roi, float downSamplingRateX, float downSamplingRateY, float amount, int repeat, BlurDirection direction, EdgeRepeatMode edgeRepeatMode)
        {
            NManagedImage managedImage;
            if (image is NGPUImage gpuImage)
            {
                managedImage = gpuImage.CopyToCpu();
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

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, float downSamplingRateX, float downSamplingRateY, float amount, int repeat, BlurDirection direction, EdgeRepeatMode edgeRepeatMode)
        {
            NGPUImage gpuImage;
            if (image is NManagedImage managedImage)
            {
                gpuImage = managedImage.CopyToGpu(device);
            }
            else
            {
                gpuImage = (NGPUImage)image;
            }

            {
                using var temp = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
                gpuImage.CopyTo(temp);
                switch (direction)
                {
                    case BlurDirection.HorizontalAndVertical:
                        using (var context = device.CreateComputeContext())
                        {
                            for (var i = 0; i < repeat; i++)
                            {
                                context.For(roi.Width, roi.Height, new BlurHorizontalProcess(temp.Data, gpuImage.Data, gpuImage.Width, amount, (int)edgeRepeatMode, roi.Left, roi.Top));
                                context.Barrier(temp.Data);
                                context.Barrier(gpuImage.Data);
                                context.For(roi.Width, roi.Height, new BlurVerticalProcess(gpuImage.Data, temp.Data, gpuImage.Width, gpuImage.Height, amount, (int)edgeRepeatMode, roi.Left, roi.Top));
                                context.Barrier(temp.Data);
                                context.Barrier(gpuImage.Data);
                            }
                        }
                        break;
                    case BlurDirection.Horizontal:
                        {
                            var src = temp;
                            var dst = gpuImage;

                            using (var context = device.CreateComputeContext())
                            {
                                for (var i = 0; i < repeat; i++)
                                {
                                    (src, dst) = (dst, src);
                                    context.For(roi.Width, roi.Height, new BlurHorizontalProcess(dst.Data, src.Data, src.Width, amount, (int)edgeRepeatMode, roi.Left, roi.Top));
                                    context.Barrier(dst.Data);
                                    context.Barrier(src.Data);
                                }
                            }

                            if (dst == temp)
                            {
                                temp.CopyTo(gpuImage);
                            }
                        }
                        break;
                    case BlurDirection.Vertical:
                        {
                            var src = temp;
                            var dst = gpuImage;

                            using (var context = device.CreateComputeContext())
                            {
                                for (var i = 0; i < repeat; i++)
                                {
                                    (src, dst) = (dst, src);
                                    context.For(roi.Width, roi.Height, new BlurVerticalProcess(dst.Data, src.Data, src.Width, src.Height, amount, (int)edgeRepeatMode, roi.Left, roi.Top));
                                    context.Barrier(dst.Data);
                                    context.Barrier(src.Data);
                                }
                            }

                            if (dst == temp)
                            {
                                temp.CopyTo(gpuImage);
                            }
                        }
                        break;
                }
            }

            return gpuImage;
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

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct BlurHorizontalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, float amount, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var c = new Float4();
            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                var tc = GetPixel(x + i, y);
                c += tc * tc.W;
                a += tc.W;
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var tc = GetPixel(x - edge, y);
                c += tc * tc.W * (edge - amount);
                a += tc.W * (edge - amount);

                tc = GetPixel(x + edge, y);
                c += tc * tc.W * (edge - amount);
                a += tc.W * (edge - amount);
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / (amount * 2.0F + 1.0F);
                result[y * width + x] = rc;
            }
            else
            {
                result[y * width + x] = 0.0F;
            }
        }

        Float4 GetPixel(int l, int y)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[y * width + Hlsl.Clamp(l, 0, width - 1)];
                case 2:
                    return image[y * width + (((l% width) + width) % width)];
                case 3:
                    {
                        var lw = width - 1;
                        var a = Hlsl.Abs(l);
                        var b = a % (lw * 2);
                        var c = b - Hlsl.Max(b - lw, 0) * 2;
                        return image[y * width + c];
                    }
                default:
                    if (l > -1 && l < width)
                    {
                        return image[y * width + l];
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
    readonly partial struct BlurVerticalProcess(ReadWriteBuffer<Float4> result, ReadWriteBuffer<Float4> image, int width, int height, float amount, int edgeRepeatMode, int startX, int startY) : IComputeShader
    {
        public void Execute()
        {
            var range = (int)Hlsl.Floor(amount);
            var x = ThreadIds.X + startX;
            var y = ThreadIds.Y + startY;

            var c = new Float4();
            var a = 0.0F;
            for (var i = -range; i <= range; i++)
            {
                var tc = GetPixel(x, y + i);
                c += tc * tc.W;
                a += tc.W;
            }

            var edge = (int)Hlsl.Ceil(amount);
            if (edge != range)
            {
                var tc = GetPixel(x, y - edge);
                c += tc * tc.W * (edge - amount);
                a += tc.W * (edge - amount);

                tc = GetPixel(x, y + edge);
                c += tc * tc.W * (edge - amount);
                a += tc.W * (edge - amount);
            }

            if (a > 0.0F)
            {
                var rc = c / a;
                rc.W = a / (amount * 2.0F + 1.0F);
                result[y * width + x] = rc;
            }
            else
            {
                result[y * width + x] = 0.0F;
            }
        }

        Float4 GetPixel(int x, int t)
        {
            switch (edgeRepeatMode)
            {
                case 1:
                    return image[Hlsl.Clamp(t, 0, height - 1) * width + x];
                case 2:
                    return image[(((t % height) + height) % height) * width + x];
                case 3:
                    {
                        var lh = height - 1;
                        var a = Hlsl.Abs(t);
                        var b = a % (lh * 2);
                        var c = b - Hlsl.Max(b - lh, 0) * 2;
                        return image[c * width + x];
                    }
                default:
                    if (t > -1 && t < height)
                    {
                        return image[t * width + x];
                    }
                    else
                    {
                        return 0.0F;
                    }
            }
        }
    }
}
