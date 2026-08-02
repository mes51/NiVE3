using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Host.Cuda
{
    /// <summary>
    /// CUDA レンダリング (OFX 1.5) に必要な範囲の CUDA Driver API
    /// nvcuda.dll は NVIDIA ドライバと共にインストールされるため、
    /// 存在しない環境では CudaContextManager が初期化に失敗し CUDA レンダリングが無効になります。
    /// cudart (Runtime API) ではなく Driver API を使う理由: DLL 名がバージョン非依存で、
    /// デバイスのプライマリコンテキストを介して cudart を使うプラグインとメモリ/ストリームを共有できるため
    /// </summary>
    public static unsafe partial class CudaNative
    {
        const string Library = "nvcuda.dll";

        public const int CUDA_SUCCESS = 0;

        // 64bit ポインタ対応版は "_v2" サフィックスのエクスポート名を持つ (無印はレガシー 32bit 版) ことに注意

        [LibraryImport(Library)]
        public static partial int cuInit(uint flags);

        [LibraryImport(Library)]
        public static partial int cuDriverGetVersion(int* driverVersion);

        [LibraryImport(Library)]
        public static partial int cuDeviceGetCount(int* count);

        [LibraryImport(Library)]
        public static partial int cuDeviceGet(int* device, int ordinal);

        [LibraryImport(Library)]
        public static partial int cuDeviceGetName(byte* name, int length, int device);

        [LibraryImport(Library)]
        public static partial int cuDevicePrimaryCtxRetain(nint* context, int device);

        [LibraryImport(Library, EntryPoint = "cuDevicePrimaryCtxRelease_v2")]
        public static partial int cuDevicePrimaryCtxRelease(int device);

        [LibraryImport(Library)]
        public static partial int cuCtxSetCurrent(nint context);

        [LibraryImport(Library)]
        public static partial int cuStreamCreate(nint* stream, uint flags);

        [LibraryImport(Library)]
        public static partial int cuStreamSynchronize(nint stream);

        [LibraryImport(Library, EntryPoint = "cuStreamDestroy_v2")]
        public static partial int cuStreamDestroy(nint stream);

        [LibraryImport(Library, EntryPoint = "cuMemAlloc_v2")]
        public static partial int cuMemAlloc(nint* devicePtr, nuint byteSize);

        [LibraryImport(Library, EntryPoint = "cuMemFree_v2")]
        public static partial int cuMemFree(nint devicePtr);

        [LibraryImport(Library, EntryPoint = "cuMemcpyHtoD_v2")]
        public static partial int cuMemcpyHtoD(nint dstDevice, void* srcHost, nuint byteSize);

        [LibraryImport(Library, EntryPoint = "cuMemcpyDtoH_v2")]
        public static partial int cuMemcpyDtoH(void* dstHost, nint srcDevice, nuint byteSize);

        [LibraryImport(Library, EntryPoint = "cuMemcpyDtoD_v2")]
        public static partial int cuMemcpyDtoD(nint dstDevice, nint srcDevice, nuint byteSize);

        [LibraryImport(Library, EntryPoint = "cuMemsetD8_v2")]
        public static partial int cuMemsetD8(nint devicePtr, byte value, nuint byteCount);

        [LibraryImport(Library)]
        public static partial int cuGetErrorName(int error, byte** name);

        /// <summary>
        /// デバイス名を取得します
        /// </summary>
        public static string GetDeviceName(int device)
        {
            var buffer = stackalloc byte[256];
            if (cuDeviceGetName(buffer, 256, device) != CUDA_SUCCESS)
            {
                return "";
            }
            return Marshal.PtrToStringUTF8((nint)buffer) ?? "";
        }

        /// <summary>
        /// エラーコードの名前を取得します (ログ用)
        /// </summary>
        public static string GetErrorName(int error)
        {
            byte* name;
            if (cuGetErrorName(error, &name) != CUDA_SUCCESS || name == null)
            {
                return error.ToString();
            }
            return Marshal.PtrToStringUTF8((nint)name) ?? error.ToString();
        }
    }
}
