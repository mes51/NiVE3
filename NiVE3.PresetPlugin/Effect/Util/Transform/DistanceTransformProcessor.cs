using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ComputeSharp;
using NiVE3.Image;

namespace NiVE3.PresetPlugin.Effect.Util.Transform
{
    static class DistanceTransformProcessor
    {
        public static float[] ProcessCpu(NManagedImage image, float threshold = 0.5F)
        {
            const float EdgeValue = 1E30F;

            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var paddedImageWidth = imageWidth + 2;
            var paddedImageHeight = imageHeight + 2;
            var paddedImageDataLength = paddedImageWidth * paddedImageHeight;

            var temp = ArrayPool<float>.Shared.Rent(paddedImageDataLength);
            temp.AsSpan(0, paddedImageDataLength).Clear();
            var imageData = image.Data;
            Parallel.For(0, imageWidth, () =>
            {
                var v = ArrayPool<int>.Shared.Rent(paddedImageHeight);
                var z = ArrayPool<float>.Shared.Rent(paddedImageHeight + 1);

                return Tuple.Create(v, z);
            }, (x, _, state) =>
            {
                var imageDataSpan = imageData.AsSpan(x);
                var tempSpan = temp.AsSpan(x + 1);

                var k = 0;
                var v = state.Item1.AsSpan(0, paddedImageHeight);
                var z = state.Item2.AsSpan(0, paddedImageHeight + 1);
                v.Clear();
                z.Clear();
                z[0] = float.NegativeInfinity;
                z[1] = float.PositiveInfinity;

                float IsEdge(ReadOnlySpan<Vector4> imageDataSpan, int i) => i < 1 || i > imageHeight ? 0.0F : (imageDataSpan[(i - 1) * imageWidth].W >= threshold ? EdgeValue : 0.0F);

                for (var q = 1; q < paddedImageHeight; q++)
                {
                    var s = 0.0F;
                    var currentS = IsEdge(imageDataSpan, q) + q * q;
                    while (true)
                    {
                        var prevEdge = IsEdge(imageDataSpan, v[k]);
                        s = (currentS - (prevEdge + v[k] * v[k])) / (2.0F * (q - v[k]));
                        if (s <= z[k])
                        {
                            k--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    k++;

                    v[k] = q;
                    z[k] = s;
                    z[k + 1] = float.PositiveInfinity;
                }

                k = 0;

                for (var q = 0; q < paddedImageHeight; q++)
                {
                    while (z[k + 1] < q)
                    {
                        k++;
                    }

                    tempSpan[q * paddedImageWidth] = (q - v[k]) * (q - v[k]) + IsEdge(imageDataSpan, v[k]);
                }

                return state;
            }, state =>
            {
                var (v, z) = state;

                ArrayPool<int>.Shared.Return(v);
                ArrayPool<float>.Shared.Return(z);
            });

            var distanceMap = ArrayPool<float>.Shared.Rent(image.DataLength);
            distanceMap.AsSpan(0, image.DataLength).Clear();

            Parallel.For(0, imageHeight, () =>
            {
                var v = ArrayPool<int>.Shared.Rent(paddedImageWidth);
                var z = ArrayPool<float>.Shared.Rent(paddedImageWidth + 1);

                return Tuple.Create(v, z);
            }, (y, _, state) =>
            {
                var tempSpan = temp.AsSpan((y + 1) * paddedImageWidth, paddedImageWidth);
                var distanceMapSpan = distanceMap.AsSpan(y * imageWidth, imageWidth);

                var k = 0;
                var v = state.Item1.AsSpan(0, paddedImageWidth);
                var z = state.Item2.AsSpan(0, paddedImageWidth + 1);
                v.Clear();
                z.Clear();
                z[0] = float.NegativeInfinity;
                z[1] = float.PositiveInfinity;

                for (var q = 1; q < paddedImageWidth; q++)
                {
                    var s = 0.0F;
                    var currentS = tempSpan[q] + q * q;
                    while (true)
                    {
                        s = (currentS - (tempSpan[v[k]] + v[k] * v[k])) / (2.0F * (q - v[k]));
                        if (s <= z[k])
                        {
                            k--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    k++;

                    v[k] = q;
                    z[k] = s;
                    z[k + 1] = float.PositiveInfinity;
                }

                k = 0;

                for (var q = 0; q <= imageWidth; q++)
                {
                    while (z[k + 1] < q)
                    {
                        k++;
                    }

                    if (q > 0)
                    {
                        distanceMapSpan[q - 1] = MathF.Sqrt((q - v[k]) * (q - v[k]) + tempSpan[v[k]]);
                    }
                }

                return state;
            }, state =>
            {
                var (v, z) = state;

                ArrayPool<int>.Shared.Return(v);
                ArrayPool<float>.Shared.Return(z);
            });

            ArrayPool<float>.Shared.Return(temp);

            return distanceMap;
        }

        public static float[] InvertProcessCpu(NManagedImage image, float threshold = 0.5F)
        {
            threshold = 1.0F - threshold;

            const float EdgeValue = 1E30F;

            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var paddedImageWidth = imageWidth + 2;
            var paddedImageHeight = imageHeight + 2;
            var paddedImageDataLength = paddedImageWidth * paddedImageHeight;

            var temp = ArrayPool<float>.Shared.Rent(paddedImageDataLength);
            temp.AsSpan(0, paddedImageDataLength).Clear();
            var imageData = image.Data;

            Parallel.For(0, paddedImageWidth, () =>
            {
                var v = ArrayPool<int>.Shared.Rent(paddedImageHeight);
                var z = ArrayPool<float>.Shared.Rent(paddedImageHeight + 1);

                return Tuple.Create(v, z);
            }, (x, _, state) =>
            {
                var imageDataSpan = imageData.AsSpan(Math.Clamp(x - 1, 0, imageWidth - 1));
                var tempSpan = temp.AsSpan(x);

                var k = 0;
                var v = state.Item1.AsSpan(0, paddedImageHeight);
                var z = state.Item2.AsSpan(0, paddedImageHeight + 1);
                v.Clear();
                z.Clear();
                z[0] = float.NegativeInfinity;
                z[1] = float.PositiveInfinity;

                float IsEdge(ReadOnlySpan<Vector4> imageDataSpan, int i) => x < 1 || x > imageWidth || i < 1 || i > imageHeight ? EdgeValue : ((1.0F - imageDataSpan[(i - 1) * imageWidth].W) >= threshold ? EdgeValue : 0.0F);

                for (var q = 1; q < paddedImageHeight; q++)
                {
                    var s = 0.0F;
                    var currentS = IsEdge(imageDataSpan, q) + q * q;
                    while (true)
                    {
                        var prevEdge = IsEdge(imageDataSpan, v[k]);
                        s = (currentS - (prevEdge + v[k] * v[k])) / (2.0F * (q - v[k]));
                        if (s <= z[k])
                        {
                            k--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    k++;

                    v[k] = q;
                    z[k] = s;
                    z[k + 1] = float.PositiveInfinity;
                }

                k = 0;

                for (var q = 0; q < paddedImageHeight; q++)
                {
                    while (z[k + 1] < q)
                    {
                        k++;
                    }

                    tempSpan[q * paddedImageWidth] = (q - v[k]) * (q - v[k]) + IsEdge(imageDataSpan, v[k]);
                }

                return state;
            }, state =>
            {
                var (v, z) = state;

                ArrayPool<int>.Shared.Return(v);
                ArrayPool<float>.Shared.Return(z);
            });

            var distanceMap = ArrayPool<float>.Shared.Rent(image.DataLength);
            distanceMap.AsSpan(0, image.DataLength).Clear();

            Parallel.For(0, imageHeight, () =>
            {
                var v = ArrayPool<int>.Shared.Rent(paddedImageWidth);
                var z = ArrayPool<float>.Shared.Rent(paddedImageWidth + 1);

                return Tuple.Create(v, z);
            }, (y, _, state) =>
            {
                var tempSpan = temp.AsSpan((y + 1) * paddedImageWidth, paddedImageWidth);
                var distanceMapSpan = distanceMap.AsSpan(y * imageWidth, imageWidth);

                var k = 0;
                var v = state.Item1.AsSpan(0, paddedImageWidth);
                var z = state.Item2.AsSpan(0, paddedImageWidth + 1);
                v.Clear();
                z.Clear();
                z[0] = float.NegativeInfinity;
                z[1] = float.PositiveInfinity;

                for (var q = 1; q < paddedImageWidth; q++)
                {
                    var s = 0.0F;
                    var currentS = tempSpan[q] + q * q;
                    while (true)
                    {
                        s = (currentS - (tempSpan[v[k]] + v[k] * v[k])) / (2.0F * (q - v[k]));
                        if (s <= z[k])
                        {
                            k--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    k++;

                    v[k] = q;
                    z[k] = s;
                    z[k + 1] = float.PositiveInfinity;
                }

                k = 0;

                for (var q = 0; q <= imageWidth; q++)
                {
                    while (z[k + 1] < q)
                    {
                        k++;
                    }

                    if (q > 0)
                    {
                        distanceMapSpan[q - 1] = MathF.Sqrt((q - v[k]) * (q - v[k]) + tempSpan[v[k]]);
                    }
                }

                return state;
            }, state =>
            {
                var (v, z) = state;

                ArrayPool<int>.Shared.Return(v);
                ArrayPool<float>.Shared.Return(z);
            });

            ArrayPool<float>.Shared.Return(temp);

            return distanceMap;
        }

        public static ReadWriteBuffer<float> ProcessGpu(GraphicsDevice device, NGPUImage image, float threshold = 0.5F)
        {
            var paddedImageWidth = image.Width + 2;
            var paddedImageHeight = image.Height + 2;

            using var vBuffer = device.AllocateReadWriteBuffer<int>(paddedImageWidth * paddedImageHeight);
            using var zBuffer = device.AllocateReadWriteBuffer<float>((paddedImageWidth + 1) * (paddedImageHeight + 1));
            using var temp = device.AllocateReadWriteBuffer<float>(paddedImageWidth * paddedImageHeight);
            var distanceMap = device.AllocateReadWriteBuffer<float>(image.Width * image.Height);

            using (var context = device.CreateComputeContext())
            {
                context.For(image.Width, new DistanceTransformPass1Process(image.Data, temp, image.Width, image.Height, vBuffer, zBuffer, threshold));
                context.Barrier(temp);
                context.For(image.Height, new DistanceTransformPass2Process(temp, distanceMap, image.Width, vBuffer, zBuffer));
            }

            return distanceMap;
        }

        public static ReadWriteBuffer<float> InvertProcessGpu(GraphicsDevice device, NGPUImage image, float threshold = 0.5F)
        {
            var paddedImageWidth = image.Width + 2;
            var paddedImageHeight = image.Height + 2;

            using var vBuffer = device.AllocateReadWriteBuffer<int>(paddedImageWidth * paddedImageHeight);
            using var zBuffer = device.AllocateReadWriteBuffer<float>((paddedImageWidth + 1) * (paddedImageHeight + 1));
            using var temp = device.AllocateReadWriteBuffer<float>(paddedImageWidth * paddedImageHeight);
            var distanceMap = device.AllocateReadWriteBuffer<float>(image.Width * image.Height);

            using (var context = device.CreateComputeContext())
            {
                context.For(paddedImageWidth, new DistanceTransformInvertPass1Process(image.Data, temp, image.Width, image.Height, vBuffer, zBuffer, threshold));
                context.Barrier(temp);
                context.For(image.Height, new DistanceTransformPass2Process(temp, distanceMap, image.Width, vBuffer, zBuffer));
            }

            return distanceMap;
        }
    }

    [ThreadGroupSize(64, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DistanceTransformPass1Process(ReadWriteBuffer<Float4> image, ReadWriteBuffer<float> temp, int width, int height, ReadWriteBuffer<int> vBuffer, ReadWriteBuffer<float> zBuffer, float threshold) : IComputeShader
    {
        readonly int PaddedWidth = width + 2;

        readonly int PaddedHeight = height + 2;

        public void Execute()
        {
            var imageX = ThreadIds.X;
            var vBufferStartPos = ThreadIds.X * PaddedHeight;
            var zBufferStartPos = ThreadIds.X * (PaddedHeight + 1);

            zBuffer[zBufferStartPos] = float.NegativeInfinity;
            zBuffer[zBufferStartPos + 1] = float.PositiveInfinity;

            var k = 0;
            for (var q = 1; q < PaddedHeight; q++)
            {
                var s = 0.0F;
                var currentS = IsEdge(imageX, q) + q * q;
                while (true)
                {
                    var vk = vBuffer[vBufferStartPos + k];
                    var prevEdge = IsEdge(imageX, vk);
                    s = (currentS - (prevEdge + vk * vk)) / (2.0F * (q - vk));
                    if (s <= zBuffer[zBufferStartPos + k])
                    {
                        k--;
                    }
                    else
                    {
                        break;
                    }
                }

                k++;

                vBuffer[vBufferStartPos + k] = q;
                zBuffer[zBufferStartPos + k] = s;
                zBuffer[zBufferStartPos + k + 1] = float.PositiveInfinity;
            }

            k = 0;

            for (var q = 0; q < PaddedHeight; q++)
            {
                while (zBuffer[zBufferStartPos + k + 1] < q)
                {
                    k++;
                }

                var vk = vBuffer[vBufferStartPos + k];
                temp[q * PaddedWidth + imageX + 1] = (q - vk) * (q - vk) + IsEdge(imageX, vk);
            }
        }

        float IsEdge(int x, int y)
        {
            const float EdgeValue = 1E30F;

            if (y < 1 || y > height || image[(y - 1) * width + x].W < threshold)
            {
                return 0.0F;
            }
            else
            {
                return EdgeValue;
            }
        }
    }

    [ThreadGroupSize(64, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DistanceTransformInvertPass1Process(ReadWriteBuffer<Float4> image, ReadWriteBuffer<float> temp, int width, int height, ReadWriteBuffer<int> vBuffer, ReadWriteBuffer<float> zBuffer, float threshold) : IComputeShader
    {
        readonly int PaddedWidth = width + 2;

        readonly int PaddedHeight = height + 2;

        readonly float InvertedThreshold = 1.0F - threshold;

        public void Execute()
        {
            var imageX = ThreadIds.X;
            var vBufferStartPos = ThreadIds.X * PaddedHeight;
            var zBufferStartPos = ThreadIds.X * (PaddedHeight + 1);

            zBuffer[zBufferStartPos] = float.NegativeInfinity;
            zBuffer[zBufferStartPos + 1] = float.PositiveInfinity;

            var k = 0;
            for (var q = 1; q < PaddedHeight; q++)
            {
                var s = 0.0F;
                var currentS = IsEdge(imageX, q) + q * q;
                while (true)
                {
                    var vk = vBuffer[vBufferStartPos + k];
                    var prevEdge = IsEdge(imageX, vk);
                    s = (currentS - (prevEdge + vk * vk)) / (2.0F * (q - vk));
                    if (s <= zBuffer[zBufferStartPos + k])
                    {
                        k--;
                    }
                    else
                    {
                        break;
                    }
                }

                k++;

                vBuffer[vBufferStartPos + k] = q;
                zBuffer[zBufferStartPos + k] = s;
                zBuffer[zBufferStartPos + k + 1] = float.PositiveInfinity;
            }

            k = 0;

            for (var q = 0; q < PaddedHeight; q++)
            {
                while (zBuffer[zBufferStartPos + k + 1] < q)
                {
                    k++;
                }

                var vk = vBuffer[vBufferStartPos + k];
                temp[q * PaddedWidth + imageX] = (q - vk) * (q - vk) + IsEdge(imageX, vk);
            }
        }

        float IsEdge(int x, int y)
        {
            const float EdgeValue = 1E30F;

            if (x < 1 || x > width || y < 1 || y > height || (1.0F - image[(y - 1) * width + x - 1].W) >= InvertedThreshold)
            {
                return EdgeValue;
            }
            else
            {
                return 0.0F;
            }
        }
    }

    [ThreadGroupSize(64, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct DistanceTransformPass2Process(ReadWriteBuffer<float> temp, ReadWriteBuffer<float> distanceMap, int width, ReadWriteBuffer<int> vBuffer, ReadWriteBuffer<float> zBuffer) : IComputeShader
    {
        readonly int PaddedWidth = width + 2;

        public void Execute()
        {
            var outputLine = ThreadIds.X * width;
            var tempAndVBufferStartPos = (ThreadIds.X + 1) * PaddedWidth;
            var zBufferStartPos = (ThreadIds.X + 1) * (PaddedWidth + 1);

            for (var i = 0; i < PaddedWidth; i++)
            {
                vBuffer[tempAndVBufferStartPos + i] = 0;
                zBuffer[zBufferStartPos + i] = 0.0F;
            }

            zBuffer[zBufferStartPos + PaddedWidth] = 0.0F;
            zBuffer[zBufferStartPos] = float.NegativeInfinity;
            zBuffer[zBufferStartPos + 1] = float.PositiveInfinity;

            var k = 0;
            for (var q = 1; q < PaddedWidth; q++)
            {
                var s = 0.0F;
                var currentS = temp[tempAndVBufferStartPos + q] + q * q;
                while (true)
                {
                    var vk = vBuffer[tempAndVBufferStartPos + k];
                    s = (currentS - (temp[tempAndVBufferStartPos + vk] + vk * vk)) / (2.0F * (q - vk));
                    if (s <= zBuffer[zBufferStartPos + k])
                    {
                        k--;
                    }
                    else
                    {
                        break;
                    }
                }

                k++;

                vBuffer[tempAndVBufferStartPos + k] = q;
                zBuffer[zBufferStartPos + k] = s;
                zBuffer[zBufferStartPos + k + 1] = float.PositiveInfinity;
            }

            k = 0;

            for (var q = 0; q <= width; q++)
            {
                while (zBuffer[zBufferStartPos + k + 1] < q)
                {
                    k++;
                }

                if (q > 0)
                {
                    var vk = vBuffer[tempAndVBufferStartPos + k];
                    distanceMap[outputLine + q - 1] = Hlsl.Sqrt((q - vk) * (q - vk) + temp[tempAndVBufferStartPos + vk]);
                }
            }
        }
    }
}
