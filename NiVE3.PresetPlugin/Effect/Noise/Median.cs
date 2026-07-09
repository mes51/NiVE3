using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Generate;
using NiVE3.PresetPlugin.Effect.Util.General;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Noise
{
    [EffectMetadata(LanguageResourceDictionary.Noise_Median_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Noise, LanguageResourceDictionary.Noise_Median_Description, ID, IsSupportGpu = true, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    [Export(typeof(IEffect))]
    public sealed class Median : IEffect
    {
        const string ID = "EA432F5F-258B-4B91-BE11-63DF0716D6D7";

        const string PropertyRadiusId = nameof(PropertyRadiusId);

        const string PropertyApplyToAlphaId = nameof(PropertyApplyToAlphaId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyRadiusId, LanguageResourceDictionary.ResourceKeys.Noise_Median_Radius, 0.0, 0.0, 10000.0, digit: 0),
                new CheckBoxProperty(PropertyApplyToAlphaId, LanguageResourceDictionary.ResourceKeys.Noise_Median_ApplyToAlpha, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var radius = (int)properties.GetValue(PropertyRadiusId, layerTime, 0.0);
            if (radius < 1 || (image.Width <= 1 && image.Height <= 1))
            {
                return image;
            }

            var applyToAlpha = properties.GetValue(PropertyApplyToAlphaId, layerTime, false);

            if (useGpu && AcceleratorObject != null)
            {
                return ProcessGpu(AcceleratorObject.CurrentDevice, image, roi, radius, applyToAlpha);
            }
            else
            {
                return ProcessCpu(image, roi, radius, applyToAlpha);
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, int radius, bool applyToAlpha)
        {
            var managedImage = image.ToManaged();

            using var medianFilter = new WaveletMatrixMedianFilterCpu(managedImage.Width, managedImage.Height);
            using var result = new NManagedImage(managedImage.Width, managedImage.Height);
            medianFilter.Apply(managedImage.Data, result.Data, radius, applyToAlpha ? ChannelMask.All : ChannelMask.Color);

            ImageBlendProcessor.TransferSameSizeCpu(managedImage, result, roi);

            return managedImage;
        }

        static NGPUImage ProcessGpu(GraphicsDevice device, NImage image, ROI roi, int radius, bool applyToAlpha)
        {
            var gpuImage = image.ToGpu(device);

            using var medianFilter = new WaveletMatrixMedianFilterGpu(device, gpuImage.Width, gpuImage.Height);
            using var result = new NGPUImage(gpuImage.Width, gpuImage.Height, device);
            medianFilter.Apply(gpuImage.Data, result.Data, radius, applyToAlpha ? ChannelMask.All : ChannelMask.Color);

            ImageBlendProcessor.TransferSameSizeGpu(device, gpuImage, result, roi);

            return gpuImage;
        }
    }

    /// <summary>Channels of a BGRA <see cref="Float4"/> pixel (X=B, Y=G, Z=R, W=A).</summary>
    [Flags]
    public enum ChannelMask
    {
        B = 1 << 0,
        G = 1 << 1,
        R = 1 << 2,
        A = 1 << 3,
        Color = B | G | R,
        All = B | G | R | A,
    }

    /// <summary>
    /// CPU fallback of <see cref="WaveletMatrixMedianFilterGpu"/> for environments
    /// without DirectX 12. Implements the same 2D wavelet matrix median filter
    /// (Moroto &amp; Umetani, SIGGRAPH Asia 2022) with the same
    /// <see cref="ValueBitsMode"/> handling (including dense-rank reduction) and
    /// bit-exact identical results. The API mirrors the GPU class but operates
    /// on <see cref="Vector4"/> arrays (row-major BGRA: X=B, Y=G, Z=R, W=A).
    ///
    /// All passes are parallelized with <see cref="Parallel"/>. On AVX2-capable
    /// CPUs the hot paths additionally use SIMD: bit-plane packing via
    /// shift+movemask (with an SSE2 fallback), and the median query processes
    /// 8 pixels per vector lane set with gathered rank lookups and branchless
    /// blends. Rank data is stored interleaved (bits word, prefix count) so a
    /// rank lookup touches a single cache line.
    ///
    /// Instances are not thread-safe: do not call methods of one instance
    /// concurrently (each call itself uses all cores internally).
    /// </summary>
    public sealed class WaveletMatrixMedianFilterCpu : IDisposable
    {
        readonly int Width;

        readonly int Height;

        readonly int DataLength;

        readonly int MaxValueBits;

        readonly int XBits;

        readonly int SlotsPerLevel;

        readonly int WordCount;

        readonly int LastWord;

        int BuiltValueBits;

        /// <summary>Interleaved bit-vector data: wm[2i] = packed bits word i, wm[2i+1] = ones before word i.</summary>
        readonly uint[] WM;

        readonly uint[] Table;

        uint[] ValA;

        uint[] ValB;

        uint[] XA;

        uint[] XB;

        uint[] NXA;

        uint[] NXB;

        readonly ulong[] SortA; // (key << 32 | index) pairs, RankReduced only

        readonly ulong[] SortB; // (key << 32 | index) pairs, RankReduced only

        public WaveletMatrixMedianFilterCpu(int width, int height)
        {
            Width = width;
            Height = height;

            var nLong = (long)width * height;
            MaxValueBits = 64 - BitOperations.LeadingZeroCount((ulong)(nLong - 1));
            BuiltValueBits = MaxValueBits;
            XBits = BitOperations.Log2((uint)width) + 1;
            SlotsPerLevel = 1 + XBits;
            var wordCountLong = (nLong + 31) / 32 + 1;
            var totalWords = MaxValueBits * (long)SlotsPerLevel * wordCountLong;

            DataLength = (int)nLong;
            WordCount = (int)wordCountLong;
            LastWord = WordCount - 1;

            WM = ArrayPool<uint>.Shared.Rent((int)totalWords * 2);
            Table = ArrayPool<uint>.Shared.Rent(DataLength);
            ValA = ArrayPool<uint>.Shared.Rent(DataLength);
            ValB = ArrayPool<uint>.Shared.Rent(DataLength);
            XA = ArrayPool<uint>.Shared.Rent(DataLength);
            XB = ArrayPool<uint>.Shared.Rent(DataLength);
            NXA = ArrayPool<uint>.Shared.Rent(DataLength);
            NXB = ArrayPool<uint>.Shared.Rent(DataLength);
            SortA = ArrayPool<ulong>.Shared.Rent(DataLength);
            SortB = ArrayPool<ulong>.Shared.Rent(DataLength);

            WM.AsSpan(0, (int)totalWords * 2).Clear();
            Table.AsSpan(0, DataLength).Clear();
            ValA.AsSpan(0, DataLength).Clear();
            ValB.AsSpan(0, DataLength).Clear();
            XA.AsSpan(0, DataLength).Clear();
            XB.AsSpan(0, DataLength).Clear();
            NXA.AsSpan(0, DataLength).Clear();
            NXB.AsSpan(0, DataLength).Clear();
            SortA.AsSpan(0, DataLength).Clear();
            SortB.AsSpan(0, DataLength).Clear();
        }

        /// <summary>
        /// Applies a median filter with a rectangular (2rx+1)x(2ry+1) window to
        /// the selected channels. <paramref name="source"/> and
        /// <paramref name="destination"/> may be the same array (in-place);
        /// unselected channels are copied through unchanged.
        /// </summary>
        public void Apply(Vector4[] source, Vector4[] destination, int radius, ChannelMask channels)
        {
            if (!ReferenceEquals(source, destination))
            {
                Array.Copy(source, destination, DataLength);
            }

            for (var channel = 0; channel < 4; channel++)
            {
                if (((int)channels & (1 << channel)) == 0)
                {
                    continue;
                }

                Build(source, channel);
                Query(destination, radius, channel);
            }
        }

        /// <summary>Constructs the 2D wavelet matrix for one channel (0=B, 1=G, 2=R, 3=A).</summary>
        public void Build(Vector4[] source, int channel)
        {
            ParallelFor(DataLength, (from, to) =>
            {
                for (var j = from; j < to; j++)
                {
                    SortA[j] = ((ulong)Key(source[j][channel]) << 32) | (uint)j;
                }
            });

            RadixSortByKey();

            // Dense-rank the sorted pairs; equal keys collapse to one integer.
            var dr = -1;
            var prev = 0U;
            for (var j = 0; j < DataLength; j++)
            {
                var key = (uint)(SortA[j] >> 32);
                if (j == 0 || key != prev)
                {
                    dr++;
                    Table[dr] = key;
                    prev = key;
                }
                var p = (int)(uint)SortA[j];
                ValA[p] = (uint)dr;
                XA[p] = (uint)(p % Width);
            }

            var distinct = dr + 1;
            if (distinct <= 1)
            {
                BuiltValueBits = 1;
            }
            else
            {
                BuiltValueBits = 64 - BitOperations.LeadingZeroCount((ulong)(distinct - 1));
            }

            BuildLevels();
        }

        /// <summary>
        /// Computes the median for every pixel from the wavelet matrix built by
        /// the last <see cref="Build"/> call and writes it into the given channel.
        /// </summary>
        public void Query(Vector4[] destination, int radius, int channel)
        {
            var valueBits = BuiltValueBits;

            if (Avx2.IsSupported)
            {
                Parallel.For(0, Height, py => QueryRowAvx2(destination, py, radius, channel, valueBits));
            }
            else
            {
                Parallel.For(0, Height, py =>
                {
                    var y0 = Math.Max(py - radius, 0);
                    var y1 = Math.Min(py + radius + 1, Height);
                    for (var px = 0; px < Width; px++)
                    {
                        QueryPixelScalar(destination, px, py, y0, y1, radius, channel, valueBits);
                    }
                });
            }
        }

        // ----------------------------------------------------------- query (SIMD)

        /// <summary>
        /// AVX2 query for one row: 8 pixels per iteration, one pixel per 32-bit
        /// lane. Rank lookups gather the interleaved (word, cum) pairs as 64-bit
        /// elements; popcounts use the PSHUFB nibble table; all data-dependent
        /// branches of the quantile descent become blends.
        /// </summary>
        unsafe void QueryRowAvx2(Vector4[] destination, int py, int radius, int channel, int valueBits)
        {
            var y0 = Math.Max(py - radius, 0);
            var y1 = Math.Min(py + radius + 1, Height);
            var rowBase = py * Width;

            var iota = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
            var zero = Vector256<int>.Zero;
            var widthV = Vector256.Create(Width);
            var hV = Vector256.Create(y1 - y0);
            var Lrow = Vector256.Create(y0 * Width);
            var Rrow = Vector256.Create(y1 * Width);

            fixed (uint* wmPtr = WM)
            fixed (uint* tablePtr = Table)
            fixed (Vector4* destPtr = destination)
            {
                var pairs = (long*)wmPtr;
                var df = (float*)destPtr;

                var px = 0;
                for (; px + 8 <= Width; px += 8)
                {
                    var pxv = Vector256.Create(px) + iota;
                    var x0 = Vector256.Max(Avx2.Subtract(pxv, Vector256.Create(radius)), zero);
                    var x1 = Vector256.Min(Avx2.Add(pxv, Vector256.Create(radius + 1)), widthV);

                    var k = Avx2.ShiftRightLogical(Avx2.MultiplyLow(x1 - x0, hV), 1);
                    var L = Lrow;
                    var R = Rrow;
                    var key = zero;

                    for (var li = 0; li < valueBits; li++)
                    {
                        var slotB = li * SlotsPerLevel;
                        var num = LowFreq8(pairs, slotB + 1, L, R, x1) - LowFreq8(pairs, slotB + 1, L, R, x0);

                        var l0 = Rank0x8(pairs, slotB * WordCount, L);
                        var r0 = Rank0x8(pairs, slotB * WordCount, R);
                        var z = Vector256.Create(Zeros(slotB));

                        var caseL = Avx2.CompareGreaterThan(num, k); // k < num
                        L = Avx2.BlendVariable((L - l0) + z, l0, caseL);
                        R = Avx2.BlendVariable((R - r0) + z, r0, caseL);
                        k = Avx2.BlendVariable(k - num, k, caseL);
                        key = Avx2.Or(key, Avx2.AndNot(caseL, Vector256.Create(1 << (valueBits - 1 - li))));
                    }

                    key = Avx2.GatherVector256((int*)tablePtr, key, 4);

                    // Inverse of the order-preserving float -> uint mapping.
                    var negMask = Avx2.ShiftRightArithmetic(key, 31);
                    var u = Avx2.BlendVariable(
                        key ^ Vector256.Create(-1),           // ~key   (positive keys)
                        key & Vector256.Create(0x7FFFFFFF),   // key & ~sign (negative keys)
                        negMask);

                    var medians = u.AsSingle();
                    for (var i = 0; i < 8; i++)
                    {
                        df[(rowBase + px + i) * 4 + channel] = medians.GetElement(i);
                    }
                }

                for (; px < Width; px++)
                {
                    QueryPixelScalar(destination, px, py, y0, y1, radius, channel, valueBits);
                }
            }
        }

        /// <summary>Rank0 of 8 positions: number of zeros in [0, p) of the bit-vector at word offset <paramref name="wordOffset"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe Vector256<int> Rank0x8(long* pairs, int wordOffset, Vector256<int> p)
        {
            var idx = Avx2.Add(Vector256.Create(wordOffset), Avx2.ShiftRightLogical(p, 5));
            var g0 = Avx2.GatherVector256(pairs, idx.GetLower(), 8);
            var g1 = Avx2.GatherVector256(pairs, idx.GetUpper(), 8);

            // Deinterleave the (word, cum) pairs of the 8 lanes.
            var t0 = Avx2.Shuffle(g0.AsInt32(), 0xD8); // [w0,w1,c0,c1 | w2,w3,c2,c3]
            var t1 = Avx2.Shuffle(g1.AsInt32(), 0xD8);
            var words = Avx2.Permute4x64(Avx2.UnpackLow(t0.AsInt64(), t1.AsInt64()), 0xD8).AsInt32();
            var cums = Avx2.Permute4x64(Avx2.UnpackHigh(t0.AsInt64(), t1.AsInt64()), 0xD8).AsInt32();

            var shift = Avx2.And(p, Vector256.Create(31)).AsUInt32();
            var mask = Avx2.ShiftLeftLogicalVariable(Vector256.Create(1U), shift).AsInt32() - Vector256.Create(1);

            var ones = cums + PopCount8(words & mask);
            return p - ones;
        }

        /// <summary>Per-lane popcount of 32-bit elements (PSHUFB nibble table).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<int> PopCount8(Vector256<int> v)
        {
            var nibbleCounts = Vector256.Create((byte)0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4);
            var lowMask = Vector256.Create((byte)0x0F);

            var lo = v.AsByte() & lowMask;
            var hi = Avx2.ShiftRightLogical(v.AsUInt32(), 4).AsByte() & lowMask;
            var counts = Avx2.Shuffle(nibbleCounts, lo) + Avx2.Shuffle(nibbleCounts, hi);

            // Sum the 4 byte counts of each 32-bit lane.
            var pairSums = Avx2.MultiplyAddAdjacent(counts, Vector256.Create((sbyte)1));
            return Avx2.MultiplyAddAdjacent(pairSums, Vector256.Create((short)1));
        }

        /// <summary>8-lane low-frequency query: count of elements &lt; x (per lane) among positions [L, R).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe Vector256<int> LowFreq8(long* pairs, int slotBase, Vector256<int> L, Vector256<int> R, Vector256<int> x)
        {
            var res = Vector256<int>.Zero;
            for (var t = 0; t < XBits; t++)
            {
                var slot = slotBase + t;
                var l0 = Rank0x8(pairs, slot * WordCount, L);
                var r0 = Rank0x8(pairs, slot * WordCount, R);
                var z = Vector256.Create(Zeros(slot));

                // Lanes whose x has bit (xBits-1-t) set take the ones branch.
                var bitSet = Avx2.CompareGreaterThan(x & Vector256.Create(1 << (XBits - 1 - t)), Vector256<int>.Zero);

                res = res + ((r0 - l0) & bitSet);
                L = Avx2.BlendVariable(l0, (L - l0) + z, bitSet);
                R = Avx2.BlendVariable(r0, (R - r0) + z, bitSet);
            }
            return res;
        }

        // --------------------------------------------------------- query (scalar)

        void QueryPixelScalar(Vector4[] destination, int px, int py, int y0, int y1, int radiusX, int channel, int valueBits)
        {
            var x0 = Math.Max(px - radiusX, 0);
            var x1 = Math.Min(px + radiusX + 1, Width);

            var k = (x1 - x0) * (y1 - y0) / 2;
            var L = y0 * Width;
            var R = y1 * Width;
            var key = 0U;

            for (var li = 0; li < valueBits; li++)
            {
                var slotB = li * SlotsPerLevel;
                var num = RangeFreq(li, L, R, x0, x1);

                var l0 = Rank0(slotB, L);
                var r0 = Rank0(slotB, R);
                if (k < num)
                {
                    L = l0;
                    R = r0;
                }
                else
                {
                    k -= num;
                    var z = Zeros(slotB);
                    L = L - l0 + z;
                    R = R - r0 + z;
                    key |= 1U << (valueBits - 1 - li);
                }
            }

            key = Table[(int)key];

            var median = UnKey(key);
            var idx = py * Width + px;
            var pixel = destination[idx];
            pixel[channel] = median;
            destination[idx] = pixel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Rank0(int slot, int p)
        {
            var idx = (slot * WordCount + (p >> 5)) << 1;
            var ones = WM[idx + 1] + (uint)BitOperations.PopCount(WM[idx] & ((1U << (p & 31)) - 1U));
            return p - (int)ones;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Zeros(int slot)
        {
            return DataLength - (int)WM[((slot * WordCount + LastWord) << 1) + 1];
        }

        /// <summary>
        /// Number of elements with x-index in [x0, x1) among positions [L, R) of
        /// the masked x-index array X_li. Equivalent to
        /// LowFreq(x1) - LowFreq(x0), but the two traversals share the levels of
        /// the common high-bit prefix of x0 and x1 (where their contributions
        /// cancel and their interval states coincide).
        /// </summary>
        int RangeFreq(int li, int L, int R, int x0, int x1)
        {
            if (x0 >= x1)
            {
                return 0;
            }

            var slotBase = li * SlotsPerLevel + 1;

            // Shared prefix: identical bits of x0 and x1 narrow one common state.
            var t = 0;
            for (; t < XBits; t++)
            {
                var shift = XBits - 1 - t;
                if ((((x0 ^ x1) >> shift) & 1) != 0)
                {
                    break;
                }

                var slot = slotBase + t;
                var l0 = Rank0(slot, L);
                var r0 = Rank0(slot, R);
                if (((x1 >> shift) & 1) == 0)
                {
                    L = l0;
                    R = r0;
                }
                else
                {
                    var z = Zeros(slot);
                    L = L - l0 + z;
                    R = R - r0 + z;
                }
            }

            // First differing bit: x0 has 0, x1 has 1 (since x0 < x1). x1's
            // traversal counts all zeros here and descends into the ones region;
            // x0's traversal descends into the zeros region.
            var slotD = slotBase + t;
            var l0d = Rank0(slotD, L);
            var r0d = Rank0(slotD, R);
            var zd = Zeros(slotD);

            var res = r0d - l0d;
            res += LowFreqTail(slotBase, t + 1, L - l0d + zd, R - r0d + zd, x1);
            res -= LowFreqTail(slotBase, t + 1, l0d, r0d, x0);
            return res;
        }

        /// <summary>Low-frequency query restricted to the levels below <paramref name="t"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int LowFreqTail(int slotBase, int t, int L, int R, int x)
        {
            var res = 0;
            for (; t < XBits; t++)
            {
                var slot = slotBase + t;
                var l0 = Rank0(slot, L);
                var r0 = Rank0(slot, R);
                if (((x >> (XBits - 1 - t)) & 1) == 0)
                {
                    L = l0;
                    R = r0;
                }
                else
                {
                    res += r0 - l0;
                    var z = Zeros(slot);
                    L = L - l0 + z;
                    R = R - r0 + z;
                }
            }
            return res;
        }

        // ------------------------------------------------------------ construction

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void BuildLevels()
        {
            var valueBits = BuiltValueBits;

            for (var li = 0; li < valueBits; li++)
            {
                var slotB = li * SlotsPerLevel;

                PackBits(ValA, slotB, valueBits - 1 - li);
                BuildCum(slotB);

                var slot0 = slotB + 1;
                PackBitsMasked(XA, slotB, slot0, XBits - 1);
                BuildCum(slot0);

                if (XBits > 1)
                {
                    ScatterMasked(slotB, slot0, XA, NXA);
                }

                for (var t = 1; t < XBits; t++)
                {
                    var slot = slotB + 1 + t;
                    PackBits(NXA, slot, XBits - 1 - t);
                    BuildCum(slot);

                    if (t < XBits - 1)
                    {
                        ScatterSingle(slot, NXA, NXB);
                        (NXA, NXB) = (NXB, NXA);
                    }
                }

                if (li < valueBits - 1)
                {
                    ScatterPair(slotB);
                    (ValA, ValB) = (ValB, ValA);
                    (XA, XB) = (XB, XA);
                }
            }
        }

        /// <summary>
        /// Packs bit <paramref name="bitIndex"/> of 32 consecutive values into
        /// one word: SIMD shifts the target bit into the sign position and uses
        /// movemask (AVX2: 4x8 lanes, else SSE2: 8x4 lanes; SSE2 always exists
        /// on x64).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void PackBits(uint[] values, int slot, int bitIndex)
        {
            var offset = slot * WordCount;
            var fullWords = DataLength >> 5;

            ParallelFor(fullWords, (from, to) =>
            {
                fixed (uint* vp = values)
                {
                    if (Avx2.IsSupported)
                    {
                        var count = Vector128.CreateScalar((uint)(31 - bitIndex));
                        for (var w = from; w < to; w++)
                        {
                            var p = vp + ((long)w << 5);
                            var m0 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p), count).AsSingle());
                            var m1 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p + 8), count).AsSingle());
                            var m2 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p + 16), count).AsSingle());
                            var m3 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p + 24), count).AsSingle());
                            WM[(offset + w) << 1] = m0 | (m1 << 8) | (m2 << 16) | (m3 << 24);
                        }
                    }
                    else
                    {
                        var count = Vector128.CreateScalar((uint)(31 - bitIndex));
                        for (var w = from; w < to; w++)
                        {
                            var p = vp + ((long)w << 5);
                            var word = 0U;
                            for (var q = 0; q < 8; q++)
                            {
                                word |= (uint)Sse.MoveMask(Sse2.ShiftLeftLogical(Sse2.LoadVector128(p + q * 4), count).AsSingle()) << (q * 4);
                            }
                            WM[(offset + w) << 1] = word;
                        }
                    }
                }
            });

            // Trailing partial word + the zero padded word.
            for (var w = fullWords; w < WordCount; w++)
            {
                var word = 0U;
                var baseIndex = w << 5;
                var count = Math.Min(DataLength - baseIndex, 32);
                for (var s = 0; s < count; s++)
                {
                    word |= ((values[baseIndex + s] >> bitIndex) & 1U) << s;
                }
                WM[(offset + w) << 1] = word;
            }
        }

        /// <summary>
        /// Same as <see cref="PackBits"/> but for the masked x-index array X_i:
        /// entries whose B_i bit is 1 count as the sentinel `width`. Word-level
        /// identity: result = maskWord selects between bit(width) and bit(x).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void PackBitsMasked(uint[] xIn, int maskSlot, int slot, int bitIndex)
        {
            var maskOffset = maskSlot * WordCount;
            var offset = slot * WordCount;
            var fullWords = DataLength >> 5;
            var sentinelBit = ((Width >> bitIndex) & 1) != 0;

            ParallelFor(fullWords, (from, to) =>
            {
                fixed (uint* vp = xIn)
                {
                    if (Avx2.IsSupported)
                    {
                        var count = Vector128.CreateScalar((uint)(31 - bitIndex));
                        for (int w = from; w < to; w++)
                        {
                            var p = vp + ((long)w << 5);
                            var m0 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p), count).AsSingle());
                            var m1 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p + 8), count).AsSingle());
                            var m2 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p + 16), count).AsSingle());
                            var m3 = (uint)Avx.MoveMask(Avx2.ShiftLeftLogical(Avx.LoadVector256(p + 24), count).AsSingle());
                            var xWord = m0 | (m1 << 8) | (m2 << 16) | (m3 << 24);
                            var maskWord = WM[(maskOffset + w) << 1];
                            if (sentinelBit)
                            {
                                WM[(offset + w) << 1] = (xWord & ~maskWord) | maskWord;
                            }
                            else
                            {
                                WM[(offset + w) << 1] = xWord & ~maskWord;
                            }
                        }
                    }
                    else
                    {
                        for (var w = from; w < to; w++)
                        {
                            var word = 0U;
                            var baseIndex = w << 5;
                            for (var s = 0; s < 32; s++)
                            {
                                word |= ((xIn[baseIndex + s] >> bitIndex) & 1U) << s;
                            }
                            var maskWord = WM[(maskOffset + w) << 1];
                            if (sentinelBit)
                            {
                                WM[(offset + w) << 1] = (word & ~maskWord) | maskWord;
                            }
                            else
                            {
                                WM[(offset + w) << 1] = word & ~maskWord;
                            }
                        }
                    }
                }
            });

            for (var w = fullWords; w < WordCount; w++)
            {
                var word = 0U;
                var baseIndex = w << 5;
                var count = Math.Min(DataLength - baseIndex, 32);
                for (var s = 0; s < count; s++)
                {
                    var xv = ((WM[(maskOffset + w) << 1] >> s) & 1U) != 0 ? (uint)Width : xIn[baseIndex + s];
                    word |= ((xv >> bitIndex) & 1U) << s;
                }
                WM[(offset + w) << 1] = word;
            }
        }

        /// <summary>Word-granularity exclusive prefix sums of 1-bits (sequential; the array is only n/32 long).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void BuildCum(int slot)
        {
            var offset = slot * WordCount;
            var running = 0U;
            for (var w = 0; w < WordCount; w++)
            {
                var idx = (offset + w) << 1;
                WM[idx + 1] = running;
                running += (uint)BitOperations.PopCount(WM[idx]);
            }
        }

        /// <summary>
        /// Stable partitions walk whole 32-bit words, tracking the rank
        /// incrementally: zeros go to j - onesBefore, ones after all zeros.
        /// Scattered writes dominate here, so these stay scalar.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ScatterPair(int slot)
        {
            var offset = slot * WordCount;
            var zerosTotal = Zeros(slot);
            var vIn = ValA;
            var vOut = ValB;
            var xIn = XA;
            var xOut = XB;

            ParallelFor((DataLength + 31) >> 5, (from, to) =>
            {
                for (var w = from; w < to; w++)
                {
                    var idx = (offset + w) << 1;
                    var word = WM[idx];
                    var ones = (int)WM[idx + 1];
                    var jBase = w << 5;
                    var count = Math.Min(DataLength - jBase, 32);
                    for (var s = 0; s < count; s++)
                    {
                        var j = jBase + s;
                        var dest = 0;
                        if (((word >> s) & 1U) == 0)
                        {
                            dest = j - ones;
                        }
                        else
                        {
                            dest = zerosTotal + ones;
                            ones++;
                        }
                        vOut[dest] = vIn[j];
                        xOut[dest] = xIn[j];
                    }
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ScatterSingle(int slot, uint[] dataIn, uint[] dataOut)
        {
            var offset = slot * WordCount;
            var zerosTotal = Zeros(slot);

            ParallelFor((DataLength + 31) >> 5, (from, to) =>
            {
                for (var w = from; w < to; w++)
                {
                    var idx = (offset + w) << 1;
                    var word = WM[idx];
                    var ones = (int)WM[idx + 1];
                    var jBase = w << 5;
                    var count = Math.Min(DataLength - jBase, 32);
                    for (var s = 0; s < count; s++)
                    {
                        var j = jBase + s;
                        var dest = 0;
                        if (((word >> s) & 1u) == 0)
                        {
                            dest = j - ones;
                        }
                        else
                        {
                            dest = zerosTotal + ones;
                            ones++;
                        }
                        dataOut[dest] = dataIn[j];
                    }
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ScatterMasked(int maskSlot, int slot, uint[] xIn, uint[] xOut)
        {
            var maskOffset = maskSlot * WordCount;
            var offset = slot * WordCount;
            var zerosTotal = Zeros(slot);
            var w32 = (uint)Width;

            ParallelFor((DataLength + 31) >> 5, (from, to) =>
            {
                for (var w = from; w < to; w++)
                {
                    var idx = (offset + w) << 1;
                    var word = WM[idx];
                    var ones = (int)WM[idx + 1];
                    var maskWord = WM[(maskOffset + w) << 1];
                    var jBase = w << 5;
                    var count = Math.Min(DataLength - jBase, 32);
                    for (var s = 0; s < count; s++)
                    {
                        var j = jBase + s;
                        var dest = 0;
                        if (((word >> s) & 1u) == 0)
                        {
                            dest = j - ones;
                        }
                        else
                        {
                            dest = zerosTotal + ones;
                            ones++;
                        }
                        xOut[dest] = ((maskWord >> s) & 1u) != 0 ? w32 : xIn[j];
                    }
                }
            });
        }

        /// <summary>
        /// Parallel stable LSD radix sort of the (key, index) pairs in
        /// <see cref="SortA"/> by the key (high 32 bits), 8-bit digits x 4 passes
        /// with per-chunk histograms.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RadixSortByKey()
        {
            const int ChunkSize = 1 << 16;
            var chunks = (DataLength + ChunkSize - 1) / ChunkSize;
            var counts = new int[chunks][];

            var src = SortA;
            var dst = SortB;

            for (var pass = 0; pass < 4; pass++)
            {
                var shift = 32 + pass * 8;
                var s = src;
                var d = dst;

                Parallel.For(0, chunks, c =>
                {
                    var count = counts[c] ??= ArrayPool<int>.Shared.Rent(256);
                    count.AsSpan().Clear();
                    var end = Math.Min((c + 1) * ChunkSize, DataLength);
                    for (var j = c * ChunkSize; j < end; j++)
                    {
                        count[(int)((s[j] >> shift) & 255u)]++;
                    }
                });

                // Digit-major exclusive scan turns counts into global start offsets.
                var total = 0;
                for (var digit = 0; digit < 256; digit++)
                {
                    for (var c = 0; c < chunks; c++)
                    {
                        var t = counts[c][digit];
                        counts[c][digit] = total;
                        total += t;
                    }
                }

                Parallel.For(0, chunks, c =>
                {
                    var offsets = counts[c];
                    var end = Math.Min((c + 1) * ChunkSize, DataLength);
                    for (var j = c * ChunkSize; j < end; j++)
                    {
                        d[offsets[(int)((s[j] >> shift) & 255u)]++] = s[j];
                    }
                });

                (src, dst) = (dst, src);
            }

            foreach (var count in counts)
            {
                if (count != null)
                {
                    ArrayPool<int>.Shared.Return(count);
                }
            }
            // 4 passes = even number of swaps: the sorted data is back in sortA.
        }

        // ---------------------------------------------------------------- helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Key(float f)
        {
            var u = BitConverter.SingleToUInt32Bits(f);
            if ((u & 0x80000000u) != 0)
            {
                return ~u;
            }
            else
            {
                return (u | 0x80000000u);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float UnKey(uint k)
        {
            if ((k & 0x80000000U) != 0)
            {
                return BitConverter.UInt32BitsToSingle((k & 0x7FFFFFFFU));
            }
            else
            {
                return BitConverter.UInt32BitsToSingle(~k);
            }
        }

        /// <summary>Runs the body over [0, count) split into contiguous ranges, one per worker.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ParallelFor(int count, Action<int, int> body)
        {
            var workers = Math.Min(Environment.ProcessorCount * 2, Math.Max(count / 4096, 1));
            if (workers <= 1)
            {
                body(0, count);
                return;
            }

            var chunk = (count + workers - 1) / workers;
            Parallel.For(0, workers, w =>
            {
                var from = w * chunk;
                var to = Math.Min(from + chunk, count);
                if (from < to)
                {
                    body(from, to);
                }
            });
        }

        public void Dispose()
        {
            ArrayPool<uint>.Shared.Return(WM);
            ArrayPool<uint>.Shared.Return(Table);
            ArrayPool<uint>.Shared.Return(ValA);
            ArrayPool<uint>.Shared.Return(ValB);
            ArrayPool<uint>.Shared.Return(XA);
            ArrayPool<uint>.Shared.Return(XB);
            ArrayPool<uint>.Shared.Return(NXA);
            ArrayPool<uint>.Shared.Return(NXB);
            ArrayPool<ulong>.Shared.Return(SortA);
            ArrayPool<ulong>.Shared.Return(SortB);
        }
    }

    /// <summary>
    /// GPU median filter with O(1) cost per pixel w.r.t. the kernel radius, based
    /// on "Constant Time Median Filter using 2D Wavelet Matrix" (Yuji Moroto and
    /// Nobuyuki Umetani, SIGGRAPH Asia 2022, https://doi.org/10.1145/3550454.3555512).
    ///
    /// The input image is a flattened row-major buffer of 32-bit float BGRA
    /// pixels (<see cref="ReadWriteBuffer{T}"/> of <see cref="Float4"/>). Floats
    /// are handled exactly (including negative values) by mapping their IEEE 754
    /// bit patterns to order-preserving 32-bit integer keys, so the filter runs
    /// 32 value-bit levels and returns bit-exact pixel values from the window.
    ///
    /// Windows are clamped at the image borders (the median is taken over the
    /// intersection of the window with the image). For "replicate border"
    /// semantics, pad the input by the radius before filtering.
    ///
    /// Usage:
    /// <code>
    /// using var filter = new WaveletMatrixMedianFilter(GraphicsDevice.GetDefault(), width, height);
    /// filter.Apply(sourceBuffer, destinationBuffer, radius: 14);
    /// </code>
    /// The instance allocates all GPU scratch memory up front and can be reused
    /// for any number of images of the same size (e.g. video frames). For
    /// interactive radius tweaking on a fixed image, call <see cref="Build"/>
    /// once per channel and then <see cref="Query"/> repeatedly - queries are
    /// independent of the radius and much cheaper than construction.
    /// </summary>
    public sealed class WaveletMatrixMedianFilterGpu : IDisposable
    {
        readonly GraphicsDevice Device;

        readonly int Width;

        readonly int Height;

        readonly int DataLength;

        readonly int MaxValueBits;   // allocated bit levels (32 or ceil(log2(N)))

        readonly int XBits;          // bits needed to represent the sentinel value `width`

        readonly int SlotsPerLevel;  // 1 (B_i) + XBits (nested wavelet matrix)

        readonly int WordCount;      // uints per bit-vector level, incl. one padded word

        readonly int LastWord;       // index of the padded word (= ceil(N/32))

        int BuiltValueBits;          // bit levels actually used by the last Build

        readonly ReadWriteBuffer<uint> Bits;

        readonly ReadWriteBuffer<uint> Cum;

        readonly ReadWriteBuffer<uint> Table;  // dense rank -> key lookup (RankReduced mode)

        ReadWriteBuffer<uint> ValA;

        ReadWriteBuffer<uint> ValB;            // (value key, x index) pair arrays, ping-pong

        ReadWriteBuffer<uint> XA;

        ReadWriteBuffer<uint> XB;

        ReadWriteBuffer<uint> NxA;

        ReadWriteBuffer<uint> NxB;             // nested wavelet matrix scratch, ping-pong

        readonly uint[] ReadbackScratch = new uint[1];

        bool Disposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ComputeValueBits(long n)
        {
            return 64 - BitOperations.LeadingZeroCount((ulong)(n - 1));
        }

        public WaveletMatrixMedianFilterGpu(GraphicsDevice device, int width, int height)
        {
            Device = device;
            Width = width;
            Height = height;

            var nLong = (long)width * height;
            MaxValueBits = ComputeValueBits(nLong);
            BuiltValueBits = MaxValueBits;
            XBits = BitOperations.Log2((uint)width) + 1;
            SlotsPerLevel = 1 + XBits;
            var wordCountLong = (nLong + 31) / 32 + 1;
            var totalWords = MaxValueBits * (long)SlotsPerLevel * wordCountLong;

            DataLength = (int)nLong;
            WordCount = (int)wordCountLong;
            LastWord = WordCount - 1;

            Bits = device.AllocateReadWriteBuffer<uint>((int)totalWords);
            Cum = device.AllocateReadWriteBuffer<uint>((int)totalWords);
            Table = device.AllocateReadWriteBuffer<uint>(DataLength);
            ValA = device.AllocateReadWriteBuffer<uint>(DataLength);
            ValB = device.AllocateReadWriteBuffer<uint>(DataLength);
            XA = device.AllocateReadWriteBuffer<uint>(DataLength);
            XB = device.AllocateReadWriteBuffer<uint>(DataLength);
            NxA = device.AllocateReadWriteBuffer<uint>(DataLength);
            NxB = device.AllocateReadWriteBuffer<uint>(DataLength);
        }

        /// <summary>
        /// Applies a median filter with a rectangular (2rx+1)x(2ry+1) window to
        /// the selected channels.
        /// </summary>
        public void Apply(ReadWriteBuffer<Float4> source, ReadWriteBuffer<Float4> destination, int radius, ChannelMask channels)
        {
            if (!ReferenceEquals(source, destination))
            {
                source.CopyTo(destination);
            }

            for (var channel = 0; channel < 4; channel++)
            {
                if (((int)channels & (1 << channel)) == 0)
                {
                    continue;
                }

                Build(source, channel);
                Query(destination, radius, channel);
            }
        }

        /// <summary>
        /// Constructs the 2D wavelet matrix for one channel (0=B, 1=G, 2=R, 3=A)
        /// of the source image. This is the expensive half of the filter; the
        /// result stays on the GPU and serves any number of <see cref="Query"/>
        /// calls with arbitrary radii.
        /// </summary>
        public void Build(ReadWriteBuffer<Float4> source, int channel)
        {
            var elemThreads = (DataLength + 3) / 4;

            // Rank reduction (float support of Section 4, tightened to dense
            // ranks): sort the (key, flat index) pairs with a 1-bit LSD radix
            // sort (32 stable partitions reusing the level-0 bits/cum region
            // as scratch), then collapse equal keys to a single dense rank so
            // the wavelet matrix only needs ceil(log2(#distinct)) levels.
            using (var ctx = Device.CreateComputeContext())
            {
                ctx.For(elemThreads, new InitChannelShader(source, ValA, XA, DataLength, Width, channel, 1));
                ctx.Barrier(ValA);
                ctx.Barrier(XA);

                for (var bit = 0; bit < 32; bit++)
                {
                    ctx.For(WordCount, new PackBitsShader(ValA, Bits, 0, bit, DataLength, WordCount));
                    ctx.Barrier(Bits);
                    BuildRank(in ctx, 0);
                    ctx.For(elemThreads, new ScatterPairShader(Bits, 0, Cum, 0, ValA, XA, ValB, XB, DataLength, LastWord));
                    ctx.Barrier(ValB);
                    ctx.Barrier(XB);
                    (ValA, ValB) = (ValB, ValA);
                    (XA, XB) = (XB, XA);
                }

                // Dense-rank the sorted keys: flag run starts, prefix-sum the
                // flags (reusing the rank machinery on the level-0 region),
                // fill the rank -> key table and scatter each pixel's dense
                // rank back to its original position.
                ctx.For(WordCount, new PackDistinctFlagsShader(ValA, Bits, 0, DataLength, WordCount));
                ctx.Barrier(Bits);
                BuildRank(in ctx, 0);
                ctx.For(elemThreads, new TableAndRankShader(ValA, XA, Bits, Cum, 0, Table, ValB, XB, DataLength, Width));
                ctx.Barrier(Table);
                ctx.Barrier(ValB);
                ctx.Barrier(XB);
                (ValA, ValB) = (ValB, ValA);
                (XA, XB) = (XB, XA);
            }

            // Cum[LastWord] of the flag bit-vector = number of distinct keys.
            Cum.CopyTo(ReadbackScratch, LastWord, 0, 1);
            var distinct = (int)ReadbackScratch[0];
            if (distinct <= 1)
            {
                BuiltValueBits = 1;
            }
            else
            {
                BuiltValueBits = 64 - BitOperations.LeadingZeroCount((ulong)(distinct - 1));
            }

            BuildLevels(elemThreads);
        }

        /// <summary>Builds the wavelet matrix levels from the prepared (value, x-index) arrays in ValA/XA.</summary>
        void BuildLevels(int elemThreads)
        {
            var valueBits = BuiltValueBits;
            using var ctx = Device.CreateComputeContext();

            for (var li = 0; li < valueBits; li++)
            {
                var slotB = li * SlotsPerLevel;

                // B_i: bit-vector of value bit (valueBits-1-li) in the current order S_i.
                ctx.For(WordCount, new PackBitsShader(ValA, Bits, slotB * WordCount, valueBits - 1 - li, DataLength, WordCount));
                ctx.Barrier(Bits);
                BuildRank(in ctx, slotB);

                // Nested wavelet matrix over X_i (partitions by x bits, MSB
                // first). The sentinel masking of X_i (entries with B_i bit 1
                // become `width`) is applied on the fly by the first pack and
                // scatter instead of materializing the masked array.
                var slot0 = slotB + 1;
                ctx.For(WordCount, new PackBitsMaskedShader(XA, Bits, slotB * WordCount, slot0 * WordCount, XBits - 1, DataLength, WordCount, Width));
                ctx.Barrier(Bits);
                BuildRank(in ctx, slot0);

                if (XBits > 1)
                {
                    ctx.For(elemThreads, new ScatterMaskedShader(Bits, slotB * WordCount, slot0 * WordCount, Cum, slot0 * WordCount, XA, NxA, DataLength, LastWord, Width));
                    ctx.Barrier(NxA);
                }

                for (var t = 1; t < XBits; t++)
                {
                    var slot = slotB + 1 + t;
                    ctx.For(WordCount, new PackBitsShader(NxA, Bits, slot * WordCount, XBits - 1 - t, DataLength, WordCount));
                    ctx.Barrier(Bits);
                    BuildRank(in ctx, slot);

                    if (t < XBits - 1)
                    {
                        ctx.For(elemThreads, new ScatterSingleShader(Bits, slot * WordCount, Cum, slot * WordCount, NxA, NxB, DataLength, LastWord));
                        ctx.Barrier(NxB);
                        (NxA, NxB) = (NxB, NxA);
                    }
                }

                // Advance S_i -> S_{i-1}: stable partition of the pairs by B_i.
                if (li < valueBits - 1)
                {
                    ctx.For(elemThreads, new ScatterPairShader(Bits, slotB * WordCount, Cum, slotB * WordCount, ValA, XA, ValB, XB, DataLength, LastWord));
                    ctx.Barrier(ValB);
                    ctx.Barrier(XB);
                    (ValA, ValB) = (ValB, ValA);
                    (XA, XB) = (XB, XA);
                }
            }
        }

        /// <summary>
        /// Computes the median for every pixel from the wavelet matrix built by
        /// the last <see cref="Build"/> call and writes it into the given channel
        /// of <paramref name="destination"/>. Cost is independent of the radii.
        /// </summary>
        public void Query(ReadWriteBuffer<Float4> destination, int radius, int channel)
        {
            Device.For(Width, Height,
                new MedianQueryShader(
                    destination,
                    Bits,
                    Cum,
                    Table,
                    Width,
                    Height,
                    DataLength,
                    WordCount,
                    LastWord,
                    XBits,
                    SlotsPerLevel,
                    BuiltValueBits,
                    radius,
                    channel
                )
            );
        }

        /// <summary>Builds the word-level rank acceleration data for one bit-vector level (single dispatch).</summary>
        void BuildRank(in ComputeContext ctx, int slot)
        {
            var offset = slot * WordCount;
            ctx.For(256, new FusedRankShader(Bits, offset, Cum, offset, WordCount));
            ctx.Barrier(Cum);
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;
            Bits.Dispose();
            Cum.Dispose();
            Table.Dispose();
            ValA.Dispose();
            ValB.Dispose();
            XA.Dispose();
            XB.Dispose();
            NxA.Dispose();
            NxB.Dispose();
        }
    }

    // ---------------------------------------------------------------------------
    // GPU shaders implementing "Constant Time Median Filter using 2D Wavelet
    // Matrix" (Moroto & Umetani, SIGGRAPH Asia 2022).
    //
    // Data layout shared by all shaders:
    //   - Each "level" (= one bit-vector with rank support) occupies a region of
    //     `wordCount` uints inside the shared `bits` and `cum` buffers.
    //     bits : packed bit-vector (bit j of the level = bit (j%32) of word j/32)
    //     cum  : exclusive prefix sum of set bits ("ones") at word granularity,
    //            i.e. cum[w] = number of 1-bits in words [0, w).
    //   - `wordCount` = ceil(n / 32) + 1. The extra padded word is always zero,
    //     so cum[ceil(n/32)] equals the total number of 1-bits of the level and
    //     Rank() stays in-bounds for positions up to n inclusive.
    //   - Level slot layout per value-bit level `li` (0 = MSB of the value key):
    //       slot li*(1+xBits)          : B_i  (the value-bit vector of the paper)
    //       slot li*(1+xBits) + 1 + t  : bit-vector t of the nested wavelet
    //                                    matrix over the masked x-index array X_i
    //                                    (t = 0 corresponds to the MSB of x).
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Extracts one channel of the BGRA image as order-preserving 32-bit integer
    /// keys and initializes the companion index array: the x-index (j % width)
    /// when <see cref="flatIndex"/> is 0, or the flat pixel index j (used as the
    /// radix sort payload in rank-reduction mode) when it is 1.
    /// Processes 4 elements per thread.
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct InitChannelShader(ReadWriteBuffer<Float4> source, ReadWriteBuffer<uint> values, ReadWriteBuffer<uint> xIndex, int n, int width, int channel, int flatIndex) : IComputeShader
    {
        public void Execute()
        {
            for (var e = 0; e < 4; e++)
            {
                var j = ThreadIds.X * 4 + e;
                if (j >= n)
                {
                    return;
                }

                var pixel = source[j];
                var f = pixel[channel];

                // Order-preserving float -> uint mapping (IEEE 754 total order).
                var u = Hlsl.AsUInt(f);
                if ((u & 0x80000000U) != 0)
                {
                    values[j] = ~u;
                }
                else
                {
                    values[j] = u | 0x80000000U;
                }

                if (flatIndex != 0)
                {
                    xIndex[j] = (uint)j;
                }
                else
                {
                    xIndex[j] = (uint)(j % width);
                }
            }
        }
    }

    /// <summary>
    /// Marks the first occurrence of each distinct key in the sorted key array
    /// as a bit-vector (bit j = 1 iff sortedKeys[j] differs from its
    /// predecessor). Running the rank pass over this bit-vector then yields the
    /// dense rank of every element and the total number of distinct values.
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PackDistinctFlagsShader(ReadWriteBuffer<uint> sortedKeys, ReadWriteBuffer<uint> bits, int bitsOffset, int n, int wordCount) : IComputeShader
    {
        public void Execute()
        {
            var w = ThreadIds.X;
            if (w >= wordCount)
            {
                return;
            }

            var word = 0U;
            var baseIndex = w * 32;
            var count = Hlsl.Min(n - baseIndex, 32);
            for (var s = 0; s < count; s++)
            {
                var j = baseIndex + s;
                var flag = 0U;
                if (j == 0 || sortedKeys[j] != sortedKeys[j - 1])
                {
                    flag = 1U;
                }
                word |= flag << s;
            }
            bits[bitsOffset + w] = word;
        }
    }

    /// <summary>
    /// Finalizes the rank reduction after the radix sort (the float support of
    /// Section 4 of the paper, tightened to dense ranks): every element receives
    /// the number of distinct keys before it, so equal pixels share one integer
    /// and the wavelet matrix only needs ceil(log2(distinct)) bit levels. The
    /// first element of each run stores its key into the rank -> key lookup
    /// table; each element's dense rank is scattered back to its original pixel
    /// position and the x-index array is re-initialized to pixel x coordinates.
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct TableAndRankShader(
        ReadWriteBuffer<uint> sortedKeys,
        ReadWriteBuffer<uint> sortedIndices,
        ReadWriteBuffer<uint> flagBits,
        ReadWriteBuffer<uint> flagCum,
        int flagOffset,
        ReadWriteBuffer<uint> table,
        ReadWriteBuffer<uint> ranksOut,
        ReadWriteBuffer<uint> xIndexOut,
        int n,
        int width
    ) : IComputeShader
    {
        public void Execute()
        {
            for (var e = 0; e < 4; e++)
            {
                var j = ThreadIds.X * 4 + e;
                if (j >= n)
                {
                    return;
                }

                var w = j >> 5;
                var word = flagBits[flagOffset + w];
                var flag = (word >> (j & 31)) & 1U;
                var flagsBefore = flagCum[flagOffset + w] + Hlsl.CountBits(word & ((1U << (j & 31)) - 1U));
                var denseRank = flagsBefore + flag - 1;

                if (flag != 0)
                {
                    table[(int)denseRank] = sortedKeys[j];
                }

                var p = (int)sortedIndices[j];
                ranksOut[p] = denseRank;
                xIndexOut[p] = (uint)(p % width);
            }
        }
    }

    /// <summary>
    /// Packs bit <see cref="bitIndex"/> of each value into the bit-vector region
    /// starting at <see cref="bitsOffset"/>. One thread per 32-bit output word;
    /// out-of-range bits (including the padded last word) are set to zero.
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PackBitsShader(ReadWriteBuffer<uint> values, ReadWriteBuffer<uint> bits, int bitsOffset, int bitIndex, int n, int wordCount) : IComputeShader
    {
        public void Execute()
        {
            var w = ThreadIds.X;
            if (w >= wordCount)
            {
                return;
            }

            var word = 0U;
            var baseIndex = w * 32;
            var count = Hlsl.Min(n - baseIndex, 32);
            for (var s = 0; s < count; s++)
            {
                word |= ((values[baseIndex + s] >> bitIndex) & 1U) << s;
            }
            bits[bitsOffset + w] = word;
        }
    }

    /// <summary>
    /// Builds the word-granularity rank data of one bit-vector level in a single
    /// dispatch: one 256-thread group where each thread popcount-sums its
    /// contiguous word chunk, an exclusive scan of the 256 chunk sums runs in
    /// group shared memory, and each thread then writes the per-word prefix sums.
    /// Dispatch with exactly one thread group (For(256)).
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct FusedRankShader(ReadWriteBuffer<uint> bits, int bitsOffset, ReadWriteBuffer<uint> cum, int cumOffset, int wordCount) : IComputeShader
    {
        [GroupShared(256)]
#pragma warning disable IDE0044 // ComputeSharp が設定するため readonly にはしない
        static uint[] Sums = null!; // populated by the runtime; the initializer is ignored in HLSL
#pragma warning restore IDE0044 // 読み取り専用修飾子を追加します

        public void Execute()
        {
            var t = ThreadIds.X;
            var chunkWords = (wordCount + 255) / 256;
            var start = t * chunkWords;
            var end = Hlsl.Min(start + chunkWords, wordCount);

            var mySum = 0U;
            for (var w = start; w < end; w++)
            {
                mySum += Hlsl.CountBits(bits[bitsOffset + w]);
            }

            // Inclusive Hillis-Steele scan over the 256 chunk sums.
            Sums[t] = mySum;
            Hlsl.GroupMemoryBarrierWithGroupSync();
            for (var o = 1; o < 256; o <<= 1)
            {
                var v = 0U;
                if (t >= o)
                {
                    v = Sums[t - o];
                }
                Hlsl.GroupMemoryBarrierWithGroupSync();
                Sums[t] += v;
                Hlsl.GroupMemoryBarrierWithGroupSync();
            }

            var running = Sums[t] - mySum; // exclusive prefix of this chunk
            for (var w = start; w < end; w++)
            {
                cum[cumOffset + w] = running;
                running += Hlsl.CountBits(bits[bitsOffset + w]);
            }
        }
    }

    /// <summary>
    /// Packs bit <see cref="bitIndex"/> of the masked x-index array X_i without
    /// materializing it: entries whose B_i bit (read from the value bit-vector at
    /// <see cref="maskOffset"/>) is 1 are treated as the sentinel value `width`.
    /// Fuses the MaskX pass of the straightforward implementation into the pack.
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct PackBitsMaskedShader(ReadWriteBuffer<uint> xIn, ReadWriteBuffer<uint> bits, int maskOffset, int bitsOffset, int bitIndex, int n, int wordCount, int width) : IComputeShader
    {
        public void Execute()
        {
            var w = ThreadIds.X;
            if (w >= wordCount)
            {
                return;
            }

            var maskWord = bits[maskOffset + w];
            var word = 0U;
            var baseIndex = w * 32;
            var count = Hlsl.Min(n - baseIndex, 32);
            for (var s = 0; s < count; s++)
            {
                var xv = 0U;
                if (((maskWord >> s) & 1U) != 0)
                {
                    xv = (uint)width;
                }
                else
                {
                    xv = xIn[baseIndex + s];
                }
                word |= ((xv >> bitIndex) & 1U) << s;
            }
            bits[bitsOffset + w] = word;
        }
    }

    /// <summary>
    /// First stable partition of the nested wavelet matrix, fused with the
    /// masking step: reads the raw x-indices, applies the sentinel substitution
    /// on the fly (using the B_i bit-vector at <see cref="maskOffset"/>), and
    /// partitions the masked values by the level bit-vector at
    /// <see cref="bitsOffset"/>.
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ScatterMaskedShader(
        ReadWriteBuffer<uint> bits,
        int maskOffset,
        int bitsOffset,
        ReadWriteBuffer<uint> cum,
        int cumOffset,
        ReadWriteBuffer<uint> xIn,
        ReadWriteBuffer<uint> xOut,
        int n,
        int lastWord,
        int width
    ) : IComputeShader
    {
        public void Execute()
        {
            for (var e = 0; e < 4; e++)
            {
                var j = ThreadIds.X * 4 + e;
                if (j >= n)
                {
                    return;
                }

                var w = j >> 5;
                var word = bits[bitsOffset + w];
                var onesBefore = cum[cumOffset + w] + Hlsl.CountBits(word & ((1U << (j & 31)) - 1U));

                var dest = 0;
                if (((word >> (j & 31)) & 1U) == 0)
                {
                    dest = j - (int)onesBefore;
                }
                else
                {
                    var zerosTotal = n - (int)cum[cumOffset + lastWord];
                    dest = zerosTotal + (int)onesBefore;
                }

                var masked = 0U;
                if (((bits[maskOffset + w] >> (j & 31)) & 1U) != 0)
                {
                    masked = (uint)width;
                }
                else
                {
                    masked = xIn[j];
                }
                xOut[dest] = masked;
            }
        }
    }

    /// <summary>
    /// Stable partition of the (value, x-index) pair arrays by the bit-vector of
    /// the level: elements with bit 0 keep their order at the front, elements
    /// with bit 1 keep their order after all zeros.
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ScatterPairShader(
        ReadWriteBuffer<uint> bits,
        int bitsOffset,
        ReadWriteBuffer<uint> cum,
        int cumOffset,
        ReadWriteBuffer<uint> valIn,
        ReadWriteBuffer<uint> xIn,
        ReadWriteBuffer<uint> valOut,
        ReadWriteBuffer<uint> xOut,
        int n,
        int lastWord
    ) : IComputeShader
    {
        public void Execute()
        {
            for (var e = 0; e < 4; e++)
            {
                var j = ThreadIds.X * 4 + e;
                if (j >= n)
                {
                    return;
                }

                var w = j >> 5;
                var word = bits[bitsOffset + w];
                var onesBefore = cum[cumOffset + w] + Hlsl.CountBits(word & ((1U << (j & 31)) - 1U));

                var dest = 0;
                if (((word >> (j & 31)) & 1U) == 0)
                {
                    dest = j - (int)onesBefore; // rank0(j)
                }
                else
                {
                    var zerosTotal = n - (int)cum[cumOffset + lastWord];
                    dest = zerosTotal + (int)onesBefore;
                }

                valOut[dest] = valIn[j];
                xOut[dest] = xIn[j];
            }
        }
    }

    /// <summary>
    /// Stable partition of a single array (used for the nested wavelet matrix
    /// over the masked x-indices).
    /// </summary>
    [ThreadGroupSize(256, 1, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct ScatterSingleShader(
        ReadWriteBuffer<uint> bits,
        int bitsOffset,
        ReadWriteBuffer<uint> cum,
        int cumOffset,
        ReadWriteBuffer<uint> dataIn,
        ReadWriteBuffer<uint> dataOut,
        int n,
        int lastWord
    ) : IComputeShader
    {
        public void Execute()
        {
            for (var e = 0; e < 4; e++)
            {
                var j = ThreadIds.X * 4 + e;
                if (j >= n)
                {
                    return;
                }

                var w = j >> 5;
                var word = bits[bitsOffset + w];
                var onesBefore = cum[cumOffset + w] + Hlsl.CountBits(word & ((1U << (j & 31)) - 1U));

                var dest = 0;
                if (((word >> (j & 31)) & 1U) == 0)
                {
                    dest = j - (int)onesBefore;
                }
                else
                {
                    var zerosTotal = n - (int)cum[cumOffset + lastWord];
                    dest = zerosTotal + (int)onesBefore;
                }

                dataOut[dest] = dataIn[j];
            }
        }
    }

    /// <summary>
    /// The runtime median query (Algorithm 5 of the paper). One thread per pixel:
    /// runs the 2D quantile query over the prebuilt wavelet matrix and writes the
    /// median of the clamped (2rx+1)x(2ry+1) window into the selected channel.
    /// </summary>
    [ThreadGroupSize(8, 8, 1)]
    [GeneratedComputeShaderDescriptor]
    readonly partial struct MedianQueryShader(
        ReadWriteBuffer<Float4> destination,
        ReadWriteBuffer<uint> bits,
        ReadWriteBuffer<uint> cum,
        ReadWriteBuffer<uint> table,
        int width,
        int height,
        int n,
        int wordCount,
        int lastWord,
        int xBits,
        int slotsPerLevel,
        int valueBits,
        int radius,
        int channel
    ) : IComputeShader
    {
        /// <summary>Number of zeros in [0, p) of the bit-vector at <paramref name="slot"/>.</summary>
        int Rank0(int slot, int p)
        {
            var b = slot * wordCount + (p >> 5);
            var ones = cum[b] + Hlsl.CountBits(bits[b] & ((1U << (p & 31)) - 1U));
            return p - (int)ones;
        }

        /// <summary>Total number of zeros of the bit-vector at <paramref name="slot"/>.</summary>
        int Zeros(int slot)
        {
            return n - (int)cum[slot * wordCount + lastWord];
        }

        /// <summary>Low-frequency query restricted to the levels from <paramref name="t"/> down.</summary>
        int LowFreqTail(int slotBase, int t, int L, int R, int x)
        {
            var res = 0;
            for (; t < xBits; t++)
            {
                var slot = slotBase + t;
                var l0 = Rank0(slot, L);
                var r0 = Rank0(slot, R);
                if (((x >> (xBits - 1 - t)) & 1) == 0)
                {
                    L = l0;
                    R = r0;
                }
                else
                {
                    res += r0 - l0;
                    var z = Zeros(slot);
                    L = L - l0 + z;
                    R = R - r0 + z;
                }
            }
            return res;
        }

        /// <summary>
        /// Number of elements with x-index in [x0, x1) among positions [L, R) of
        /// the masked x-index array X_li. Equivalent to LowFreq(x1)-LowFreq(x0),
        /// but the identical high bits of x0 and x1 are traversed once (their
        /// contributions cancel and the interval states coincide there).
        /// </summary>
        int RangeFreq(int li, int L, int R, int x0, int x1)
        {
            var slotBase = li * slotsPerLevel + 1;

            // Levels above the highest differing bit of x0 and x1 (x0 < x1, so
            // the xor is non-zero) narrow one shared interval state.
            var sharedLevels = xBits - 1 - (int)Hlsl.FirstBitHigh((uint)(x0 ^ x1));
            for (var t = 0; t < sharedLevels; t++)
            {
                var slot = slotBase + t;
                var l0 = Rank0(slot, L);
                var r0 = Rank0(slot, R);
                if (((x1 >> (xBits - 1 - t)) & 1) == 0)
                {
                    L = l0;
                    R = r0;
                }
                else
                {
                    var z = Zeros(slot);
                    L = L - l0 + z;
                    R = R - r0 + z;
                }
            }

            // First differing bit: x0 has 0, x1 has 1. x1's traversal counts all
            // zeros here and descends into the ones region; x0's descends into
            // the zeros region (skipped entirely for x0 == 0: it adds nothing).
            var slotD = slotBase + sharedLevels;
            var l0d = Rank0(slotD, L);
            var r0d = Rank0(slotD, R);
            var zd = Zeros(slotD);

            var res = r0d - l0d;
            res += LowFreqTail(slotBase, sharedLevels + 1, L - l0d + zd, R - r0d + zd, x1);
            if (x0 > 0)
            {
                res -= LowFreqTail(slotBase, sharedLevels + 1, l0d, r0d, x0);
            }
            return res;
        }

        public void Execute()
        {
            var px = ThreadIds.X;
            var py = ThreadIds.Y;
            if (px >= width || py >= height)
            {
                return;
            }

            var x0 = Hlsl.Max(px - radius, 0);
            var x1 = Hlsl.Min(px + radius + 1, width);
            var y0 = Hlsl.Max(py - radius, 0);
            var y1 = Hlsl.Min(py + radius + 1, height);

            var k = (x1 - x0) * (y1 - y0) / 2; // 0-indexed median position
            var L = y0 * width;
            var R = y1 * width;
            var key = 0U;

            for (var li = 0; li < valueBits; li++)
            {
                var slotB = li * slotsPerLevel;

                // Number of window elements whose value bit `valueBits-1-li` is 0:
                // rank within [L, R) restricted to x-index in [x0, x1).
                var num = RangeFreq(li, L, R, x0, x1);

                var l0 = Rank0(slotB, L);
                var r0 = Rank0(slotB, R);
                if (k < num)
                {
                    // The answer bit is 0: descend into the zeros region.
                    L = l0;
                    R = r0;
                }
                else
                {
                    // The answer bit is 1: descend into the ones region.
                    k -= num;
                    var z = Zeros(slotB);
                    L = L - l0 + z;
                    R = R - r0 + z;
                    key |= 1U << (valueBits - 1 - li);
                }
            }

            // In rank-reduction mode the quantile result is a rank; map it back
            // to the original float key through the lookup table.
            key = table[(int)key];

            // Inverse of the order-preserving float -> uint mapping.
            var u = 0U;
            if ((key & 0x80000000U) != 0)
            {
                u = key & 0x7FFFFFFFU;
            }
            else
            {
                u = ~key;
            }
            var median = Hlsl.AsFloat(u);

            var idx = py * width + px;
            var pixel = destination[idx];
            pixel[channel] = median;
            destination[idx] = pixel;
        }
    }
}
