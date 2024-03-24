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
        static readonly Vector128<float> ByteToFloat128 = Vector128.Create(0.00392156862745098F);

        /// <summary>
        /// 8bpcから32bpcに変換します
        /// </summary>
        /// <param name="fromImage">元となる8bpcの画像データ</param>
        /// <param name="toImage">変換先の32bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertToBGRA128(ReadOnlySpan<byte> fromImage, Span<Vector4> toImage, int pixelCount)
        {
            ConvertToBGRA128(MemoryMarshal.Cast<byte, int>(fromImage.Slice(0, pixelCount * 4)), toImage, pixelCount);
        }

        /// <summary>
        /// 8bpcから32bpcに変換します
        /// </summary>
        /// <param name="fromImage">元となる8bpcの画像データ</param>
        /// <param name="toImage">変換先の32bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        public static unsafe void ConvertToBGRA128(ReadOnlySpan<int> fromImage, Span<Vector4> toImage, int pixelCount)
        {
            ref int pixelDataRef = ref MemoryMarshal.GetReference(fromImage);
            ref Vector4 resultDataRef = ref MemoryMarshal.GetReference(toImage);
            fixed (int* fixedPixelData = &pixelDataRef)
            fixed (Vector128<float>* fixedResultData = &Unsafe.As<Vector4, Vector128<float>>(ref resultDataRef))
            {
                int* pixelData = fixedPixelData;
                Vector128<float>* resultData = fixedResultData;
                Parallel.For(0, pixelCount, i =>
                {
                    var c = Sse2.ConvertScalarToVector128Int32(pixelData[i]).AsByte();
                    var cv = Sse2.UnpackLow(Sse2.UnpackLow(c, Vector128<byte>.Zero), Vector128<byte>.Zero).AsInt32();

                    resultData[i] = Sse.Multiply(Sse2.ConvertToVector128Single(cv), ByteToFloat128);
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
            ConvertToBGRA32(fromImage, MemoryMarshal.Cast<byte, int>(toImage.Slice(0, pixelCount * 4)), pixelCount);
        }

        /// <summary>
        /// 32bpcから8bpcに変換します
        /// </summary>
        /// <param name="fromImage">元となる32bpcの画像データ</param>
        /// <param name="toImage">変換先の8bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        public static unsafe void ConvertToBGRA32(ReadOnlySpan<Vector4> fromImage, Span<int> toImage, int pixelCount)
        {
            ref Vector4 pixelDataRef = ref MemoryMarshal.GetReference(fromImage);
            ref int resultDataRef = ref MemoryMarshal.GetReference(toImage);
            fixed (Vector128<float>* fixedPixelData = &Unsafe.As<Vector4, Vector128<float>>(ref pixelDataRef))
            fixed (int* fixedResultData = &resultDataRef)
            {
                Vector128<float>* pixelData = fixedPixelData;
                int* resultData = fixedResultData;
                Parallel.For(0, pixelCount, i =>
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
            ConvertToBGR32(fromImage, MemoryMarshal.Cast<byte, int>(toImage.Slice(0, pixelCount * 4)), pixelCount);
        }

        /// <summary>
        /// 32bpcからBGR32に変換します
        /// </summary>
        /// <param name="fromImage">元となる32bpcの画像データ</param>
        /// <param name="toImage">変換先の8bpcの画像データ</param>
        /// <param name="pixelCount">ピクセルの数</param>
        public static unsafe void ConvertToBGR32(ReadOnlySpan<Vector4> fromImage, Span<int> toImage, int pixelCount)
        {
            ref Vector4 pixelDataRef = ref MemoryMarshal.GetReference(fromImage);
            ref int resultDataRef = ref MemoryMarshal.GetReference(toImage);
            fixed (Vector128<float>* fixedPixelData = &Unsafe.As<Vector4, Vector128<float>>(ref pixelDataRef))
            fixed (int* fixedResultData = &resultDataRef)
            {
                Vector128<float>* pixelData = fixedPixelData;
                int* resultData = fixedResultData;
                Parallel.For(0, pixelCount, i =>
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
