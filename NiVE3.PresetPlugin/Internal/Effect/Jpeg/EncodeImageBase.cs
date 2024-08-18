using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Internal.Effect.Jpeg
{
    abstract class EncodeImageBase
    {
        const float InvSqrt2 = (float)(1.0 / 1.4142135623730951);

        protected static Vector256<float>[] Coefficient { get; }

        protected static Vector256<float>[] CoefficientTransposed { get; }

        public int Width { get; }

        public int Height { get; }

        public uint[] EncodedData { get; protected set; } = [];

        public int EncodedDataLength { get; protected set; }

        public QuantizeTable[] QuantizeTables { get; protected init; } = [];

        protected int McuBlockCols { get; }

        protected int McuBlockRows { get; }

        protected abstract int[][] InterleaveGroupMap { get; }

        protected abstract HuffmanTable[] DCHuffmanTables { get; }

        protected abstract HuffmanTable[] ACHuffmanTables { get; }

        static EncodeImageBase()
        {
            Coefficient = new Vector256<float>[Mcu.LineSize];
            CoefficientTransposed = new Vector256<float>[Mcu.LineSize];

            var coefficient = MemoryMarshal.Cast<Vector256<float>, float>(Coefficient);
            for (int y = 0, pos = 0; y < Mcu.LineSize; y++)
            {
                for (var x = 0; x < Mcu.LineSize; x++, pos++)
                {
                    coefficient[pos] = MathF.Cos(((2.0F * x + 1.0F) * y * MathF.PI) / (2.0F * Mcu.LineSize));
                    if (y == 0)
                    {
                        coefficient[pos] *= InvSqrt2;
                    }
                }
            }

            var coeffTFloat = MemoryMarshal.Cast<Vector256<float>, float>(CoefficientTransposed);
            for (int y = 0, pos = 0; y < Mcu.LineSize; y++)
            {
                for (var x = 0; x < Mcu.LineSize; x++, pos++)
                {
                    coeffTFloat[x * Mcu.LineSize + y] = coefficient[pos];
                }
            }
        }

        protected EncodeImageBase(int width, int height)
        {
            Width = width;
            Height = height;

            McuBlockCols = (int)MathF.Ceiling(width / (float)Mcu.LineSize);
            McuBlockRows = (int)MathF.Ceiling(height / (float)Mcu.LineSize);
        }

        public abstract void Compress(Vector4[] image);

        public abstract void Decompress(Vector4[] image);

        protected static (int, uint[]) Encode(Mcu[] quantizedMcu, int rowCount, int[] colCounts, int[][] interleaveMap, HuffmanTable[] dcHuffmanTables, HuffmanTable[] acHuffmanTables)
        {
            Span<int> prevDC = stackalloc int[3];

            var eobCodes = acHuffmanTables.Select(ht => ht.GetCode(0, 0)).ToArray();

            var writer = new BitWriter((rowCount / colCounts.Length) * colCounts.Sum() * Mcu.LineSize);
            for (int row = 0, mpos = 0; row < rowCount; row++)
            {
                var currentInterleaveMap = interleaveMap[row % interleaveMap.Length].AsSpan();
                var colCount = colCounts[row % colCounts.Length];
                for (var col = 0; col < colCount; col++, mpos++)
                {
                    var channel = currentInterleaveMap[col % currentInterleaveMap.Length];
                    ref var mcu = ref quantizedMcu[mpos];
                    writer.WriteHuffmanCode(0, mcu.DCElement - prevDC[channel], dcHuffmanTables[channel]);
                    prevDC[channel] = mcu.DCElement;

                    var lastNonZeroIndex = GetLastNonZeroIndex(ref mcu);
                    var acht = acHuffmanTables[channel];
                    var zeroRun = 0;
                    for (var n = 1; n <= lastNonZeroIndex; n++)
                    {
                        var v = Unsafe.Add(ref mcu.DCElement, n);
                        if (v == 0)
                        {
                            zeroRun++;
                        }
                        else
                        {
                            writer.WriteHuffmanCode(zeroRun, v, acht);
                            zeroRun = 0;
                        }
                    }
                    if (lastNonZeroIndex < 63)
                    {
                        var (eb, ec) = eobCodes[channel];
                        writer.Write(ec, eb);
                    }
                }
            }

            return (writer.WritedLength, writer.ToArray());
        }

        protected static Mcu[] Decode(uint[] data, int rowCount, int[] colCounts, int[][] interleaveMap, HuffmanTable[] dcHuffmanTables, HuffmanTable[] acHuffmanTables)
        {
            ReadOnlySpan<int> ZigZag =
            [
                0, 1, 8, 16, 9, 2, 3, 10,
                17, 24, 32, 25, 18, 11, 4, 5,
                12, 19, 26, 33, 40, 48, 41, 34,
                27, 20, 13, 6, 7, 14, 21, 28,
                35, 42, 49, 56, 57, 50, 43, 36,
                29, 22, 15, 23, 30, 37, 44, 51,
                58, 59, 52, 45, 38, 31, 39, 46,
                53, 60, 61, 54, 47, 55, 62, 63
            ];
            Span<int> prevDC = stackalloc int[3];

            var eobCodes = acHuffmanTables.Select(ht => ht.GetCode(0, 0)).ToArray();

            var result = ArrayPool<Mcu>.Shared.Rent(colCounts.RepeatInfinity().Take(rowCount).Sum());
            var reader = new BitReader(data);

            for (int row = 0, mpos = 0; row < rowCount; row++)
            {
                var currentInterleaveMap = interleaveMap[row % interleaveMap.Length].AsSpan();
                var colCount = colCounts[row % colCounts.Length];
                for (var col = 0; col < colCount; col++, mpos++)
                {
                    var channel = currentInterleaveMap[col % currentInterleaveMap.Length];
                    ref var mcu = ref result[mpos];

                    var (success, dcDiff) = reader.ReadHuffmanCode(dcHuffmanTables[channel]);
                    prevDC[channel] += dcDiff;
                    mcu.DCElement = prevDC[channel];

                    var acht = acHuffmanTables[channel];
                    for (var n = 1; n < Mcu.BlockSize; n++)
                    {
                        var (zeroRunLength, value) = reader.ReadHuffmanCode(acht);

                        if (zeroRunLength == 0 && value == 0)
                        {
                            // EOB
                            break;
                        }

                        var totalZeroRunLength = 0;
                        while (zeroRunLength == 15 && value == 0)
                        {
                            totalZeroRunLength += 16;
                            (zeroRunLength, value) = reader.ReadHuffmanCode(acht);
                        }
                        totalZeroRunLength += zeroRunLength;

                        n += totalZeroRunLength;
                        if (n < Mcu.BlockSize)
                        {
                            Unsafe.Add(ref mcu.DCElement, ZigZag[n]) = value;
                        }
                    }
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void FillOutSideBlock<T>(Span<T> imageData, int width, int height, int row, int col)
        {
            var overX = Math.Min((col + 1) * Mcu.LineSize - width, Mcu.LineSize);
            if (overX > 0)
            {
                for (int y = 0, imageY = row * Mcu.LineSize; y < Mcu.LineSize && imageY < height; y++, imageY++)
                {
                    var start = y * Mcu.LineSize + Mcu.LineSize - overX;
                    imageData.Slice(start, overX).Fill(imageData[start - 1]);
                }
            }
            var overY = (row + 1) * Mcu.LineSize - height;
            if (overY > 0)
            {
                var lastLine = (Mcu.LineSize - overY - 1) * Mcu.LineSize;
                for (var y = 0; y < overY; y++)
                {
                    imageData.Slice(lastLine, Mcu.LineSize).CopyTo(imageData[(lastLine + y * Mcu.LineSize)..]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector4 ToYCbCr(in Vector4 rgb)
        {
            return new Vector4(
                Vector4.Dot(rgb, new Vector4(0.299F, 0.587F, 0.114F, 0.0F)),
                0.5F + Vector4.Dot(rgb, new Vector4(-0.168736F, -0.331264F, 0.5F, 0.0F)),
                0.5F + Vector4.Dot(rgb, new Vector4(0.5F, -0.418688F, -0.081312F, 0.0F)),
                0.0F
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector4 ToRgb(Vector4 ycbcr)
        {
            ycbcr -= new Vector4(0.0F, 0.5F, 0.5F, 0.0F);
            return new Vector4(
                Vector4.Dot(ycbcr, new Vector4(1.0F, 0.0F, 1.402F, 0.0F)),
                Vector4.Dot(ycbcr, new Vector4(1.0F, -0.344136F, -0.714136F, 0.0F)),
                Vector4.Dot(ycbcr, new Vector4(1.0F, 1.772F, 0.0F, 0.0F)),
                1.0F
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Dct(ReadOnlySpan<float> imageData, Span<float> mcu)
        {
            Span<float> s = stackalloc float[Mcu.BlockSize];

            var imageVector = MemoryMarshal.Cast<float, Vector256<float>>(imageData);
            for (var v = 0; v < Mcu.LineSize; v++)
            {
                for (int u = 0, spos = v; u < Mcu.LineSize; u++, spos += Mcu.LineSize)
                {
                    s[spos] = Vector256.Dot(imageVector[v], Coefficient[u]);
                }
            }

            var vs = MemoryMarshal.Cast<float, Vector256<float>>(s);

            for (int v = 0, spos = 0; v < Mcu.LineSize; v++)
            {
                for (var u = 0; u < Mcu.LineSize; u++, spos++)
                {
                    mcu[spos] = Vector256.Dot(vs[u], Coefficient[v]) * 0.25F;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void IDct(ReadOnlySpan<float> mcu, Span<float> imageData)
        {
            Span<float> s = stackalloc float[Mcu.BlockSize];

            var data = MemoryMarshal.Cast<float, Vector256<float>>(mcu);
            for (var v = 0; v < Mcu.LineSize; v++)
            {
                for (int u = 0, pos = v; u < Mcu.LineSize; u++, pos += Mcu.LineSize)
                {
                    s[pos] = Vector256.Dot(data[v], CoefficientTransposed[u]);
                }
            }

            var vs = MemoryMarshal.Cast<float, Vector256<float>>(s);

            for (int v = 0, pos = 0; v < Mcu.LineSize; v++)
            {
                for (var u = 0; u < Mcu.LineSize; u++, pos++)
                {
                    imageData[pos] = Vector256.Dot(vs[u], CoefficientTransposed[v]) * 0.25F;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Quantize(ReadOnlySpan<float> mcu, ReadOnlySpan<Vector256<float>> qTable, ref Mcu result)
        {
            var mcuVector = MemoryMarshal.Cast<float, Vector256<float>>(mcu);
            ref var resultVector = ref result.Row1;

            for (var i = 0; i < Mcu.LineSize; i++)
            {
                Unsafe.Add(ref resultVector, i) = Vector256.ConvertToInt32(mcuVector[i] / qTable[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Dequantize(ref Mcu quantizedMcu, ReadOnlySpan<Vector256<float>> qTable, Span<float> mcu)
        {
            var mcuVector = MemoryMarshal.Cast<float, Vector256<float>>(mcu);

            mcuVector[0] = Vector256.ConvertToSingle(quantizedMcu.Row1) * qTable[0];
            mcuVector[1] = Vector256.ConvertToSingle(quantizedMcu.Row2) * qTable[1];
            mcuVector[2] = Vector256.ConvertToSingle(quantizedMcu.Row3) * qTable[2];
            mcuVector[3] = Vector256.ConvertToSingle(quantizedMcu.Row4) * qTable[3];
            mcuVector[4] = Vector256.ConvertToSingle(quantizedMcu.Row5) * qTable[4];
            mcuVector[5] = Vector256.ConvertToSingle(quantizedMcu.Row6) * qTable[5];
            mcuVector[6] = Vector256.ConvertToSingle(quantizedMcu.Row7) * qTable[6];
            mcuVector[7] = Vector256.ConvertToSingle(quantizedMcu.Row8) * qTable[7];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetLastNonZeroIndex(ref Mcu mcu)
        {
            var flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row8, Vector256<int>.Zero));
            if (flags != 255)
            {
                return 55 + BitOperations.TrailingZeroCount(~flags);
            }

            flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row7, Vector256<int>.Zero));
            if (flags != 255)
            {
                return 47 + BitOperations.TrailingZeroCount(~flags);
            }

            flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row6, Vector256<int>.Zero));
            if (flags != 255)
            {
                return 39 + BitOperations.TrailingZeroCount(~flags);
            }

            flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row5, Vector256<int>.Zero));
            if (flags != 255)
            {
                return 31 + BitOperations.TrailingZeroCount(~flags);
            }

            flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row4, Vector256<int>.Zero));
            if (flags != 255)
            {
                return 23 + BitOperations.TrailingZeroCount(~flags);
            }

            flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row3, Vector256<int>.Zero));
            if (flags != 255)
            {
                return 15 + BitOperations.TrailingZeroCount(~flags);
            }

            flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row2, Vector256<int>.Zero));
            if (flags != 255)
            {
                return 7 + BitOperations.TrailingZeroCount(~flags);
            }

            flags = Vector256.ExtractMostSignificantBits(Vector256.Equals(mcu.Row1, Vector256<int>.Zero));
            return BitOperations.TrailingZeroCount(~flags) - 1;
        }
    }
}
