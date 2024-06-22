using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Resource;
using NiVE3.SourceGenerator.LanguageResourceGenerator;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;

namespace NiVE3.PresetPlugin.Resource
{
    [MarkupableResourceDictionary, HasLanguageKey]
    partial class LanguageResourceDictionary : LanguageResourceDictionaryBase
    {
        public static LanguageResourceDictionary Dictionary { get; }

        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        // Effects

        [DefaultValue("ポイント制御")]
        public const string ExpressionControl_PointControl_Name = nameof(ExpressionControl_PointControl_Name);

        [DefaultValue("エクスプレッションで使用するポイント制御")]
        public const string ExpressionControl_PointControl_Description = nameof(ExpressionControl_PointControl_Description);

        [LanguageKey, DefaultValue("ポイント")]
        public const string ExpressionControl_PointControl_PropertyName = nameof(ExpressionControl_PointControl_PropertyName);

        [DefaultValue("スライダー制御")]
        public const string ExpressionControl_SliderControl_Name = nameof(ExpressionControl_SliderControl_Name);

        [DefaultValue("エクスプレッションで使用するスライダー制御")]
        public const string ExpressionControl_SliderControl_Description = nameof(ExpressionControl_SliderControl_Description);

        [LanguageKey, DefaultValue("スライダー")]
        public const string ExpressionControl_SliderControl_PropertyName = nameof(ExpressionControl_SliderControl_PropertyName);

        // BoxBlur

        [DefaultValue("ボックスブラー")]
        public const string Blur_BoxBlur_Name = nameof(Blur_BoxBlur_Name);

        [DefaultValue("ボックスブラーを適用します")]
        public const string Blur_BoxBlur_Description = nameof(Blur_BoxBlur_Description);

        [LanguageKey, DefaultValue("半径")]
        public const string Blur_BoxBlur_Amount = nameof(Blur_BoxBlur_Amount);

        [LanguageKey, DefaultValue("繰り返し")]
        public const string Blur_BoxBlur_Repeat = nameof(Blur_BoxBlur_Repeat);

        [LanguageKey, DefaultValue("方向")]
        public const string Blur_BoxBlur_Direction = nameof(Blur_BoxBlur_Direction);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Blur_BoxBlur_EdgeRepeatMode = nameof(Blur_BoxBlur_EdgeRepeatMode);

        // PixelSort

        [DefaultValue("ピクセルソート")]
        public const string Stylize_PixelSort_Name = nameof(Stylize_PixelSort_Name);

        [DefaultValue("画像のピクセルを閾値を基準にソートします")]
        public const string Stylize_PixelSort_Description = nameof(Stylize_PixelSort_Description);

        [LanguageKey, DefaultValue("閾値")]
        public const string Stylize_PixelSort_Threshold = nameof(Stylize_PixelSort_Threshold);

        [LanguageKey, DefaultValue("モード")]
        public const string Stylize_PixelSort_Mode = nameof(Stylize_PixelSort_Mode);

        [LanguageKey, DefaultValue("ソート")]
        public const string Stylize_PixelSort_Sort = nameof(Stylize_PixelSort_Sort);

        [LanguageKey, DefaultValue("チャンネル")]
        public const string Stylize_PixelSort_Channel = nameof(Stylize_PixelSort_Channel);

        // Dynamics

        [DefaultValue("ダイナミクス")]
        public const string Audio_Dynamics_Name = nameof(Audio_Dynamics_Name);

        [DefaultValue("音声のダイナミクスを操作します")]
        public const string Audio_Dynamics_Description = nameof(Audio_Dynamics_Description);

        [LanguageKey, DefaultValue("タイプ")]
        public const string Audio_Dynamics_DynamicsProcessorType = nameof(Audio_Dynamics_DynamicsProcessorType);

        [LanguageKey, DefaultValue("スレッシュホールド")]
        public const string Audio_Dynamics_Threshold = nameof(Audio_Dynamics_Threshold);

        [LanguageKey, DefaultValue("レシオ")]
        public const string Audio_Dynamics_Ratio = nameof(Audio_Dynamics_Ratio);

        [LanguageKey, DefaultValue("ゲイン")]
        public const string Audio_Dynamics_Gain = nameof(Audio_Dynamics_Gain);

        [LanguageKey, DefaultValue("アタック")]
        public const string Audio_Dynamics_Attack = nameof(Audio_Dynamics_Attack);

        [LanguageKey, DefaultValue("リリース")]
        public const string Audio_Dynamics_Release = nameof(Audio_Dynamics_Release);

        // ParametricEqualizer

        [DefaultValue("パラメトリックイコライザ")]
        public const string Audio_ParametricEqualizer_Name = nameof(Audio_ParametricEqualizer_Name);

