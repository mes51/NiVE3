using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace NiVE3.Image
{
    public static unsafe class ImageConversion
    {
        const float ByteToFloat = 0.00392156862745098F;

        static readonly Vector<float> MaxBase = new Vector<float>(Enumerable.Repeat(new float[] { 0.0F, 0.0F, 0.0F, 1.0F }, Vector<float>.Count / 4).SelectMany(_ => _).ToArray());

        /// <summary>
        /// 8bpcから32bpcに変換します
        /// </summary>
        /// <param name="fromImage">元となる8bpcの画像データ</param>
        /// <param name="toImage">変換先の32bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertToBGRA128(ReadOnlySpan<byte> fromImage, Span<Vector4> toImage, int pixelCount)
        {
            ConvertToBGRA128(MemoryMarshal.Cast<byte, int>(fromImage[..(pixelCount * 4)]), toImage, pixelCount);
        }

        /// <summary>
        /// 8bpcから32bpcに変換します
        /// </summary>
        /// <param name="fromImage">元となる8bpcの画像データ</param>
        /// <param name="toImage">変換先の32bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        public static unsafe void ConvertToBGRA128(ReadOnlySpan<int> fromImage, Span<Vector4> toImage, int pixelCount)
        {
            ref var pixelDataRef = ref MemoryMarshal.GetReference(fromImage);
            ref var resultDataRef = ref MemoryMarshal.GetReference(toImage);
            fixed (int* fixedPixelData = &pixelDataRef)
            fixed (Vector128<float>* fixedResultData = &Unsafe.As<Vector4, Vector128<float>>(ref resultDataRef))
            {
                var vPixelData = (Vector<byte>*)fixedPixelData;
                var vResultData = (Vector<float>*)fixedResultData;
                var stride = Vector<float>.Count; // Vector<float>.Count / 4channel * 4px
                Parallel.For(0, pixelCount / stride, i =>
                {
                    var c = vPixelData[i];
                    Vector.Widen(c, out var ps1, out var ps2);
                    Vector.Widen(ps1, out var pi1, out var pi2);
                    Vector.Widen(ps2, out var pi3, out var pi4);
                    vResultData[i * 4] = Vector.ConvertToSingle(pi1) * ByteToFloat;
                    vResultData[i * 4 + 1] = Vector.ConvertToSingle(pi2) * ByteToFloat;
                    vResultData[i * 4 + 2] = Vector.ConvertToSingle(pi3) * ByteToFloat;
                    vResultData[i * 4 + 3] = Vector.ConvertToSingle(pi4) * ByteToFloat;
                });

                var pixelData = fixedPixelData;
                var resultData = fixedResultData;
                Parallel.For(pixelCount - (pixelCount % stride), pixelCount, i =>
                {
                    var c = Sse2.ConvertScalarToVector128Int32(pixelData[i]).AsByte();
                    var cv = Sse2.UnpackLow(Sse2.UnpackLow(c, Vector128<byte>.Zero), Vector128<byte>.Zero).AsInt32();

                    resultData[i] = Sse2.ConvertToVector128Single(cv) * ByteToFloat;
                });
            }
        }

        /// <summary>
        /// 32bpcから8bpcに変換します
        /// </summary>
        /// <param name="fromImage">元となる32bpcの画像データ</param>
        /// <param name="toImage">変換先の8bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertToBGRA32(ReadOnlySpan<Vector4> fromImage, Span<byte> toImage, int pixelCount)
        {
            ConvertToBGRA32(fromImage, MemoryMarshal.Cast<byte, int>(toImage[..(pixelCount * 4)]), pixelCount);
        }

        /// <summary>
        /// 32bpcから8bpcに変換します
        /// </summary>
        /// <param name="fromImage">元となる32bpcの画像データ</param>
        /// <param name="toImage">変換先の8bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        public static unsafe void ConvertToBGRA32(ReadOnlySpan<Vector4> fromImage, Span<int> toImage, int pixelCount)
        {
            ref var pixelDataRef = ref MemoryMarshal.GetReference(fromImage);
            ref var resultDataRef = ref MemoryMarshal.GetReference(toImage);
            fixed (Vector128<float>* fixedPixelData = &Unsafe.As<Vector4, Vector128<float>>(ref pixelDataRef))
            fixed (int* fixedResultData = &resultDataRef)
            {
                var vPixelData = (Vector<float>*)fixedPixelData;
                var vResultData = (Vector<byte>*)fixedResultData;
                var stride = Vector<float>.Count; // Vector<float>.Count / 4channel * 4px
                Parallel.For(0, pixelCount / stride, i =>
                {
                    var p1 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4], Vector<float>.Zero), Vector<float>.One) * 255.0F);
                    var p2 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4 + 1], Vector<float>.Zero), Vector<float>.One) * 255.0F);
                    var p3 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4 + 2], Vector<float>.Zero), Vector<float>.One) * 255.0F);
                    var p4 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4 + 3], Vector<float>.Zero), Vector<float>.One) * 255.0F);
                    var ps1 = Vector.Narrow(p1, p2);
                    var ps2 = Vector.Narrow(p3, p4);
                    vResultData[i] = Vector.Narrow(ps1, ps2);
                });

                var pixelData = fixedPixelData;
                var resultData = fixedResultData;
                Parallel.For(pixelCount - (pixelCount % stride), pixelCount, i =>
                {
                    var p = Sse41.RoundCurrentDirection(pixelData[i] * 255.0F);
                    var p32 = Sse41.Min(Sse41.Max(Sse2.ConvertToVector128Int32(p), Vector128<int>.Zero), Vector128.Create(255));
                    var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                    var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                    resultData[i] = Sse2.ConvertToInt32(p8.AsInt32());
                });
            }
        }

        /// <summary>
        /// 32bpcからBGR32に変換します
        /// </summary>
        /// <param name="fromImage">元となる32bpcの画像データ</param>
        /// <param name="toImage">変換先の8bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        public static void ConvertToBGR32(ReadOnlySpan<Vector4> fromImage, Span<byte> toImage, int pixelCount)
        {
            ConvertToBGR32(fromImage, MemoryMarshal.Cast<byte, int>(toImage[..(pixelCount * 4)]), pixelCount);
        }

        /// <summary>
        /// 32bpcからBGR32に変換します
        /// </summary>
        /// <param name="fromImage">元となる32bpcの画像データ</param>
        /// <param name="toImage">変換先の8bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        public static unsafe void ConvertToBGR32(ReadOnlySpan<Vector4> fromImage, Span<int> toImage, int pixelCount)
        {
            ref var pixelDataRef = ref MemoryMarshal.GetReference(fromImage);
            ref var resultDataRef = ref MemoryMarshal.GetReference(toImage);
            fixed (Vector128<float>* fixedPixelData = &Unsafe.As<Vector4, Vector128<float>>(ref pixelDataRef))
            fixed (int* fixedResultData = &resultDataRef)
            {
                var maxBase = MaxBase;
                var vPixelData = (Vector<float>*)fixedPixelData;
                var vResultData = (Vector<byte>*)fixedResultData;
                var stride = Vector<float>.Count; // Vector<float>.Count / 4channel * 4px
                Parallel.For(0, pixelCount / stride, i =>
                {
                    var p1 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4], maxBase), Vector<float>.One) * 255.0F);
                    var p2 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4 + 1], maxBase), Vector<float>.One) * 255.0F);
                    var p3 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4 + 2], maxBase), Vector<float>.One) * 255.0F);
                    var p4 = Vector.ConvertToUInt32(Vector.Min(Vector.Max(vPixelData[i * 4 + 3], maxBase), Vector<float>.One) * 255.0F);
                    var ps1 = Vector.Narrow(p1, p2);
                    var ps2 = Vector.Narrow(p3, p4);
                    vResultData[i] = Vector.Narrow(ps1, ps2);
                });

                var pixelData = fixedPixelData;
                var resultData = fixedResultData;
                Parallel.For(pixelCount - (pixelCount % stride), pixelCount, i =>
                {
                    var p = Sse41.RoundCurrentDirection(pixelData[i] * 255.0F);
                    var p32 = Sse41.Insert(Sse2.ConvertToVector128Int32(p), 255, 3);
                    p32 = Sse41.Min(Sse41.Max(p32, Vector128<int>.Zero), Vector128.Create(255));
                    var p16 = Sse2.PackSignedSaturate(p32, Vector128<int>.Zero);
                    var p8 = Sse2.PackUnsignedSaturate(p16, Vector128<short>.Zero);
                    resultData[i] = Sse2.ConvertToInt32(p8.AsInt32());
                });
            }
        }
    }
}
