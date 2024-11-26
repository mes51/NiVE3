using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Image
{
    static class Blur
    {
        public static void BoxBlur(NManagedImage image, float horizontal, float vertical)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageData = image.Data;
            var temp = ArrayPool<Vector4>.Shared.Rent(imageData.Length);
            temp.AsSpan().Clear();

            if (horizontal > 0.0F)
            {
                var pz = (int)Math.Ceiling(horizontal);
                var fmz = pz - horizontal;
                var fz = 1.0F - fmz;
                var count = horizontal * 2.0F + 1.0F;

                Parallel.For(0, imageHeight, h =>
                {
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    var data = imageData.AsSpan(0, image.DataLength);

                    {
                        var w = -pz - 1;
                        if (fmz > 0.0F)
                        {
                            var p = GetPixelForX(data, imageWidth, w, h);
                            var ta = p.W * fz;
                            rgb += p * ta;
                            a += ta;

                            p = GetPixelForX(data, imageWidth, pz - 1, h);
                            ta = p.W * fz;
                            rgb += p * ta;
                            a += ta;
                        }
                        for (var limit = pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                        {
                            var p = GetPixelForX(data, imageWidth, w, h);
                            var ta = p.W;
                            rgb += p * ta;
                            a += ta;
                        }
                    }

                    for (var w = 0; w < imageWidth; w++)
                    {
                        var l = w - pz - 1;
                        var p = GetPixelForX(data, imageWidth, l, h);
                        var ta = p.W * fz;
                        rgb -= p * ta;
                        a -= ta;
                        l++;

                        if (fmz > 0.0F)
                        {
                            p = GetPixelForX(data, imageWidth, l, h);
                            ta = p.W * fmz;
                            rgb -= p * ta;
                            a -= ta;
                        }
                        l = w + pz;

                        p = GetPixelForX(data, imageWidth, l, h);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                        l--;

                        if (fmz > 0.0F)
                        {
                            p = GetPixelForX(data, imageWidth, l, h);
                            ta = p.W * fmz;
                            rgb += p * ta;
                            a += ta;
                        }

                        if (w > -1 && a > 0.0F)
                        {
                            var result = rgb / a;
                            result.W = a / count;
                            temp[w * imageHeight + h] = result;
                        }
                    }
                });
            }
            else
            {
                Parallel.For(0, imageHeight, y =>
                {
                    var data = imageData.AsSpan(y * imageWidth, imageWidth);
                    for (var x = 0; x < imageWidth; x++)
                    {
                        temp[x * imageHeight + y] = data[x];
                    }
                });
            }

            if (vertical > 0.0F)
            {
                var pz = (int)Math.Ceiling(vertical);
                var fmz = pz - vertical;
                var fz = 1.0F - fmz;
                var count = vertical * 2.0F + 1.0F;

                Parallel.For(0, imageWidth, w =>
                {
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    var data = temp.AsSpan(0, image.DataLength);

                    {
                        var h = -pz - 1;
                        if (fmz > 0.0F)
                        {
                            var p = GetPixelForX(temp, imageHeight, h, w);
                            var ta = p.W * fz;
                            rgb += p * ta;
                            a += ta;

                            p = GetPixelForX(temp, imageHeight, pz - 1, w);
                            ta = p.W * fz;
                            rgb += p * ta;
                            a += ta;

                            h++;
                        }
                        for (var limit = pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                        {
                            var p = GetPixelForX(temp, imageHeight, h, w);
                            var ta = p.W;
                            rgb += p * ta;
                            a += ta;
                        }
                    }

                    for (var h = 0; h < imageHeight; h++)
                    {
                        var t = h - pz - 1;
                        var p = GetPixelForX(temp, imageHeight, t, w);
                        var ta = p.W * fz;
                        rgb -= p * ta;
                        a -= ta;
                        t++;

                        if (fmz > 0.0F)
                        {
                            p = GetPixelForX(temp, imageHeight, t, w);
                            ta = p.W * fmz;
                            rgb -= p * ta;
                            a -= ta;
                        }
                        t = h + pz;

                        p = GetPixelForX(temp, imageHeight, t, w);
                        ta = p.W * fz;
                        rgb += p * ta;
                        a += ta;
                        t--;

                        if (fmz > 0.0F)
                        {
                            p = GetPixelForX(temp, imageHeight, t, w);
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
            }
            else
            {
                Parallel.For(0, imageWidth, x =>
                {
                    var data = temp.AsSpan(x * imageHeight, imageHeight);
                    for (var y = 0; y < imageHeight; y++)
                    {
                        imageData[y * imageWidth + x] = data[y];
                    }
                });
            }

            ArrayPool<Vector4>.Shared.Return(temp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 GetPixelForX(Span<Vector4> data, int stride, int x, int y)
        {
            if (x > -1 && x < stride)
            {
                return data[y * stride + x];
            }
            else
            {
                return Vector4.Zero;
            }
        }
    }
}
