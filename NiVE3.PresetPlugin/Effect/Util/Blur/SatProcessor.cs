using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Image;

namespace NiVE3.PresetPlugin.Effect.Util.Blur
{
    // NOTE: floatだとバイリニアで補間したときに誤差が大きすぎて半径1以下で正しくぼかせないのでdoubleのみとする
    // TODO: 何かしら誤差が出ない or double非対応なGPUでも精度補償出来る方法を探す
    static class SatProcessor
    {
        /*
        public static NManagedImage ProcessCpu(NManagedImage managedImage, int horizontalMargin, int verticalMargin, EdgeRepeatMode edgeRepeatMode)
        {
            var satWidth = managedImage.Width + horizontalMargin * 2;
            var satHeight = managedImage.Height + verticalMargin * 2;
            var satImage = new NManagedImage(satWidth, satHeight);

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            var satImageData = satImage.Data;

            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    Parallel.For(0, satHeight, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan(y * satWidth, satWidth);
                        var imageDataSpan = imageData.AsSpan(CoordWrap.Wrap(y - verticalMargin, imageHeight) * imageWidth, imageWidth);

                        var sum = Vector4.Zero;
                        for (var x = 0; x < satWidth; x++)
                        {
                            var color = imageDataSpan[CoordWrap.Wrap(x - horizontalMargin, imageWidth)];
                            var a = color.W;
                            color *= a;
                            color.W = a;
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                    });
                    break;
                case EdgeRepeatMode.Repeat:
                    Parallel.For(0, satHeight, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan(y * satWidth, satWidth);
                        var imageDataSpan = imageData.AsSpan(CoordWrap.Repeat(y - verticalMargin, imageHeight) * imageWidth, imageWidth);

                        var sum = Vector4.Zero;
                        for (var x = 0; x < satWidth; x++)
                        {
                            var color = imageDataSpan[CoordWrap.Repeat(x - horizontalMargin, imageWidth)];
                            var a = color.W;
                            color *= a;
                            color.W = a;
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                    });
                    break;
                case EdgeRepeatMode.Mirror:
                    Parallel.For(0, satHeight, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan(y * satWidth, satWidth);
                        var imageDataSpan = imageData.AsSpan(CoordWrap.Mirror(y - verticalMargin, imageHeight) * imageWidth, imageWidth);

                        var sum = Vector4.Zero;
                        for (var x = 0; x < satWidth; x++)
                        {
                            var color = imageDataSpan[CoordWrap.Mirror(x - horizontalMargin, imageWidth)];
                            var a = color.W;
                            color *= a;
                            color.W = a;
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                    });
                    break;
                default:
                    Parallel.For(verticalMargin, satHeight - verticalMargin, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan(y * satWidth, satWidth);
                        var imageDataSpan = imageData.AsSpan((y - verticalMargin) * imageWidth, imageWidth);

                        var sum = Vector4.Zero;
                        for (int x = horizontalMargin, limit = satWidth - horizontalMargin; x < limit; x++)
                        {
                            var color = imageDataSpan[x - horizontalMargin];
                            var a = color.W;
                            color *= a;
                            color.W = a;
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                        satImageDataSpan[(satWidth - horizontalMargin)..].Fill(satImageDataSpan[satWidth - horizontalMargin - 1]);
                    });
                    break;
            }

            Parallel.For(0, satWidth, x =>
            {
                var sum = satImageData[x];
                for (var y = 1; y < satHeight; y++)
                {
                    var pos = y * satWidth + x;
                    sum += satImageData[pos];
                    satImageData[pos] = sum;
                }
            });

            return satImage;
        }
        */

        public static (int satWidth, int satHeight, Vector256<double>[] data) ProcessCpuDouble(NManagedImage managedImage, int horizontalMargin, int verticalMargin, EdgeRepeatMode edgeRepeatMode)
        {
            var satWidth = managedImage.Width + horizontalMargin * 2;
            var satHeight = managedImage.Height + verticalMargin * 2;
            var satImageData = ArrayPool<Vector256<double>>.Shared.Rent(satWidth * satHeight);

            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var imageData = managedImage.Data;
            switch (edgeRepeatMode)
            {
                case EdgeRepeatMode.Wrap:
                    Parallel.For(0, satHeight, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan(y * satWidth, satWidth);
                        var imageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(imageData.AsSpan(CoordWrap.Wrap(y - verticalMargin, imageHeight) * imageWidth, imageWidth));

                        var sum = Vector256<double>.Zero;
                        for (var x = 0; x < satWidth; x++)
                        {
                            var color = Avx.ConvertToVector256Double(imageDataSpan[CoordWrap.Wrap(x - horizontalMargin, imageWidth)]);
                            var a = color.GetElement(3);
                            color *= Vector256.Create(a, a, a, 1.0);
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                    });
                    break;
                case EdgeRepeatMode.Repeat:
                    Parallel.For(0, satHeight, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan(y * satWidth, satWidth);
                        var imageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(imageData.AsSpan(CoordWrap.Repeat(y - verticalMargin, imageHeight) * imageWidth, imageWidth));

                        var sum = Vector256<double>.Zero;
                        for (var x = 0; x < satWidth; x++)
                        {
                            var color = Avx.ConvertToVector256Double(imageDataSpan[CoordWrap.Repeat(x - horizontalMargin, imageWidth)]);
                            var a = color.GetElement(3);
                            color *= Vector256.Create(a, a, a, 1.0);
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                    });
                    break;
                case EdgeRepeatMode.Mirror:
                    Parallel.For(0, satHeight, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan(y * satWidth, satWidth);
                        var imageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(imageData.AsSpan(CoordWrap.Mirror(y - verticalMargin, imageHeight) * imageWidth, imageWidth));

                        var sum = Vector256<double>.Zero;
                        for (var x = 0; x < satWidth; x++)
                        {
                            var color = Avx.ConvertToVector256Double(imageDataSpan[CoordWrap.Mirror(x - horizontalMargin, imageWidth)]);
                            var a = color.GetElement(3);
                            color *= Vector256.Create(a, a, a, 1.0);
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                    });
                    break;
                default:
                    Parallel.For(verticalMargin, satHeight - verticalMargin, y =>
                    {
                        var satImageDataSpan = satImageData.AsSpan((y * satWidth), satWidth);
                        var imageDataSpan = MemoryMarshal.Cast<Vector4, Vector128<float>>(imageData.AsSpan((y - verticalMargin) * imageWidth, imageWidth));

                        var sum = Vector256<double>.Zero;
                        for (int x = horizontalMargin, limit = satWidth - horizontalMargin; x < limit; x++)
                        {
                            var color = Avx.ConvertToVector256Double(imageDataSpan[x - horizontalMargin]);
                            var a = color.GetElement(3);
                            color *= Vector256.Create(a, a, a, 1.0);
                            sum += color;
                            satImageDataSpan[x] = sum;
                        }
                        satImageDataSpan[(satWidth - horizontalMargin)..].Fill(satImageDataSpan[satWidth - horizontalMargin - 1]);
                    });
                    break;
            }

            Parallel.For(0, satWidth, x =>
            {
                var sum = satImageData[x];
                for (var y = 1; y < satHeight; y++)
                {
                    var pos = y * satWidth + x;
                    sum += satImageData[pos];
                    satImageData[pos] = sum;
                }
            });

            return (satWidth, satHeight, satImageData);
        }

