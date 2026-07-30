using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Host.GL
{
    /// <summary>
    /// OpenGL レンダリング用のオフスクリーンコンテキストの管理
    /// 全ての GL 呼び出しは専用スレッドに集約されます (OFX の GL レンダリングアクションもこのスレッド上で呼び出します)
    /// </summary>
    public sealed unsafe class GlContextManager : IDisposable
    {
        static readonly Lazy<GlContextManager?> SharedLazy = new Lazy<GlContextManager?>(TryCreate, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// プロセス共有の GL コンテキスト。GL が使用できない環境では null
        /// </summary>
        public static GlContextManager? Shared => SharedLazy.Value;

        public string GlVersion { get; private set; } = "";

        public string GlRenderer { get; private set; } = "";

        /// <summary>
        /// 現在のスレッドが GL スレッドかどうか
        /// </summary>
        public bool IsOnGlThread => Thread.CurrentThread == WorkerThread;

        // FBO 拡張関数 (GL スレッド上でのみ使用)
        public delegate* unmanaged<int, uint*, void> GenFramebuffers { get; private set; }

        public delegate* unmanaged<uint, uint, void> BindFramebuffer { get; private set; }

        public delegate* unmanaged<uint, uint, uint, uint, int, void> FramebufferTexture2D { get; private set; }

        public delegate* unmanaged<uint, uint> CheckFramebufferStatus { get; private set; }

        public delegate* unmanaged<int, uint*, void> DeleteFramebuffers { get; private set; }

        // 状態リセット用の拡張関数 (古い GL 環境では取得できないことがあるため null 許容)
        delegate* unmanaged<uint, void> UseProgram { get; set; }

        delegate* unmanaged<uint, void> ActiveTexture { get; set; }

        delegate* unmanaged<uint, void> BindVertexArray { get; set; }

        delegate* unmanaged<uint, uint, void> BindBuffer { get; set; }

        Thread WorkerThread { get; }

        BlockingCollection<(Action Action, TaskCompletionSource Completion)> Queue { get; } = new BlockingCollection<(Action, TaskCompletionSource)>();

        nint Window { get; set; }

        nint DeviceContext { get; set; }

        nint GlContext { get; set; }

        bool InitializeSucceeded { get; set; }

        GlContextManager()
        {
            WorkerThread = new Thread(WorkerLoop)
            {
                Name = "NiVE3.OpenFX GL Thread",
                IsBackground = true
            };
        }

        static GlContextManager? TryCreate()
        {
            var manager = new GlContextManager();
            var ready = new ManualResetEventSlim();

            manager.Queue.Add((() =>
            {
                manager.InitializeSucceeded = manager.InitializeOnWorkerThread();
                ready.Set();
            }, new TaskCompletionSource()));

            manager.WorkerThread.Start();
            ready.Wait();

            if (!manager.InitializeSucceeded)
            {
                OfxLog.Warn("OpenGL コンテキストの作成に失敗したため、OpenGL レンダリングは無効になります");
                manager.Dispose();
                return null;
            }

            OfxLog.Info($"OpenGL 初期化完了: {manager.GlVersion} / {manager.GlRenderer}");
            return manager;
        }

        bool InitializeOnWorkerThread()
        {
            try
            {
                Window = GlNative.CreateWindowExW(0, "STATIC", "NiVE3.OpenFX.GL", 0, 0, 0, 1, 1, 0, 0, 0, 0);
                if (Window == 0)
                {
                    return false;
                }
                DeviceContext = GlNative.GetDC(Window);
                if (DeviceContext == 0)
                {
                    return false;
                }

                var pfd = new GlNative.PIXELFORMATDESCRIPTOR
                {
                    nSize = (ushort)sizeof(GlNative.PIXELFORMATDESCRIPTOR),
                    nVersion = 1,
                    dwFlags = GlNative.PFD_DRAW_TO_WINDOW | GlNative.PFD_SUPPORT_OPENGL,
                    iPixelType = GlNative.PFD_TYPE_RGBA,
                    cColorBits = 32,
                    cAlphaBits = 8,
                    cDepthBits = 24,
                    cStencilBits = 8
                };
                var format = GlNative.ChoosePixelFormat(DeviceContext, in pfd);
                if (format == 0 || !GlNative.SetPixelFormat(DeviceContext, format, in pfd))
                {
                    return false;
                }

                GlContext = GlNative.wglCreateContext(DeviceContext);
                if (GlContext == 0 || !GlNative.wglMakeCurrent(DeviceContext, GlContext))
                {
                    return false;
                }

                GlVersion = Marshal.PtrToStringUTF8((nint)GlNative.glGetString(GlNative.GL_VERSION)) ?? "";
                GlRenderer = Marshal.PtrToStringUTF8((nint)GlNative.glGetString(GlNative.GL_RENDERER)) ?? "";

                GenFramebuffers = (delegate* unmanaged<int, uint*, void>)GlNative.GetExtensionFunction("glGenFramebuffers");
                BindFramebuffer = (delegate* unmanaged<uint, uint, void>)GlNative.GetExtensionFunction("glBindFramebuffer");
                FramebufferTexture2D = (delegate* unmanaged<uint, uint, uint, uint, int, void>)GlNative.GetExtensionFunction("glFramebufferTexture2D");
                CheckFramebufferStatus = (delegate* unmanaged<uint, uint>)GlNative.GetExtensionFunction("glCheckFramebufferStatus");
                DeleteFramebuffers = (delegate* unmanaged<int, uint*, void>)GlNative.GetExtensionFunction("glDeleteFramebuffers");

                UseProgram = (delegate* unmanaged<uint, void>)GlNative.GetExtensionFunction("glUseProgram");
                ActiveTexture = (delegate* unmanaged<uint, void>)GlNative.GetExtensionFunction("glActiveTexture");
                BindVertexArray = (delegate* unmanaged<uint, void>)GlNative.GetExtensionFunction("glBindVertexArray");
                BindBuffer = (delegate* unmanaged<uint, uint, void>)GlNative.GetExtensionFunction("glBindBuffer");

                return GenFramebuffers != null && BindFramebuffer != null && FramebufferTexture2D != null && CheckFramebufferStatus != null && DeleteFramebuffers != null;
            }
            catch (Exception ex)
            {
                OfxLog.Warn($"OpenGL 初期化中にエラーが発生しました: {ex.Message}");
                return false;
            }
        }

        void WorkerLoop()
        {
            foreach (var (action, completion) in Queue.GetConsumingEnumerable())
            {
                try
                {
                    action();
                    completion.TrySetResult();
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            }

            // 終了処理 (GL スレッド上で行う)
            if (GlContext != 0)
            {
                GlNative.wglMakeCurrent(0, 0);
                GlNative.wglDeleteContext(GlContext);
                GlContext = 0;
            }
            if (DeviceContext != 0)
            {
                GlNative.ReleaseDC(Window, DeviceContext);
                DeviceContext = 0;
            }
            if (Window != 0)
            {
                GlNative.DestroyWindow(Window);
                Window = 0;
            }
        }

        /// <summary>
        /// 直前に実行されたプラグインが残した GL 状態を既定相当へリセットします (GL スレッド上で呼び出してください)
        /// プラグインには状態を復元する義務がないため、各プラグインの実行前に呼び出します
        /// (GLSL プログラムが残ったままだと、固定機能パイプラインで描画するプラグインが正しく描画できない等)
        /// </summary>
        public void ResetRenderState()
        {
            if (UseProgram != null)
            {
                UseProgram(0);
            }
            if (BindVertexArray != null)
            {
                BindVertexArray(0);
            }
            if (BindBuffer != null)
            {
                BindBuffer(GlNative.GL_ARRAY_BUFFER, 0);
                BindBuffer(GlNative.GL_ELEMENT_ARRAY_BUFFER, 0);
            }
            if (ActiveTexture != null)
            {
                ActiveTexture(GlNative.GL_TEXTURE0);
            }
            GlNative.glBindTexture(GlNative.GL_TEXTURE_2D, 0);
            GlNative.glDisable(GlNative.GL_TEXTURE_2D);
            GlNative.glDisable(GlNative.GL_BLEND);
            GlNative.glDisable(GlNative.GL_DEPTH_TEST);
            GlNative.glDisable(GlNative.GL_SCISSOR_TEST);
            GlNative.glDisable(GlNative.GL_STENCIL_TEST);
            GlNative.glDisable(GlNative.GL_CULL_FACE);
            GlNative.glDisable(GlNative.GL_ALPHA_TEST);
            GlNative.glDisable(GlNative.GL_LIGHTING);
            GlNative.glColor4f(1.0F, 1.0F, 1.0F, 1.0F);
        }

        /// <summary>
        /// テクスチャアップロード (glTexImage2D) 前にピクセル転送状態をリセットします (GL スレッド上で呼び出してください)
        /// プラグインが GL_PIXEL_UNPACK_BUFFER をバインドしたままだと、ポインタ渡しのアップロードが壊れるため
        /// </summary>
        public void ResetUnpackState()
        {
            if (BindBuffer != null)
            {
                BindBuffer(GlNative.GL_PIXEL_UNPACK_BUFFER, 0);
            }
            GlNative.glPixelStorei(GlNative.GL_UNPACK_ALIGNMENT, 4);
            GlNative.glPixelStorei(GlNative.GL_UNPACK_ROW_LENGTH, 0);
            GlNative.glPixelStorei(GlNative.GL_UNPACK_SKIP_ROWS, 0);
            GlNative.glPixelStorei(GlNative.GL_UNPACK_SKIP_PIXELS, 0);
        }

        /// <summary>
        /// 読み戻し (glReadPixels) 前にピクセル転送状態をリセットします (GL スレッド上で呼び出してください)
        /// プラグインが GL_PIXEL_PACK_BUFFER をバインドしたままだと、ポインタ渡しの読み戻しが壊れるため
        /// </summary>
        public void ResetPackState()
        {
            if (BindBuffer != null)
            {
                BindBuffer(GlNative.GL_PIXEL_PACK_BUFFER, 0);
            }
            GlNative.glPixelStorei(GlNative.GL_PACK_ALIGNMENT, 4);
            GlNative.glPixelStorei(GlNative.GL_PACK_ROW_LENGTH, 0);
            GlNative.glPixelStorei(GlNative.GL_PACK_SKIP_ROWS, 0);
            GlNative.glPixelStorei(GlNative.GL_PACK_SKIP_PIXELS, 0);
        }

        /// <summary>
        /// GL スレッド上で処理を実行し、完了まで待機します
        /// </summary>
        /// <param name="action">実行する処理</param>
        public void Invoke(Action action)
        {
            if (IsOnGlThread)
            {
                action();
                return;
            }

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Queue.Add((action, completion));
            completion.Task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// GL スレッド上で処理を実行し、結果を取得します
        /// </summary>
        /// <typeparam name="T">結果の型</typeparam>
        /// <param name="func">実行する処理</param>
        /// <returns>処理の結果</returns>
        public T Invoke<T>(Func<T> func)
        {
            var result = default(T)!;
            Invoke(() => { result = func(); });
            return result;
        }

        public void Dispose()
        {
            Queue.CompleteAdding();
        }
    }
}
