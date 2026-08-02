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

namespace NiVE3.OpenFX.Host.CL
{
    /// <summary>
    /// OpenCL Buffers レンダリング (OFX 1.5) でプラグインへ渡す画像
    /// kOfxImagePropData に cl_mem (バッファ) を設定します。レイアウトは CPU 画像と同じ
    /// (RGBA float・下から上・RowBytes = width * 16) です
    /// </summary>
    public sealed unsafe class OfxClImage : IDisposable
    {
        static readonly ConcurrentDictionary<nint, OfxClImage> Registry = new ConcurrentDictionary<nint, OfxClImage>();

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

        /// <summary>
        /// 画像データを保持する OpenCL バッファ (cl_mem)
        /// </summary>
        public nint Buffer { get; private set; }

        public bool Disposed { get; private set; }

        OfxClImage(nint buffer, int width, int height, bool hostOwned, string name)
        {
            Buffer = buffer;
            Width = width;
            Height = height;
            HostOwned = hostOwned;

            Properties = new PropertySet($"ClImage:{name}");
            Properties.SetAll(OfxNames.PropType, OfxNames.TypeImage);
            Properties.SetAll(OfxNames.ImagePropData, buffer);
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
        /// BGRA (上から下) の画像データから OpenCL バッファの画像を作成します
        /// </summary>
        /// <param name="cl">OpenCL コンテキスト</param>
        /// <param name="pixels">画像データ</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="hostOwned">ホストが所有する画像かどうか</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成された画像。バッファの作成に失敗した場合は null</returns>
        public static OfxClImage? FromBgraTopDown(ClContextManager cl, ReadOnlySpan<Vector4> pixels, int width, int height, bool hostOwned, string name)
        {
            var byteSize = (nuint)((long)width * height * 4 * sizeof(float));
            var staging = (float*)NativeMemory.Alloc(byteSize);
            try
            {
                ImageBridge.ToOfx(pixels, width, height, staging);
                int error;
                var buffer = ClNative.clCreateBuffer(cl.Context, ClNative.CL_MEM_READ_WRITE | ClNative.CL_MEM_COPY_HOST_PTR, byteSize, staging, &error);
                if (buffer == 0 || error != ClNative.CL_SUCCESS)
                {
                    OfxLog.Warn($"OpenCL バッファの作成に失敗しました (error: {error})");
                    return null;
                }
                return new OfxClImage(buffer, width, height, hostOwned, name);
            }
            finally
            {
                NativeMemory.Free(staging);
            }
        }

        /// <summary>
        /// 空 (透明) の OpenCL バッファの画像を作成します
        /// </summary>
        /// <param name="cl">OpenCL コンテキスト</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="hostOwned">ホストが所有する画像かどうか</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成された画像。バッファの作成に失敗した場合は null</returns>
        public static OfxClImage? CreateEmpty(ClContextManager cl, int width, int height, bool hostOwned, string name)
        {
            var byteSize = (nuint)((long)width * height * 4 * sizeof(float));
            var staging = NativeMemory.AllocZeroed(byteSize);
            try
            {
                int error;
                var buffer = ClNative.clCreateBuffer(cl.Context, ClNative.CL_MEM_READ_WRITE | ClNative.CL_MEM_COPY_HOST_PTR, byteSize, staging, &error);
                if (buffer == 0 || error != ClNative.CL_SUCCESS)
                {
                    OfxLog.Warn($"OpenCL バッファの作成に失敗しました (error: {error})");
                    return null;
                }
                return new OfxClImage(buffer, width, height, hostOwned, name);
            }
            finally
            {
                NativeMemory.Free(staging);
            }
        }

        /// <summary>
        /// 画像ハンドルから OfxClImage を取得します
        /// </summary>
        /// <param name="handle">画像ハンドル</param>
        /// <returns>対応する OfxClImage。存在しない場合は null</returns>
        public static OfxClImage? Resolve(nint handle)
        {
            return Registry.TryGetValue(handle, out var image) ? image : null;
        }

        /// <summary>
        /// バッファの内容を BGRA (上から下) に変換して取得します (ブロッキング読み戻し)
        /// </summary>
        /// <param name="cl">OpenCL コンテキスト</param>
        /// <returns>変換された画像データ。読み戻しに失敗した場合は null</returns>
        public Vector4[]? ToBgraTopDown(ClContextManager cl)
        {
            var byteSize = (nuint)((long)Width * Height * 4 * sizeof(float));
            var staging = (float*)NativeMemory.Alloc(byteSize);
            try
            {
                var error = ClNative.clEnqueueReadBuffer(cl.Queue, Buffer, 1, 0, byteSize, staging, 0, null, null);
                if (error != ClNative.CL_SUCCESS)
                {
                    OfxLog.Warn($"OpenCL バッファの読み戻しに失敗しました (error: {error})");
                    return null;
                }
                var result = new Vector4[Width * Height];
                ImageBridge.FromOfx(staging, Width, Height, result);
                return result;
            }
            finally
            {
                NativeMemory.Free(staging);
            }
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                Registry.TryRemove(Handle, out _);
                if (Buffer != 0)
                {
                    ClNative.clReleaseMemObject(Buffer);
                    Buffer = 0;
                }
                Properties.Dispose();
            }
        }
    }
}
