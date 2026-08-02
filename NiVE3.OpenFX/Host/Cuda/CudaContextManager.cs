using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Host.Cuda
{
    /// <summary>
    /// CUDA レンダリング (OFX 1.5) 用のコンテキストとストリームの管理。
    /// 最初の CUDA デバイスの「プライマリコンテキスト」を使用します
    /// (cudart を使うプラグインは同じプライマリコンテキストで動作するため、
    /// ホストが確保したデバイスメモリとストリームをそのまま共有できる)。
    /// 単一のストリームを共有するため、レンダリングは RenderLock で直列化します
    /// </summary>
    public sealed unsafe class CudaContextManager : IDisposable
    {
        static readonly Lazy<CudaContextManager?> SharedLazy = new Lazy<CudaContextManager?>(TryCreate, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// プロセス共有の CUDA コンテキスト。CUDA が使用できない環境では null
        /// </summary>
        public static CudaContextManager? Shared => SharedLazy.Value;

        /// <summary>
        /// 使用しているデバイス名 (ログ用)
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// 使用しているデバイス (CUdevice)
        /// </summary>
        public int Device { get; }

        /// <summary>
        /// デバイスのプライマリコンテキスト (CUcontext)
        /// </summary>
        public nint Context { get; private set; }

        /// <summary>
        /// プラグインへ渡すストリーム (CUstream。cudaStream_t と互換)
        /// </summary>
        public nint Stream { get; private set; }

        /// <summary>
        /// 単一のストリームを共有するため、レンダリング全体を直列化するためのロック
        /// </summary>
        public object RenderLock { get; } = new object();

        CudaContextManager(int device, nint context, nint stream, string deviceName)
        {
            Device = device;
            Context = context;
            Stream = stream;
            DeviceName = deviceName;
        }

        static CudaContextManager? TryCreate()
        {
            try
            {
                var error = CudaNative.cuInit(0);
                if (error != CudaNative.CUDA_SUCCESS)
                {
                    OfxLog.Warn($"CUDA の初期化に失敗したため、CUDA レンダリングは無効になります ({CudaNative.GetErrorName(error)})");
                    return null;
                }

                int deviceCount;
                if (CudaNative.cuDeviceGetCount(&deviceCount) != CudaNative.CUDA_SUCCESS || deviceCount < 1)
                {
                    OfxLog.Warn("CUDA デバイスが見つからないため、CUDA レンダリングは無効になります");
                    return null;
                }

                int device;
                if (CudaNative.cuDeviceGet(&device, 0) != CudaNative.CUDA_SUCCESS)
                {
                    OfxLog.Warn("CUDA デバイスの取得に失敗したため、CUDA レンダリングは無効になります");
                    return null;
                }

                nint context;
                error = CudaNative.cuDevicePrimaryCtxRetain(&context, device);
                if (error != CudaNative.CUDA_SUCCESS || context == 0)
                {
                    OfxLog.Warn($"CUDA プライマリコンテキストの取得に失敗しました ({CudaNative.GetErrorName(error)})");
                    return null;
                }

                CudaNative.cuCtxSetCurrent(context);

                nint stream;
                error = CudaNative.cuStreamCreate(&stream, 0);
                if (error != CudaNative.CUDA_SUCCESS || stream == 0)
                {
                    OfxLog.Warn($"CUDA ストリームの作成に失敗しました ({CudaNative.GetErrorName(error)})");
                    CudaNative.cuDevicePrimaryCtxRelease(device);
                    return null;
                }

                var deviceName = CudaNative.GetDeviceName(device);
                OfxLog.Info($"CUDA 初期化完了: {deviceName}");
                return new CudaContextManager(device, context, stream, deviceName);
            }
            catch (DllNotFoundException)
            {
                OfxLog.Warn("nvcuda.dll が見つからないため、CUDA レンダリングは無効になります");
                return null;
            }
            catch (EntryPointNotFoundException ex)
            {
                OfxLog.Warn($"CUDA ドライバが古いため、CUDA レンダリングは無効になります ({ex.Message})");
                return null;
            }
            catch (Exception ex)
            {
                OfxLog.Warn($"CUDA の初期化中にエラーが発生しました: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 呼び出しスレッドのカレントコンテキストをプライマリコンテキストに設定します。
        /// CUDA 操作の前に RenderLock 下で呼び出してください (コンテキストはスレッドごとの状態のため)
        /// </summary>
        public void MakeCurrent()
        {
            CudaNative.cuCtxSetCurrent(Context);
        }

        public void Dispose()
        {
            if (Stream != 0)
            {
                CudaNative.cuStreamDestroy(Stream);
                Stream = 0;
            }
            if (Context != 0)
            {
                CudaNative.cuDevicePrimaryCtxRelease(Device);
                Context = 0;
            }
        }
    }
}
