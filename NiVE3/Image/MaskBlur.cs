using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.InternalShader.Mask;

namespace NiVE3.Image
{
    static class MaskBlur
    {
        public static void ProcessGpu(GraphicsDevice device, GPURasterizedMaskImage gpuMask, float horizontal, float vertical)
        {
            if (horizontal <= 0.0F && vertical <= 0.0F)
            {
                return;
            }

            using var temp = new GPURasterizedMaskImage(gpuMask.Width, gpuMask.Height, device);

            if (horizontal > 0.0F && vertical > 0.0F)
            {
                using var context = device.CreateComputeContext();
                context.For(gpuMask.Width, gpuMask.Height, new MaskBlurHorizontal(gpuMask.Data, temp.Data, gpuMask.Width, horizontal));
                context.Barrier(gpuMask.Data);
                context.For(gpuMask.Width, gpuMask.Height, new MaskBlurVertical(temp.Data, gpuMask.Data, gpuMask.Width, gpuMask.Height, vertical));
            }
            else if (horizontal > 0.0F)
            {
                gpuMask.CopyTo(temp);
                device.For(gpuMask.Width, gpuMask.Height, new MaskBlurHorizontal(temp.Data, gpuMask.Data, gpuMask.Width, horizontal));
            }
            else
            {
                gpuMask.CopyTo(temp);
                device.For(gpuMask.Width, gpuMask.Height, new MaskBlurVertical(temp.Data, gpuMask.Data, gpuMask.Width, gpuMask.Height, vertical));
            }
        }

        public static void ProcessCpu(ManagedRasterizedMaskImage managedMask, float horizontal, float vertical)
        {
            if (horizontal <= 0.0F && vertical <= 0.0F)
            {
                return;
            }

            var imageWidth = managedMask.Width;
            var imageHeight = managedMask.Height;
            var maskData = managedMask.Data;
            var temp = ArrayPool<float>.Shared.Rent(maskData.Length);
            temp.AsSpan().Clear();

            if (horizontal > 0.0F)
            {
                var pz = (int)Math.Ceiling(horizontal);
                var fmz = pz - horizontal;
                var fz = 1.0F - fmz;
                var count = horizontal * 2.0F + 1.0F;

                Parallel.For(0, imageHeight, h =>
                {
                    var a = 0.0F;
                    var data = maskData.AsSpan(0, managedMask.DataLength);

                    {
                        var w = -pz - 1;
                        if (fmz > 0.0F)
                        {
                            a += GetPixelForX(data, imageWidth, w, h) * fz;
                            a += GetPixelForX(data, imageWidth, pz - 1, h) * fz;
                        }
                        for (var limit = pz - (fmz > 0.0F ? 2 : 1); w <= limit; w++)
                        {
                            a += GetPixelForX(data, imageWidth, w, h);
                        }
                    }

                    for (var w = 0; w < imageWidth; w++)
                    {
                        var l = w - pz - 1;
                        a -= GetPixelForX(data, imageWidth, l, h) * fz;
                        l++;

                        if (fmz > 0.0F)
                        {
                            a -= GetPixelForX(data, imageWidth, l, h) * fmz;
                        }
                        l = w + pz;

                        a += GetPixelForX(data, imageWidth, l, h) * fz;
                        l--;

                        if (fmz > 0.0F)
                        {
                            a += GetPixelForX(data, imageWidth, l, h) * fmz;
                        }

                        if (w > -1 && a > 0.0F)
                        {
                            temp[w * imageHeight + h] = a / count;
                        }
                    }
                });
            }
            else
            {
                Parallel.For(0, imageHeight, y =>
                {
                    var data = maskData.AsSpan(y * imageWidth, imageWidth);
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
                    var a = 0.0F;
                    var data = temp.AsSpan(0, managedMask.DataLength);

                    {
                        var h = -pz - 1;
                        if (fmz > 0.0F)
                        {
                            a += GetPixelForX(temp, imageHeight, h, w) * fz;
                            a += GetPixelForX(temp, imageHeight, pz - 1, w) * fz;

                            h++;
                        }
                        for (var limit = pz - (fmz > 0.0F ? 2 : 1); h <= limit; h++)
                        {
                            a += GetPixelForX(temp, imageHeight, h, w);
                        }
                    }

                    for (var h = 0; h < imageHeight; h++)
                    {
                        var t = h - pz - 1;
                        a -= GetPixelForX(temp, imageHeight, t, w) * fz;
                        t++;

                        if (fmz > 0.0F)
                        {
                            a -= GetPixelForX(temp, imageHeight, t, w) * fmz;
                        }
                        t = h + pz;

                        a += GetPixelForX(temp, imageHeight, t, w) * fz;
                        t--;

                        if (fmz > 0.0F)
                        {
                            a += GetPixelForX(temp, imageHeight, t, w) * fmz;
                        }

                        if (a > 0.0F)
                        {
                            maskData[h * imageWidth + w] = a / count;
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
                        maskData[y * imageWidth + x] = data[y];
                    }
                });
            }

            ArrayPool<float>.Shared.Return(temp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetPixelForX(Span<float> data, int stride, int x, int y)
        {
            if (x > -1 && x < stride)
            {
                return data[y * stride + x];
            }
            else
            {
                return 0.0F;
            }
        }
    }
}
