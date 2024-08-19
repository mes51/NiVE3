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

        // BrokenJpeg

        [DefaultValue("BrokenJpeg")]
        public const string Stylize_BrokenJpeg_Name = nameof(Stylize_BrokenJpeg_Name);

        [DefaultValue("画像をJPEGとして圧縮、データを壊す事でグリッチ様の効果を生成します")]
        public const string Stylize_BrokenJpeg_Description = nameof(Stylize_BrokenJpeg_Description);

        [LanguageKey, DefaultValue("圧縮品質")]
        public const string Stylize_BrokenJpeg_CompressQuality = nameof(Stylize_BrokenJpeg_CompressQuality);

        [LanguageKey, DefaultValue("色空間")]
        public const string Stylize_BrokenJpeg_ColorSpace = nameof(Stylize_BrokenJpeg_ColorSpace);

        [LanguageKey, DefaultValue("背景色")]
        public const string Stylize_BrokenJpeg_BackgroundColor = nameof(Stylize_BrokenJpeg_BackgroundColor);

        [LanguageKey, DefaultValue("画像データの破損")]
        public const string Stylize_BrokenJpeg_BokenScan = nameof(Stylize_BrokenJpeg_BokenScan);

        [LanguageKey, DefaultValue("破損箇所数")]
        public const string Stylize_BrokenJpeg_BokenScan_BrokenCount = nameof(Stylize_BrokenJpeg_BokenScan_BrokenCount);

        [LanguageKey, DefaultValue("破損範囲開始")]
        public const string Stylize_BrokenJpeg_BokenScan_BrokenRangeBegin = nameof(Stylize_BrokenJpeg_BokenScan_BrokenRangeBegin);

        [LanguageKey, DefaultValue("破損範囲終了")]
        public const string Stylize_BrokenJpeg_BokenScan_BrokenRangeEnd = nameof(Stylize_BrokenJpeg_BokenScan_BrokenRangeEnd);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Stylize_BrokenJpeg_BokenScan_RandomSeed = nameof(Stylize_BrokenJpeg_BokenScan_RandomSeed);

        [LanguageKey, DefaultValue("量子化テーブル(輝度)の破損")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_Luminance = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_Luminance);

        [LanguageKey, DefaultValue("量子化テーブル(色差)の破損")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_Chrominance = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_Chrominance);

        [LanguageKey, DefaultValue("量子化テーブルを壊す")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_Enabled = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_Enabled);

        [LanguageKey, DefaultValue("破損箇所")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenPosition = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenPosition);

        [LanguageKey, DefaultValue("置き換える値")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_ReplaceValue = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_ReplaceValue);

        [LanguageKey, DefaultValue("破損箇所数")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenCount = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_BrokenCount);

        [LanguageKey, DefaultValue("置き換え最大値")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_MaxValue = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_MaxValue);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Stylize_BrokenJpeg_BrokenQuantizeTable_RandomSeed = nameof(Stylize_BrokenJpeg_BrokenQuantizeTable_RandomSeed);

        // Level

        [DefaultValue("レベル")]
        public const string ColorCollection_Level_Name = nameof(ColorCollection_Level_Name);

        [DefaultValue("画像のレベルとガンマを調整します")]
        public const string ColorCollection_Level_Description = nameof(ColorCollection_Level_Description);

        [LanguageKey, DefaultValue("チャンネル")]
        public const string ColorCollection_Level_Channel = nameof(ColorCollection_Level_Channel);

        [LanguageKey, DefaultValue("黒入力レベル")]
        public const string ColorCollection_Level_BlackInLevel = nameof(ColorCollection_Level_BlackInLevel);

        [LanguageKey, DefaultValue("白入力レベル")]
        public const string ColorCollection_Level_WhiteInLevel = nameof(ColorCollection_Level_WhiteInLevel);

        [LanguageKey, DefaultValue("黒出力レベル")]
        public const string ColorCollection_Level_BlackOutLevel = nameof(ColorCollection_Level_BlackOutLevel);

        [LanguageKey, DefaultValue("白出力レベル")]
        public const string ColorCollection_Level_WhiteOutLevel = nameof(ColorCollection_Level_WhiteOutLevel);

        [LanguageKey, DefaultValue("ガンマ")]
        public const string ColorCollection_Level_Gamma = nameof(ColorCollection_Level_Gamma);

        // LuminanceAndContrast

        [DefaultValue("輝度&コントラスト")]
        public const string ColorCollection_LuminanceAndContrast_Name = nameof(ColorCollection_LuminanceAndContrast_Name);

        [DefaultValue("画像の輝度とコントラストを調整します")]
        public const string ColorCollection_LuminanceAndContrast_Description = nameof(ColorCollection_LuminanceAndContrast_Description);

        [LanguageKey, DefaultValue("輝度")]
        public const string ColorCollection_LuminanceAndContrast_Luminance = nameof(ColorCollection_LuminanceAndContrast_Luminance);

        [LanguageKey, DefaultValue("コントラスト")]
        public const string ColorCollection_LuminanceAndContrast_Contrast = nameof(ColorCollection_LuminanceAndContrast_Contrast);

        // HueAndSaturation

        [DefaultValue("色相と彩度")]
        public const string ColorCollection_HueAndSaturation_Name = nameof(ColorCollection_HueAndSaturation_Name);

        [DefaultValue("画像の色相と彩度、明度を調整します")]
        public const string ColorCollection_HueAndSaturation_Description = nameof(ColorCollection_HueAndSaturation_Description);

        [LanguageKey, DefaultValue("色相")]
        public const string ColorCollection_HueAndSaturation_Hue = nameof(ColorCollection_HueAndSaturation_Hue);

        [LanguageKey, DefaultValue("彩度")]
        public const string ColorCollection_HueAndSaturation_Saturation = nameof(ColorCollection_HueAndSaturation_Saturation);

        [LanguageKey, DefaultValue("明度")]
        public const string ColorCollection_HueAndSaturation_Lightness = nameof(ColorCollection_HueAndSaturation_Lightness);

        // RandomNoise

        [DefaultValue("ノイズ")]
        public const string Noise_RandomNoise_Name = nameof(Noise_RandomNoise_Name);

        [DefaultValue("ランダムなノイズを生成します")]
        public const string Noise_RandomNoise_Description = nameof(Noise_RandomNoise_Description);

        [LanguageKey, DefaultValue("量")]
        public const string Noise_RandomNoise_Amount = nameof(Noise_RandomNoise_Amount);

        [LanguageKey, DefaultValue("カラーノイズ")]
        public const string Noise_RandomNoise_IsColorNoise = nameof(Noise_RandomNoise_IsColorNoise);

        [LanguageKey, DefaultValue("シード値")]
        public const string Noise_RandomNoise_Seed = nameof(Noise_RandomNoise_Seed);

        // FractalNoise

        [DefaultValue("フラクタルノイズ")]
        public const string Noise_FractalNoise_Name = nameof(Noise_FractalNoise_Name);

        [DefaultValue("ノイズからパターンを生成します")]
        public const string Noise_FractalNoise_Description = nameof(Noise_FractalNoise_Description);

        [LanguageKey, DefaultValue("フラクタルの種類")]
        public const string Noise_FractalNoise_FractalType = nameof(Noise_FractalNoise_FractalType);

        [LanguageKey, DefaultValue("ノイズの種類")]
        public const string Noise_FractalNoise_NoiseType = nameof(Noise_FractalNoise_NoiseType);

        [LanguageKey, DefaultValue("反転")]
        public const string Noise_FractalNoise_Invert = nameof(Noise_FractalNoise_Invert);

        [LanguageKey, DefaultValue("コントラスト")]
        public const string Noise_FractalNoise_Contrast = nameof(Noise_FractalNoise_Contrast);

        [LanguageKey, DefaultValue("明るさ")]
        public const string Noise_FractalNoise_Luminance = nameof(Noise_FractalNoise_Luminance);

        [LanguageKey, DefaultValue("トランスフォーム")]
        public const string Noise_FractalNoise_Transform = nameof(Noise_FractalNoise_Transform);

        [LanguageKey, DefaultValue("位置")]
        public const string Noise_FractalNoise_Transform_Position = nameof(Noise_FractalNoise_Transform_Position);

        [LanguageKey, DefaultValue("スケール")]
        public const string Noise_FractalNoise_Transform_Scale = nameof(Noise_FractalNoise_Transform_Scale);

        [LanguageKey, DefaultValue("回転")]
        public const string Noise_FractalNoise_Transform_Angle = nameof(Noise_FractalNoise_Transform_Angle);

        [LanguageKey, DefaultValue("複雑度")]
        public const string Noise_FractalNoise_Octave = nameof(Noise_FractalNoise_Octave);

        [LanguageKey, DefaultValue("繰り返し設定")]
        public const string Noise_FractalNoise_OctaveSetting = nameof(Noise_FractalNoise_OctaveSetting);

        [LanguageKey, DefaultValue("影響度")]
        public const string Noise_FractalNoise_OctaveSetting_Amount = nameof(Noise_FractalNoise_OctaveSetting_Amount);

        [LanguageKey, DefaultValue("位置のオフセット")]
        public const string Noise_FractalNoise_OctaveSetting_PositionOffset = nameof(Noise_FractalNoise_OctaveSetting_PositionOffset);

        [LanguageKey, DefaultValue("スケール")]
        public const string Noise_FractalNoise_OctaveSetting_Scale = nameof(Noise_FractalNoise_OctaveSetting_Scale);

        [LanguageKey, DefaultValue("回転")]
        public const string Noise_FractalNoise_OctaveSetting_Angle = nameof(Noise_FractalNoise_OctaveSetting_Angle);

        [LanguageKey, DefaultValue("スケールの中心を合わせる")]
        public const string Noise_FractalNoise_OctaveSetting_CenteringScale = nameof(Noise_FractalNoise_OctaveSetting_CenteringScale);

        [LanguageKey, DefaultValue("展開")]
        public const string Noise_FractalNoise_Evolution = nameof(Noise_FractalNoise_Evolution);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Noise_FractalNoise_RandomSeed = nameof(Noise_FractalNoise_RandomSeed);

        [LanguageKey, DefaultValue("不透明度")]
        public const string Noise_FractalNoise_Opacity = nameof(Noise_FractalNoise_Opacity);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Noise_FractalNoise_BlendMode = nameof(Noise_FractalNoise_BlendMode);

        // DisplacementMap

        [DefaultValue("ディスプレイスメントマップ")]
        public const string Distortion_DisplacementMap_Name = nameof(Distortion_DisplacementMap_Name);

        [DefaultValue("レイヤーの色を元に画像をゆがめます")]
        public const string Distortion_DisplacementMap_Description = nameof(Distortion_DisplacementMap_Description);

        [LanguageKey, DefaultValue("ソースレイヤー")]
        public const string Distortion_DisplacementMap_SourceLayer = nameof(Distortion_DisplacementMap_SourceLayer);

        [LanguageKey, DefaultValue("水平チャンネル")]
        public const string Distortion_DisplacementMap_HorizontalChannel = nameof(Distortion_DisplacementMap_HorizontalChannel);

        [LanguageKey, DefaultValue("水平最大移動距離")]
        public const string Distortion_DisplacementMap_HorizontalMaxMove = nameof(Distortion_DisplacementMap_HorizontalMaxMove);

        [LanguageKey, DefaultValue("垂直チャンネル")]
        public const string Distortion_DisplacementMap_VerticalChannel = nameof(Distortion_DisplacementMap_VerticalChannel);

        [LanguageKey, DefaultValue("垂直最大移動距離")]
        public const string Distortion_DisplacementMap_VerticalMaxMove = nameof(Distortion_DisplacementMap_VerticalMaxMove);

        [LanguageKey, DefaultValue("ソースレイヤーの配置")]
        public const string Distortion_DisplacementMap_SourceLayerPosition = nameof(Distortion_DisplacementMap_SourceLayerPosition);

        [LanguageKey, DefaultValue("画像をループする")]
        public const string Distortion_DisplacementMap_IsLoopImage = nameof(Distortion_DisplacementMap_IsLoopImage);

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

        // Renderer setting view

        [ShowInMarkup, DefaultValue("アンチエイリアスを有効にする")]
        public const string DefaultRendererSettingView_EnableAntiAlias = nameof(DefaultRendererSettingView_EnableAntiAlias);

        [ShowInMarkup, DefaultValue("影のアンチエイリアスを有効にする")]
        public const string DefaultRendererSettingView_EnableShadowAntiAlias = nameof(DefaultRendererSettingView_EnableShadowAntiAlias);

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

        // Dialog

        [LanguageKey, DefaultValue("OK")]
        public const string Dialog_OK = nameof(Dialog_OK);

        [LanguageKey, DefaultValue("キャンセル")]
        public const string Dialog_Cancel = nameof(Dialog_Cancel);

        [LanguageKey, DefaultValue("背景色の選択")]
        public const string Dialog_ColorDialog_Title_BackgroundColor = nameof(Dialog_ColorDialog_Title_BackgroundColor);

        // enum

        [DefaultValue("通常")]
        public const string BlendMode_Normal = nameof(BlendMode_Normal);

        [DefaultValue("置換")]
        public const string BlendMode_Replace = nameof(BlendMode_Replace);

        [DefaultValue("加算")]
        public const string BlendMode_Add = nameof(BlendMode_Add);

        [DefaultValue("減算")]
        public const string BlendMode_Subtract = nameof(BlendMode_Subtract);

        [DefaultValue("乗算")]
        public const string BlendMode_Multiply = nameof(BlendMode_Multiply);

        [DefaultValue("スクリーン")]
        public const string BlendMode_Screen = nameof(BlendMode_Screen);

        [DefaultValue("オーバーレイ")]
        public const string BlendMode_Overlay = nameof(BlendMode_Overlay);

        [DefaultValue("ハードライト")]
        public const string BlendMode_HardLight = nameof(BlendMode_HardLight);

        [DefaultValue("ソフトライト")]
        public const string BlendMode_SoftLight = nameof(BlendMode_SoftLight);

        [DefaultValue("ビビッドライト")]
        public const string BlendMode_VividLight = nameof(BlendMode_VividLight);

        [DefaultValue("リニアライト")]
        public const string BlendMode_LinearLight = nameof(BlendMode_LinearLight);

        [DefaultValue("ピンライト")]
        public const string BlendMode_PinLight = nameof(BlendMode_PinLight);

        [DefaultValue("覆い焼きカラー")]
        public const string BlendMode_ColorDodge = nameof(BlendMode_ColorDodge);

        [DefaultValue("覆い焼きリニア")]
        public const string BlendMode_LinearDodge = nameof(BlendMode_LinearDodge);

        [DefaultValue("焼き込みカラー")]
        public const string BlendMode_ColorBurn = nameof(BlendMode_ColorBurn);

        [DefaultValue("焼き込みリニア")]
        public const string BlendMode_LinearBurn = nameof(BlendMode_LinearBurn);

        [DefaultValue("比較(暗)")]
        public const string BlendMode_Darken = nameof(BlendMode_Darken);

        [DefaultValue("比較(明)")]
        public const string BlendMode_Lighten = nameof(BlendMode_Lighten);

        [DefaultValue("差")]
        public const string BlendMode_Difference = nameof(BlendMode_Difference);

        [DefaultValue("除外")]
        public const string BlendMode_Exclusion = nameof(BlendMode_Exclusion);

        [DefaultValue("色相")]
        public const string BlendMode_Hue = nameof(BlendMode_Hue);

        [DefaultValue("彩度")]
        public const string BlendMode_Saturation = nameof(BlendMode_Saturation);

        [DefaultValue("カラー")]
        public const string BlendMode_Color = nameof(BlendMode_Color);

        [DefaultValue("輝度")]
        public const string BlendMode_Luminance = nameof(BlendMode_Luminance);

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
        public const string ChannelType_RGB = nameof(ChannelType_RGB);

        [DefaultValue("赤")]
        public const string ChannelType_R = nameof(ChannelType_R);

        [DefaultValue("緑")]
        public const string ChannelType_G = nameof(ChannelType_G);

        [DefaultValue("青")]
        public const string ChannelType_B = nameof(ChannelType_B);

        [DefaultValue("アルファ")]
        public const string ChannelType_A = nameof(ChannelType_A);

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

        [DefaultValue("基本")]
        public const string FractalType_Normal = nameof(FractalType_Normal);

        [DefaultValue("タービュランス")]
        public const string FractalType_Turbulent = nameof(FractalType_Turbulent);

        [DefaultValue("最大")]
        public const string FractalType_Max = nameof(FractalType_Max);

        [DefaultValue("ブロック")]
        public const string NoiseType_Block = nameof(NoiseType_Block);

        [DefaultValue("リニア")]
        public const string NoiseType_Linear = nameof(NoiseType_Linear);

        [DefaultValue("スムーズ")]
        public const string NoiseType_SmoothLinear = nameof(NoiseType_SmoothLinear);

        [DefaultValue("パーリン")]
        public const string NoiseType_Parlin = nameof(NoiseType_Parlin);

        [DefaultValue("赤")]
        public const string DisplacemenMapChannelType_R = nameof(DisplacemenMapChannelType_R);

        [DefaultValue("緑")]
        public const string DisplacemenMapChannelType_G = nameof(DisplacemenMapChannelType_G);

        [DefaultValue("青")]
        public const string DisplacemenMapChannelType_B = nameof(DisplacemenMapChannelType_B);

        [DefaultValue("アルファ")]
        public const string DisplacemenMapChannelType_A = nameof(DisplacemenMapChannelType_A);

        [DefaultValue("輝度")]
        public const string DisplacemenMapChannelType_Luminance = nameof(DisplacemenMapChannelType_Luminance);

        [DefaultValue("色相")]
        public const string DisplacemenMapChannelType_Hue = nameof(DisplacemenMapChannelType_Hue);

        [DefaultValue("彩度")]
        public const string DisplacemenMapChannelType_Saturation = nameof(DisplacemenMapChannelType_Saturation);

        [DefaultValue("明度")]
        public const string DisplacemenMapChannelType_Lightness = nameof(DisplacemenMapChannelType_Lightness);

        [DefaultValue("オン")]
        public const string DisplacemenMapChannelType_On = nameof(DisplacemenMapChannelType_On);

        [DefaultValue("半分")]
        public const string DisplacemenMapChannelType_Half = nameof(DisplacemenMapChannelType_Half);

        [DefaultValue("オフ")]
        public const string DisplacemenMapChannelType_Off = nameof(DisplacemenMapChannelType_Off);

        [DefaultValue("中央配置")]
        public const string DisplacementSourceLayerPositionType_Center = nameof(DisplacementSourceLayerPositionType_Center);

        [DefaultValue("リサイズ")]
        public const string DisplacementSourceLayerPositionType_Stretch = nameof(DisplacementSourceLayerPositionType_Stretch);

        [DefaultValue("ループ")]
        public const string DisplacementSourceLayerPositionType_Loop = nameof(DisplacementSourceLayerPositionType_Loop);

        [DefaultValue("RGB")]
        public const string JpegColorSpace_Rgb = nameof(JpegColorSpace_Rgb);

        [DefaultValue("YCbCr 4:4:4")]
        public const string JpegColorSpace_YCbCr444 = nameof(JpegColorSpace_YCbCr444);

        [DefaultValue("YCbCr 4:2:2")]
        public const string JpegColorSpace_YCbCr422 = nameof(JpegColorSpace_YCbCr422);

        [DefaultValue("YCbCr 4:2:0")]
        public const string JpegColorSpace_YCbCr420 = nameof(JpegColorSpace_YCbCr420);

        [DefaultValue("YCbCr 4:1:1")]
        public const string JpegColorSpace_YCbCr411 = nameof(JpegColorSpace_YCbCr411);

        [DefaultValue("YCbCr 4:1:0")]
        public const string JpegColorSpace_YCbCr410 = nameof(JpegColorSpace_YCbCr410);

        // unit

        [LanguageKey, DefaultValue("ms")]
        public const string Unit_MilliSecond = nameof(Unit_MilliSecond);

        [LanguageKey, DefaultValue("dB")]
        public const string Unit_Decibel = nameof(Unit_Decibel);

        [LanguageKey, DefaultValue("Hz")]
        public const string Unit_Hertz = nameof(Unit_Hertz);

        [LanguageKey, DefaultValue("%")]
        public const string Unit_Percent = nameof(Unit_Percent);

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
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    file sealed class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(string defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public string DefaultValue { get; }

        public string FromVersion { get; set; } = "0.0.0.0";
    }
}
