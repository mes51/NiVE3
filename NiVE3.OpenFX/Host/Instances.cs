using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// パラメータの値の種別
    /// </summary>
    public enum OfxParamValueKind
    {
        None,
        Int,
        Double,
        String
    }

    /// <summary>
    /// OFX パラメータ型に関するユーティリティ
    /// </summary>
    public static class OfxParamTypes
    {
        /// <summary>
        /// パラメータ型の値の種別を取得します
        /// </summary>
        /// <param name="paramType">OFX のパラメータ型名</param>
        public static OfxParamValueKind GetValueKind(string paramType)
        {
            return paramType switch
            {
                OfxNames.ParamTypeInteger or OfxNames.ParamTypeInteger2D or OfxNames.ParamTypeInteger3D
                    or OfxNames.ParamTypeBoolean or OfxNames.ParamTypeChoice => OfxParamValueKind.Int,
                OfxNames.ParamTypeDouble or OfxNames.ParamTypeDouble2D or OfxNames.ParamTypeDouble3D
                    or OfxNames.ParamTypeRGB or OfxNames.ParamTypeRGBA => OfxParamValueKind.Double,
                OfxNames.ParamTypeString or OfxNames.ParamTypeCustom or OfxNames.ParamTypeStrChoice => OfxParamValueKind.String,
                _ => OfxParamValueKind.None
            };
        }

        /// <summary>
        /// パラメータ値を安全に double へ変換します
        /// (Sapphire 等、数値パラメータのプロパティに文字列を設定するプラグインがあるため、例外を投げない)
        /// </summary>
        /// <param name="value">変換する値</param>
        /// <returns>変換された値。変換できない場合は 0</returns>
        public static double ToDouble(object? value)
        {
            return value switch
            {
                double d => d,
                int i => i,
                string s => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0.0,
                _ => 0.0
            };
        }

        /// <summary>
        /// パラメータ値を安全に int へ変換します
        /// </summary>
        /// <param name="value">変換する値</param>
        /// <returns>変換された値。変換できない場合は 0</returns>
        public static int ToInt(object? value)
        {
            return value switch
            {
                int i => i,
                double d => (int)Math.Round(d),
                string s => int.TryParse(s, out var parsed) ? parsed : 0,
                _ => 0
            };
        }

        /// <summary>
        /// パラメータ型の次元数を取得します
        /// </summary>
        /// <param name="paramType">OFX のパラメータ型名</param>
        public static int GetDimension(string paramType)
        {
            return paramType switch
            {
                OfxNames.ParamTypeInteger2D or OfxNames.ParamTypeDouble2D => 2,
                OfxNames.ParamTypeInteger3D or OfxNames.ParamTypeDouble3D or OfxNames.ParamTypeRGB => 3,
                OfxNames.ParamTypeRGBA => 4,
                OfxNames.ParamTypeGroup or OfxNames.ParamTypePage or OfxNames.ParamTypePushButton => 0,
                _ => 1
            };
        }
    }

    /// <summary>
    /// パラメータのインスタンス。現在値を保持します
    /// </summary>
    public sealed unsafe class ParamInstance : IDisposable
    {
        public string Name { get; }

        public string ParamType { get; }

        public OfxParamValueKind ValueKind { get; }

        public int Dimension { get; }

        /// <summary>
        /// このパラメータの OFX ハンドル (OfxParamHandle)
        /// </summary>
        public nint Handle { get; }

        /// <summary>
        /// このインスタンス固有のプロパティセット (プラグインが実行時に Secret/Enabled 等を書き換える)
        /// </summary>
        public PropertySet Properties { get; }

        /// <summary>
        /// 現在値 (次元ごと)。int / double / string を保持します
        /// </summary>
        public object?[] Values { get; }

        /// <summary>
        /// プラグインが paramSetValue で値を書き換えた際に発生します
        /// </summary>
        public event Action<ParamInstance>? ValueChangedByPlugin;

        /// <summary>
        /// プラグインが Secret / Enabled プロパティを書き換えた際に発生します (動的な UI 状態切替)
        /// </summary>
        public event Action<ParamInstance>? UiStateChanged;

        // paramGetValue が返す文字列の生存期間管理
        Dictionary<int, nint> NativeStrings { get; } = new Dictionary<int, nint>();

        public ParamInstance(ParamDescriptor descriptor)
        {
            Name = descriptor.Name;
            ParamType = descriptor.ParamType;
            ValueKind = OfxParamTypes.GetValueKind(ParamType);
            Dimension = OfxParamTypes.GetDimension(ParamType);
            Handle = HandleTable.Alloc(this);
            Properties = descriptor.Properties.Clone($"ParamInstance:{Name}");
            Properties.SingleValueChanged = key =>
            {
                if (key is OfxNames.ParamPropSecret or OfxNames.ParamPropEnabled)
                {
                    UiStateChanged?.Invoke(this);
                }
            };

            Values = new object?[Dimension];
            for (var i = 0; i < Dimension; i++)
            {
                if (Properties.TryGet(OfxNames.ParamPropDefault, i, out var value))
                {
                    Values[i] = value;
                }
                else
                {
                    Values[i] = ValueKind switch
                    {
                        OfxParamValueKind.Int => 0,
                        OfxParamValueKind.Double => 0.0,
                        OfxParamValueKind.String => "",
                        _ => null
                    };
                }
            }

            // StrChoice の既定値が列挙値 (ChoiceEnum) にない場合は先頭の列挙値を使う (スペック推奨の動作)
            if (ParamType == OfxNames.ParamTypeStrChoice)
            {
                var enumCount = Properties.GetDimension(OfxNames.ParamPropChoiceEnum);
                if (enumCount > 0)
                {
                    var enums = Enumerable.Range(0, enumCount).Select(i => Properties.GetOrDefault(OfxNames.ParamPropChoiceEnum, i) as string).ToArray();
                    if (Values[0] is not string current || !enums.Contains(current))
                    {
                        Values[0] = enums[0] ?? "";
                    }
                }
            }
        }

        /// <summary>
        /// ホスト側からパラメータの値を設定します
        /// </summary>
        /// <param name="index">次元のインデックス</param>
        /// <param name="value">設定する値</param>
        public void SetValue(int index, object? value)
        {
            Values[index] = value;
            FreeNativeString(index);
        }

        /// <summary>
        /// paramGetValue 系の呼び出しに応答し、ネイティブポインタへ値を書き込みます
        /// </summary>
        /// <param name="slots">書き込み先ポインタ (次元数分)</param>
        /// <returns>ステータス</returns>
        public OfxStatus WriteValuesTo(ReadOnlySpan<nint> slots)
        {
            for (var i = 0; i < Dimension; i++)
            {
                var slot = slots[i];
                if (slot == 0)
                {
                    return OfxStatus.ErrValue;
                }

                switch (ValueKind)
                {
                    case OfxParamValueKind.Int:
                        *(int*)slot = OfxParamTypes.ToInt(Values[i]);
                        break;
                    case OfxParamValueKind.Double:
                        *(double*)slot = OfxParamTypes.ToDouble(Values[i]);
                        break;
                    case OfxParamValueKind.String:
                        *(byte**)slot = (byte*)GetNativeString(i);
                        break;
                }
            }
            return OfxStatus.OK;
        }

        /// <summary>
        /// paramSetValue 系の呼び出しに応答し、可変長引数のレジスタ/スタック値を型に応じて解釈して格納します
        /// (Win x64 の可変長引数 ABI では浮動小数点値も汎用レジスタに複製されるため、nint からの再解釈で取得できる)
        /// </summary>
        /// <param name="slots">受け取った引数の値 (次元数分)</param>
        /// <returns>ステータス</returns>
        public OfxStatus ReadValuesFrom(ReadOnlySpan<nint> slots)
        {
            for (var i = 0; i < Dimension; i++)
            {
                switch (ValueKind)
                {
                    case OfxParamValueKind.Int:
                        Values[i] = (int)slots[i];
                        break;
                    case OfxParamValueKind.Double:
                        Values[i] = BitConverter.Int64BitsToDouble(slots[i]);
                        break;
                    case OfxParamValueKind.String:
                        Values[i] = Marshal.PtrToStringUTF8(slots[i]) ?? "";
                        FreeNativeString(i);
                        break;
                }
            }
            ValueChangedByPlugin?.Invoke(this);
            return OfxStatus.OK;
        }

        nint GetNativeString(int index)
        {
            if (!NativeStrings.TryGetValue(index, out var ptr))
            {
                ptr = Marshal.StringToCoTaskMemUTF8(Values[index] as string ?? "");
                NativeStrings[index] = ptr;
            }
            return ptr;
        }

        void FreeNativeString(int index)
        {
            if (NativeStrings.Remove(index, out var ptr))
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        public void Dispose()
        {
            foreach (var ptr in NativeStrings.Values)
            {
                Marshal.FreeCoTaskMem(ptr);
            }
            NativeStrings.Clear();
            Properties.Dispose();
            HandleTable.Free(Handle);
        }
    }

    /// <summary>
    /// パラメータインスタンスの集合 (OfxParamSetHandle に対応)
    /// </summary>
    public sealed class ParamSetInstance : IDisposable
    {
        public nint Handle { get; }

        public PropertySet Properties { get; }

        public List<ParamInstance> Params { get; } = new List<ParamInstance>();

        public ParamSetInstance(string name, ParamSetDescriptor descriptor)
        {
            Handle = HandleTable.Alloc(this);
            Properties = descriptor.Properties.Clone($"{name}.ParamSetInstance");
            foreach (var paramDescriptor in descriptor.Params)
            {
                Params.Add(new ParamInstance(paramDescriptor));
            }
        }

        /// <summary>
        /// 名前からパラメータインスタンスを取得します
        /// </summary>
        /// <param name="name">パラメータ名</param>
        /// <returns>パラメータインスタンス。存在しない場合は null</returns>
        public ParamInstance? Find(string name)
        {
            return Params.FirstOrDefault(p => p.Name == name);
        }

        public void Dispose()
        {
            Properties.Dispose();
            foreach (var param in Params)
            {
                param.Dispose();
            }
            HandleTable.Free(Handle);
        }
    }

    /// <summary>
    /// クリップのインスタンス
    /// </summary>
    public sealed class ClipInstance : IDisposable
    {
        public string Name { get; }

        /// <summary>
        /// プラグインが定義した順序 (メイン入力クリップの決定に使用)
        /// </summary>
        public int Order { get; }

        public nint Handle { get; }

        public PropertySet Properties { get; }

        /// <summary>
        /// このクリップを所有するエフェクトインスタンス
        /// </summary>
        public EffectInstance? Owner { get; internal set; }

        /// <summary>
        /// 省略可能なクリップかどうか
        /// </summary>
        public bool IsOptional => Properties.GetOrDefault(OfxNames.ImageClipPropOptional, 0) is int optional && optional != 0;

        public ClipInstance(ClipDescriptor descriptor, OfxProjectSettings settings)
        {
            Name = descriptor.Name;
            Order = descriptor.Order;
            Handle = HandleTable.Alloc(this);
            Properties = descriptor.Properties.Clone($"ClipInstance:{Name}");

            Properties.SetAll(OfxNames.ImageEffectPropComponents, OfxNames.ComponentRGBA);
            Properties.SetAll(OfxNames.ImageEffectPropPixelDepth, OfxNames.BitDepthFloat);
            Properties.SetAll(OfxNames.ImageClipPropUnmappedComponents, OfxNames.ComponentRGBA);
            Properties.SetAll(OfxNames.ImageClipPropUnmappedPixelDepth, OfxNames.BitDepthFloat);
            Properties.SetAll(OfxNames.ImageEffectPropPreMultiplication, OfxNames.ImageUnPreMultiplied);
            Properties.SetAll(OfxNames.ImagePropPixelAspectRatio, 1.0);
            Properties.SetAll(OfxNames.ImageEffectPropFrameRate, settings.FrameRate);
            Properties.SetAll(OfxNames.ImageEffectPropFrameRange, 0.0, settings.DurationFrames);
            Properties.SetAll(OfxNames.ImageEffectPropUnmappedFrameRate, settings.FrameRate);
            Properties.SetAll(OfxNames.ImageEffectPropUnmappedFrameRange, 0.0, settings.DurationFrames);
            Properties.SetAll(OfxNames.ImageClipPropFieldOrder, OfxNames.ImageFieldNone);
            // Connected はメイン入力の決定後に EffectInstance が設定する
            Properties.SetAll(OfxNames.ImageClipPropConnected, 0);
            Properties.SetAll(OfxNames.ImageClipPropContinuousSamples, 0);
        }

        /// <summary>
        /// クリップの接続状態を設定します
        /// </summary>
        /// <param name="connected">接続されているかどうか</param>
        public void SetConnected(bool connected)
        {
            Properties.SetAll(OfxNames.ImageClipPropConnected, connected ? 1 : 0);
        }

        public void Dispose()
        {
            Properties.Dispose();
            HandleTable.Free(Handle);
        }
    }

    /// <summary>
    /// エフェクトのインスタンスが動作するプロジェクト (コンポジション) の設定
    /// </summary>
    public sealed record OfxProjectSettings
    {
        public double Width { get; init; } = 1920.0;

        public double Height { get; init; } = 1080.0;

        public double FrameRate { get; init; } = 30.0;

        /// <summary>
        /// 長さ (フレーム数)
        /// </summary>
        public double DurationFrames { get; init; } = 300.0;
    }

    /// <summary>
    /// エフェクトのインスタンス
    /// </summary>
    public sealed class EffectInstance : IDisposable
    {
        public nint Handle { get; }

        public PropertySet Properties { get; }

        public ParamSetInstance Params { get; }

        public Dictionary<string, ClipInstance> Clips { get; } = new Dictionary<string, ClipInstance>();

        public OfxProjectSettings Settings { get; }

        /// <summary>
        /// タイムライン上の現在時間 (フレーム)。TimeLine Suite が返します
        /// </summary>
        public double CurrentTime { get; set; }

        /// <summary>
        /// 現在のレンダリングのスケール (ダウンサンプリング時は 1 未満)
        /// </summary>
        public (double X, double Y) CurrentRenderScale { get; set; } = (1.0, 1.0);

        /// <summary>
        /// クリップの画像を供給するプロバイダ。レンダリング前に設定します
        /// </summary>
        public IOfxFrameProvider? FrameProvider { get; set; }

        /// <summary>
        /// メイン入力クリップ (レイヤーの画像を割り当てるクリップ) の名前。
        /// "Source" が存在すればそれ、無ければ定義順で最初の必須入力クリップ、
        /// それも無ければ定義順で最初の入力クリップです (Generator 等の入力なしは null)
        /// </summary>
        public string? MainInputClipName { get; }

        /// <summary>
        /// メイン入力・Output 以外の入力クリップの一覧 (定義順)。
        /// UseLayerImageProperty で他レイヤーの画像を割り当てる対象です
        /// </summary>
        public IEnumerable<ClipInstance> ExtraInputClips
            => Clips.Values.Where(c => c.Name != "Output" && c.Name != MainInputClipName).OrderBy(c => c.Order);

        static string? SelectMainInputClip(IEnumerable<ClipInstance> clips)
        {
            var inputs = clips.Where(c => c.Name != "Output").OrderBy(c => c.Order).ToArray();
            return inputs.FirstOrDefault(c => c.Name == "Source")?.Name
                ?? inputs.FirstOrDefault(c => !c.IsOptional)?.Name
                ?? inputs.FirstOrDefault()?.Name;
        }

        /// <summary>
        /// 現在のレンダリングの出力先画像。Render アクションの間だけ設定されます
        /// </summary>
        public OfxImage? OutputImage { get; set; }

        /// <summary>
        /// プラグインが clipGetImage で取得した入力画像の一覧 (後始末用)
        /// </summary>
        internal List<OfxImage> FetchedImages { get; } = new List<OfxImage>();

        /// <summary>
        /// プラグインが clipLoadTexture で取得したテクスチャの一覧 (後始末用)
        /// </summary>
        internal List<GL.OfxGlTexture> LoadedGlTextures { get; } = new List<GL.OfxGlTexture>();

        /// <summary>
        /// 現在の OpenCL レンダリングの出力先画像。OpenCL の Render アクションの間だけ設定されます
        /// (設定されている間、clipGetImage は cl_mem バッファの画像を返します)
        /// </summary>
        public CL.OfxClImage? OutputClImage { get; set; }

        /// <summary>
        /// プラグインが clipGetImage で取得した OpenCL 入力画像の一覧 (後始末用)
        /// </summary>
        internal List<CL.OfxClImage> FetchedClImages { get; } = new List<CL.OfxClImage>();

        /// <summary>
        /// 現在の CUDA レンダリングの出力先画像。CUDA の Render アクションの間だけ設定されます
        /// (設定されている間、clipGetImage は CUDA デバイスポインタの画像を返します)
        /// </summary>
        public Cuda.OfxCudaImage? OutputCudaImage { get; set; }

        /// <summary>
        /// プラグインが clipGetImage で取得した CUDA 入力画像の一覧 (後始末用)
        /// </summary>
        internal List<Cuda.OfxCudaImage> FetchedCudaImages { get; } = new List<Cuda.OfxCudaImage>();

        /// <summary>
        /// 現在の GL レンダリングの出力先テクスチャ。GL Render アクションの間だけ設定されます
        /// (openfx-misc 系プラグインは clipLoadTexture(Output) でこれを取得し、自前の FBO で描画する)
        /// </summary>
        public GL.OfxGlTexture? OutputGlTexture { get; set; }

        /// <summary>
        /// OpenGLContextAttached アクションを通知済みかどうか
        /// (attach/detach は対で呼ぶ必要があるため、インスタンス破棄時に Detached を送るのに使用します)
        /// </summary>
        public bool GlContextAttached { get; set; }

        public EffectInstance(string name, EffectDescriptor contextDescriptor, string context, OfxProjectSettings settings)
        {
            Handle = HandleTable.Alloc(this);
            Settings = settings;
            Properties = contextDescriptor.Properties.Clone($"{name}.Instance");
            Params = new ParamSetInstance(name, contextDescriptor.Params);
            foreach (var clipDescriptor in contextDescriptor.Clips.Values)
            {
                Clips[clipDescriptor.Name] = new ClipInstance(clipDescriptor, settings) { Owner = this };
            }

            // メイン入力クリップ (レイヤー画像を割り当てる) を決定し、Output と共に接続扱いにする
            // 追加入力クリップの接続状態は、レイヤー画像の割り当てに応じてアダプタが更新する
            MainInputClipName = SelectMainInputClip(Clips.Values);
            foreach (var clip in Clips.Values)
            {
                clip.SetConnected(clip.Name == "Output" || clip.Name == MainInputClipName);
            }

            Properties.SetAll(OfxNames.PropType, OfxNames.TypeImageEffectInstance);
            Properties.SetAll(OfxNames.ImageEffectPropContext, context);
            Properties.SetAll(OfxNames.PropInstanceData, (nint)0);
            Properties.SetAll(OfxNames.PropIsInteractive, 0);
            Properties.SetAll(OfxNames.ImageEffectPropProjectSize, settings.Width, settings.Height);
            Properties.SetAll(OfxNames.ImageEffectPropProjectOffset, 0.0, 0.0);
            Properties.SetAll(OfxNames.ImageEffectPropProjectExtent, settings.Width, settings.Height);
            Properties.SetAll(OfxNames.ImageEffectPropProjectPixelAspectRatio, 1.0);
            Properties.SetAll(OfxNames.ImageEffectInstancePropEffectDuration, settings.DurationFrames);
            Properties.SetAll(OfxNames.ImageEffectPropFrameRate, settings.FrameRate);
            Properties.SetAll(OfxNames.ImageEffectInstancePropSequentialRender, 0);
        }

        public void Dispose()
        {
            foreach (var image in FetchedImages.Where(i => !i.Disposed))
            {
                image.Dispose();
            }
            FetchedImages.Clear();
            OutputImage?.Dispose();
            OutputImage = null;
            foreach (var image in FetchedClImages.Where(i => !i.Disposed))
            {
                image.Dispose();
            }
            FetchedClImages.Clear();
            OutputClImage?.Dispose();
            OutputClImage = null;
            foreach (var image in FetchedCudaImages.Where(i => !i.Disposed))
            {
                image.Dispose();
            }
            FetchedCudaImages.Clear();
            OutputCudaImage?.Dispose();
            OutputCudaImage = null;

            Properties.Dispose();
            Params.Dispose();
            foreach (var clip in Clips.Values)
            {
                clip.Dispose();
            }
            HandleTable.Free(Handle);
        }
    }
}
