using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Host.CL
{
    /// <summary>
    /// OpenCL Buffers レンダリング (OFX 1.5) 用のコンテキストとコマンドキューの管理
    /// GPU デバイスを 1 つ選択し、プロセス共有のコンテキスト/キューを保持します
    /// OpenCL はスレッドアフィニティを持たないため専用スレッドは不要ですが、
    /// 単一のキューを共有するためレンダリングは RenderLock で直列化します
    /// </summary>
    public sealed unsafe class ClContextManager : IDisposable
    {
        static readonly Lazy<ClContextManager?> SharedLazy = new Lazy<ClContextManager?>(TryCreate, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// プロセス共有の OpenCL コンテキスト。OpenCL が使用できない環境では null
        /// </summary>
        public static ClContextManager? Shared => SharedLazy.Value;

        /// <summary>
        /// 使用しているデバイス名 (ログ用)
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// OpenCL コンテキスト (cl_context)
        /// </summary>
        public nint Context { get; private set; }

        /// <summary>
        /// プラグインへ渡すコマンドキュー (cl_command_queue)
        /// </summary>
        public nint Queue { get; private set; }

        /// <summary>
        /// 単一のコマンドキューを共有するため、レンダリング全体を直列化するためのロック
        /// </summary>
        public object RenderLock { get; } = new object();

        ClContextManager(nint context, nint queue, string deviceName)
        {
            Context = context;
            Queue = queue;
            DeviceName = deviceName;
        }

        static ClContextManager? TryCreate()
        {
            try
            {
                uint platformCount;
                if (ClNative.clGetPlatformIDs(0, null, &platformCount) != ClNative.CL_SUCCESS || platformCount == 0)
                {
                    OfxLog.Warn("OpenCL プラットフォームが見つからないため、OpenCL レンダリングは無効になります");
                    return null;
                }

                var platforms = stackalloc nint[(int)platformCount];
                ClNative.clGetPlatformIDs(platformCount, platforms, null);

                // 最初に見つかった GPU デバイスを使用する
                for (var i = 0; i < platformCount; i++)
                {
                    uint deviceCount;
                    if (ClNative.clGetDeviceIDs(platforms[i], ClNative.CL_DEVICE_TYPE_GPU, 0, null, &deviceCount) != ClNative.CL_SUCCESS || deviceCount == 0)
                    {
                        continue;
                    }
                    nint device;
                    if (ClNative.clGetDeviceIDs(platforms[i], ClNative.CL_DEVICE_TYPE_GPU, 1, &device, null) != ClNative.CL_SUCCESS)
                    {
                        continue;
                    }

                    var properties = stackalloc nint[3] { ClNative.CL_CONTEXT_PLATFORM, platforms[i], 0 };
                    int error;
                    var context = ClNative.clCreateContext(properties, 1, &device, 0, 0, &error);
                    if (context == 0 || error != ClNative.CL_SUCCESS)
                    {
                        OfxLog.Warn($"OpenCL コンテキストの作成に失敗しました (error: {error})");
                        continue;
                    }

                    var queue = ClNative.clCreateCommandQueue(context, device, 0, &error);
                    if (queue == 0 || error != ClNative.CL_SUCCESS)
                    {
                        OfxLog.Warn($"OpenCL コマンドキューの作成に失敗しました (error: {error})");
                        ClNative.clReleaseContext(context);
                        continue;
                    }

                    var deviceName = ClNative.GetDeviceName(device);
                    OfxLog.Info($"OpenCL 初期化完了: {ClNative.GetPlatformName(platforms[i])} / {deviceName}");
                    return new ClContextManager(context, queue, deviceName);
                }

                OfxLog.Warn("OpenCL の GPU デバイスが見つからないため、OpenCL レンダリングは無効になります");
                return null;
            }
            catch (DllNotFoundException)
            {
                OfxLog.Warn("OpenCL.dll が見つからないため、OpenCL レンダリングは無効になります");
                return null;
            }
            catch (Exception ex)
            {
                OfxLog.Warn($"OpenCL の初期化中にエラーが発生しました: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (Queue != 0)
            {
                ClNative.clReleaseCommandQueue(Queue);
                Queue = 0;
            }
            if (Context != 0)
            {
                ClNative.clReleaseContext(Context);
                Context = 0;
            }
        }
    }
}