        [DefaultValue("音声にイコライザを適用します")]
        public const string Audio_ParametricEqualizer_Description = nameof(Audio_ParametricEqualizer_Description);

        [LanguageKey, DefaultValue("バンド")]
        public const string Audio_ParametricEqualizer_BandPoints = nameof(Audio_ParametricEqualizer_BandPoints);

        [LanguageKey, DefaultValue("ピーク")]
        public const string Audio_ParametricEqualizer_BandPoint_Peak = nameof(Audio_ParametricEqualizer_BandPoint_Peak);

        [LanguageKey, DefaultValue("ハイパス")]
        public const string Audio_ParametricEqualizer_BandPoint_HighPass = nameof(Audio_ParametricEqualizer_BandPoint_HighPass);

        [LanguageKey, DefaultValue("ローパス")]
        public const string Audio_ParametricEqualizer_BandPoint_LowPass = nameof(Audio_ParametricEqualizer_BandPoint_LowPass);

        [LanguageKey, DefaultValue("ハイシェルフ")]
        public const string Audio_ParametricEqualizer_BandPoint_HighShelf = nameof(Audio_ParametricEqualizer_BandPoint_HighShelf);

        [LanguageKey, DefaultValue("ローシェルフ")]
        public const string Audio_ParametricEqualizer_BandPoint_LowShelf = nameof(Audio_ParametricEqualizer_BandPoint_LowShelf);

        [LanguageKey, DefaultValue("周波数")]
        public const string Audio_ParametricEqualizer_Frequency = nameof(Audio_ParametricEqualizer_Frequency);

        [LanguageKey, DefaultValue("Q")]
        public const string Audio_ParametricEqualizer_Q = nameof(Audio_ParametricEqualizer_Q);

        [LanguageKey, DefaultValue("ゲイン")]
        public const string Audio_ParametricEqualizer_Gain = nameof(Audio_ParametricEqualizer_Gain);

        // Renderers

        [DefaultValue("デフォルトレンダラ")]
        public const string Renderer_DefaultRenderer_Name = nameof(Renderer_DefaultRenderer_Name);

        [DefaultValue("NiVE標準のレンダラ")]
        public const string Renderer_DefaultRenderer_Description = nameof(Renderer_DefaultRenderer_Description);

        // Outputs

        [DefaultValue("AVI出力")]
        public const string AviOutput_Name = nameof(AviOutput_Name);

        [DefaultValue("動画をAVIで出力します")]
        public const string AviOutput_Description = nameof(AviOutput_Description);

        [DefaultValue("画像シーケンス出力")]
        public const string SequentialImageOutput_Name = nameof(SequentialImageOutput_Name);

        [DefaultValue("動画を連番画像として出力します")]
        public const string SequentialImageOutput_Description = nameof(SequentialImageOutput_Description);

        [DefaultValue("Wave出力")]
        public const string WaveOutput_Name = nameof(WaveOutput_Name);

        [DefaultValue("音声をPCM形式で出力します")]
        public const string WaveOutput_Description = nameof(WaveOutput_Description);

        // Output setting views

        [ShowInMarkup, DefaultValue("ビデオ出力")]
        public const string AviOutputSettingView_Group_Video = nameof(AviOutputSettingView_Group_Video);

        [ShowInMarkup, DefaultValue("チャンネル:")]
        public const string AviOutputSettingView_Group_Video_OutputChannel = nameof(AviOutputSettingView_Group_Video_OutputChannel);

        [ShowInMarkup, DefaultValue("コーデック:")]
        public const string AviOutputSettingView_Group_Video_Codec = nameof(AviOutputSettingView_Group_Video_Codec);

        [ShowInMarkup, DefaultValue("品質")]
        public const string AviOutputSettingView_Group_Video_Quality = nameof(AviOutputSettingView_Group_Video_Quality);

        [ShowInMarkup, DefaultValue("キーフレームを使用する")]
        public const string AviOutputSettingView_Group_Video_UseKeyFrame = nameof(AviOutputSettingView_Group_Video_UseKeyFrame);

        [ShowInMarkup, DefaultValue("コーデックの設定")]
        public const string AviOutputSettingView_Group_Video_ConfigureCodec = nameof(AviOutputSettingView_Group_Video_ConfigureCodec);

        [ShowInMarkup, DefaultValue("オーディオ出力")]
        public const string AviOutputSettingView_Group_Audio = nameof(AviOutputSettingView_Group_Audio);

        [ShowInMarkup, DefaultValue("サンプリングレート:")]
        public const string AviOutputSettingView_Group_Audio_SamplingRate = nameof(AviOutputSettingView_Group_Audio_SamplingRate);

        [ShowInMarkup, DefaultValue("ビット:")]
        public const string AviOutputSettingView_Group_Audio_BitsPerSample = nameof(AviOutputSettingView_Group_Audio_BitsPerSample);

