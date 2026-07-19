using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;

namespace NiVE3.PresetPlugin.Effect.Util.Blur
{
    static class FastGaussianBlurProcessor
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessCpu(NManagedImage image, ROI roi, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            var sigmaX = CalcSigma(horizontalAmount);
            var sigmaY = CalcSigma(verticalAmount);

            SlidingDctGaussianFilterCpu.Apply(image.Data, image.Width, image.Height, roi, sigmaX, sigmaY, edgeRepeatMode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessGpu(GraphicsDevice device, NGPUImage image, ROI roi, float horizontalAmount, float verticalAmount, EdgeRepeatMode edgeRepeatMode)
        {
            var sigmaX = CalcSigma(horizontalAmount);
            var sigmaY = CalcSigma(verticalAmount);

            SlidingDctGaussianFilterGpu.Apply(device, image.Data, image.Width, image.Height, roi, sigmaX, sigmaY, edgeRepeatMode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalcSigma(float radius)
        {
            const double Sigma = 0.1;
            return (float)Math.Sqrt(Math.Pow(radius + 1.0, 2.0) * Sigma);
        }
    }

    // https://www.jstage.jst.go.jp/article/mta/3/1/3_12/_article

    file static class SlidingDctGaussianFilterCpu
    {
        private const int VerticalStripWidth = 128;

        const int SpectrumTerms = 3; // 1 ～ 3

        public static void Apply(Vector4[] source, int width, int height, ROI roi, float sigmaX, float sigmaY, EdgeRepeatMode edgeRepeatMode)
        {
            var roiWidth = roi.Width;

            var cX = FilterCoefficients.Create(sigmaX, SpectrumTerms);
            var cY = FilterCoefficients.Create(sigmaY, SpectrumTerms);

            var yBegin = Math.Max(0, roi.Top - cY.Radius - 1);
            var yEnd = Math.Min(height - 1, roi.Top + roi.Height - 1 + cY.Radius + 1);

            var tempBuffer = ArrayPool<Vector4>.Shared.Rent((yEnd - yBegin + 1) * roiWidth);

            Parallel.For(yBegin, yEnd, y =>
            {
                HorizontalLine(source.AsSpan(y * width, width), tempBuffer.AsSpan(y * roiWidth, roiWidth), roi.Left, width, cX, edgeRepeatMode);
            });

            var stripCount = (roiWidth + VerticalStripWidth - 1) / VerticalStripWidth;
            Parallel.For(0, stripCount, s =>
            {
                var stripBegin = s * VerticalStripWidth;
                var stripEnd = Math.Min(roiWidth, stripBegin + VerticalStripWidth);
                VerticalStrip(tempBuffer, source, width, roi, yBegin, stripBegin, stripEnd, height, cY, edgeRepeatMode);
            });

            ArrayPool<Vector4>.Shared.Return(tempBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CalcCoord(int p, int size, EdgeRepeatMode edgeRepeatMode)
        {
            if (p >= 0 && p < size)
            {
                return p;
            }

            switch (edgeRepeatMode)
            {

                case EdgeRepeatMode.Wrap:
                    return CoordWrap.Wrap(p, size);
                case EdgeRepeatMode.Repeat:
                    return CoordWrap.Repeat(p, size);
                case EdgeRepeatMode.Mirror:
                    return CoordWrap.Mirror(p, size);
                default:
                    return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector4 GetPixelFromLine(ReadOnlySpan<Vector4> line, int p, int width, EdgeRepeatMode edgeRepeatMode)
        {
            var mapped = CalcCoord(p, width, edgeRepeatMode);

            if (mapped < 0)
            {
                return Vector4.Zero;
            }

            var value = line[mapped];
            return value * new Vector4(value.W, value.W, value.W, 1.0F);
        }

        static void HorizontalLine(ReadOnlySpan<Vector4> imageDataSpan, Span<Vector4> dst, int startX, int width, FilterCoefficients c, EdgeRepeatMode edgeRepeatMode)
        {
            var radius = c.Radius;
            var length = dst.Length;

            var sum = Vector4.Zero;
            var p1 = Vector4.Zero;
            var p2 = Vector4.Zero;
            var p3 = Vector4.Zero;
            var q1 = Vector4.Zero;
            var q2 = Vector4.Zero;
            var q3 = Vector4.Zero;

            for (var t = -radius; t <= radius; t++)
            {
                var fPrev = GetPixelFromLine(imageDataSpan, startX + t - 1, width, edgeRepeatMode);
                var fCurr = GetPixelFromLine(imageDataSpan, startX + t, width, edgeRepeatMode);

                var angle = c.Omega * t;
                var w1 = c.A1 * MathF.Cos(angle);
                var w2 = c.A2 * MathF.Cos(angle * 2.0F);
                var w3 = c.A3 * MathF.Cos(angle * 3.0F);

                sum += fCurr;
                p1 += w1 * fPrev;
                p2 += w2 * fPrev;
                p3 += w3 * fPrev;
                q1 += w1 * fCurr;
                q2 += w2 * fCurr;
                q3 += w3 * fCurr;
            }

            for (var x = 0; x < length; x++)
            {
                dst[x] = c.A0 * sum + q1 + q2 + q3;

                var p = startX + x;
                var fL2 = GetPixelFromLine(imageDataSpan, p - radius - 1, width, edgeRepeatMode);
                var fL1 = GetPixelFromLine(imageDataSpan, p - radius, width, edgeRepeatMode);
                var fR1 = GetPixelFromLine(imageDataSpan, p + radius, width, edgeRepeatMode);
                var fR2 = GetPixelFromLine(imageDataSpan, p + radius + 1, width, edgeRepeatMode);
                var delta = fL2 - fL1 - fR1 + fR2;

                var n1 = c.C1 * q1 - p1 + c.D1 * delta;
                var n2 = c.C2 * q2 - p2 + c.D2 * delta;
                var n3 = c.C3 * q3 - p3 + c.D3 * delta;

                p1 = q1;
                p2 = q2;
                p3 = q3;
                q1 = n1;
                q2 = n2;
                q3 = n3;

                sum += fR2 - fL1;
            }
        }

        static void VerticalStrip(ReadOnlySpan<Vector4> src, Span<Vector4> dst, int width, ROI roi, int yBegin, int stripBegin, int stripEnd, int height, FilterCoefficients c, EdgeRepeatMode edgeRepeatMode)
        {
            var radius = c.Radius;
            var strip = stripEnd - stripBegin;

            var pool = ArrayPool<Vector4>.Shared;
            var sum = pool.Rent(strip);
            var p1 = pool.Rent(strip);
            var p2 = pool.Rent(strip);
            var p3 = pool.Rent(strip);
            var q1 = pool.Rent(strip);
            var q2 = pool.Rent(strip);
            var q3 = pool.Rent(strip);

            p1.AsSpan(0, strip).Clear();
            p2.AsSpan(0, strip).Clear();
            p3.AsSpan(0, strip).Clear();
            q1.AsSpan(0, strip).Clear();
            q2.AsSpan(0, strip).Clear();
            q3.AsSpan(0, strip).Clear();

            var roiWidth = roi.Width;
            var roiHeight = roi.Height;
            var startX = roi.Left;
            var startY = roi.Top;

            int CalcRow(int p)
            {
                var mapped = CalcCoord(p, height, edgeRepeatMode);
                return mapped < 0 ? -1 : (mapped - yBegin) * roiWidth + stripBegin;
            }

            for (var t = -radius; t <= radius; t++)
            {
                var rowPrev = CalcRow(startY + t - 1);
                var rowCurr = CalcRow(startY + t);

                var angle = c.Omega * t;
                var w1 = c.A1 * MathF.Cos(angle);
                var w2 = c.A2 * MathF.Cos(angle * 2.0F);
                var w3 = c.A3 * MathF.Cos(angle * 3.0F);

                for (var i = 0; i < strip; i++)
                {
                    var fPrev = rowPrev < 0 ? Vector4.Zero : src[rowPrev + i];
                    var fCurr = rowCurr < 0 ? Vector4.Zero : src[rowCurr + i];

                    sum[i] += fCurr;
                    p1[i] += w1 * fPrev;
                    p2[i] += w2 * fPrev;
                    p3[i] += w3 * fPrev;
                    q1[i] += w1 * fCurr;
                    q2[i] += w2 * fCurr;
                    q3[i] += w3 * fCurr;
                }
            }

            for (var y = 0; y < roiHeight; y++)
            {
                var rowOut = (startY + y) * width + startX + stripBegin;
                var rowL2 = CalcRow(startY + y - radius - 1);
                var rowL1 = CalcRow(startY + y - radius);
                var rowR1 = CalcRow(startY + y + radius);
                var rowR2 = CalcRow(startY + y + radius + 1);

                for (var i = 0; i < strip; i++)
                {
                    var result = c.A0 * sum[i] + q1[i] + q2[i] + q3[i];
                    if (result.W > 1E-6F)
                    {
                        var a = result.W;
                        result /= a;
                        result.W = a;
                    }
                    else
                    {
                        result = Const.EmptyPixel;
                    }
                    dst[rowOut + i] = result;

                    var fL2 = rowL2 < 0 ? Vector4.Zero : src[rowL2 + i];
                    var fL1 = rowL1 < 0 ? Vector4.Zero : src[rowL1 + i];
                    var fR1 = rowR1 < 0 ? Vector4.Zero : src[rowR1 + i];
                    var fR2 = rowR2 < 0 ? Vector4.Zero : src[rowR2 + i];
                    var delta = fL2 - fL1 - fR1 + fR2;

                    var n1 = c.C1 * q1[i] - p1[i] + c.D1 * delta;
                    var n2 = c.C2 * q2[i] - p2[i] + c.D2 * delta;
                    var n3 = c.C3 * q3[i] - p3[i] + c.D3 * delta;

                    p1[i] = q1[i];
                    p2[i] = q2[i];
                    p3[i] = q3[i];
                    q1[i] = n1;
                    q2[i] = n2;
                    q3[i] = n3;

                    sum[i] += fR2 - fL1;
                }
            }

            pool.Return(sum);
            pool.Return(p1);
            pool.Return(p2);
            pool.Return(p3);
            pool.Return(q1);
            pool.Return(q2);
            pool.Return(q3);
        }
    }

    file static class SlidingDctGaussianFilterGpu
    {
        const int SpectrumTerms = 3; // 1 ～ 3

        public static void Apply(GraphicsDevice device, ReadWriteBuffer<Float4> source, int width, int height, ROI roi, float sigmaX, float sigmaY, EdgeRepeatMode edgeRepeatMode)
        {
            var roiWidth = roi.Width;
            var roiHeight = roi.Height;

            var cX = FilterCoefficients.Create(sigmaX, SpectrumTerms);
            var cY = FilterCoefficients.Create(sigmaY, SpectrumTerms);

            var yBegin = Math.Max(0, roi.Top - cY.Radius - 1);
            var yEnd = Math.Min(height - 1, roi.Top + roiHeight - 1 + cY.Radius + 1);
            var lineCount = yEnd - yBegin + 1;

            using var tempBuffer = device.AllocateReadWriteBuffer<Float4>(lineCount * roiWidth);

            device.For(
                lineCount,
                new SlidingDct5LineShader(
                    source,
                    tempBuffer,
                    width,
                    roi.Left,
                    yBegin * width,
                    width,
                    1,
                    0,
                    roiWidth,
                    1,
                    cX.Radius,
                    (int)edgeRepeatMode,
                    true,
                    cX.ToGpu()
                )
            );

            device.For(
                roiWidth,
                new SlidingDct5LineShader(
                    tempBuffer,
                    source,
                    height,
                    roi.Top,
                    -yBegin * roiWidth,
                    1,
                    roiWidth,
                    roi.Top * width + roi.Left,
                    1,
                    width,
                    cY.Radius,
                    (int)edgeRepeatMode,
                    false,
                    cY.ToGpu()
                )
            );
        }
    }

    file class FilterCoefficients
    {
        public readonly int Radius;

        public readonly float Omega;

        public readonly float A0;

        public readonly float A1;

        public readonly float A2;

        public readonly float A3;

        public readonly float C1;

        public readonly float C2;

        public readonly float C3;

        public readonly float D1;

        public readonly float D2;

        public readonly float D3;

        private FilterCoefficients(int radius, double omega, ReadOnlySpan<double> a)
        {
            Radius = radius;
            Omega = (float)omega;
            A0 = (float)a[0];
            A1 = (float)a[1];
            A2 = (float)a[2];
            A3 = (float)a[3];
            C1 = (float)(2.0 * Math.Cos(omega));
            C2 = (float)(2.0 * Math.Cos(omega * 2.0));
            C3 = (float)(2.0 * Math.Cos(omega * 3.0));
            D1 = (float)(a[1] * Math.Cos(omega * radius));
            D2 = (float)(a[2] * Math.Cos(omega * 2.0 * radius));
            D3 = (float)(a[3] * Math.Cos(omega * 3.0 * radius));
        }

        public static FilterCoefficients Identity { get; } = CreateIdentity();

        public GpuFilterCoefficients ToGpu()
        {
            return new GpuFilterCoefficients(Omega, new Float4(A0, A1, A2, A3), C1, C2, C3, D1, D2, D3);
        }

        static FilterCoefficients CreateIdentity()
        {
            Span<double> a = [1.0, 0.0, 0.0, 0.0];
            return new FilterCoefficients(0, 2.0 * Math.PI, a);
        }

        public static FilterCoefficients Create(double sigma, int spectrumTerms)
        {
            if (sigma <= 0.0)
            {
                return Identity;
            }

            var radius = OptimizeRadius(sigma, spectrumTerms);
            var omega = 2.0 * Math.PI / (2 * radius + 1);

            Span<double> a = stackalloc double[4];

            var norm = 0.0;
            for (var t = -radius; t <= radius; t++)
            {
                norm += Gauss(t, sigma);
            }

            a[0] = 1.0 / (2 * radius + 1);

            for (var k = 1; k <= spectrumTerms; k++)
            {
                var s = 0.0;
                for (var t = -radius; t <= radius; t++)
                {
                    s += Gauss(t, sigma) * Math.Cos(omega * k * t);
                }

                a[k] = 2.0 / (2 * radius + 1) * (s / norm);
            }

            return new FilterCoefficients(radius, omega, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int OptimizeRadius(double sigma, int spectrumTerms)
        {
            var rMin = Math.Max(1, spectrumTerms);
            var rMax = Math.Max(rMin, (int)Math.Ceiling(sigma * 5.0) + spectrumTerms + 2);

            var best = rMin;
            var bestError = double.MaxValue;

            for (var r = rMin; r <= rMax; r++)
            {
                var phi = (2 * r + 1) / (2.0 * sigma);
                var psi = Math.PI * sigma * (2 * spectrumTerms + 1) / (2 * r + 1);
                var error = Erfc(phi) + Erfc(psi);

                if (error < bestError)
                {
                    bestError = error;
                    best = r;
                }
            }

            return best;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double Gauss(double t, double sigma)
        {
            return Math.Exp(-(t * t) / (2.0 * sigma * sigma));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double Erfc(double x)
        {
            var t = 1.0 / (1.0 + 0.3275911 * x);
            var y = t * (0.254829592 + t * (-0.284496736 + t * (1.421413741 + t * (-1.453152027 + t * 1.061405429))));
            return y * Math.Exp(-x * x);
        }
    }

    readonly record struct GpuFilterCoefficients(float Omega, Float4 A, float C1, float C2, float C3, float D1, float D2, float D3)
    {
        public readonly float Omega = Omega;

        public readonly Float4 A = A;

        public readonly float C1 = C1;

        public readonly float C2 = C2;

        public readonly float C3 = C3;

        public readonly float D1 = D1;

        public readonly float D2 = D2;

        public readonly float D3 = D3;
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.X)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct SlidingDct5LineShader(
        ReadWriteBuffer<Float4> source,
        ReadWriteBuffer<Float4> destination,
        int size,
        int offset,
        int srcOrigin,
        int srcLineStride,
        int srcCoordStride,
        int dstOrigin,
        int dstLineStride,
        int dstElementStride,
        int radius,
        int edgeRepeatMode,
        bool isInput,
        GpuFilterCoefficients c) : IComputeShader
    {
        Float4 GetPixel(int srcBase, int p)
        {
            switch (edgeRepeatMode)
            {
                case 1: // Wrap
                    p = CoordWrapGpu.Wrap(p, size);
                    break;
                case 2: // Repeat
                    p = CoordWrapGpu.Repeat(p, size);
                    break;
                case 3: // Mirror
                    p = CoordWrapGpu.Mirror(p, size);
                    break;
                default:
                    if (p < 0 || p >= size)
                    {
                        return Float4.Zero;
                    }
                    break;
            }

            var value = source[srcBase + p * srcCoordStride];
            if (isInput)
            {
                value.XYZ *= value.W;
            }

            return value;
        }

        public void Execute()
        {
            var length = size - offset;

            var line = ThreadIds.X;

            var srcBase = srcOrigin + line * srcLineStride;
            var dstBase = dstOrigin + line * dstLineStride;

            var sum = Float4.Zero;
            var p1 = Float4.Zero;
            var p2 = Float4.Zero;
            var p3 = Float4.Zero;
            var q1 = Float4.Zero;
            var q2 = Float4.Zero;
            var q3 = Float4.Zero;

            for (var t = -radius; t <= radius; t++)
            {
                var fPrev = GetPixel(srcBase, offset + t - 1);
                var fCurr = GetPixel(srcBase, offset + t);

                var cos = Hlsl.Cos(c.Omega * t * new Float3(1.0F, 2.0F, 3.0F));
                var w = c.A.YZW * cos;

                sum += fCurr;
                p1 += w.X * fPrev;
                p2 += w.Y * fPrev;
                p3 += w.Z * fPrev;
                q1 += w.X * fCurr;
                q2 += w.Y * fCurr;
                q3 += w.Z * fCurr;
            }

            for (var x = 0; x < length; x++)
            {
                var result = c.A.X * sum + q1 + q2 + q3;

                if (!isInput)
                {
                    if (result.W > 1E-6F)
                    {
                        result.XYZ /= result.W;
                    }
                    else
                    {
                        result = Const.EmptyPixelFloat4;
                    }
                }

                destination[dstBase + x * dstElementStride] = result;

                var p = offset + x;
                var fL2 = GetPixel(srcBase, p - radius - 1);
                var fL1 = GetPixel(srcBase, p - radius);
                var fR1 = GetPixel(srcBase, p + radius);
                var fR2 = GetPixel(srcBase, p + radius + 1);
                var delta = fL2 - fL1 - fR1 + fR2;

                var n1 = c.C1 * q1 - p1 + c.D1 * delta;
                var n2 = c.C2 * q2 - p2 + c.D2 * delta;
                var n3 = c.C3 * q3 - p3 + c.D3 * delta;

                p1 = q1;
                p2 = q2;
                p3 = q3;
                q1 = n1;
                q2 = n2;
                q3 = n3;

                sum += fR2 - fL1;
            }
        }
    }
}
