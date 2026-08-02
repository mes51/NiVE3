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

namespace NiVE3.OpenFX.Host.Cuda
{
    /// <summary>
    /// CUDA レンダリング (OFX 1.5) でプラグインへ渡す画像
    /// kOfxImagePropData に CUDA デバイスポインタを設定します。レイアウトは CPU 画像と同じ
    /// (RGBA float・下から上・RowBytes = width * 16) です
    /// </summary>
    public sealed unsafe class OfxCudaImage : IDisposable
    {
        static readonly ConcurrentDictionary<nint, OfxCudaImage> Registry = new ConcurrentDictionary<nint, OfxCudaImage>();

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
        /// 画像データを保持する CUDA デバイスポインタ (CUdeviceptr)
        /// </summary>
        public nint DevicePointer { get; private set; }

        public bool Disposed { get; private set; }

        OfxCudaImage(nint devicePointer, int width, int height, bool hostOwned, string name)
        {
            DevicePointer = devicePointer;
            Width = width;
            Height = height;
            HostOwned = hostOwned;

            Properties = new PropertySet($"CudaImage:{name}");
            Properties.SetAll(OfxNames.PropType, OfxNames.TypeImage);
            Properties.SetAll(OfxNames.ImagePropData, devicePointer);
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
        /// BGRA (上から下) の画像データから CUDA デバイスメモリの画像を作成します
        /// </summary>
        /// <param name="cuda">CUDA コンテキスト</param>
        /// <param name="pixels">画像データ</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="hostOwned">ホストが所有する画像かどうか</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成された画像。デバイスメモリの確保に失敗した場合は null</returns>
        public static OfxCudaImage? FromBgraTopDown(CudaContextManager cuda, ReadOnlySpan<Vector4> pixels, int width, int height, bool hostOwned, string name)
        {
            var byteSize = (nuint)((long)width * height * 4 * sizeof(float));
            var staging = (float*)NativeMemory.Alloc(byteSize);
            try
            {
                ImageBridge.ToOfx(pixels, width, height, staging);

                nint devicePointer;
                var error = CudaNative.cuMemAlloc(&devicePointer, byteSize);
                if (error != CudaNative.CUDA_SUCCESS || devicePointer == 0)
                {
                    OfxLog.Warn($"CUDA デバイスメモリの確保に失敗しました ({CudaNative.GetErrorName(error)})");
                    return null;
                }

                error = CudaNative.cuMemcpyHtoD(devicePointer, staging, byteSize);
                if (error != CudaNative.CUDA_SUCCESS)
                {
                    OfxLog.Warn($"CUDA デバイスメモリへの転送に失敗しました ({CudaNative.GetErrorName(error)})");
                    CudaNative.cuMemFree(devicePointer);
                    return null;
                }
                return new OfxCudaImage(devicePointer, width, height, hostOwned, name);
            }
            finally
            {
                NativeMemory.Free(staging);
            }
        }

        /// <summary>
        /// 空 (透明) の CUDA デバイスメモリの画像を作成します
        /// </summary>
        /// <param name="cuda">CUDA コンテキスト</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="hostOwned">ホストが所有する画像かどうか</param>
        /// <param name="name">識別用の名前</param>
        /// <returns>作成された画像。デバイスメモリの確保に失敗した場合は null</returns>
        public static OfxCudaImage? CreateEmpty(CudaContextManager cuda, int width, int height, bool hostOwned, string name)
        {
            var byteSize = (nuint)((long)width * height * 4 * sizeof(float));

            nint devicePointer;
            var error = CudaNative.cuMemAlloc(&devicePointer, byteSize);
            if (error != CudaNative.CUDA_SUCCESS || devicePointer == 0)
            {
                OfxLog.Warn($"CUDA デバイスメモリの確保に失敗しました ({CudaNative.GetErrorName(error)})");
                return null;
            }

            error = CudaNative.cuMemsetD8(devicePointer, 0, byteSize);
            if (error != CudaNative.CUDA_SUCCESS)
            {
                OfxLog.Warn($"CUDA デバイスメモリのクリアに失敗しました ({CudaNative.GetErrorName(error)})");
                CudaNative.cuMemFree(devicePointer);
                return null;
            }
            return new OfxCudaImage(devicePointer, width, height, hostOwned, name);
        }

        /// <summary>
        /// 画像ハンドルから OfxCudaImage を取得します
        /// </summary>
        /// <param name="handle">画像ハンドル</param>
        /// <returns>対応する OfxCudaImage。存在しない場合は null</returns>
        public static OfxCudaImage? Resolve(nint handle)
        {
            return Registry.TryGetValue(handle, out var image) ? image : null;
        }

        /// <summary>
        /// デバイスメモリの内容を BGRA (上から下) に変換して取得します (ブロッキング読み戻し)
        /// </summary>
        /// <param name="cuda">CUDA コンテキスト</param>
        /// <returns>変換された画像データ。読み戻しに失敗した場合は null</returns>
        public Vector4[]? ToBgraTopDown(CudaContextManager cuda)
        {
            var byteSize = (nuint)((long)Width * Height * 4 * sizeof(float));
            var staging = (float*)NativeMemory.Alloc(byteSize);
            try
            {
                var error = CudaNative.cuMemcpyDtoH(staging, DevicePointer, byteSize);
                if (error != CudaNative.CUDA_SUCCESS)
                {
                    OfxLog.Warn($"CUDA デバイスメモリの読み戻しに失敗しました ({CudaNative.GetErrorName(error)})");
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
                if (DevicePointer != 0)
                {
                    CudaNative.cuMemFree(DevicePointer);
                    DevicePointer = 0;
                }
                Properties.Dispose();
            }
        }
    }
}
