using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// クリップの画像をホストへ要求するためのインターフェース
    /// NiVE3 統合時は LayerModel からの画像取得を実装する
    /// </summary>
    public interface IOfxFrameProvider
    {
        /// <summary>
        /// 指定したクリップ・時間のソース画像を取得します
        /// </summary>
        /// <param name="clipName">クリップ名</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <returns>BGRA (上から下) の画像データ。取得できない場合は null</returns>
        (Vector4[] Pixels, int Width, int Height)? GetSourceFrame(string clipName, double time);

        /// <summary>
        /// 指定したクリップ・時間のソース画像のサイズを取得します
        /// </summary>
        /// <param name="clipName">クリップ名</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <returns>画像のサイズ。取得できない場合は null</returns>
        (int Width, int Height)? GetSourceBounds(string clipName, double time);
    }

    /// <summary>
    /// NiVE3 の画像 (BGRA float・上から下) と OFX の画像 (RGBA float・下から上) の相互変換
    /// </summary>
    public static unsafe class ImageBridge
    {
        /// <summary>
        /// BGRA (上から下) の画像データを OFX の RGBA (下から上) バッファへ変換します
        /// </summary>
        /// <param name="source">変換元の画像データ</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="dest">変換先のバッファ (width * height * 4 float)</param>
        public static void ToOfx(ReadOnlySpan<Vector4> source, int width, int height, float* dest)
        {
            fixed (Vector4* sourcePtr = source)
            {
                var src = sourcePtr;
                Parallel.For(0, height, y =>
                {
                    var srcRow = src + (long)(height - 1 - y) * width;
                    var destRow = dest + (long)y * width * 4;
                    for (var x = 0; x < width; x++)
                    {
                        var p = srcRow[x];
                        destRow[x * 4 + 0] = p.Z;
                        destRow[x * 4 + 1] = p.Y;
                        destRow[x * 4 + 2] = p.X;
                        destRow[x * 4 + 3] = p.W;
                    }
                });
            }
        }

        /// <summary>
        /// OFX の RGBA (下から上) バッファを BGRA (上から下) の画像データへ変換します
        /// </summary>
        /// <param name="source">変換元のバッファ (width * height * 4 float)</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="dest">変換先の画像データ</param>
        public static void FromOfx(float* source, int width, int height, Span<Vector4> dest)
        {
            fixed (Vector4* destPtr = dest)
            {
                var dst = destPtr;
                Parallel.For(0, height, y =>
                {
                    var srcRow = source + (long)(height - 1 - y) * width * 4;
                    var destRow = dst + (long)y * width;
                    for (var x = 0; x < width; x++)
                    {
                        destRow[x] = new Vector4(
                            srcRow[x * 4 + 2],
                            srcRow[x * 4 + 1],
                            srcRow[x * 4 + 0],
                            srcRow[x * 4 + 3]);
                    }
                });
            }
        }
    }

    /// <summary>
    /// OFX の画像。ネイティブの RGBA float バッファとプロパティセットを保持します
    /// OFX 上では画像のハンドル = プロパティセットのハンドルです
    /// </summary>
    public sealed unsafe class OfxImage : IDisposable
    {
        static readonly ConcurrentDictionary<nint, OfxImage> Registry = new ConcurrentDictionary<nint, OfxImage>();

        static long NextId;

        public int Width { get; }

        public int Height { get; }

        /// <summary>
        /// ホストが所有する画像 (Output) かどうか。true の場合 clipReleaseImage では解放されません
        /// </summary>
        public bool HostOwned { get; }

        public PropertySet Properties { get; }

        /// <summary>
        /// OFX 上の画像ハンドル (プロパティセットのハンドル)
        /// </summary>
        public nint Handle => Properties.Handle;

        public float* Data { get; private set; }

        public bool Disposed { get; private set; }

        OfxImage(int width, int height, bool hostOwned, string name)
        {
            Width = width;
            Height = height;
            HostOwned = hostOwned;

            Data = (float*)NativeMemory.AllocZeroed((nuint)((long)width * height * 4 * sizeof(float)));
            Properties = new PropertySet($"Image:{name}");
            Properties.SetAll(OfxNames.PropType, OfxNames.TypeImage);
            Properties.SetAll(OfxNames.ImagePropData, (nint)Data);
            Properties.SetAll(OfxNames.ImagePropBounds, 0, 0, width, height);
            Properties.SetAll(OfxNames.ImagePropRegionOfDefinition, 0, 0, width, height);
            Properties.SetAll(OfxNames.ImagePropRowBytes, width * 4 * sizeof(float));
            Properties.SetAll(OfxNames.ImagePropField, OfxNames.ImageFieldNone);
            Properties.SetAll(OfxNames.ImageEffectPropComponents, OfxNames.ComponentRGBA);
            Properties.SetAll(OfxNames.ImageEffectPropPixelDepth, OfxNames.BitDepthFloat);
            Properties.SetAll(OfxNames.ImageEffectPropPreMultiplication, OfxNames.ImageUnPreMultiplied);
            Properties.SetAll(OfxNames.ImageEffectPropRenderScale, 1.0, 1.0);
            Properties.SetAll(OfxNames.ImagePropPixelAspectRatio, 1.0);
            Properties.SetAll(OfxNames.ImagePropUniqueIdentifier, $"{name}#{Interlocked.Increment(ref NextId)}");

            Registry[Handle] = this;
        }

        /// <summary>
        /// BGRA (上から下) の画像データから OFX 画像を作成します
        /// </summary>
        /// <param name="pixels">画像データ</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="hostOwned">ホストが所有する画像かどうか</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成された OFX 画像</returns>
        public static OfxImage FromBgraTopDown(ReadOnlySpan<Vector4> pixels, int width, int height, bool hostOwned, string name)
        {
            var image = new OfxImage(width, height, hostOwned, name);
            ImageBridge.ToOfx(pixels, width, height, image.Data);
            return image;
        }

        /// <summary>
        /// 空 (透明) の OFX 画像を作成します
        /// </summary>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="hostOwned">ホストが所有する画像かどうか</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成された OFX 画像</returns>
        public static OfxImage CreateEmpty(int width, int height, bool hostOwned, string name)
        {
            return new OfxImage(width, height, hostOwned, name);
        }

        /// <summary>
        /// 画像ハンドルから OfxImage を取得します
        /// </summary>
        /// <param name="handle">画像ハンドル</param>
        /// <returns>対応する OfxImage。存在しない場合は null</returns>
        public static OfxImage? Resolve(nint handle)
        {
            return Registry.TryGetValue(handle, out var image) ? image : null;
        }

        /// <summary>
        /// 画像データを BGRA (上から下) に変換して取得します
        /// </summary>
        /// <returns>変換された画像データ</returns>
        public Vector4[] ToBgraTopDown()
        {
            var result = new Vector4[Width * Height];
            ImageBridge.FromOfx(Data, Width, Height, result);
            return result;
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                Registry.TryRemove(Handle, out _);
                Properties.Dispose();
                NativeMemory.Free(Data);
                Data = null;
            }
        }
    }
}