        /*
        public static NGPUImage ProcessGpu(GraphicsDevice device, NGPUImage gpuImage, int horizontalMargin, int verticalMargin, EdgeRepeatMode edgeRepeatMode)
        {
            var satWidth = gpuImage.Width + horizontalMargin * 2;
            var satHeight = gpuImage.Height + verticalMargin * 2;
            var satHalfWidth = satWidth / 2;
            var satHalfHeight = satHeight / 2;
            var satImage = new NGPUImage(satWidth, satHeight, device);

            using (var context = device.CreateComputeContext())
            {
                context.For(2, satHeight, new SatStep1Process(gpuImage.Data, gpuImage.Width, gpuImage.Height, satImage.Data, satWidth, satHalfWidth, horizontalMargin, verticalMargin, (int)edgeRepeatMode));
                context.Barrier(satImage.Data);

                context.For(satWidth - satHalfWidth, satHeight, new SatStep2Process(satImage.Data, satWidth, satHalfWidth));
                context.Barrier(satImage.Data);

                context.For(satWidth, 2, new SatStep3Process(satImage.Data, satWidth, satHeight, satHalfHeight));
                context.Barrier(satImage.Data);

                context.For(satWidth, satHeight - satHalfHeight, new SatStep4Prodcess(satImage.Data, satWidth, satHalfHeight));
            }

            return satImage;
        }
        */
    }

    /*
    [ThreadGroupSize(2, 32, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SatStep1Process(ReadWriteBuffer<Float4> image, int width, int height, ReadWriteBuffer<Float4> satImage, int satWidth, int satHalfWidth, int horizontalMargin, int verticalMargin, int edgeRepeatMode) : IComputeShader
    {
        public void Execute()
        {
            var x = GroupIds.X == 0 ? 0 : satHalfWidth;
            var limit = GroupIds.X == 0 ? satHalfWidth : satWidth;

            var imageX = x - horizontalMargin;
            var imageY = ThreadIds.Y - verticalMargin;
            var line = ThreadIds.Y * satWidth;
            var sum = Float4.Zero;
            for (; x < limit; x++, imageX++)
            {
                var color = GetPixel(imageX, imageY);
                sum += new Float4(color.XYZ * color.W, color.W);
                satImage[line + x] = sum;
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
    readonly partial struct SatStep2Process(ReadWriteBuffer<Float4> satImage, int satWidth, int satHalfWidth) : IComputeShader
    {
        public void Execute()
        {
            var x = ThreadIds.X + satHalfWidth;
            var line = ThreadIds.Y * satWidth;

            satImage[line + x] += satImage[line + satHalfWidth - 1];
        }
    }

    [ThreadGroupSize(32, 2, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SatStep3Process(ReadWriteBuffer<Float4> satImage, int satWidth, int satHeight, int satHalfHeight) : IComputeShader
    {
        public void Execute()
        {
            var y = GroupIds.Y == 0 ? 0 : satHalfHeight;
            var limit = GroupIds.Y == 0 ? satHalfHeight : satHeight;

            var sum = Float4.Zero;
            for (;  y < limit; y++)
            {
                var pos = y * satWidth + ThreadIds.X;
                sum += satImage[pos];
                satImage[pos] = sum;
            }
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SatStep4Prodcess(ReadWriteBuffer<Float4> satImage, int satWidth, int satHalfHeight) : IComputeShader
    {
        public void Execute()
        {
            var y = ThreadIds.Y + satHalfHeight;

            satImage[y * satWidth + ThreadIds.X] += satImage[(satHalfHeight - 1) * satWidth + ThreadIds.X];
        }
    }
    */
}
