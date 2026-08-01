using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.OpenFX.Bridge;
using NiVE3.OpenFX.Host;
using NiVE3.OpenFX.Interop;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.OpenFX.Integration
{
    /// <summary>
    /// OFX エフェクトを NiVE3 のエフェクトとして動作させるアダプタ
    /// MEF は経由せず、EffectModel が要求する IEffect の呼び出し面のみを満たします
    /// </summary>
    public sealed class OfxEffectAdapter : IEffect, IPropertyEditAwareEffect
    {
        // 追加入力クリップに対応するプロパティ ID の接頭辞 (パラメータ名との衝突を避ける)
        const string ClipPropertyPrefix = "ofxClip:";

        OfxEffectDefinition Definition { get; }

        OfxHostRuntime Runtime => Definition.Registry.Runtime;

        EffectInstance? Instance { get; set; }

        PropertyBase[]? CachedProperties { get; set; }

        // PushButton クリック時に InstanceChanged の前へ最新値を反映するための、直近のプロパティ一覧
        IPropertyObject[]? LastProperties { get; set; }

        Time LastTime { get; set; }

        double LastFrameRate { get; set; } = 30.0;

        bool TemporalAccessWarned { get; set; }

        // 同一条件 (入力・パラメータ・時間・サイズ) の再レンダリングをプラグインへ流さないためのキャッシュ。
        // 再描画の高速化に加え、同一時刻の連続レンダリングで内部キャッシュが壊れるプラグイン (Chromabba 等) への回避策になる
        ulong LastRenderKey { get; set; }

        Vector4[]? LastRenderOutput { get; set; }

        bool IsRenderEveryFrame { get; set; }

        /// <summary>
        /// プラグインがパラメータ連動で値を書き換えた際に発生します (EffectModel がプロパティモデルへ反映する)
        /// </summary>
        public event EventHandler<PropertyValuesWritebackEventArgs>? PropertyValuesWriteback;

        // 直近に GetClipPreferences を実行した時点のパラメータ値のハッシュ (再実行の要否判定用)
        ulong LastPrefsParamsHash { get; set; }

        // OnPropertyValuesEdited で通知中のパラメータ名 (編集中のパラメータへの書き戻しを抑止する)
        string? NotifyingParamName { get; set; }

        // プラグインへの通知 (InstanceChanged) 実行中かどうか。実行中の再通知はプラグインへの再入クラッシュを招くため遮断する
        bool IsNotifyingPlugin { get; set; }

        // 通知中に発生した書き戻しの一時バッファ (アクション完了後にまとめてイベントを発生させる)
        List<KeyValuePair<string, object?>>? PendingWritebacks { get; set; }

        // 追加入力クリップに割り当てられたレイヤーの直近の値 (変更検出用)
        Dictionary<string, UseLayerImageTarget> LastClipTargets { get; } = new Dictionary<string, UseLayerImageTarget>();

        // Undo/Redo 復元通知の実行中かどうか。この間の値の書き戻しは破棄する (復元された値を壊さないため)
        bool DropWritebacks { get; set; }

        object Lock { get; } = new object();

        internal OfxEffectAdapter(OfxEffectDefinition definition)
        {
            Definition = definition;
        }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            // OFX の GPU レンダリングは ComputeSharp とは独立した OpenGL で行うため、ここでは何もしない
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            lock (Lock)
            {
                if (!EnsureInstance(sourceSize.Width, sourceSize.Height))
                {
                    return [];
                }
                CachedProperties ??= [.. BuildClipProperties(Instance!), .. OfxParamBridge.BuildProperties(Instance!, OnButtonClicked)];
                return CachedProperties;
            }
        }

        public bool IsNeedRenderFrame(IPropertyObject[] properties, Time layerTime)
        {
            return IsRenderEveryFrame;
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            lock (Lock)
            {
                if (!EnsureInstance(image.Width, image.Height))
                {
                    return image;
                }
                var instance = Instance!;

                LastProperties = properties;
                LastTime = layerTime;
                LastFrameRate = composition.FrameRate;

                UpdateProjectProperties(instance, composition);
                OfxParamBridge.ApplyValues(instance, properties, layerTime);

                // 編集通知を経ないパラメータ値の変化 (プロジェクト読込直後の初回レンダリング等) も
                // GetClipPreferences (FrameVarying 等) へ反映する
                if (CalcParamsHash(instance) != LastPrefsParamsHash)
                {
                    UpdateClipPreferences(instance);
                }

                var ofxTime = ToOfxTime(layerTime, composition.FrameRate);
                // NiVE3 の downSamplingRate は除数 (2 で半分の解像度)、OFX の renderScale は係数 (0.5 で半分)
                var renderScaleX = downSamplingRateX > 0.0 ? 1.0 / downSamplingRateX : 1.0;
                var renderScaleY = downSamplingRateY > 0.0 ? 1.0 / downSamplingRateY : 1.0;
                var managed = image.ToManaged();
                var extraOwned = new List<NManagedImage>();
                var frames = new Dictionary<string, (Vector4[] Pixels, int Width, int Height)>
                {
                    [instance.MainInputClipName ?? "Source"] = (managed.Data, image.Width, image.Height)
                };
                try
                {
                    // 追加入力クリップに割り当てられたレイヤーの画像を解決し、接続状態を更新する
                    foreach (var clip in instance.ExtraInputClips)
                    {
                        var target = GetClipTarget(properties, clip.Name, layerTime);
                        LastClipTargets[clip.Name] = target;
                        var extraSource = ResolveLayerImage(target, composition, layerTime, downSamplingRateX, useGpu);
                        if (extraSource != null)
                        {
                            var extraManaged = extraSource.ToManaged();
                            if (!ReferenceEquals(extraManaged, extraSource))
                            {
                                extraOwned.Add(extraManaged);
                            }
                            frames[clip.Name] = (extraManaged.Data, extraManaged.Width, extraManaged.Height);
                        }
                        clip.SetConnected(frames.ContainsKey(clip.Name));
                    }

                    var renderKey = CalcRenderKey(instance, frames, image.Width, image.Height, ofxTime, useGpu, renderScaleX, renderScaleY);
                    if (renderKey == LastRenderKey && LastRenderOutput != null && LastRenderOutput.Length >= image.Width * image.Height)
                    {
                        var cached = new NManagedImage(image.Width, image.Height, false)
                        {
                            Origin = image.Origin
                        };
                        Array.Copy(LastRenderOutput, cached.Data, image.Width * image.Height);
                        return cached;
                    }

#if NIVE3_OFX_DIAGNOSTICS
                    long? dumpSequence = null;
                    if (OfxDebugDump.Directory != null)
                    {
                        dumpSequence = OfxDebugDump.Save($"{Definition.Plugin.Identifier}_in_t{ofxTime:F1}", managed.Data, image.Width, image.Height);
                    }
#endif

                    var provider = new AdapterFrameProvider(this, frames);

                    var (output, status) = RenderOnce(instance, ofxTime, image.Width, image.Height, provider, useGpu, renderScaleX, renderScaleY);

                    if (output == null)
                    {
                        OfxLog.Warn($"OFX レンダリングに失敗しました: {Definition.Plugin.Identifier}: {status}");
                        return image;
                    }

#if NIVE3_OFX_DIAGNOSTICS
                    if (dumpSequence != null)
                    {
                        OfxDebugDump.Save($"{Definition.Plugin.Identifier}_out_t{ofxTime:F1}", output, image.Width, image.Height, dumpSequence);
                    }
#endif

                    LastRenderKey = renderKey;
                    LastRenderOutput = output;

                    var result = new NManagedImage(image.Width, image.Height, false)
                    {
                        Origin = image.Origin
                    };
                    Array.Copy(output, result.Data, output.Length);
                    return result;
                }
                finally
                {
                    if (!ReferenceEquals(managed, image))
                    {
                        managed.Dispose();
                    }
                    foreach (var owned in extraOwned)
                    {
                        owned.Dispose();
                    }
                }
            }
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            return audio;
        }

        public void Dispose()
        {
            lock (Lock)
            {
                if (Instance != null)
                {
                    Runtime.DestroyInstance(Definition.Plugin, Instance);
                    Instance = null;
                }
                CachedProperties = null;
                LastProperties = null;
                LastRenderOutput = null;
            }
        }

        bool EnsureInstance(int width, int height)
        {
            if (Instance != null)
            {
                return true;
            }

            var contextDescriptor = Definition.GetContextDescriptor();
            if (contextDescriptor == null)
            {
                return false;
            }

            var settings = new OfxProjectSettings
            {
                Width = width,
                Height = height,
                FrameRate = LastFrameRate
            };
            var (instance, status) = Runtime.CreateInstance(Definition.Plugin, contextDescriptor, Definition.Context, settings);
            if (status is not (OfxStatus.OK or OfxStatus.ReplyDefault))
            {
                OfxLog.Warn($"CreateInstance に失敗しました: {Definition.Plugin.Identifier}: {status}");
                instance.Dispose();
                return false;
            }
            Instance = instance;

            // プラグインによるパラメータ値の書き換え (paramSetValue) をプロパティモデルへ書き戻せるようにする
            foreach (var param in instance.Params.Params)
            {
                param.ValueChangedByPlugin += Param_ValueChangedByPlugin;
            }

            // クリップの希望設定を取得し、毎フレーム変化するエフェクトかどうかを反映する
            UpdateClipPreferences(instance);
            return true;
        }

        /// <summary>
        /// プロパティの値が編集された際に EffectModel から呼び出されます (UI スレッド)
        /// 変更されたパラメータをプラグインへ InstanceChanged で通知します
        /// </summary>
        /// <param name="properties">エフェクトのプロパティ一覧</param>
        public void OnPropertyValuesEdited(IPropertyObject[] properties)
        {
            lock (Lock)
            {
                if (Instance == null || IsNotifyingPlugin)
                {
                    return;
                }
                LastProperties = properties;

                // 値を反映し、変化したパラメータを検出して通知する
                var before = Instance.Params.Params
                    .Where(p => p.Dimension > 0)
                    .Select(p => (Param: p, Values: (object?[])p.Values.Clone()))
                    .ToArray();
                OfxParamBridge.ApplyValues(Instance, properties, LastTime);

                IsNotifyingPlugin = true;
                PendingWritebacks = new List<KeyValuePair<string, object?>>();
                try
                {
                    var paramsChanged = false;
                    foreach (var (param, oldValues) in before)
                    {
                        if (!oldValues.SequenceEqual(param.Values))
                        {
                            paramsChanged = true;
                            // 編集されたパラメータ自身への書き戻しは抑止する (編集操作と競合するため)
                            NotifyingParamName = param.Name;
                            try
                            {
                                var status = Runtime.NotifyParamChanged(Definition.Plugin, Instance, param.Name, ToOfxTime(LastTime, LastFrameRate));
                                OfxLog.Info($"InstanceChanged({Definition.Plugin.Identifier}/{param.Name}): {status}");
                            }
                            finally
                            {
                                NotifyingParamName = null;
                            }
                        }
                    }

                    var clipsChanged = NotifyClipTargetChanges(properties);

                    if (paramsChanged || clipsChanged)
                    {
                        UpdateClipPreferences(Instance);
                    }
                }
                finally
                {
                    IsNotifyingPlugin = false;
                    FlushPendingWritebacks();
                }
            }
        }

        /// <summary>
        /// Undo/Redo による値の復元後に EffectModel から呼び出されます (UI スレッド)
        /// 変化したパラメータをプラグインへ通知して表示/有効状態などの内部状態を更新させます
        /// この間の値の書き戻しは破棄し、通知後にモデルの値 (復元された値) を再適用します
        /// </summary>
        /// <param name="properties">エフェクトのプロパティ一覧</param>
        public void OnPropertyValuesRestored(IPropertyObject[] properties)
        {
            lock (Lock)
            {
                if (Instance == null || IsNotifyingPlugin)
                {
                    return;
                }
                LastProperties = properties;

                var before = Instance.Params.Params
                    .Where(p => p.Dimension > 0)
                    .Select(p => (Param: p, Values: (object?[])p.Values.Clone()))
                    .ToArray();
                OfxParamBridge.ApplyValues(Instance, properties, LastTime);

                IsNotifyingPlugin = true;
                DropWritebacks = true;
                try
                {
                    foreach (var (param, oldValues) in before)
                    {
                        if (!oldValues.SequenceEqual(param.Values))
                        {
                            var status = Runtime.NotifyParamChanged(Definition.Plugin, Instance, param.Name, ToOfxTime(LastTime, LastFrameRate));
                            OfxLog.Info($"InstanceChanged(復元/{Definition.Plugin.Identifier}/{param.Name}): {status}");
                        }
                    }

                    NotifyClipTargetChanges(properties);
                }
                finally
                {
                    DropWritebacks = false;
                    IsNotifyingPlugin = false;
                }

                // 通知中にプラグインが値を書き換えた場合に備え、復元された値を再適用する
                OfxParamBridge.ApplyValues(Instance, properties, LastTime);
                UpdateClipPreferences(Instance);
            }
        }

        // プラグインのアクション完了後に、収集した書き戻しをまとめて通知する
        void FlushPendingWritebacks()
        {
            var pending = PendingWritebacks;
            PendingWritebacks = null;
            if (pending is { Count: > 0 })
            {
                PropertyValuesWriteback?.Invoke(this, new PropertyValuesWritebackEventArgs(pending, true));
            }
        }

        void Param_ValueChangedByPlugin(ParamInstance param)
        {
            if (DropWritebacks || param.Name == NotifyingParamName)
            {
                return;
            }

            var value = OfxParamBridge.ConvertToPropertyValue(param);
            if (value == null)
            {
                return;
            }

            if (PendingWritebacks != null)
            {
                // ユーザー操作起因のアクション実行中: アクション完了後にまとめて通知する
                PendingWritebacks.RemoveAll(pair => pair.Key == param.Name);
                PendingWritebacks.Add(new KeyValuePair<string, object?>(param.Name, value));
            }
            else
            {
                // レンダリング中のステータス更新など: 履歴に積まない表示更新として即時通知する
                PropertyValuesWriteback?.Invoke(this, new PropertyValuesWritebackEventArgs([new KeyValuePair<string, object?>(param.Name, value)], false));
            }
        }

        void OnButtonClicked(string paramName)
        {
            // ButtonPropertyControl のクリックから UI スレッド上で呼ばれる
            lock (Lock)
            {
                if (Instance == null || IsNotifyingPlugin)
                {
                    return;
                }
                if (LastProperties != null)
                {
                    OfxParamBridge.ApplyValues(Instance, LastProperties, LastTime);
                }

                IsNotifyingPlugin = true;
                PendingWritebacks = new List<KeyValuePair<string, object?>>();
                try
                {
                    var status = Runtime.NotifyParamChanged(Definition.Plugin, Instance, paramName, ToOfxTime(LastTime, LastFrameRate));
                    OfxLog.Info($"InstanceChanged({Definition.Plugin.Identifier}/{paramName}): {status}");
                    UpdateClipPreferences(Instance);
                }
                finally
                {
                    IsNotifyingPlugin = false;
                    FlushPendingWritebacks();
                }
            }
        }

        static void UpdateProjectProperties(EffectInstance instance, ICompositionObject composition)
        {
            var width = (double)composition.Width;
            var height = (double)composition.Height;
            instance.Properties.SetAll(OfxNames.ImageEffectPropProjectSize, width, height);
            instance.Properties.SetAll(OfxNames.ImageEffectPropProjectExtent, width, height);
            instance.Properties.SetAll(OfxNames.ImageEffectPropFrameRate, composition.FrameRate);
            foreach (var clip in instance.Clips.Values)
            {
                clip.Properties.SetAll(OfxNames.ImageEffectPropFrameRate, composition.FrameRate);
            }
        }

        static double ToOfxTime(Time time, double frameRate)
        {
            // OFX の時間はフレーム番号 (double)
            return Time.FromTime((double)time, frameRate).Frame;
        }

        (Vector4[]? Output, OfxStatus Status) RenderOnce(EffectInstance instance, double ofxTime, int width, int height, IOfxFrameProvider provider, bool useGpu, double renderScaleX, double renderScaleY)
        {
            if (useGpu && Definition.Metadata.IsSupportGpu)
            {
                var (glOutput, glStatus) = Runtime.RenderFrameGL(Definition.Plugin, instance, ofxTime, width, height, provider, renderScaleX, renderScaleY);
                if (glOutput != null)
                {
                    return (glOutput, glStatus);
                }
                OfxLog.Warn($"OpenGL レンダリングに失敗したため CPU で再実行します: {Definition.Plugin.Identifier}: {glStatus}");
            }
            return Runtime.RenderFrame(Definition.Plugin, instance, ofxTime, width, height, provider, renderScaleX, renderScaleY);
        }

        static ulong CalcRenderKey(EffectInstance instance, IReadOnlyDictionary<string, (Vector4[] Pixels, int Width, int Height)> frames, int width, int height, double ofxTime, bool useGpu, double renderScaleX, double renderScaleY)
        {
            var hash = new XxHash3();
            foreach (var (clipName, frame) in frames.OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                hash.Append(Encoding.UTF8.GetBytes(clipName));
                hash.Append(BitConverter.GetBytes(frame.Width));
                hash.Append(BitConverter.GetBytes(frame.Height));
                hash.Append(MemoryMarshal.AsBytes(frame.Pixels.AsSpan(0, frame.Width * frame.Height)));
            }
            hash.Append(BitConverter.GetBytes(ofxTime));
            hash.Append(BitConverter.GetBytes(width));
            hash.Append(BitConverter.GetBytes(height));
            hash.Append(BitConverter.GetBytes(useGpu));
            hash.Append(BitConverter.GetBytes(renderScaleX));
            hash.Append(BitConverter.GetBytes(renderScaleY));
            AppendParamValues(hash, instance);
            return hash.GetCurrentHashAsUInt64();
        }

        static ulong CalcParamsHash(EffectInstance instance)
        {
            var hash = new XxHash3();
            AppendParamValues(hash, instance);
            return hash.GetCurrentHashAsUInt64();
        }

        static void AppendParamValues(XxHash3 hash, EffectInstance instance)
        {
            foreach (var param in instance.Params.Params)
            {
                foreach (var value in param.Values)
                {
                    switch (value)
                    {
                        case int intValue:
                            hash.Append(BitConverter.GetBytes(intValue));
                            break;
                        case double doubleValue:
                            hash.Append(BitConverter.GetBytes(doubleValue));
                            break;
                        case string stringValue:
                            hash.Append(Encoding.UTF8.GetBytes(stringValue));
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 追加入力クリップに対応する UseLayerImageProperty の一覧を生成します
        /// </summary>
        static PropertyBase[] BuildClipProperties(EffectInstance instance)
        {
            return instance.ExtraInputClips
                .Select(clip => new UseLayerImageProperty(
                    ClipPropertyPrefix + clip.Name,
                    clip.Properties.GetOrDefault(OfxNames.PropLabel, 0) as string is { Length: > 0 } label ? label : clip.Name))
                .ToArray<PropertyBase>();
        }

        static UseLayerImageTarget GetClipTarget(IReadOnlyCollection<IPropertyObject> properties, string clipName, Time time)
        {
            var property = properties.FirstOrDefault(p => p.Id == ClipPropertyPrefix + clipName);
            return property?.GetValue(time) as UseLayerImageTarget ?? UseLayerImageTarget.Empty;
        }

        static NImage? ResolveLayerImage(UseLayerImageTarget target, ICompositionObject composition, Time time, double downSamplingRate, bool useGpu)
        {
            if (target == UseLayerImageTarget.Empty)
            {
                return null;
            }
            var layer = composition.GetLayer(target.LayerId);
            if (layer == null)
            {
                return null;
            }
            return target.ImageProcessType switch
            {
                LayerImageProcessType.Masked => layer.GetMaskedImage(time, downSamplingRate, useGpu),
                LayerImageProcessType.Effected => layer.GetEffectedImage(time, downSamplingRate, useGpu),
                _ => layer.GetRawImage(time, downSamplingRate, useGpu)
            };
        }

        /// <summary>
        /// 追加入力クリップへのレイヤー割り当ての変更を検出し、接続状態の更新とプラグインへの通知を行います
        /// (OnPropertyValuesEdited / OnPropertyValuesRestored の通知ブロック内から呼び出します)
        /// </summary>
        /// <param name="properties">エフェクトのプロパティ一覧</param>
        /// <returns>1 つ以上のクリップの割り当てが変化したかどうか</returns>
        bool NotifyClipTargetChanges(IPropertyObject[] properties)
        {
            var instance = Instance;
            if (instance == null)
            {
                return false;
            }
            var changed = false;
            foreach (var clip in instance.ExtraInputClips)
            {
                var target = GetClipTarget(properties, clip.Name, LastTime);
                var previous = LastClipTargets.TryGetValue(clip.Name, out var stored) ? stored : UseLayerImageTarget.Empty;
                if (previous == target)
                {
                    continue;
                }
                changed = true;
                LastClipTargets[clip.Name] = target;
                clip.SetConnected(target != UseLayerImageTarget.Empty);
                var status = Runtime.NotifyClipChanged(Definition.Plugin, instance, clip.Name, ToOfxTime(LastTime, LastFrameRate));
                OfxLog.Info($"InstanceChanged(クリップ {Definition.Plugin.Identifier}/{clip.Name}): {status}");
            }
            return changed;
        }

        /// <summary>
        /// GetClipPreferences を実行し、FrameVarying (毎フレーム再レンダリングの要否) を反映します
        /// GMIC の Animate Random Seed のように、パラメータ値によって FrameVarying が変わるプラグインに
        /// 追従するため、パラメータ変更後にも呼び出します
        /// </summary>
        /// <param name="instance">対象のインスタンス</param>
        void UpdateClipPreferences(EffectInstance instance)
        {
            LastPrefsParamsHash = CalcParamsHash(instance);
            var (outArgs, prefStatus) = Runtime.GetClipPreferences(Definition.Plugin, instance);
            using (outArgs)
            {
                if (prefStatus != OfxStatus.OK)
                {
                    return;
                }
                var frameVarying = outArgs.GetOrDefault(OfxNames.ImageEffectPropFrameVarying, 0) is int value && value != 0;
                if (frameVarying != IsRenderEveryFrame)
                {
                    IsRenderEveryFrame = frameVarying;
                    OfxLog.Info($"FrameVarying が変化しました: {Definition.Plugin.Identifier} -> {frameVarying}");
                }
            }
        }

        /// <summary>
        /// レンダリング対象の画像とレイヤー割り当て済みの追加入力を OFX のクリップとして供給するプロバイダ
        /// v1 では時間によらず現在のフレームを返します (temporal access は近似)
        /// </summary>
        sealed class AdapterFrameProvider : IOfxFrameProvider
        {
            OfxEffectAdapter Adapter { get; }

            IReadOnlyDictionary<string, (Vector4[] Pixels, int Width, int Height)> Frames { get; }

            public AdapterFrameProvider(OfxEffectAdapter adapter, IReadOnlyDictionary<string, (Vector4[] Pixels, int Width, int Height)> frames)
            {
                Adapter = adapter;
                Frames = frames;
            }

            public (Vector4[] Pixels, int Width, int Height)? GetSourceFrame(string clipName, double time)
            {
                if (!Frames.TryGetValue(clipName, out var frame))
                {
                    return null;
                }
                if (Math.Abs(time - (Adapter.Instance?.CurrentTime ?? time)) > 0.5 && !Adapter.TemporalAccessWarned)
                {
                    Adapter.TemporalAccessWarned = true;
                    OfxLog.Warn($"{clipName} への時間指定アクセスは現在のフレームで近似されます (v1 の制限)");
                }
                return frame;
            }

            public (int Width, int Height)? GetSourceBounds(string clipName, double time)
            {
                return Frames.TryGetValue(clipName, out var frame) ? (frame.Width, frame.Height) : null;
            }
        }
    }
}