        [ShowInMarkup, DefaultValue("サンプリングレート:")]
        public const string WaveOutputSettingView_Group_Audio_SamplingRate = nameof(WaveOutputSettingView_Group_Audio_SamplingRate);

        [ShowInMarkup, DefaultValue("ビット:")]
        public const string WaveOutputSettingView_Group_Audio_BitsPerSample = nameof(WaveOutputSettingView_Group_Audio_BitsPerSample);

        // enum

        [DefaultValue("水平&垂直")]
        public const string BlurDirection_HorizontalAndVertical = nameof(BlurDirection_HorizontalAndVertical);

        [DefaultValue("水平")]
        public const string BlurDirection_Horizontal = nameof(BlurDirection_Horizontal);

        [DefaultValue("垂直")]
        public const string BlurDirection_Vertical = nameof(BlurDirection_Vertical);

        [DefaultValue("なし")]
        public const string EdgeRepeatMode_None = nameof(EdgeRepeatMode_None);

        [DefaultValue("エッジのみ繰り返し")]
        public const string EdgeRepeatMode_Wrap = nameof(EdgeRepeatMode_Wrap);

        [DefaultValue("繰り返し")]
        public const string EdgeRepeatMode_Repeat = nameof(EdgeRepeatMode_Repeat);

        [DefaultValue("鏡面繰り返し")]
        public const string EdgeRepeatMode_Mirror = nameof(EdgeRepeatMode_Mirror);

        [DefaultValue("明るさ")]
        public const string ThresholdMode_Brightness = nameof(ThresholdMode_Brightness);

        [DefaultValue("暗さ")]
        public const string ThresholdMode_Darkness = nameof(ThresholdMode_Darkness);

        [DefaultValue("縦")]
        public const string SortMode_Vertical = nameof(SortMode_Vertical);

        [DefaultValue("横")]
        public const string SortMode_Horizontal = nameof(SortMode_Horizontal);

        [DefaultValue("RGB")]
        public const string SortTargetChannel_RGB = nameof(SortTargetChannel_RGB);

        [DefaultValue("赤")]
        public const string SortTargetChannel_R = nameof(SortTargetChannel_R);

        [DefaultValue("緑")]
        public const string SortTargetChannel_G = nameof(SortTargetChannel_G);

        [DefaultValue("青")]
        public const string SortTargetChannel_B = nameof(SortTargetChannel_B);

        [DefaultValue("アルファ")]
        public const string SortTargetChannel_A = nameof(SortTargetChannel_A);

        [DefaultValue("コンプレッサー")]
        public const string DynamicsMode_Compressor = nameof(DynamicsMode_Compressor);

        [DefaultValue("リミッター")]
        public const string DynamicsMode_Limiter = nameof(DynamicsMode_Limiter);

        [DefaultValue("エクスパンダー")]
        public const string DynamicsMode_Expander = nameof(DynamicsMode_Expander);

        [DefaultValue("ノイズゲート")]
        public const string DynamicsMode_NoiseGate = nameof(DynamicsMode_NoiseGate);

        [DefaultValue("RGB")]
        public const string OutputChannel_Rgb = nameof(OutputChannel_Rgb);

        [DefaultValue("RGBA")]
        public const string OutputChannel_Rgba = nameof(OutputChannel_Rgba);

        [DefaultValue("アルファのみ")]
        public const string OutputChannel_AlphaOnly = nameof(OutputChannel_AlphaOnly);

        // unit

        [LanguageKey, DefaultValue("ms")]
        public const string Unit_MilliSecond = nameof(Unit_MilliSecond);

        [LanguageKey, DefaultValue("dB")]
        public const string Unit_Decibel = nameof(Unit_Decibel);

        [LanguageKey, DefaultValue("Hz")]
        public const string Unit_Hertz = nameof(Unit_Hertz);

        static LanguageResourceDictionary()
        {
            LanguageKeys = typeof(LanguageResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
                .Select(f => (f.Name, f.GetCustomAttribute<DefaultValueAttribute>()))
                .Where(t => t.Item2 != null)
                .ToDictionary(t => t.Name, t => Tuple.Create(t.Item2!.DefaultValue, Version.Parse(t.Item2!.FromVersion)));

            Dictionary = [];
        }

        public LanguageResourceDictionary()
        {
            Reload();
        }

        protected override void Reload()
        {
            // TODO: 言語情報読み込み&バージョン比較後適用
            foreach (var (key, (defaultValue, version)) in LanguageKeys)
            {
                this[key] = defaultValue;
            }
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        sealed class DefaultValueAttribute : Attribute
        {
            public DefaultValueAttribute(string defaultValue)
            {
                DefaultValue = defaultValue;
            }

            public string DefaultValue { get; }

            public string FromVersion { get; set; } = "0.0.0.0";
        }
    }
}
