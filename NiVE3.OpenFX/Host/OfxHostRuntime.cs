using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiVE3.OpenFX.Host.GL;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// OFX ホストの本体。ホストプロパティの構築とプラグインのライフサイクル管理を行います
    /// </summary>
    public sealed unsafe class OfxHostRuntime : IDisposable
    {
        /// <summary>
        /// ホストのプロパティセット
        /// </summary>
        public PropertySet HostProperties { get; }

        OfxHostNative* HostStruct { get; }

        public OfxHostRuntime()
        {
            HostProperties = BuildHostProperties();
            // GL コンテキストが作成できない環境では OpenGL レンダリング非対応を宣言する
            HostProperties.SetAll(OfxNames.ImageEffectPropOpenGLRenderSupported, GlContextManager.Shared != null ? "true" : "false");
            // OpenCL (Buffers) レンダリング対応 (OFX 1.5)。デバイスが使用できない環境では非対応を宣言する
            HostProperties.SetAll(OfxNames.ImageEffectPropOpenCLRenderSupported, CL.ClContextManager.Shared != null ? "true" : "false");
            // CUDA レンダリング対応 (OFX 1.5)。NVIDIA デバイスが使用できない環境では非対応を宣言する
            var cudaSupported = Cuda.CudaContextManager.Shared != null ? "true" : "false";
            HostProperties.SetAll(OfxNames.ImageEffectPropCudaRenderSupported, cudaSupported);
            HostProperties.SetAll(OfxNames.ImageEffectPropCudaStreamSupported, cudaSupported);
            HostStruct = (OfxHostNative*)NativeMemory.Alloc((nuint)sizeof(OfxHostNative));
            HostStruct->HostProps = HostProperties.Handle;
            HostStruct->FetchSuite = SuiteRegistry.FetchSuitePointer;
        }

        static PropertySet BuildHostProperties()
        {
            var props = new PropertySet("Host");

            props.SetAll(OfxNames.PropType, OfxNames.TypeImageEffectHost);
            props.SetAll(OfxNames.PropName, "jp.mes51.nive3");
            props.SetAll(OfxNames.PropLabel, "NiVE3");
            props.SetAll(OfxNames.PropVersion, 3, 0, 0);
            props.SetAll(OfxNames.PropVersionLabel, "3.0.0");
            props.SetAll(OfxNames.PropAPIVersion, 1, 4);

            props.SetAll(OfxNames.ImageEffectHostPropIsBackground, 0);
            props.SetAll(OfxNames.ImageEffectPropSupportsOverlays, 0);
            props.SetAll(OfxNames.ImageEffectPropSupportsMultiResolution, 1);
            props.SetAll(OfxNames.ImageEffectPropSupportsTiles, 0);
            props.SetAll(OfxNames.ImageEffectPropTemporalClipAccess, 1);
            // 対応コンテキストは Filter を基本とし、実プラグインの実態調査のため General も宣言しておく
            props.SetAll(OfxNames.ImageEffectPropSupportedContexts, OfxNames.ContextFilter, OfxNames.ContextGeneral);
            props.SetAll(OfxNames.ImageEffectPropSupportedComponents, OfxNames.ComponentRGBA);
            props.SetAll(OfxNames.ImageEffectPropSupportedPixelDepths, OfxNames.BitDepthFloat);
            props.SetAll(OfxNames.ImageEffectPropSupportsMultipleClipDepths, 0);
            props.SetAll(OfxNames.ImageEffectPropSupportsMultipleClipPARs, 0);
            props.SetAll(OfxNames.ImageEffectPropSetableFrameRate, 0);
            props.SetAll(OfxNames.ImageEffectPropSetableFielding, 0);
            props.SetAll(OfxNames.ImageEffectPropOpenGLRenderSupported, "true");
            props.SetAll(OfxNames.ImageEffectHostPropNativeOrigin, OfxNames.HostNativeOriginTopLeft);
            props.SetAll(OfxNames.ImageEffectInstancePropSequentialRender, 0);
            props.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
            props.SetAll(OfxNames.PropHostOSHandle, (nint)0);

            // 1.5 系 GPU レンダリングの対応状況 (OpenCL Buffers はコンストラクタでデバイス検出後に上書きされる)
            props.SetAll(OfxNames.ImageEffectPropCudaRenderSupported, "false");
            props.SetAll(OfxNames.ImageEffectPropCudaStreamSupported, "false");
            props.SetAll(OfxNames.ImageEffectPropMetalRenderSupported, "false");
            props.SetAll(OfxNames.ImageEffectPropOpenCLRenderSupported, "false");
            props.SetAll(OfxNames.ImageEffectPropOpenCLSupported, "false");
            props.SetAll(OfxNames.ParamHostPropSupportsStrChoice, 1);
            props.SetAll(OfxNames.ParamHostPropSupportsStrChoiceAnimation, 0);

            props.SetAll(OfxNames.ParamHostPropSupportsCustomInteract, 0);
            props.SetAll(OfxNames.ParamHostPropSupportsStringAnimation, 0);
            props.SetAll(OfxNames.ParamHostPropSupportsChoiceAnimation, 1);
            props.SetAll(OfxNames.ParamHostPropSupportsBooleanAnimation, 1);
            props.SetAll(OfxNames.ParamHostPropSupportsCustomAnimation, 0);
            props.SetAll(OfxNames.ParamHostPropSupportsParametricAnimation, 0);
            props.SetAll(OfxNames.ParamHostPropMaxParameters, -1);
            props.SetAll(OfxNames.ParamHostPropMaxPages, 0);
            props.SetAll(OfxNames.ParamHostPropPageRowColumnCount, 0, 0);

            return props;
        }

        /// <summary>
        /// プラグインにホストを設定し、Load アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <returns>Load アクションのステータス</returns>
        public OfxStatus LoadPlugin(OfxPluginInfo plugin)
        {
            plugin.SetHost(HostStruct);
            return plugin.CallAction(OfxNames.ActionLoad, 0, 0, 0);
        }

        /// <summary>
        /// Describe アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <returns>デスクリプタと Describe アクションのステータス</returns>
        public (EffectDescriptor Descriptor, OfxStatus Status) Describe(OfxPluginInfo plugin)
        {
            var descriptor = new EffectDescriptor(plugin.Identifier);
            descriptor.Properties.SetAll(OfxNames.PluginPropFilePath, plugin.BundlePath);
            var status = plugin.CallAction(OfxNames.ActionDescribe, descriptor.Handle, 0, 0);
            return (descriptor, status);
        }

        /// <summary>
        /// DescribeInContext アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="context">コンテキスト名</param>
        /// <returns>コンテキストごとのデスクリプタとアクションのステータス</returns>
        public (EffectDescriptor Descriptor, OfxStatus Status) DescribeInContext(OfxPluginInfo plugin, string context)
        {
            var descriptor = new EffectDescriptor($"{plugin.Identifier}[{context}]");
            descriptor.Properties.SetAll(OfxNames.PluginPropFilePath, plugin.BundlePath);
            using var inArgs = new PropertySet("DescribeInContext.InArgs");
            inArgs.SetAll(OfxNames.ImageEffectPropContext, context);
            var status = plugin.CallAction(OfxNames.ImageEffectActionDescribeInContext, descriptor.Handle, inArgs.Handle, 0);
            return (descriptor, status);
        }

        /// <summary>
        /// Unload アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        public OfxStatus UnloadPlugin(OfxPluginInfo plugin)
        {
            return plugin.CallAction(OfxNames.ActionUnload, 0, 0, 0);
        }

        /// <summary>
        /// エフェクトのインスタンスを生成し、CreateInstance アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="contextDescriptor">DescribeInContext で構築されたデスクリプタ</param>
        /// <param name="context">コンテキスト名</param>
        /// <param name="settings">プロジェクト設定</param>
        /// <returns>生成されたインスタンスとアクションのステータス</returns>
        public (EffectInstance Instance, OfxStatus Status) CreateInstance(OfxPluginInfo plugin, EffectDescriptor contextDescriptor, string context, OfxProjectSettings settings)
        {
            var instance = new EffectInstance(plugin.Identifier, contextDescriptor, context, settings);
            var status = plugin.CallAction(OfxNames.ActionCreateInstance, instance.Handle, 0, 0);
            return (instance, status);
        }

        /// <summary>
        /// DestroyInstance アクションを呼び出し、インスタンスを破棄します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">破棄するインスタンス</param>
        /// <returns>アクションのステータス</returns>
        public OfxStatus DestroyInstance(OfxPluginInfo plugin, EffectInstance instance)
        {
            // OpenGLContextAttached を送っている場合は、破棄前に対になる Detached を GL スレッドで送る
            if (instance.GlContextAttached && GlContextManager.Shared is { } gl)
            {
                gl.Invoke(() => plugin.CallAction(OfxNames.ActionOpenGLContextDetached, instance.Handle, 0, 0));
                instance.GlContextAttached = false;
            }

            var status = plugin.CallAction(OfxNames.ActionDestroyInstance, instance.Handle, 0, 0);
            instance.Dispose();
            return status;
        }

        /// <summary>
        /// GetClipPreferences アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <returns>プラグインが設定した outArgs プロパティセットとアクションのステータス</returns>
        public (PropertySet OutArgs, OfxStatus Status) GetClipPreferences(OfxPluginInfo plugin, EffectInstance instance)
        {
            var outArgs = new PropertySet("ClipPreferences.OutArgs");
            var status = plugin.CallAction(OfxNames.ImageEffectActionGetClipPreferences, instance.Handle, 0, outArgs.Handle);
            return (outArgs, status);
        }

        /// <summary>
        /// パラメータ変更を Begin/InstanceChanged/End の一連のアクションでプラグインへ通知します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="paramName">変更されたパラメータ名</param>
        /// <param name="time">変更が発生した時間 (フレーム)</param>
        /// <param name="reason">変更理由 (OfxNames.ChangeUserEdited など)</param>
        /// <returns>InstanceChanged アクションのステータス</returns>
        public OfxStatus NotifyParamChanged(OfxPluginInfo plugin, EffectInstance instance, string paramName, double time, string reason = OfxNames.ChangeUserEdited)
        {
            return NotifyChangedCore(plugin, instance, OfxNames.TypeParameter, paramName, time, reason);
        }

        /// <summary>
        /// クリップの接続変更を Begin/InstanceChanged/End の一連のアクションでプラグインへ通知します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="clipName">変更されたクリップ名</param>
        /// <param name="time">変更が発生した時間 (フレーム)</param>
        /// <param name="reason">変更理由 (OfxNames.ChangeUserEdited など)</param>
        /// <returns>InstanceChanged アクションのステータス</returns>
        public OfxStatus NotifyClipChanged(OfxPluginInfo plugin, EffectInstance instance, string clipName, double time, string reason = OfxNames.ChangeUserEdited)
        {
            return NotifyChangedCore(plugin, instance, OfxNames.TypeClip, clipName, time, reason);
        }

        OfxStatus NotifyChangedCore(OfxPluginInfo plugin, EffectInstance instance, string type, string objectName, double time, string reason)
        {
            using var beginArgs = new PropertySet("BeginInstanceChanged.InArgs");
            beginArgs.SetAll(OfxNames.PropChangeReason, reason);
            plugin.CallAction(OfxNames.ActionBeginInstanceChanged, instance.Handle, beginArgs.Handle, 0);

            using var inArgs = new PropertySet("InstanceChanged.InArgs");
            inArgs.SetAll(OfxNames.PropType, type);
            inArgs.SetAll(OfxNames.PropName, objectName);
            inArgs.SetAll(OfxNames.PropChangeReason, reason);
            inArgs.SetAll(OfxNames.PropTime, time);
            inArgs.SetAll(OfxNames.ImageEffectPropRenderScale, 1.0, 1.0);
            var status = plugin.CallAction(OfxNames.ActionInstanceChanged, instance.Handle, inArgs.Handle, 0);

            using var endArgs = new PropertySet("EndInstanceChanged.InArgs");
            endArgs.SetAll(OfxNames.PropChangeReason, reason);
            plugin.CallAction(OfxNames.ActionEndInstanceChanged, instance.Handle, endArgs.Handle, 0);

            return status;
        }

        /// <summary>
        /// GetRegionOfDefinition アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <returns>RoD とアクションのステータス。ReplyDefault の場合はホスト既定 (ソースと同じ) を使用します</returns>
        public (OfxRectD Rod, OfxStatus Status) GetRegionOfDefinition(OfxPluginInfo plugin, EffectInstance instance, double time)
        {
            using var inArgs = new PropertySet("GetRoD.InArgs");
            inArgs.SetAll(OfxNames.PropTime, time);
            inArgs.SetAll(OfxNames.ImageEffectPropRenderScale, 1.0, 1.0);
            using var outArgs = new PropertySet("GetRoD.OutArgs");
            outArgs.SetAll(OfxNames.ImageEffectPropRegionOfDefinition, 0.0, 0.0, 0.0, 0.0);

            var status = plugin.CallAction(OfxNames.ImageEffectActionGetRegionOfDefinition, instance.Handle, inArgs.Handle, outArgs.Handle);

            var rod = new OfxRectD();
            if (status == OfxStatus.OK)
            {
                outArgs.TryGet(OfxNames.ImageEffectPropRegionOfDefinition, 0, out var x1);
                outArgs.TryGet(OfxNames.ImageEffectPropRegionOfDefinition, 1, out var y1);
                outArgs.TryGet(OfxNames.ImageEffectPropRegionOfDefinition, 2, out var x2);
                outArgs.TryGet(OfxNames.ImageEffectPropRegionOfDefinition, 3, out var y2);
                rod = new OfxRectD
                {
                    X1 = Convert.ToDouble(x1),
                    Y1 = Convert.ToDouble(y1),
                    X2 = Convert.ToDouble(x2),
                    Y2 = Convert.ToDouble(y2)
                };
            }
            return (rod, status);
        }

        /// <summary>
        /// IsIdentity アクションを呼び出します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <param name="width">レンダリング領域の幅</param>
        /// <param name="height">レンダリング領域の高さ</param>
        /// <returns>エフェクトが何もしない場合はパススルー元のクリップ名。それ以外は null</returns>
        public string? IsIdentity(OfxPluginInfo plugin, EffectInstance instance, double time, int width, int height)
        {
            using var inArgs = new PropertySet("IsIdentity.InArgs");
            inArgs.SetAll(OfxNames.PropTime, time);
            inArgs.SetAll(OfxNames.ImageEffectPropFieldToRender, OfxNames.ImageFieldNone);
            inArgs.SetAll(OfxNames.ImageEffectPropRenderWindow, 0, 0, width, height);
            inArgs.SetAll(OfxNames.ImageEffectPropRenderScale, 1.0, 1.0);
            using var outArgs = new PropertySet("IsIdentity.OutArgs");
            outArgs.SetAll(OfxNames.PropName, "");
            outArgs.SetAll(OfxNames.PropTime, time);

            var status = plugin.CallAction(OfxNames.ImageEffectActionIsIdentity, instance.Handle, inArgs.Handle, outArgs.Handle);
            if (status == OfxStatus.OK)
            {
                outArgs.TryGet(OfxNames.PropName, 0, out var name);
                return name as string ?? "Source";
            }
            return null;
        }

        /// <summary>
        /// 1 フレームをレンダリングします (BeginSequenceRender → Render → EndSequenceRender)
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <param name="width">出力画像の幅</param>
        /// <param name="height">出力画像の高さ</param>
        /// <param name="frameProvider">クリップ画像のプロバイダ</param>
        /// <returns>レンダリング結果 (BGRA・上から下) とステータス</returns>
        public (Vector4[]? Output, OfxStatus Status) RenderFrame(OfxPluginInfo plugin, EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, double renderScaleX = 1.0, double renderScaleY = 1.0)
        {
            instance.CurrentTime = time;
            instance.FrameProvider = frameProvider;
            instance.CurrentRenderScale = (renderScaleX, renderScaleY);
            instance.OutputImage = OfxImage.CreateEmpty(width, height, true, "Output");
            instance.OutputImage.Properties.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
            try
            {
                using var sequenceArgs = new PropertySet("SequenceRender.InArgs");
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameRange, time, time);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameStep, 1.0);
                sequenceArgs.SetAll(OfxNames.PropIsInteractive, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(sequenceArgs);
                plugin.CallAction(OfxNames.ImageEffectActionBeginSequenceRender, instance.Handle, sequenceArgs.Handle, 0);

                using var renderArgs = new PropertySet("Render.InArgs");
                renderArgs.SetAll(OfxNames.PropTime, time);
                renderArgs.SetAll(OfxNames.ImageEffectPropFieldToRender, OfxNames.ImageFieldNone);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderWindow, 0, 0, width, height);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                renderArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(renderArgs);
                var status = plugin.CallAction(OfxNames.ImageEffectActionRender, instance.Handle, renderArgs.Handle, 0);

                plugin.CallAction(OfxNames.ImageEffectActionEndSequenceRender, instance.Handle, sequenceArgs.Handle, 0);

                Vector4[]? output = null;
                if (status is OfxStatus.OK or OfxStatus.ReplyDefault)
                {
                    WaitForOutputSettled(plugin, instance.OutputImage, width, height);
                    output = instance.OutputImage.ToBgraTopDown();
                }
                return (output, status);
            }
            finally
            {
                lock (instance.FetchedImages)
                {
                    var leaked = instance.FetchedImages.Count(i => !i.Disposed);
                    if (leaked > 0)
                    {
                        OfxLog.Warn($"プラグインが {leaked} 枚の入力画像を clipReleaseImage していません (ホスト側で解放します)");
                        foreach (var image in instance.FetchedImages.Where(i => !i.Disposed))
                        {
                            image.Dispose();
                        }
                    }
                    instance.FetchedImages.Clear();
                }
                instance.OutputImage?.Dispose();
                instance.OutputImage = null;
            }
        }

        /// <summary>
        /// 1 フレームを OpenCL (Buffers) でレンダリングします (OFX 1.5)
        /// 入出力画像を cl_mem バッファとして渡し、プラグインはホストのコマンドキューへ処理を投入します
        /// 単一のキューを共有するため、レンダリングは直列化されます
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <param name="width">出力画像の幅</param>
        /// <param name="height">出力画像の高さ</param>
        /// <param name="frameProvider">クリップ画像のプロバイダ</param>
        /// <returns>レンダリング結果 (BGRA・上から下) とステータス。OpenCL が使用できない場合は ErrMissingHostFeature</returns>
        public (Vector4[]? Output, OfxStatus Status) RenderFrameCL(OfxPluginInfo plugin, EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, double renderScaleX = 1.0, double renderScaleY = 1.0)
        {
            var cl = CL.ClContextManager.Shared;
            if (cl == null)
            {
                return (null, OfxStatus.ErrMissingHostFeature);
            }

            return RenderFrameCLCore(instance, time, width, height, frameProvider, renderScaleX, renderScaleY, () =>
            {
                using var sequenceArgs = new PropertySet("SequenceRenderCL.InArgs");
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameRange, time, time);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameStep, 1.0);
                sequenceArgs.SetAll(OfxNames.PropIsInteractive, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(sequenceArgs);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropOpenCLEnabled, 1);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropOpenCLCommandQueue, cl.Queue);
                plugin.CallAction(OfxNames.ImageEffectActionBeginSequenceRender, instance.Handle, sequenceArgs.Handle, 0);

                using var renderArgs = new PropertySet("RenderCL.InArgs");
                renderArgs.SetAll(OfxNames.PropTime, time);
                renderArgs.SetAll(OfxNames.ImageEffectPropFieldToRender, OfxNames.ImageFieldNone);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderWindow, 0, 0, width, height);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                renderArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(renderArgs);
                renderArgs.SetAll(OfxNames.ImageEffectPropOpenCLEnabled, 1);
                renderArgs.SetAll(OfxNames.ImageEffectPropOpenCLCommandQueue, cl.Queue);
                var status = plugin.CallAction(OfxNames.ImageEffectActionRender, instance.Handle, renderArgs.Handle, 0);

                plugin.CallAction(OfxNames.ImageEffectActionEndSequenceRender, instance.Handle, sequenceArgs.Handle, 0);
                return status;
            });
        }

        /// <summary>
        /// OpenCL レンダリングの共通処理 (バッファ準備 → renderBody 実行 → 完了待ち → 読み戻し)
        /// renderBody は検証用に差し替え可能です
        /// </summary>
        public (Vector4[]? Output, OfxStatus Status) RenderFrameCLCore(EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, double renderScaleX, double renderScaleY, Func<OfxStatus> renderBody)
        {
            var cl = CL.ClContextManager.Shared;
            if (cl == null)
            {
                return (null, OfxStatus.ErrMissingHostFeature);
            }

            lock (cl.RenderLock)
            {
                instance.CurrentTime = time;
                instance.FrameProvider = frameProvider;
                instance.CurrentRenderScale = (renderScaleX, renderScaleY);
                instance.OutputClImage = CL.OfxClImage.CreateEmpty(cl, width, height, true, "Output");
                if (instance.OutputClImage == null)
                {
                    return (null, OfxStatus.Failed);
                }
                instance.OutputClImage.Properties.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                try
                {
                    var status = renderBody();

                    Vector4[]? output = null;
                    if (status is OfxStatus.OK or OfxStatus.ReplyDefault)
                    {
                        // プラグインは非同期のままキューへ投入して戻ってよい仕様のため、完了を待ってから読み戻す
                        CL.ClNative.clFinish(cl.Queue);
                        output = instance.OutputClImage.ToBgraTopDown(cl);
                        if (output == null)
                        {
                            status = OfxStatus.Failed;
                        }
                    }
                    return (output, status);
                }
                finally
                {
                    lock (instance.FetchedClImages)
                    {
                        var leaked = instance.FetchedClImages.Count(i => !i.Disposed);
                        if (leaked > 0)
                        {
                            OfxLog.Warn($"プラグインが {leaked} 枚の OpenCL 入力画像を clipReleaseImage していません (ホスト側で解放します)");
                            foreach (var image in instance.FetchedClImages.Where(i => !i.Disposed))
                            {
                                image.Dispose();
                            }
                        }
                        instance.FetchedClImages.Clear();
                    }
                    instance.OutputClImage?.Dispose();
                    instance.OutputClImage = null;
                }
            }
        }

        /// <summary>
        /// 1 フレームを CUDA でレンダリングします (OFX 1.5)
        /// 入出力画像は CUDA デバイスポインタとして渡し、レンダリング後にホストメモリへ読み戻します
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <param name="width">出力画像の幅</param>
        /// <param name="height">出力画像の高さ</param>
        /// <param name="frameProvider">クリップ画像のプロバイダ</param>
        /// <param name="useStream">プラグインが CudaStreamSupported を宣言している場合 true (仕様上、両者が対応する場合のみ CudaStream を渡す)</param>
        /// <returns>レンダリング結果 (BGRA・上から下) とステータス。CUDA が使用できない場合は ErrMissingHostFeature</returns>
        public (Vector4[]? Output, OfxStatus Status) RenderFrameCuda(OfxPluginInfo plugin, EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, double renderScaleX = 1.0, double renderScaleY = 1.0, bool useStream = false)
        {
            var cuda = Cuda.CudaContextManager.Shared;
            if (cuda == null)
            {
                return (null, OfxStatus.ErrMissingHostFeature);
            }

            return RenderFrameCudaCore(instance, time, width, height, frameProvider, renderScaleX, renderScaleY, () =>
            {
                using var sequenceArgs = new PropertySet("SequenceRenderCuda.InArgs");
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameRange, time, time);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameStep, 1.0);
                sequenceArgs.SetAll(OfxNames.PropIsInteractive, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(sequenceArgs);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropCudaEnabled, 1);
                if (useStream)
                {
                    sequenceArgs.SetAll(OfxNames.ImageEffectPropCudaStream, cuda.Stream);
                }
                plugin.CallAction(OfxNames.ImageEffectActionBeginSequenceRender, instance.Handle, sequenceArgs.Handle, 0);

                using var renderArgs = new PropertySet("RenderCuda.InArgs");
                renderArgs.SetAll(OfxNames.PropTime, time);
                renderArgs.SetAll(OfxNames.ImageEffectPropFieldToRender, OfxNames.ImageFieldNone);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderWindow, 0, 0, width, height);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                renderArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(renderArgs);
                renderArgs.SetAll(OfxNames.ImageEffectPropCudaEnabled, 1);
                if (useStream)
                {
                    renderArgs.SetAll(OfxNames.ImageEffectPropCudaStream, cuda.Stream);
                }
                var status = plugin.CallAction(OfxNames.ImageEffectActionRender, instance.Handle, renderArgs.Handle, 0);

                plugin.CallAction(OfxNames.ImageEffectActionEndSequenceRender, instance.Handle, sequenceArgs.Handle, 0);
                return status;
            });
        }

        /// <summary>
        /// CUDA レンダリングの共通処理 (デバイスメモリ準備 → renderBody 実行 → 完了待ち → 読み戻し)
        /// renderBody は検証用に差し替え可能です
        /// </summary>
        public (Vector4[]? Output, OfxStatus Status) RenderFrameCudaCore(EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, double renderScaleX, double renderScaleY, Func<OfxStatus> renderBody)
        {
            var cuda = Cuda.CudaContextManager.Shared;
            if (cuda == null)
            {
                return (null, OfxStatus.ErrMissingHostFeature);
            }

            lock (cuda.RenderLock)
            {
                // コンテキストはスレッドごとの状態のため、レンダリングスレッド上で毎回設定する
                cuda.MakeCurrent();
                instance.CurrentTime = time;
                instance.FrameProvider = frameProvider;
                instance.CurrentRenderScale = (renderScaleX, renderScaleY);
                instance.OutputCudaImage = Cuda.OfxCudaImage.CreateEmpty(cuda, width, height, true, "Output");
                if (instance.OutputCudaImage == null)
                {
                    return (null, OfxStatus.Failed);
                }
                instance.OutputCudaImage.Properties.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                try
                {
                    var status = renderBody();

                    Vector4[]? output = null;
                    if (status is OfxStatus.OK or OfxStatus.ReplyDefault)
                    {
                        // プラグインは非同期のままストリームへ投入して戻ってよい仕様のため、完了を待ってから読み戻す
                        Cuda.CudaNative.cuStreamSynchronize(cuda.Stream);
                        output = instance.OutputCudaImage.ToBgraTopDown(cuda);
                        if (output == null)
                        {
                            status = OfxStatus.Failed;
                        }
                    }
                    return (output, status);
                }
                finally
                {
                    lock (instance.FetchedCudaImages)
                    {
                        var leaked = instance.FetchedCudaImages.Count(i => !i.Disposed);
                        if (leaked > 0)
                        {
                            OfxLog.Warn($"プラグインが {leaked} 枚の CUDA 入力画像を clipReleaseImage していません (ホスト側で解放します)");
                            foreach (var image in instance.FetchedCudaImages.Where(i => !i.Disposed))
                            {
                                image.Dispose();
                            }
                        }
                        instance.FetchedCudaImages.Clear();
                    }
                    instance.OutputCudaImage?.Dispose();
                    instance.OutputCudaImage = null;
                }
            }
        }

        /// <summary>
        /// 1 フレームを OpenGL でレンダリングします
        /// アクション呼び出しを含む全処理が GL スレッド上で実行されます
        /// </summary>
        /// <param name="plugin">対象のプラグイン</param>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <param name="width">出力画像の幅</param>
        /// <param name="height">出力画像の高さ</param>
        /// <param name="frameProvider">クリップ画像のプロバイダ</param>
        /// <returns>レンダリング結果 (BGRA・上から下) とステータス。GL が使用できない場合は ErrMissingHostFeature</returns>
        public (Vector4[]? Output, OfxStatus Status) RenderFrameGL(OfxPluginInfo plugin, EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, double renderScaleX = 1.0, double renderScaleY = 1.0)
        {
            return RenderFrameGLCore(instance, time, width, height, frameProvider, renderScaleX, renderScaleY, () =>
            {
                // 初回の GL レンダリング前に OpenGLContextAttached を通知する (1.5。対応する Detached は DestroyInstance で送る)
                if (!instance.GlContextAttached)
                {
                    var attachStatus = plugin.CallAction(OfxNames.ActionOpenGLContextAttached, instance.Handle, 0, 0);
                    if (attachStatus is not (OfxStatus.OK or OfxStatus.ReplyDefault))
                    {
                        OfxLog.Warn($"OpenGLContextAttached が失敗したため GL レンダリングを中止します: {attachStatus}");
                        return OfxStatus.Failed;
                    }
                    instance.GlContextAttached = true;
                }

                using var sequenceArgs = new PropertySet("SequenceRenderGL.InArgs");
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameRange, time, time);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropFrameStep, 1.0);
                sequenceArgs.SetAll(OfxNames.PropIsInteractive, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(sequenceArgs);
                sequenceArgs.SetAll(OfxNames.ImageEffectPropOpenGLEnabled, 1);
                plugin.CallAction(OfxNames.ImageEffectActionBeginSequenceRender, instance.Handle, sequenceArgs.Handle, 0);

                using var renderArgs = new PropertySet("RenderGL.InArgs");
                renderArgs.SetAll(OfxNames.PropTime, time);
                renderArgs.SetAll(OfxNames.ImageEffectPropFieldToRender, OfxNames.ImageFieldNone);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderWindow, 0, 0, width, height);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderScale, renderScaleX, renderScaleY);
                renderArgs.SetAll(OfxNames.ImageEffectPropSequentialRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropInteractiveRenderStatus, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropRenderQualityDraft, 0);
                renderArgs.SetAll(OfxNames.ImageEffectPropThumbnailRender, "false");
                ApplyGpuDisabledArgs(renderArgs);
                renderArgs.SetAll(OfxNames.ImageEffectPropOpenGLEnabled, 1);
                var status = plugin.CallAction(OfxNames.ImageEffectActionRender, instance.Handle, renderArgs.Handle, 0);

                plugin.CallAction(OfxNames.ImageEffectActionEndSequenceRender, instance.Handle, sequenceArgs.Handle, 0);
                return status;
            });
        }

        /// <summary>
        /// OpenGL レンダリングの共通処理 (FBO 準備 → renderBody 実行 → 読み戻し)
        /// renderBody は GL スレッド上で実行されます (検証用に描画処理を差し替え可能)
        /// </summary>
        /// <param name="instance">対象のインスタンス</param>
        /// <param name="time">時間 (フレーム)</param>
        /// <param name="width">出力画像の幅</param>
        /// <param name="height">出力画像の高さ</param>
        /// <param name="frameProvider">クリップ画像のプロバイダ</param>
        /// <param name="renderBody">描画処理 (通常は Render アクションの呼び出し)</param>
        /// <returns>レンダリング結果 (BGRA・上から下) とステータス</returns>
        public (Vector4[]? Output, OfxStatus Status) RenderFrameGLCore(EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, Func<OfxStatus> renderBody)
        {
            return RenderFrameGLCore(instance, time, width, height, frameProvider, 1.0, 1.0, renderBody);
        }

        public (Vector4[]? Output, OfxStatus Status) RenderFrameGLCore(EffectInstance instance, double time, int width, int height, IOfxFrameProvider frameProvider, double renderScaleX, double renderScaleY, Func<OfxStatus> renderBody)
        {
            var gl = GlContextManager.Shared;
            if (gl == null)
            {
                return (null, OfxStatus.ErrMissingHostFeature);
            }

            instance.CurrentTime = time;
            instance.FrameProvider = frameProvider;
            instance.CurrentRenderScale = (renderScaleX, renderScaleY);

            var status = OfxStatus.Failed;
            Vector4[]? output = null;
            gl.Invoke(() => (output, status) = RenderOnGlThread(gl, instance, width, height, renderBody));
            return (output, status);
        }

        (Vector4[]? Output, OfxStatus Status) RenderOnGlThread(GlContextManager gl, EffectInstance instance, int width, int height, Func<OfxStatus> renderBody)
        {
            uint framebuffer = 0;
            Vector4[]? output = null;
            var status = OfxStatus.Failed;
            try
            {
                // 出力先のテクスチャと FBO を準備する
                // プラグインは「バインド済み FBO へ描画」または「clipLoadTexture(Output) で取得した
                // テクスチャへ自前の FBO で描画」(openfx-misc 系) のどちらかを行うが、
                // どちらも同じテクスチャに書き込まれるため、読み戻しは共通になる
                instance.OutputGlTexture = OfxGlTexture.CreateEmpty(width, height, true, "Output");

                gl.GenFramebuffers(1, &framebuffer);
                gl.BindFramebuffer(GlNative.GL_FRAMEBUFFER, framebuffer);
                gl.FramebufferTexture2D(GlNative.GL_FRAMEBUFFER, GlNative.GL_COLOR_ATTACHMENT0, GlNative.GL_TEXTURE_2D, instance.OutputGlTexture.TextureId, 0);
                if (gl.CheckFramebufferStatus(GlNative.GL_FRAMEBUFFER) != GlNative.GL_FRAMEBUFFER_COMPLETE)
                {
                    OfxLog.Warn("FBO の作成に失敗しました");
                    return (null, OfxStatus.Failed);
                }

                // OFX の座標系 (左下原点・ピクセル座標) に合わせたビューポートと射影を設定する
                GlNative.glViewport(0, 0, width, height);
                GlNative.glMatrixMode(GlNative.GL_PROJECTION);
                GlNative.glLoadIdentity();
                GlNative.glOrtho(0.0, width, 0.0, height, -1.0, 1.0);
                GlNative.glMatrixMode(GlNative.GL_MODELVIEW);
                GlNative.glLoadIdentity();
                GlNative.glClearColor(0.0F, 0.0F, 0.0F, 0.0F);
                GlNative.glClear(GlNative.GL_COLOR_BUFFER_BIT);

                // 直前に実行された別のプラグインが残した GL 状態 (シェーダープログラム等) をリセットする
                gl.ResetRenderState();

                status = renderBody();

                if (status is OfxStatus.OK or OfxStatus.ReplyDefault)
                {
                    var buffer = (float*)NativeMemory.Alloc((nuint)((long)width * height * 4 * sizeof(float)));
                    try
                    {
                        // プラグインが自前の FBO をバインドした可能性があるため、読み戻し前にバインドし直す
                        gl.BindFramebuffer(GlNative.GL_FRAMEBUFFER, framebuffer);
                        GlNative.glFinish();
                        gl.ResetPackState();
                        GlNative.glReadPixels(0, 0, width, height, GlNative.GL_RGBA, GlNative.GL_FLOAT, buffer);
                        output = new Vector4[width * height];
                        ImageBridge.FromOfx(buffer, width, height, output);
                    }
                    finally
                    {
                        NativeMemory.Free(buffer);
                    }
                }

                return (output, status);
            }
            finally
            {
                gl.BindFramebuffer(GlNative.GL_FRAMEBUFFER, 0);
                if (framebuffer != 0)
                {
                    gl.DeleteFramebuffers(1, &framebuffer);
                }
                instance.OutputGlTexture?.Dispose();
                instance.OutputGlTexture = null;

                lock (instance.LoadedGlTextures)
                {
                    var leaked = instance.LoadedGlTextures.Count(t => !t.Disposed);
                    if (leaked > 0)
                    {
                        OfxLog.Warn($"プラグインが {leaked} 枚のテクスチャを clipFreeTexture していません (ホスト側で解放します)");
                        foreach (var texture in instance.LoadedGlTextures.Where(t => !t.Disposed))
                        {
                            texture.Dispose();
                        }
                    }
                    instance.LoadedGlTextures.Clear();
                }
            }
        }

        /// <summary>
        /// 出力バッファへの書き込みが完了する (内容が変化しなくなる) まで待機します
        /// 一部のプラグイン (WebGPU 製の Chromabba 等) は Render アクションから
        /// 非同期の GPU 処理完了前に戻るため、そのまま読み出すと未完成の画像 (縞状の乱れ) になる
        /// 正常なプラグインではハッシュ比較 1 回 (数 ms 以下) で済みます
        /// </summary>
        /// <param name="plugin">対象のプラグイン (ログ用)</param>
        /// <param name="outputImage">出力画像</param>
        /// <param name="width">出力画像の幅</param>
        /// <param name="height">出力画像の高さ</param>
        static readonly HashSet<string> AsyncWritingPlugins = new HashSet<string>();

        unsafe void WaitForOutputSettled(OfxPluginInfo plugin, OfxImage outputImage, int width, int height)
        {
            var byteLength = (long)width * height * 4 * sizeof(float);
            var span = new ReadOnlySpan<byte>(outputImage.Data, checked((int)Math.Min(byteLength, int.MaxValue)));

            bool knownAsync;
            lock (AsyncWritingPlugins)
            {
                knownAsync = AsyncWritingPlugins.Contains(plugin.Identifier);
            }

            var previousHash = XxHash3.HashToUInt64(span);
            if (!knownAsync)
            {
                // 通常ケース: 連続 2 回のハッシュが一致すれば完了とみなす (数 ms 以下)
                if (XxHash3.HashToUInt64(span) == previousHash)
                {
                    return;
                }
                lock (AsyncWritingPlugins)
                {
                    AsyncWritingPlugins.Add(plugin.Identifier);
                }
                OfxLog.Warn($"Render アクション後も出力への書き込みが継続しています (非同期プラグインとして待機します): {plugin.Identifier}");
            }

            // 非同期プラグイン: 10ms 間隔でハッシュが変化しなくなるまで待つ (最大 2 秒)
            var stableCount = 0;
            for (var attempt = 0; attempt < 200; attempt++)
            {
                Thread.Sleep(10);
                var hash = XxHash3.HashToUInt64(span);
                if (hash == previousHash)
                {
                    stableCount++;
                    if (stableCount >= 2)
                    {
                        return;
                    }
                }
                else
                {
                    stableCount = 0;
                }
                previousHash = hash;
            }
            OfxLog.Warn($"出力の書き込み完了を待機しましたが安定しませんでした: {plugin.Identifier}");
        }

        // GPU レンダリング (1.5 系拡張) は使用しないことをレンダリング引数で明示する
        static void ApplyGpuDisabledArgs(PropertySet args)
        {
            args.SetAll(OfxNames.ImageEffectPropCudaEnabled, 0);
            args.SetAll(OfxNames.ImageEffectPropCudaStream, (nint)0);
            args.SetAll(OfxNames.ImageEffectPropMetalEnabled, 0);
            args.SetAll(OfxNames.ImageEffectPropMetalCommandQueue, (nint)0);
            args.SetAll(OfxNames.ImageEffectPropOpenCLEnabled, 0);
            args.SetAll(OfxNames.ImageEffectPropOpenCLCommandQueue, (nint)0);
            args.SetAll(OfxNames.ImageEffectPropOpenGLEnabled, 0);
        }

        public void Dispose()
        {
            NativeMemory.Free(HostStruct);
            HostProperties.Dispose();
        }
    }
}
