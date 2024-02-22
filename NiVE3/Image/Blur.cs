using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            var pz = (int)Math.Ceiling(vertical);
            var fmz = pz - vertical;
            var fz = 1.0F - fmz;
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var temp = ArrayPool<Vector4>.Shared.Rent(image.DataLength / 4);
            temp.AsSpan(0, image.DataLength / 4).Clear();

            if (vertical > 0.0F)
            {
                Parallel.For(0, imageWidth, delegate (int w)
                {
                    var count = 0.0F;
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    var data = MemoryMarshal.Cast<float, Vector4>(image.GetDataSpan());
                    var tempSpan = temp.AsSpan();
                    for (int h = -pz; h < imageHeight; h++)
                    {
                        int t = h - pz;
                        if (t > -1)
                        {
                            var p = data[t * imageWidth + w];
                            var ta = p.W * fz;
                            rgb -= p * ta;
                            a -= ta;
                            count -= fz;
                        }
                        t++;
                        if (fmz > 0.0F && t > -1)
                        {
                            var p = data[t * imageWidth + w];
                            var ta = p.W * fmz;
                            rgb -= p * ta;
                            a -= ta;
                            count -= fmz;
                        }
                        t = h + pz;
                        if (t < imageHeight)
                        {
                            var p = data[t * imageWidth + w];
                            var ta = p.W * fz;
                            rgb += p * ta;
                            a += ta;
                            count += fz;
                        }
                        t--;
                        if (fmz > 0.0F && t > -1 && t < imageHeight)
                        {
                            var p = data[t * imageWidth + w];
                            var ta = p.W * fmz;
                            rgb += p * ta;
                            a += ta;
                            count += fmz;
                        }
                        if (h > -1 && a > 0.0F)
                        {
                            var result = rgb / a;
                            result.W = a / count;
                            tempSpan[h * imageWidth + w] = result;
                        }
                    }
                });
            }
            else
            {
                MemoryMarshal.Cast<float, Vector4>(image.GetDataSpan()).CopyTo(temp);
            }

            if (horizontal > 0.0F)
            {
                pz = (int)MathF.Ceiling(horizontal);
                fmz = pz - horizontal;
                fz = 1.0F - fmz;
                Parallel.For(0, imageHeight, delegate (int h)
                {
                    var count = 0.0F;
                    var rgb = Vector4.Zero;
                    var a = 0.0F;
                    var data = MemoryMarshal.Cast<float, Vector4>(image.GetDataSpan());
                    var tempSpan = temp.AsSpan();
                    for (int w = -pz; w < imageWidth; w++)
                    {
                        int l = w - pz;
                        if (l > -1)
                        {
                            var p = tempSpan[h * imageWidth + l];
                            var ta = p.W * fz;
                            rgb -= p * ta;
                            a -= ta;
                            count -= fz;
                        }
                        l++;
                        if (fmz > 0.0F && l > -1)
                        {
                            var p = tempSpan[h * imageWidth + l];
                            var ta = p.W * fmz;
                            rgb -= p * ta;
                            a -= ta;
                            count -= fmz;
                        }
                        l = w + pz;
                        if (l < imageWidth)
                        {
                            var p = tempSpan[h * imageWidth + l];
                            var ta = p.W * fz;
                            rgb += p * ta;
                            a += ta;
                            count += fz;
                        }
                        l--;
                        if (fmz > 0.0F && l > -1 && l < imageWidth)
                        {
                            var p = tempSpan[h * imageWidth + l];
                            var ta = p.W * fmz;
                            rgb += p * ta;
                            a += ta;
                            count += fmz;
                        }
                        if (w > -1 && a > 0.0F)
                        {
                            var result = rgb / a;
                            result.W = a / count;
                            data[h * imageWidth + w] = result;
                        }
                    }
                });
            }
            else
            {
                temp.AsSpan(0, image.DataLength / 4).CopyTo(MemoryMarshal.Cast<float, Vector4>(image.GetDataSpan()));
            }

            ArrayPool<Vector4>.Shared.Return(temp, true);
        }
    }
}
