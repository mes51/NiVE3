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

        // Property Controls

        [ShowInMarkup, DefaultValue("編集")]
        public const string ToneCurvePropertyControl_EditChannel = nameof(ToneCurvePropertyControl_EditChannel);

        [ShowInMarkup, DefaultValue("RGB")]
        public const string ToneCurvePropertyControl_EditChannel_Rgb = nameof(ToneCurvePropertyControl_EditChannel_Rgb);

        [ShowInMarkup, DefaultValue("R")]
        public const string ToneCurvePropertyControl_EditChannel_R = nameof(ToneCurvePropertyControl_EditChannel_R);

        [ShowInMarkup, DefaultValue("G")]
        public const string ToneCurvePropertyControl_EditChannel_G = nameof(ToneCurvePropertyControl_EditChannel_G);

        [ShowInMarkup, DefaultValue("B")]
        public const string ToneCurvePropertyControl_EditChannel_B = nameof(ToneCurvePropertyControl_EditChannel_B);

        [ShowInMarkup, DefaultValue("A")]
        public const string ToneCurvePropertyControl_EditChannel_A = nameof(ToneCurvePropertyControl_EditChannel_A);

        [ShowInMarkup, DefaultValue("L")]
        public const string ToneCurvePropertyControl_EditViewSize_L = nameof(ToneCurvePropertyControl_EditViewSize_L);

        [ShowInMarkup, DefaultValue("M")]
        public const string ToneCurvePropertyControl_EditViewSize_M = nameof(ToneCurvePropertyControl_EditViewSize_M);

        [ShowInMarkup, DefaultValue("S")]
        public const string ToneCurvePropertyControl_EditViewSize_S = nameof(ToneCurvePropertyControl_EditViewSize_S);

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

        [DefaultValue("3Dポイント制御")]
        public const string ExpressionControl_Point3DControl_Name = nameof(ExpressionControl_Point3DControl_Name);

        [DefaultValue("エクスプレッションで使用する3次元のポイント制御")]
        public const string ExpressionControl_Point3DControl_Description = nameof(ExpressionControl_Point3DControl_Description);

        [LanguageKey, DefaultValue("ポイント")]
        public const string ExpressionControl_Point3DControl_PropertyName = nameof(ExpressionControl_Point3DControl_PropertyName);

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

        // GaussianBlur

        [DefaultValue("ガウスブラー")]
        public const string Blur_GaussianBlur_Name = nameof(Blur_GaussianBlur_Name);

        [DefaultValue("ガウスブラーを適用します")]
        public const string Blur_GaussianBlur_Description = nameof(Blur_GaussianBlur_Description);

        [LanguageKey, DefaultValue("半径")]
        public const string Blur_GaussianBlur_Amount = nameof(Blur_GaussianBlur_Amount);

        [LanguageKey, DefaultValue("方向")]
        public const string Blur_GaussianBlur_Direction = nameof(Blur_GaussianBlur_Direction);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Blur_GaussianBlur_EdgeRepeatMode = nameof(Blur_GaussianBlur_EdgeRepeatMode);

        // DirectionalBlur

        [DefaultValue("方向ブラー")]
        public const string Blur_DirectionalBlur_Name = nameof(Blur_DirectionalBlur_Name);

        [DefaultValue("指定した方向にブラーを掛けます")]
        public const string Blur_DirectionalBlur_Description = nameof(Blur_DirectionalBlur_Description);

        [LanguageKey, DefaultValue("方向")]
        public const string Blur_DirectionalBlur_Angle = nameof(Blur_DirectionalBlur_Angle);

        [LanguageKey, DefaultValue("半径")]
        public const string Blur_DirectionalBlur_Amount = nameof(Blur_DirectionalBlur_Amount);

        [LanguageKey, DefaultValue("片方向のみ")]
        public const string Blur_DirectionalBlur_IsSingleDirection = nameof(Blur_DirectionalBlur_IsSingleDirection);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Blur_DirectionalBlur_EdgeRepeatMode = nameof(Blur_DirectionalBlur_EdgeRepeatMode);

        [LanguageKey, DefaultValue("高速モード(CPU用)")]
        public const string Blur_DirectionalBlur_FastMode = nameof(Blur_DirectionalBlur_FastMode);

        // GaussianDirectionalBlur

        [DefaultValue("ガウス方向ブラー")]
        public const string Blur_GaussianDirectionalBlur_Name = nameof(Blur_GaussianDirectionalBlur_Name);

        [DefaultValue("指定した方向にガウスブラーを掛けます")]
        public const string Blur_GaussianDirectionalBlur_Description = nameof(Blur_GaussianDirectionalBlur_Description);

        [LanguageKey, DefaultValue("方向")]
        public const string Blur_GaussianDirectionalBlur_Angle = nameof(Blur_GaussianDirectionalBlur_Angle);

        [LanguageKey, DefaultValue("半径")]
        public const string Blur_GaussianDirectionalBlur_Amount = nameof(Blur_GaussianDirectionalBlur_Amount);

        [LanguageKey, DefaultValue("片方向のみ")]
        public const string Blur_GaussianDirectionalBlur_IsSingleDirection = nameof(Blur_GaussianDirectionalBlur_IsSingleDirection);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Blur_GaussianDirectionalBlur_EdgeRepeatMode = nameof(Blur_GaussianDirectionalBlur_EdgeRepeatMode);

        [LanguageKey, DefaultValue("高速モード(CPU用)")]
        public const string Blur_GaussianDirectionalBlur_FastMode = nameof(Blur_GaussianDirectionalBlur_FastMode);

        // RadialBlur

        [DefaultValue("放射ブラー")]
        public const string Blur_RadialBlur_Name = nameof(Blur_RadialBlur_Name);

        [DefaultValue("指定した点を中心に放射状にブラーを掛けます")]
        public const string Blur_RadialBlur_Description = nameof(Blur_RadialBlur_Description);

        [LanguageKey, DefaultValue("中心点")]
        public const string Blur_RadialBlur_Center = nameof(Blur_RadialBlur_Center);

        [LanguageKey, DefaultValue("量")]
        public const string Blur_RadialBlur_Amount = nameof(Blur_RadialBlur_Amount);

        [LanguageKey, DefaultValue("高速モード(CPU用)")]
        public const string Blur_RadialBlur_FastMode = nameof(Blur_RadialBlur_FastMode);

        // BilateralBlur

        [DefaultValue("バイラテラルブラー")]
        public const string Blur_BilateralBlur_Name = nameof(Blur_BilateralBlur_Name);

        [DefaultValue("バイラテラルブラーを適用します")]
        public const string Blur_BilateralBlur_Description = nameof(Blur_BilateralBlur_Description);

        [LanguageKey, DefaultValue("半径")]
        public const string Blur_BilateralBlur_Amount = nameof(Blur_BilateralBlur_Amount);

        [LanguageKey, DefaultValue("輪郭")]
        public const string Blur_BilateralBlur_Contour = nameof(Blur_BilateralBlur_Contour);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Blur_BilateralBlur_EdgeRepeatMode = nameof(Blur_BilateralBlur_EdgeRepeatMode);

        // LensBlur

        [DefaultValue("レンズブラー")]
        public const string Blur_LensBlur_Name = nameof(Blur_LensBlur_Name);

        [DefaultValue("レンズブラーを適用します")]
        public const string Blur_LensBlur_Description = nameof(Blur_LensBlur_Description);

        [LanguageKey, DefaultValue("半径")]
        public const string Blur_LensBlur_Amount = nameof(Blur_LensBlur_Amount);

        [LanguageKey, DefaultValue("アイリス")]
        public const string Blur_LensBlur_Iris = nameof(Blur_LensBlur_Iris);

        [LanguageKey, DefaultValue("アイリスの形状")]
        public const string Blur_LensBlur_Iris_Type = nameof(Blur_LensBlur_Iris_Type);

        [LanguageKey, DefaultValue("アイリスの角丸")]
        public const string Blur_LensBlur_Iris_CornerRound = nameof(Blur_LensBlur_Iris_CornerRound);

        [LanguageKey, DefaultValue("アイリスにマスクを使用する")]
        public const string Blur_LensBlur_Iris_UseLayerMask = nameof(Blur_LensBlur_Iris_UseLayerMask);

        [LanguageKey, DefaultValue("使用するマスク")]
        public const string Blur_LensBlur_Iris_TargetLayerMask = nameof(Blur_LensBlur_Iris_TargetLayerMask);

        [LanguageKey, DefaultValue("アイリスの角度")]
        public const string Blur_LensBlur_Iris_Angle = nameof(Blur_LensBlur_Iris_Angle);

        [LanguageKey, DefaultValue("ハイライト")]
        public const string Blur_LensBlur_Highlight = nameof(Blur_LensBlur_Highlight);

        [LanguageKey, DefaultValue("強さ")]
        public const string Blur_LensBlur_Highlight_Gain = nameof(Blur_LensBlur_Highlight_Gain);

        [LanguageKey, DefaultValue("閾値")]
        public const string Blur_LensBlur_Highlight_Threshold = nameof(Blur_LensBlur_Highlight_Threshold);

        [LanguageKey, DefaultValue("彩度")]
        public const string Blur_LensBlur_Highlight_Saturation = nameof(Blur_LensBlur_Highlight_Saturation);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Blur_LensBlur_EdgeRepeatMode = nameof(Blur_LensBlur_EdgeRepeatMode);

        // CombineBlur

        [DefaultValue("合成ブラー")]
        public const string Blur_CombineBlur_Name = nameof(Blur_CombineBlur_Name);

        [DefaultValue("他のレイヤーを元にブラーを適用します")]
        public const string Blur_CombineBlur_Description = nameof(Blur_CombineBlur_Description);

        [LanguageKey, DefaultValue("ソースレイヤー")]
        public const string Blur_CombineBlur_SourceLayer = nameof(Blur_CombineBlur_SourceLayer);

        [LanguageKey, DefaultValue("チャンネル")]
        public const string Blur_CombineBlur_Channel = nameof(Blur_CombineBlur_Channel);

        [LanguageKey, DefaultValue("ソースレイヤーの位置")]
        public const string Blur_CombineBlur_SourceLayerPosition = nameof(Blur_CombineBlur_SourceLayerPosition);

        [LanguageKey, DefaultValue("水平半径")]
        public const string Blur_CombineBlur_HorizontalAmount = nameof(Blur_CombineBlur_HorizontalAmount);

        [LanguageKey, DefaultValue("垂直半径")]
        public const string Blur_CombineBlur_VerticalAmount = nameof(Blur_CombineBlur_VerticalAmount);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Blur_CombineBlur_EdgeRepeatMode = nameof(Blur_CombineBlur_EdgeRepeatMode);

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

        [LanguageKey, DefaultValue("ソート順")]
        public const string Stylize_PixelSort_SortOrder = nameof(Stylize_PixelSort_SortOrder);

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

        // Binarization

        [DefaultValue("二値化")]
        public const string Stylize_Binarization_Name = nameof(Stylize_Binarization_Name);

        [DefaultValue("画像を閾値未満と閾値以上で2色に変換します")]
        public const string Stylize_Binarization_Description = nameof(Stylize_Binarization_Description);

        [LanguageKey, DefaultValue("閾値")]
        public const string Stylize_Binarization_Threshold = nameof(Stylize_Binarization_Threshold);

        [LanguageKey, DefaultValue("ハイライト")]
        public const string Stylize_Binarization_HighlightColor = nameof(Stylize_Binarization_HighlightColor);

        [LanguageKey, DefaultValue("ハイライトの不透明度")]
        public const string Stylize_Binarization_HighlightOpacity = nameof(Stylize_Binarization_HighlightOpacity);

        [LanguageKey, DefaultValue("シャドウ")]
        public const string Stylize_Binarization_ShadowColor = nameof(Stylize_Binarization_ShadowColor);

        [LanguageKey, DefaultValue("シャドウの不透明度")]
        public const string Stylize_Binarization_ShadowOpacity = nameof(Stylize_Binarization_ShadowOpacity);

        // Glow

        [DefaultValue("グロー")]
        public const string Stylize_Glow_Name = nameof(Stylize_Glow_Name);

        [DefaultValue("画像を発光させたような効果を適用します")]
        public const string Stylize_Glow_Description = nameof(Stylize_Glow_Description);

        [LanguageKey, DefaultValue("グロー範囲")]
        public const string Stylize_Glow_Range = nameof(Stylize_Glow_Range);

        [LanguageKey, DefaultValue("グロー強度")]
        public const string Stylize_Glow_Strength = nameof(Stylize_Glow_Strength);

        [LanguageKey, DefaultValue("閾値")]
        public const string Stylize_Glow_Threshold = nameof(Stylize_Binarization_Threshold);

        [LanguageKey, DefaultValue("グローの色")]
        public const string Stylize_Glow_Color = nameof(Stylize_Glow_Color);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Stylize_Glow_BlendMode = nameof(Stylize_Glow_BlendMode);

        [LanguageKey, DefaultValue("合成順")]
        public const string Stylize_Glow_CompositeOrder = nameof(Stylize_Glow_CompositeOrder);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Stylize_Glow_EdgeRepeatMode = nameof(Stylize_Glow_EdgeRepeatMode);

        [LanguageKey, DefaultValue("グローの方向")]
        public const string Stylize_Glow_Direction = nameof(Stylize_Glow_Direction);

        [LanguageKey, DefaultValue("グローのみ表示")]
        public const string Stylize_Glow_DrawGlowOnly = nameof(Stylize_Glow_DrawGlowOnly);

        // LightShaft

        [DefaultValue("ライトシャフト")]
        public const string Stylize_LightShaft_Name = nameof(Stylize_LightShaft_Name);

        [DefaultValue("明るい部分から光の柱が伸びているような効果を適用します")]
        public const string Stylize_LightShaft_Description = nameof(Stylize_Glow_Description);

        [LanguageKey, DefaultValue("中心点")]
        public const string Stylize_LightShaft_Center = nameof(Stylize_LightShaft_Center);

        [LanguageKey, DefaultValue("長さ")]
        public const string Stylize_LightShaft_Length = nameof(Stylize_LightShaft_Length);

        [LanguageKey, DefaultValue("光柱の強度")]
        public const string Stylize_LightShaft_Strength = nameof(Stylize_LightShaft_Strength);

        [LanguageKey, DefaultValue("閾値")]
        public const string Stylize_LightShaft_Threshold = nameof(Stylize_LightShaft_Threshold);

        [LanguageKey, DefaultValue("光柱の色")]
        public const string Stylize_LightShaft_Color = nameof(Stylize_LightShaft_Color);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Stylize_LightShaft_BlendMode = nameof(Stylize_LightShaft_BlendMode);

        [LanguageKey, DefaultValue("合成順")]
        public const string Stylize_LightShaft_CompositeOrder = nameof(Stylize_LightShaft_CompositeOrder);

        [LanguageKey, DefaultValue("光柱のみ表示")]
        public const string Stylize_LightShaft_DrawLightShaftOnly = nameof(Stylize_LightShaft_DrawLightShaftOnly);

        [LanguageKey, DefaultValue("高速モード(CPU用)")]
        public const string Stylize_LightShaft_FastMode = nameof(Stylize_LightShaft_FastMode);

        // ChromaticAberration

        [DefaultValue("色収差")]
        public const string Stylize_ChromaticAberration_Name = nameof(Stylize_ChromaticAberration_Name);

        [DefaultValue("画像に色収差が発生したような効果を適用します")]
        public const string Stylize_ChromaticAberration_Description = nameof(Stylize_ChromaticAberration_Description);

        [LanguageKey, DefaultValue("色差のチャンネル")]
        public const string Stylize_ChromaticAberration_Channel = nameof(Stylize_ChromaticAberration_Channel);

        [LanguageKey, DefaultValue("トランスフォーム")]
        public const string Stylize_ChromaticAberration_Transform_Group = nameof(Stylize_ChromaticAberration_Transform_Group);

        [LanguageKey, DefaultValue("アンカーポイント")]
        public const string Stylize_ChromaticAberration_Transform_AnchorPoint = nameof(Stylize_ChromaticAberration_Transform_AnchorPoint);

        [LanguageKey, DefaultValue("位置")]
        public const string Stylize_ChromaticAberration_Transform_Position = nameof(Stylize_ChromaticAberration_Transform_Position);

        [LanguageKey, DefaultValue("スケール")]
        public const string Stylize_ChromaticAberration_Transform_Scale = nameof(Stylize_ChromaticAberration_Transform_Scale);

        [LanguageKey, DefaultValue("回転")]
        public const string Stylize_ChromaticAberration_Transform_Angle = nameof(Stylize_ChromaticAberration_Transform_Angle);

        [LanguageKey, DefaultValue("歪み")]
        public const string Stylize_ChromaticAberration_Distortion_Group = nameof(Stylize_ChromaticAberration_Distortion_Group);

        [LanguageKey, DefaultValue("歪み")]
        public const string Stylize_ChromaticAberration_Distortion_Distortion = nameof(Stylize_ChromaticAberration_Distortion_Distortion);

        [LanguageKey, DefaultValue("色差の歪み")]
        public const string Stylize_ChromaticAberration_Distortion_ChromaDistortion = nameof(Stylize_ChromaticAberration_Distortion_ChromaDistortion);

        [LanguageKey, DefaultValue("範囲外の画像を繰り返す")]
        public const string Stylize_ChromaticAberration_IsMirrorEdge = nameof(Stylize_ChromaticAberration_IsMirrorEdge);

        // StarBurst

        [DefaultValue("スターバースト")]
        public const string Stylize_StarBurst_Name = nameof(Stylize_StarBurst_Name);

        [DefaultValue("画像の明るい部分を放射状に光らせます")]
        public const string Stylize_StarBurst_Description = nameof(Stylize_StarBurst_Description);

        [LanguageKey, DefaultValue("光条の強さ")]
        public const string Stylize_StarBurst_Strength = nameof(Stylize_StarBurst_Strength);

        [LanguageKey, DefaultValue("光条の本数")]
        public const string Stylize_StarBurst_Count = nameof(Stylize_StarBurst_Count);

        [LanguageKey, DefaultValue("光条の長さ")]
        public const string Stylize_StarBurst_Length = nameof(Stylize_StarBurst_Length);

        [LanguageKey, DefaultValue("光条の角度")]
        public const string Stylize_StarBurst_Angle = nameof(Stylize_StarBurst_Angle);

        [LanguageKey, DefaultValue("閾値")]
        public const string Stylize_StarBurst_Threshold = nameof(Stylize_StarBurst_Threshold);

        [LanguageKey, DefaultValue("光源のブラー")]
        public const string Stylize_StarBurst_LightBlur = nameof(Stylize_StarBurst_LightBlur);

        [LanguageKey, DefaultValue("光条の色")]
        public const string Stylize_StarBurst_Color = nameof(Stylize_StarBurst_Color);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Stylize_StarBurst_BlendMode = nameof(Stylize_StarBurst_BlendMode);

        [LanguageKey, DefaultValue("合成順")]
        public const string Stylize_StarBurst_CompositeOrder = nameof(Stylize_StarBurst_CompositeOrder);

        [LanguageKey, DefaultValue("エッジの繰り返しモード")]
        public const string Stylize_StarBurst_EdgeRepeatMode = nameof(Stylize_StarBurst_EdgeRepeatMode);

        [LanguageKey, DefaultValue("光条のみ表示")]
        public const string Stylize_StarBurst_DrawStarBurstOnly = nameof(Stylize_StarBurst_DrawStarBurstOnly);

        // DropShadow

        [DefaultValue("ドロップシャドウ")]
        public const string Stylize_DropShadow_Name = nameof(Stylize_DropShadow_Name);

        [DefaultValue("画像のアルファを元に影を生成します")]
        public const string Stylize_DropShadow_Description = nameof(Stylize_DropShadow_Description);

        [LanguageKey, DefaultValue("角度")]
        public const string Stylize_DropShadow_Angle = nameof(Stylize_DropShadow_Angle);

        [LanguageKey, DefaultValue("距離")]
        public const string Stylize_DropShadow_Distance = nameof(Stylize_DropShadow_Distance);

        [LanguageKey, DefaultValue("影の色")]
        public const string Stylize_DropShadow_ShadowColor = nameof(Stylize_DropShadow_ShadowColor);

        [LanguageKey, DefaultValue("影の不透明度")]
        public const string Stylize_DropShadow_ShadowOpacity = nameof(Stylize_DropShadow_ShadowOpacity);

        [LanguageKey, DefaultValue("影のぼかし")]
        public const string Stylize_DropShadow_ShadowBlur = nameof(Stylize_DropShadow_ShadowBlur);

        [LanguageKey, DefaultValue("影のみ表示")]
        public const string Stylize_DropShadow_DrawShadowOnly = nameof(Stylize_DropShadow_DrawShadowOnly);

        // Vignette

        [DefaultValue("ビネット")]
        public const string Stylize_Vignette_Name = nameof(Stylize_Vignette_Name);

        [DefaultValue("画像の周辺を暗くします")]
        public const string Stylize_Vignette_Description = nameof(Stylize_Vignette_Description);

        [LanguageKey, DefaultValue("減光量")]
        public const string Stylize_Vignette_Amount = nameof(Stylize_Vignette_Amount);

        [LanguageKey, DefaultValue("半径")]
        public const string Stylize_Vignette_Radius = nameof(Stylize_Vignette_Radius);

        // Vignette

        [DefaultValue("モザイク")]
        public const string Stylize_Mosaic_Name = nameof(Stylize_Mosaic_Name);

        [DefaultValue("画像にモザイクを掛けます")]
        public const string Stylize_Mosaic_Description = nameof(Stylize_Mosaic_Description);

        [LanguageKey, DefaultValue("水平ブロック数")]
        public const string Stylize_Mosaic_HorizontalBlock = nameof(Stylize_Mosaic_HorizontalBlock);

        [LanguageKey, DefaultValue("垂直ブロック数")]
        public const string Stylize_Mosaic_VerticalBlock = nameof(Stylize_Mosaic_VerticalBlock);

        // LongShadow

        [DefaultValue("ロングシャドウ")]
        public const string Stylize_LongShadow_Name = nameof(Stylize_LongShadow_Name);

        [DefaultValue("画像のα値を元に、長い影を描画します")]
        public const string Stylize_LongShadow_Description = nameof(Stylize_LongShadow_Description);

        [LanguageKey, DefaultValue("長さ")]
        public const string Stylize_LongShadow_Length = nameof(Stylize_LongShadow_Length);

        [LanguageKey, DefaultValue("影の形状")]
        public const string Stylize_LongShadow_ShapeType = nameof(Stylize_LongShadow_ShapeType);

        [LanguageKey, DefaultValue("角度")]
        public const string Stylize_LongShadow_Angle = nameof(Stylize_LongShadow_Angle);

        [LanguageKey, DefaultValue("放射の中心位置")]
        public const string Stylize_LongShadow_RadiateCenter = nameof(Stylize_LongShadow_RadiateCenter);

        [LanguageKey, DefaultValue("α値の閾値")]
        public const string Stylize_LongShadow_AlphaThreshold = nameof(Stylize_LongShadow_AlphaThreshold);

        [LanguageKey, DefaultValue("グラデーション")]
        public const string Stylize_LongShadow_Gradient = nameof(Stylize_LongShadow_Gradient);

        [LanguageKey, DefaultValue("OKLab色空間で補間する")]
        public const string Stylize_LongShadow_UseOkLabInterpolation = nameof(Stylize_LongShadow_UseOkLabInterpolation);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Stylize_LongShadow_BlendMode = nameof(Stylize_LongShadow_BlendMode);

        // ExtractEdge

        [DefaultValue("輪郭抽出")]
        public const string Stylize_ExtractEdge_Name = nameof(Stylize_ExtractEdge_Name);

        [DefaultValue("画像の輪郭を抽出します")]
        public const string Stylize_ExtractEdge_Description = nameof(Stylize_ExtractEdge_Description);

        [LanguageKey, DefaultValue("太さ")]
        public const string Stylize_ExtractEdge_Width = nameof(Stylize_ExtractEdge_Width);

        [LanguageKey, DefaultValue("反転")]
        public const string Stylize_ExtractEdge_Invert = nameof(Stylize_ExtractEdge_Invert);

        [LanguageKey, DefaultValue("モノクロ")]
        public const string Stylize_ExtractEdge_Monochrome = nameof(Stylize_ExtractEdge_Monochrome);

        [LanguageKey, DefaultValue("元画像のブレンド")]
        public const string Stylize_ExtractEdge_BlendOriginal = nameof(Stylize_ExtractEdge_BlendOriginal);

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

        // AutoContrastCorrection

        [DefaultValue("自動コントラスト")]
        public const string ColorCollection_AutoContrastCorrection_Name = nameof(ColorCollection_AutoContrastCorrection_Name);

        [DefaultValue("ヒストグラムを元にコントラストを自動で調整します")]
        public const string ColorCollection_AutoContrastCorrection_Description = nameof(ColorCollection_AutoContrastCorrection_Description);

        [LanguageKey, DefaultValue("シャドウのクリップ")]
        public const string ColorCollection_AutoContrastCorrection_ShadowClip = nameof(ColorCollection_AutoContrastCorrection_ShadowClip);

        [LanguageKey, DefaultValue("ハイライトのクリップ")]
        public const string ColorCollection_AutoContrastCorrection_HighlightClip = nameof(ColorCollection_AutoContrastCorrection_HighlightClip);

        [LanguageKey, DefaultValue("元画像のブレンド")]
        public const string ColorCollection_AutoContrastCorrection_BlendOriginal = nameof(ColorCollection_AutoContrastCorrection_BlendOriginal);

        // MultiTone

        [DefaultValue("マルチトーン")]
        public const string ColorCollection_MultiTone_Name = nameof(ColorCollection_MultiTone_Name);

        [DefaultValue("指定した色でハイライト～シャドウまでの色を設定します")]
        public const string ColorCollection_MultiTone_Description = nameof(ColorCollection_MultiTone_Description);

        [LanguageKey, DefaultValue("使用するミッドトーンの数")]
        public const string ColorCollection_MultiTone_UseMidToneCount = nameof(ColorCollection_MultiTone_UseMidToneCount);

        [LanguageKey, DefaultValue("シャドウ")]
        public const string ColorCollection_MultiTone_ShadowColor = nameof(ColorCollection_MultiTone_ShadowColor);

        [LanguageKey, DefaultValue("ミッドトーン1")]
        public const string ColorCollection_MultiTone_MidToneColor1 = nameof(ColorCollection_MultiTone_MidToneColor1);

        [LanguageKey, DefaultValue("ミッドトーン2")]
        public const string ColorCollection_MultiTone_MidToneColor2 = nameof(ColorCollection_MultiTone_MidToneColor2);

        [LanguageKey, DefaultValue("ミッドトーン3")]
        public const string ColorCollection_MultiTone_MidToneColor3 = nameof(ColorCollection_MultiTone_MidToneColor3);

        [LanguageKey, DefaultValue("ミッドトーン4")]
        public const string ColorCollection_MultiTone_MidToneColor4 = nameof(ColorCollection_MultiTone_MidToneColor4);

        [LanguageKey, DefaultValue("ハイライト")]
        public const string ColorCollection_MultiTone_HighlightColor = nameof(ColorCollection_MultiTone_HighlightColor);

        // ToneCurve

        [DefaultValue("トーンカーブ")]
        public const string ColorCollection_ToneCurve_Name = nameof(ColorCollection_ToneCurve_Name);

        [DefaultValue("画像の色をグラフに応じて調整します")]
        public const string ColorCollection_ToneCurve_Description = nameof(ColorCollection_ToneCurve_Description);

        [LanguageKey, DefaultValue("トーンカーブ")]
        public const string ColorCollection_ToneCurve_ToneCurve = nameof(ColorCollection_ToneCurve_ToneCurve);

        // Sepia

        [DefaultValue("セピア")]
        public const string ColorCollection_Sepia_Name = nameof(ColorCollection_Sepia_Name);

        [DefaultValue("画像をセピア調に変換します")]
        public const string ColorCollection_Sepia_Description = nameof(ColorCollection_Sepia_Description);

        [LanguageKey, DefaultValue("元画像のブレンド")]
        public const string ColorCollection_Sepia_BlendOriginal = nameof(ColorCollection_Sepia_BlendOriginal);

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

        [DefaultValue("レイヤーの色を元に画像を歪ませます")]
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

        // WaveWarp

        [DefaultValue("波形ワープ")]
        public const string Distortion_WaveWarp_Name = nameof(Distortion_WaveWarp_Name);

        [DefaultValue("画像をを正弦波やノコギリ波などの波状に変形します")]
        public const string Distortion_WaveWarp_Description = nameof(Distortion_WaveWarp_Description);

        [LanguageKey, DefaultValue("波形")]
        public const string Distortion_WaveWarp_Type = nameof(Distortion_WaveWarp_Type);

        [LanguageKey, DefaultValue("振幅")]
        public const string Distortion_WaveWarp_Amplitude = nameof(Distortion_WaveWarp_Amplitude);

        [LanguageKey, DefaultValue("波形の間隔")]
        public const string Distortion_WaveWarp_Interval = nameof(Distortion_WaveWarp_Interval);

        [LanguageKey, DefaultValue("速度")]
        public const string Distortion_WaveWarp_Speed = nameof(Distortion_WaveWarp_Speed);

        [LanguageKey, DefaultValue("角度")]
        public const string Distortion_WaveWarp_Angle = nameof(Distortion_WaveWarp_Angle);

        [LanguageKey, DefaultValue("位相")]
        public const string Distortion_WaveWarp_Phase = nameof(Distortion_WaveWarp_Phase);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Distortion_WaveWarp_RandomSeed = nameof(Distortion_WaveWarp_RandomSeed);

        // FishEye

        [DefaultValue("魚眼")]
        public const string Distortion_FishEye_Name = nameof(Distortion_FishEye_Name);

        [DefaultValue("画像を魚眼レンズで見たときのように歪ませます")]
        public const string Distortion_FishEye_Description = nameof(Distortion_FishEye_Description);

        [LanguageKey, DefaultValue("量")]
        public const string Distortion_FishEye_Amount = nameof(Distortion_FishEye_Amount);

        // PolarDistortion

        [DefaultValue("極座標")]
        public const string Distortion_PolarDistortion_Name = nameof(Distortion_PolarDistortion_Name);

        [DefaultValue("画像を直交座標から極座標に変換、またはその逆を行います")]
        public const string Distortion_PolarDistortion_Description = nameof(Distortion_PolarDistortion_Description);

        [LanguageKey, DefaultValue("変換")]
        public const string Distortion_PolarDistortion_Transform = nameof(Distortion_PolarDistortion_Transform);

        [LanguageKey, DefaultValue("変換方向")]
        public const string Distortion_PolarDistortion_Mode = nameof(Distortion_PolarDistortion_Mode);

        [LanguageKey, DefaultValue("画像のオフセット")]
        public const string Distortion_PolarDistortion_ImageOffset = nameof(Distortion_PolarDistortion_ImageOffset);

        [LanguageKey, DefaultValue("表示領域のオフセット")]
        public const string Distortion_PolarDistortion_DisplayAreaOffset = nameof(Distortion_PolarDistortion_DisplayAreaOffset);

        [LanguageKey, DefaultValue("前処理/後処理用")]
        public const string Distortion_PolarDistortion_ForPreOrPostProcess = nameof(Distortion_PolarDistortion_ForPreOrPostProcess);

        // GlassDistortion

        [DefaultValue("ガラス歪み")]
        public const string Distortion_GlassDistortion_Name = nameof(Distortion_GlassDistortion_Name);

        [DefaultValue("ソースレイヤーのグラデーションの向きに応じて画像を歪ませます")]
        public const string Distortion_GlassDistortion_Description = nameof(Distortion_GlassDistortion_Description);

        [LanguageKey, DefaultValue("ソースレイヤー")]
        public const string Distortion_GlassDistortion_SourceLayer = nameof(Distortion_GlassDistortion_SourceLayer);

        [LanguageKey, DefaultValue("チャンネル")]
        public const string Distortion_GlassDistortion_Channel = nameof(Distortion_GlassDistortion_Channel);

        [LanguageKey, DefaultValue("ソースレイヤーの位置")]
        public const string Distortion_GlassDistortion_SourceLayerPosition = nameof(Distortion_GlassDistortion_SourceLayerPosition);

        [LanguageKey, DefaultValue("適用率")]
        public const string Distortion_GlassDistortion_Rate = nameof(Distortion_GlassDistortion_Rate);

        [LanguageKey, DefaultValue("移動量")]
        public const string Distortion_GlassDistortion_DisplacementAmount = nameof(Distortion_GlassDistortion_DisplacementAmount);

        // Twist

        [DefaultValue("渦")]
        public const string Distortion_Twist_Name = nameof(Distortion_Twist_Name);

        [DefaultValue("画像を中心点から回転させます")]
        public const string Distortion_Twist_Description = nameof(Distortion_Twist_Description);

        [LanguageKey, DefaultValue("回転")]
        public const string Distortion_Twist_Angle = nameof(Distortion_Twist_Angle);

        [LanguageKey, DefaultValue("中心点")]
        public const string Distortion_Twist_Center = nameof(Distortion_Twist_Center);

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

        // ChangeROI

        [DefaultValue("範囲変更")]
        public const string Utility_ChangeROI_Name = nameof(Utility_ChangeROI_Name);

        [DefaultValue("エフェクトを適用する範囲を変更します")]
        public const string Utility_ChangeROI_Description = nameof(Utility_ChangeROI_Description);

        [LanguageKey, DefaultValue("左")]
        public const string Utility_ChangeROI_Left = nameof(Utility_ChangeROI_Left);

        [LanguageKey, DefaultValue("上")]
        public const string Utility_ChangeROI_Top = nameof(Utility_ChangeROI_Top);

        [LanguageKey, DefaultValue("右")]
        public const string Utility_ChangeROI_Right = nameof(Utility_ChangeROI_Right);

        [LanguageKey, DefaultValue("下")]
        public const string Utility_ChangeROI_Bottom = nameof(Utility_ChangeROI_Bottom);

        [DefaultValue("ヒストグラム")]
        public const string Utility_Histogram_Name = nameof(Utility_Histogram_Name);

        [DefaultValue("ヒストグラムを計測し、画像に表示します")]
        public const string Utility_Histogram_Description = nameof(Utility_Histogram_Description);

        [LanguageKey, DefaultValue("チャンネル")]
        public const string Utility_Histogram_Channel = nameof(Utility_Histogram_Channel);

        [LanguageKey, DefaultValue("表示位置")]
        public const string Utility_Histogram_Position = nameof(Utility_Histogram_Position);

        [LanguageKey, DefaultValue("スケール")]
        public const string Utility_Histogram_Scale = nameof(Utility_Histogram_Scale);

        // Median

        [DefaultValue("メディアン")]
        public const string Noise_Median_Name = nameof(Noise_Median_Name);

        [DefaultValue("画像に対し、メディアンフィルタを適用します")]
        public const string Noise_Median_Description = nameof(Noise_Median_Description);

        [LanguageKey, DefaultValue("範囲")]
        public const string Noise_Median_Radius = nameof(Noise_Median_Radius);

        [LanguageKey, DefaultValue("アルファにも適用する")]
        public const string Noise_Median_ApplyToAlpha = nameof(Noise_Median_ApplyToAlpha);

        // Fill

        [DefaultValue("塗り")]
        public const string Generate_Fill_Name = nameof(Generate_Fill_Name);

        [DefaultValue("画像を指定した色で塗りつぶします")]
        public const string Generate_Fill_Description = nameof(Generate_Fill_Description);

        [LanguageKey, DefaultValue("色")]
        public const string Generate_Fill_Color = nameof(Generate_Fill_Color);

        [LanguageKey, DefaultValue("アルファを維持する")]
        public const string Generate_Fill_Keep_Alpha = nameof(Generate_Fill_Keep_Alpha);

        // Gradient

        [DefaultValue("グラデーション")]
        public const string Generate_Gradient_Name = nameof(Generate_Gradient_Name);

        [DefaultValue("画像を指定した2色のグラデーションで塗りつぶします")]
        public const string Generate_Gradient_Description = nameof(Generate_Gradient_Description);

        [LanguageKey, DefaultValue("開始点")]
        public const string Generate_Gradient_BeginPoint = nameof(Generate_Gradient_BeginPoint);

        [LanguageKey, DefaultValue("開始色")]
        public const string Generate_Gradient_BeginColor = nameof(Generate_Gradient_BeginColor);

        [LanguageKey, DefaultValue("開始不透明度")]
        public const string Generate_Gradient_BeginOpacity = nameof(Generate_Gradient_BeginOpacity);

        [LanguageKey, DefaultValue("終了点")]
        public const string Generate_Gradient_EndPoint = nameof(Generate_Gradient_EndPoint);

        [LanguageKey, DefaultValue("終了色")]
        public const string Generate_Gradient_EndColor = nameof(Generate_Gradient_EndColor);

        [LanguageKey, DefaultValue("終了不透明度")]
        public const string Generate_Gradient_EndOpacity = nameof(Generate_Gradient_EndOpacity);

        [LanguageKey, DefaultValue("形状")]
        public const string Generate_Gradient_Type = nameof(Generate_Gradient_Type);

        [LanguageKey, DefaultValue("OKLab色空間で補間する")]
        public const string Generate_Gradient_UseOkLabInterpolation = nameof(Generate_Gradient_UseOkLabInterpolation);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Generate_Gradient_BlendMode = nameof(Generate_Gradient_BlendMode);

        [LanguageKey, DefaultValue("元画像のブレンド")]
        public const string Generate_Gradient_BlendOriginal = nameof(Generate_Gradient_BlendOriginal);

        // CheckerBoard

        [DefaultValue("チェッカーボード")]
        public const string Generate_CheckerBoard_Name = nameof(Generate_CheckerBoard_Name);

        [DefaultValue("格子状に四角形を描画します")]
        public const string Generate_CheckerBoard_Description = nameof(Generate_CheckerBoard_Description);

        [LanguageKey, DefaultValue("色1")]
        public const string Generate_CheckerBoard_Color1 = nameof(Generate_CheckerBoard_Color1);

        [LanguageKey, DefaultValue("不透明度1")]
        public const string Generate_CheckerBoard_Opacity1 = nameof(Generate_CheckerBoard_Opacity1);

        [LanguageKey, DefaultValue("色2")]
        public const string Generate_CheckerBoard_Color2 = nameof(Generate_CheckerBoard_Color2);

        [LanguageKey, DefaultValue("不透明度2")]
        public const string Generate_CheckerBoard_Opacity2 = nameof(Generate_CheckerBoard_Opacity2);

        [LanguageKey, DefaultValue("アンカー")]
        public const string Generate_CheckerBoard_Anchor = nameof(Generate_CheckerBoard_Anchor);

        [LanguageKey, DefaultValue("グリッドサイズ")]
        public const string Generate_CheckerBoard_GridSize = nameof(Generate_CheckerBoard_GridSize);

        [LanguageKey, DefaultValue("タイプ")]
        public const string Generate_CheckerBoard_GridSize_Type = nameof(Generate_CheckerBoard_GridSize_Type);

        [LanguageKey, DefaultValue("コーナー")]
        public const string Generate_CheckerBoard_GridSize_Corner = nameof(Generate_CheckerBoard_GridSize_Corner);

        [LanguageKey, DefaultValue("幅")]
        public const string Generate_CheckerBoard_GridSize_Width = nameof(Generate_CheckerBoard_GridSize_Width);

        [LanguageKey, DefaultValue("高さ")]
        public const string Generate_CheckerBoard_GridSize_Height = nameof(Generate_CheckerBoard_GridSize_Height);

        [LanguageKey, DefaultValue("ぼかし幅")]
        public const string Generate_CheckerBoard_BlurWidth = nameof(Generate_CheckerBoard_BlurWidth);

        [LanguageKey, DefaultValue("ぼかし高さ")]
        public const string Generate_CheckerBoard_BlurHeight = nameof(Generate_CheckerBoard_BlurHeight);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Generate_CheckerBoard_BlendMode = nameof(Generate_CheckerBoard_BlendMode);

        // AudioSpectrum

        [DefaultValue("オーディオスペクトラム")]
        public const string Generate_AudioSpectrum_Name = nameof(Generate_AudioSpectrum_Name);

        [DefaultValue("音声のスペクトラムを描画します")]
        public const string Generate_AudioSpectrum_Description = nameof(Generate_AudioSpectrum_Description);

        [LanguageKey, DefaultValue("レイヤー")]
        public const string Generate_AudioSpectrum_Layer = nameof(Generate_AudioSpectrum_Layer);

        [LanguageKey, DefaultValue("音声のサンプル数")]
        public const string Generate_AudioSpectrum_AudioLength = nameof(Generate_AudioSpectrum_AudioLength);

        [LanguageKey, DefaultValue("窓関数")]
        public const string Generate_AudioSpectrum_WindowFunction = nameof(Generate_AudioSpectrum_WindowFunction);

        [LanguageKey, DefaultValue("音声のオフセット")]
        public const string Generate_AudioSpectrum_AudioOffset = nameof(Generate_AudioSpectrum_AudioOffset);

        [LanguageKey, DefaultValue("周波数カウント")]
        public const string Generate_AudioSpectrum_FrequencyBandCount = nameof(Generate_AudioSpectrum_FrequencyBandCount);

        [LanguageKey, DefaultValue("開始点")]
        public const string Generate_AudioSpectrum_BeginPoint = nameof(Generate_AudioSpectrum_BeginPoint);

        [LanguageKey, DefaultValue("終了点")]
        public const string Generate_AudioSpectrum_EndPoint = nameof(Generate_AudioSpectrum_EndPoint);

        [LanguageKey, DefaultValue("パスに沿って配置する")]
        public const string Generate_AudioSpectrum_UseMaskPath = nameof(Generate_AudioSpectrum_UseMaskPath);

        [LanguageKey, DefaultValue("パス")]
        public const string Generate_AudioSpectrum_MaskPath = nameof(Generate_AudioSpectrum_MaskPath);

        [LanguageKey, DefaultValue("最大高さ")]
        public const string Generate_AudioSpectrum_MaxHeight = nameof(Generate_AudioSpectrum_MaxHeight);

        [LanguageKey, DefaultValue("スペクトラムの幅")]
        public const string Generate_AudioSpectrum_SpectrumWidth = nameof(Generate_AudioSpectrum_SpectrumWidth);

        [LanguageKey, DefaultValue("周波数のスケール")]
        public const string Generate_AudioSpectrum_FrequencyScaleType = nameof(Generate_AudioSpectrum_FrequencyScaleType);

        [LanguageKey, DefaultValue("表示")]
        public const string Generate_AudioSpectrum_DisplayMode = nameof(Generate_AudioSpectrum_DisplayMode);

        [LanguageKey, DefaultValue("形状")]
        public const string Generate_AudioSpectrum_SpectrumShapeType = nameof(Generate_AudioSpectrum_SpectrumShapeType);

        [LanguageKey, DefaultValue("色")]
        public const string Generate_AudioSpectrum_Color = nameof(Generate_AudioSpectrum_Color);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Generate_AudioSpectrum_BlendMode = nameof(Generate_AudioSpectrum_BlendMode);

        // MultiColorGradient

        [DefaultValue("多色グラデーション")]
        public const string Generate_MultiColorGradient_Name = nameof(Generate_MultiColorGradient_Name);

        [DefaultValue("複数の色でのグラデーションを描画します")]
        public const string Generate_MultiColorGradient_Description = nameof(Generate_MultiColorGradient_Description);

        [LanguageKey, DefaultValue("ポイント1")]
        public const string Generate_MultiColorGradient_Point1_Point = nameof(Generate_MultiColorGradient_Point1_Point);

        [LanguageKey, DefaultValue("カラー1")]
        public const string Generate_MultiColorGradient_Point1_Color = nameof(Generate_MultiColorGradient_Point1_Color);

        [LanguageKey, DefaultValue("ポイント2")]
        public const string Generate_MultiColorGradient_Point2_Point = nameof(Generate_MultiColorGradient_Point2_Point);

        [LanguageKey, DefaultValue("カラー2")]
        public const string Generate_MultiColorGradient_Point2_Color = nameof(Generate_MultiColorGradient_Point2_Color);

        [LanguageKey, DefaultValue("他のカラー")]
        public const string Generate_MultiColorGradient_ColorPoints = nameof(Generate_MultiColorGradient_ColorPoints);

        [LanguageKey, DefaultValue("カラー")]
        public const string Generate_MultiColorGradient_ColorPoints_ColorPoint = nameof(Generate_MultiColorGradient_ColorPoints_ColorPoint);

        [LanguageKey, DefaultValue("ポイント")]
        public const string Generate_MultiColorGradient_ColorPoints_ColorPoint_Point = nameof(Generate_MultiColorGradient_ColorPoints_ColorPoint_Point);

        [LanguageKey, DefaultValue("カラー")]
        public const string Generate_MultiColorGradient_ColorPoints_ColorPoint_Color = nameof(Generate_MultiColorGradient_ColorPoints_ColorPoint_Color);

        [LanguageKey, DefaultValue("OKLab色空間で補間する")]
        public const string Generate_MultiColorGradient_UseOkLabInterpolation = nameof(Generate_MultiColorGradient_UseOkLabInterpolation);

        [LanguageKey, DefaultValue("ブレンド")]
        public const string Generate_MultiColorGradient_Blend = nameof(Generate_MultiColorGradient_Blend);

        [LanguageKey, DefaultValue("不透明度")]
        public const string Generate_MultiColorGradient_Opacity = nameof(Generate_MultiColorGradient_Opacity);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Generate_MultiColorGradient_BlendMode = nameof(Generate_MultiColorGradient_BlendMode);

        // Unmult

        [DefaultValue("Unmult")]
        public const string Channel_Unmult_Name = nameof(Channel_Unmult_Name);

        [DefaultValue("黒背景に合成された画像からαを復元します")]
        public const string Channel_Unmult_Description = nameof(Channel_Unmult_Description);

        // SolidComposite

        [DefaultValue("単色合成")]
        public const string Channel_SolidComposite_Name = nameof(Channel_SolidComposite_Name);

        [DefaultValue("指定した色を前景、または背景に合成します")]
        public const string Channel_SolidComposite_Description = nameof(Channel_SolidComposite_Description);

        [LanguageKey, DefaultValue("ソースの不透明度")]
        public const string Channel_SolidComposite_SourceOpacity = nameof(Channel_SolidComposite_SourceOpacity);

        [LanguageKey, DefaultValue("カラー")]
        public const string Channel_SolidComposite_Color = nameof(Channel_SolidComposite_Color);

        [LanguageKey, DefaultValue("不透明度")]
        public const string Channel_SolidComposite_Opacity = nameof(Channel_SolidComposite_Opacity);

        [LanguageKey, DefaultValue("ソースの合成順")]
        public const string Channel_SolidComposite_Order = nameof(Channel_SolidComposite_Order);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Channel_SolidComposite_BlendMode = nameof(Channel_SolidComposite_BlendMode);

        // Clamp

        [DefaultValue("クランプ")]
        public const string Channel_Clamp_Name = nameof(Channel_Clamp_Name);

        [DefaultValue("色の値の範囲を指定した範囲に制限します")]
        public const string Channel_Clamp_Description = nameof(Channel_Clamp_Description);

        [LanguageKey, DefaultValue("最大値")]
        public const string Channel_Clamp_Max = nameof(Channel_Clamp_Max);

        [LanguageKey, DefaultValue("最小値")]
        public const string Channel_Clamp_Min = nameof(Channel_Clamp_Min);

        [LanguageKey, DefaultValue("アルファもクランプする")]
        public const string Channel_Clamp_IsClampAlpha = nameof(Channel_Clamp_IsClampAlpha);

        // Invert

        [DefaultValue("反転")]
        public const string Channel_Invert_Name = nameof(Channel_Invert_Name);

        [DefaultValue("画像の色を反転します")]
        public const string Channel_Invert_Description = nameof(Channel_Invert_Description);

        [LanguageKey, DefaultValue("チャンネル")]
        public const string Channel_Invert_Channel = nameof(Channel_Invert_Channel);

        [LanguageKey, DefaultValue("元画像のブレンド")]
        public const string Channel_Invert_BlendOriginal = nameof(Channel_Invert_BlendOriginal);

        // MinMax

        [DefaultValue("最大・最小")]
        public const string Channel_MinMax_Name = nameof(Channel_MinMax_Name);

        [DefaultValue("指定半径内の色の最大、または最小値で置き換えます")]
        public const string Channel_MinMax_Description = nameof(Channel_MinMax_Description);

        [LanguageKey, DefaultValue("モード")]
        public const string Channel_MinMax_Mode = nameof(Channel_MinMax_Mode);

        [LanguageKey, DefaultValue("チャンネル")]
        public const string Channel_MinMax_Channel = nameof(Channel_MinMax_Channel);

        [LanguageKey, DefaultValue("半径")]
        public const string Channel_MinMax_Radius = nameof(Channel_MinMax_Radius);

        // BlendLayer

        [DefaultValue("ブレンド")]
        public const string Channel_BlendLayer_Name = nameof(Channel_BlendLayer_Name);

        [DefaultValue("指定したレイヤーをブレンドします")]
        public const string Channel_BlendLayer_Description = nameof(Channel_BlendLayer_Description);

        [LanguageKey, DefaultValue("ソースレイヤー")]
        public const string Channel_BlendLayer_SourceLayer = nameof(Channel_BlendLayer_SourceLayer);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Channel_BlendLayer_BlendMode = nameof(Channel_BlendLayer_BlendMode);

        [LanguageKey, DefaultValue("ソースレイヤーの不透明度")]
        public const string Channel_BlendLayer_SourceOpacity = nameof(Channel_BlendLayer_SourceOpacity);

        [LanguageKey, DefaultValue("ソースレイヤーの配置")]
        public const string Channel_BlendLayer_SourceLayerPosition = nameof(Channel_BlendLayer_SourceLayerPosition);

        // Particle

        [DefaultValue("パーティクル")]
        public const string Simulation_Particle_Name = nameof(Simulation_Particle_Name);

        [DefaultValue("細かい粒子状の平面を描画します")]
        public const string Simulation_Particle_Description = nameof(Simulation_Particle_Description);

        [LanguageKey, DefaultValue("キャノン")]
        public const string Simulation_Particle_Cannon = nameof(Simulation_Particle_Cannon);

        [LanguageKey, DefaultValue("パーティクル/秒")]
        public const string Simulation_Particle_Cannon_GenerationRate = nameof(Simulation_Particle_Cannon_GenerationRate);

        [LanguageKey, DefaultValue("パーティクルの生存時間")]
        public const string Simulation_Particle_Cannon_ParticleLifeTime = nameof(Simulation_Particle_Cannon_ParticleLifeTime);

        [LanguageKey, DefaultValue("位置")]
        public const string Simulation_Particle_Cannon_Position = nameof(Simulation_Particle_Cannon_Position);

        [LanguageKey, DefaultValue("キャノン半径")]
        public const string Simulation_Particle_Cannon_Radius = nameof(Simulation_Particle_Cannon_Radius);

        [LanguageKey, DefaultValue("方向")]
        public const string Simulation_Particle_Cannon_Direction = nameof(Simulation_Particle_Cannon_Direction);

        [LanguageKey, DefaultValue("方向の拡散")]
        public const string Simulation_Particle_Cannon_RandomDirection = nameof(Simulation_Particle_Cannon_RandomDirection);

        [LanguageKey, DefaultValue("パーティクルの初速")]
        public const string Simulation_Particle_Cannon_InitialParticleSpeed = nameof(Simulation_Particle_Cannon_InitialParticleSpeed);

        [LanguageKey, DefaultValue("パーティクルの初速のばらつき")]
        public const string Simulation_Particle_Cannon_RandomInitialParticleSpeed = nameof(Simulation_Particle_Cannon_RandomInitialParticleSpeed);

        [LanguageKey, DefaultValue("キャノンの移動速度を加算する")]
        public const string Simulation_Particle_Cannon_AddCannonMoveVelocity = nameof(Simulation_Particle_Cannon_AddCannonMoveVelocity);

        [LanguageKey, DefaultValue("パーティクルの回転速度")]
        public const string Simulation_Particle_Cannon_ParticleRotateSpeed = nameof(Simulation_Particle_Cannon_ParticleRotateSpeed);

        [LanguageKey, DefaultValue("X回転")]
        public const string Simulation_Particle_Cannon_ParticleRotateSpeed_X = nameof(Simulation_Particle_Cannon_ParticleRotateSpeed_X);

        [LanguageKey, DefaultValue("Y回転")]
        public const string Simulation_Particle_Cannon_ParticleRotateSpeed_Y = nameof(Simulation_Particle_Cannon_ParticleRotateSpeed_Y);

        [LanguageKey, DefaultValue("Z回転")]
        public const string Simulation_Particle_Cannon_ParticleRotateSpeed_Z = nameof(Simulation_Particle_Cannon_ParticleRotateSpeed_Z);

        [LanguageKey, DefaultValue("パーティクルの回転速度のばらつき")]
        public const string Simulation_Particle_Cannon_RandomParticleRotateSpeed = nameof(Simulation_Particle_Cannon_RandomParticleRotateSpeed);

        [LanguageKey, DefaultValue("パーティクル")]
        public const string Simulation_Particle_Partucle = nameof(Simulation_Particle_Partucle);

        [LanguageKey, DefaultValue("生成時カラー")]
        public const string Simulation_Particle_Partucle_BirthColor = nameof(Simulation_Particle_Partucle_BirthColor);

        [LanguageKey, DefaultValue("生成時カラーのばらつき")]
        public const string Simulation_Particle_Partucle_BirthColorVariation = nameof(Simulation_Particle_Partucle_BirthColorVariation);

        [LanguageKey, DefaultValue("色相")]
        public const string Simulation_Particle_Partucle_BirthColorVariation_Hue = nameof(Simulation_Particle_Partucle_BirthColorVariation_Hue);

        [LanguageKey, DefaultValue("彩度")]
        public const string Simulation_Particle_Partucle_BirthColorVariation_Saturation = nameof(Simulation_Particle_Partucle_BirthColorVariation_Saturation);

        [LanguageKey, DefaultValue("明度")]
        public const string Simulation_Particle_Partucle_BirthColorVariation_Value = nameof(Simulation_Particle_Partucle_BirthColorVariation_Value);

        [LanguageKey, DefaultValue("消滅時カラー")]
        public const string Simulation_Particle_Partucle_DeadColor = nameof(Simulation_Particle_Partucle_DeadColor);

        [LanguageKey, DefaultValue("消滅時カラーのばらつき")]
        public const string Simulation_Particle_Partucle_DeadColorVariation = nameof(Simulation_Particle_Partucle_DeadColorVariation);

        [LanguageKey, DefaultValue("色相")]
        public const string Simulation_Particle_Partucle_DeadColorVariation_Hue = nameof(Simulation_Particle_Partucle_DeadColorVariation_Hue);

        [LanguageKey, DefaultValue("彩度")]
        public const string Simulation_Particle_Partucle_DeadColorVariation_Saturation = nameof(Simulation_Particle_Partucle_DeadColorVariation_Saturation);

        [LanguageKey, DefaultValue("明度")]
        public const string Simulation_Particle_Partucle_DeadColorVariation_Value = nameof(Simulation_Particle_Partucle_DeadColorVariation_Value);

        [LanguageKey, DefaultValue("カラーマップ")]
        public const string Simulation_Particle_Partucle_ColorGraph = nameof(Simulation_Particle_Partucle_ColorGraph);

        [LanguageKey, DefaultValue("生成時サイズ")]
        public const string Simulation_Particle_Partucle_BirthSize = nameof(Simulation_Particle_Partucle_BirthSize);

        [LanguageKey, DefaultValue("生成時サイズのばらつき")]
        public const string Simulation_Particle_Partucle_BirthSizeVariation = nameof(Simulation_Particle_Partucle_BirthSizeVariation);

        [LanguageKey, DefaultValue("消滅時サイズ")]
        public const string Simulation_Particle_Partucle_DeadSize = nameof(Simulation_Particle_Partucle_DeadSize);

        [LanguageKey, DefaultValue("消滅時サイズのばらつき")]
        public const string Simulation_Particle_Partucle_DeadSizeVariation = nameof(Simulation_Particle_Partucle_DeadSizeVariation);

        [LanguageKey, DefaultValue("サイズマップ")]
        public const string Simulation_Particle_Partucle_SizeGraph = nameof(Simulation_Particle_Partucle_SizeGraph);

        [LanguageKey, DefaultValue("生成時不透明度")]
        public const string Simulation_Particle_Partucle_BirthOpacity = nameof(Simulation_Particle_Partucle_BirthOpacity);

        [LanguageKey, DefaultValue("生成時不透明度のばらつき")]
        public const string Simulation_Particle_Partucle_BirthOpacityVariation = nameof(Simulation_Particle_Partucle_BirthOpacityVariation);

        [LanguageKey, DefaultValue("消滅時不透明度")]
        public const string Simulation_Particle_Partucle_DeadOpacity = nameof(Simulation_Particle_Partucle_DeadOpacity);

        [LanguageKey, DefaultValue("消滅時不透明度のばらつき")]
        public const string Simulation_Particle_Partucle_DeadOpacityVariation = nameof(Simulation_Particle_Partucle_DeadOpacityVariation);

        [LanguageKey, DefaultValue("不透明度マップ")]
        public const string Simulation_Particle_Partucle_OpacityGraph = nameof(Simulation_Particle_Partucle_OpacityGraph);

        [LanguageKey, DefaultValue("ワールド")]
        public const string Simulation_Particle_World = nameof(Simulation_Particle_World);

        [LanguageKey, DefaultValue("重力")]
        public const string Simulation_Particle_World_Gravity = nameof(Simulation_Particle_World_Gravity);

        [LanguageKey, DefaultValue("重力方向")]
        public const string Simulation_Particle_World_GravityDirection = nameof(Simulation_Particle_World_GravityDirection);

        [LanguageKey, DefaultValue("空気抵抗")]
        public const string Simulation_Particle_World_AirRegistance = nameof(Simulation_Particle_World_AirRegistance);

        [LanguageKey, DefaultValue("カメラ")]
        public const string Simulation_Particle_Camera = nameof(Simulation_Particle_Camera);

        [LanguageKey, DefaultValue("コンポジションカメラを使用する")]
        public const string Simulation_Particle_Camera_UseComposition = nameof(Simulation_Particle_Camera_UseComposition);

        [LanguageKey, DefaultValue("目標点")]
        public const string Simulation_Particle_Camera_PointOfInterest = nameof(Simulation_Particle_Camera_PointOfInterest);

        [LanguageKey, DefaultValue("位置")]
        public const string Simulation_Particle_Camera_Position = nameof(Simulation_Particle_Camera_Position);

        [LanguageKey, DefaultValue("方向")]
        public const string Simulation_Particle_Camera_Orientation = nameof(Simulation_Particle_Camera_Orientation);

        [LanguageKey, DefaultValue("X回転")]
        public const string Simulation_Particle_Camera_XAngle = nameof(Simulation_Particle_Camera_XAngle);

        [LanguageKey, DefaultValue("Y回転")]
        public const string Simulation_Particle_Camera_YAngle = nameof(Simulation_Particle_Camera_YAngle);

        [LanguageKey, DefaultValue("Z回転")]
        public const string Simulation_Particle_Camera_ZAngle = nameof(Simulation_Particle_Camera_ZAngle);

        [LanguageKey, DefaultValue("ズーム")]
        public const string Simulation_Particle_Camera_Zoom = nameof(Simulation_Particle_Camera_Zoom);

        [LanguageKey, DefaultValue("ソースレイヤー")]
        public const string Simulation_Particle_SourceLayer = nameof(Simulation_Particle_SourceLayer);

        [LanguageKey, DefaultValue("レイヤー")]
        public const string Simulation_Particle_SourceLayer_Layer = nameof(Simulation_Particle_SourceLayer_Layer);

        [LanguageKey, DefaultValue("参照するレイヤー時間を指定する")]
        public const string Simulation_Particle_SourceLayer_UseSpecificReferenceTime = nameof(Simulation_Particle_SourceLayer_UseSpecificReferenceTime);

        [LanguageKey, DefaultValue("参照するレイヤー時間")]
        public const string Simulation_Particle_SourceLayer_SpecificReferenceTime = nameof(Simulation_Particle_SourceLayer_SpecificReferenceTime);

        [LanguageKey, DefaultValue("レンダリング設定")]
        public const string Simulation_Particle_Rendering = nameof(Simulation_Particle_Rendering);

        [LanguageKey, DefaultValue("アンチエイリアス")]
        public const string Simulation_Particle_Rendering_AntiAlias = nameof(Simulation_Particle_Rendering_AntiAlias);

        [LanguageKey, DefaultValue("パーティクル間のブレンドモード")]
        public const string Simulation_Particle_Rendering_ParticleBlendMode = nameof(Simulation_Particle_Rendering_ParticleBlendMode);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public const string Simulation_Particle_Rendering_BlendMode = nameof(Simulation_Particle_Rendering_BlendMode);

        [LanguageKey, DefaultValue("合成順")]
        public const string Simulation_Particle_Rendering_CompositeOrder = nameof(Simulation_Particle_Rendering_CompositeOrder);

        [LanguageKey, DefaultValue("オプション")]
        public const string Simulation_Particle_Option = nameof(Simulation_Particle_Option);

        [LanguageKey, DefaultValue("パーティクルのシミュレーション粒度")]
        public const string Simulation_Particle_Option_SimulationRate = nameof(Simulation_Particle_Option_SimulationRate);

        [LanguageKey, DefaultValue("パーティクルのシミュレーション開始時間のオフセット")]
        public const string Simulation_Particle_Option_SimulationStartTimeOffset = nameof(Simulation_Particle_Option_SimulationStartTimeOffset);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Simulation_Particle_Option_RandomSeed = nameof(Simulation_Particle_Option_RandomSeed);

        // Shatter

        [DefaultValue("シャター")]
        public const string Simulation_Shatter_Name = nameof(Simulation_Shatter_Name);

        [DefaultValue("画像を指定された図形に分割、力を掛けて粉砕します")]
        public const string Simulation_Shatter_Description = nameof(Simulation_Shatter_Description);

        [LanguageKey, DefaultValue("形状")]
        public const string Simulation_Shatter_Shape = nameof(Simulation_Shatter_Shape);

        [LanguageKey, DefaultValue("形状")]
        public const string Simulation_Shatter_Shape_ShapeType = nameof(Simulation_Shatter_Shape_ShapeType);

        [LanguageKey, DefaultValue("形状のランダムシード")]
        public const string Simulation_Shatter_Shape_ShapeRandomSeed = nameof(Simulation_Shatter_Shape_ShapeRandomSeed);

        [LanguageKey, DefaultValue("サイズ")]
        public const string Simulation_Shatter_Shape_Size = nameof(Simulation_Shatter_Shape_Size);

        [LanguageKey, DefaultValue("フォース")]
        public const string Simulation_Shatter_Force = nameof(Simulation_Shatter_Force);

        [LanguageKey, DefaultValue("フォース")]
        public const string Simulation_Shatter_Force_ForceItem = nameof(Simulation_Shatter_Force_ForceItem);

        [LanguageKey, DefaultValue("フォース")]
        public const string Simulation_Shatter_Force_Force = nameof(Simulation_Shatter_Force_Force);

        [LanguageKey, DefaultValue("位置")]
        public const string Simulation_Shatter_Force_Position = nameof(Simulation_Shatter_Force_Position);

        [LanguageKey, DefaultValue("フォースの半径")]
        public const string Simulation_Shatter_Force_Radius = nameof(Simulation_Shatter_Force_Radius);

        [LanguageKey, DefaultValue("フォースのパワー")]
        public const string Simulation_Shatter_Force_Power = nameof(Simulation_Shatter_Force_Power);

        [LanguageKey, DefaultValue("フォースを掛け始める時間")]
        public const string Simulation_Shatter_Force_StartTime = nameof(Simulation_Shatter_Force_StartTime);

        [LanguageKey, DefaultValue("ワールド")]
        public const string Simulation_Shatter_World = nameof(Simulation_Shatter_World);

        [LanguageKey, DefaultValue("重力")]
        public const string Simulation_Shatter_World_Gravity = nameof(Simulation_Shatter_World_Gravity);

        [LanguageKey, DefaultValue("重力方向")]
        public const string Simulation_Shatter_World_GravityDirection = nameof(Simulation_Shatter_World_GravityDirection);

        [LanguageKey, DefaultValue("空気抵抗")]
        public const string Simulation_Shatter_World_AirRegistance = nameof(Simulation_Shatter_World_AirRegistance);

        [LanguageKey, DefaultValue("カメラ")]
        public const string Simulation_Shatter_Camera = nameof(Simulation_Shatter_Camera);

        [LanguageKey, DefaultValue("コンポジションカメラを使用する")]
        public const string Simulation_Shatter_Camera_UseComposition = nameof(Simulation_Shatter_Camera_UseComposition);

        [LanguageKey, DefaultValue("目標点")]
        public const string Simulation_Shatter_Camera_PointOfInterest = nameof(Simulation_Shatter_Camera_PointOfInterest);

        [LanguageKey, DefaultValue("位置")]
        public const string Simulation_Shatter_Camera_Position = nameof(Simulation_Shatter_Camera_Position);

        [LanguageKey, DefaultValue("方向")]
        public const string Simulation_Shatter_Camera_Orientation = nameof(Simulation_Shatter_Camera_Orientation);

        [LanguageKey, DefaultValue("X回転")]
        public const string Simulation_Shatter_Camera_XAngle = nameof(Simulation_Shatter_Camera_XAngle);

        [LanguageKey, DefaultValue("Y回転")]
        public const string Simulation_Shatter_Camera_YAngle = nameof(Simulation_Shatter_Camera_YAngle);

        [LanguageKey, DefaultValue("Z回転")]
        public const string Simulation_Shatter_Camera_ZAngle = nameof(Simulation_Shatter_Camera_ZAngle);

        [LanguageKey, DefaultValue("ズーム")]
        public const string Simulation_Shatter_Camera_Zoom = nameof(Simulation_Shatter_Camera_Zoom);

        [LanguageKey, DefaultValue("レンダリング")]
        public const string Simulation_Shatter_Rendering = nameof(Simulation_Shatter_Rendering);

        [LanguageKey, DefaultValue("表示")]
        public const string Simulation_Shatter_Rendering_DisplayType = nameof(Simulation_Shatter_Rendering_DisplayType);

        [LanguageKey, DefaultValue("アンチエイリアス")]
        public const string Simulation_Shatter_Rendering_AntiAlias = nameof(Simulation_Particle_Rendering_AntiAlias);

        [LanguageKey, DefaultValue("オプション")]
        public const string Simulation_Shatter_Option = nameof(Simulation_Shatter_Option);

        [LanguageKey, DefaultValue("シャターのシミュレーション粒度")]
        public const string Simulation_Shatter_Option_SimulationRate = nameof(Simulation_Shatter_Option_SimulationRate);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Simulation_Shatter_Option_RandomSeed = nameof(Simulation_Shatter_Option_RandomSeed);

        // ColorKey

        [DefaultValue("カラーキー")]
        public const string Keying_ColorKey_Name = nameof(Keying_ColorKey_Name);

        [DefaultValue("指定した色でキーイングを行います")]
        public const string Keying_ColorKey_Description = nameof(Keying_ColorKey_Description);

        [LanguageKey, DefaultValue("キーカラー")]
        public const string Keying_ColorKey_KeyColor = nameof(Keying_ColorKey_KeyColor);

        [LanguageKey, DefaultValue("許容範囲")]
        public const string Keying_ColorKey_Tolerance = nameof(Keying_ColorKey_Tolerance);

        [LanguageKey, DefaultValue("柔らかさ")]
        public const string Keying_ColorKey_Softness = nameof(Keying_ColorKey_Softness);

        // LuminanceKey

        [DefaultValue("ルミナンスキー")]
        public const string Keying_LuminanceKey_Name = nameof(Keying_LuminanceKey_Name);

        [DefaultValue("指定した輝度でキーイングを行います")]
        public const string Keying_LuminanceKey_Description = nameof(Keying_LuminanceKey_Description);

        [LanguageKey, DefaultValue("輝度")]
        public const string Keying_LuminanceKey_KeyLuminance = nameof(Keying_LuminanceKey_KeyLuminance);

        [LanguageKey, DefaultValue("許容範囲")]
        public const string Keying_LuminanceKey_Tolerance = nameof(Keying_LuminanceKey_Tolerance);

        [LanguageKey, DefaultValue("柔らかさ")]
        public const string Keying_LuminanceKey_Softness = nameof(Keying_LuminanceKey_Softness);

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

        // Inputs

        [DefaultValue("ランダムノイズ")]
        public const string Input_RandomNoiseProceduralInput_Name = nameof(Input_RandomNoiseProceduralInput_Name);

        [DefaultValue("ランダムノイズを生成します")]
        public const string Input_RandomNoiseProceduralInput_Description = nameof(Input_RandomNoiseProceduralInput_Description);

        [LanguageKey, DefaultValue("サイズ")]
        public const string Input_RandomNoiseProceduralInput_ImageSize = nameof(Input_RandomNoiseProceduralInput_ImageSize);

        [LanguageKey, DefaultValue("カラーノイズ")]
        public const string Input_RandomNoiseProceduralInput_IsColorNoise = nameof(Input_RandomNoiseProceduralInput_IsColorNoise);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Input_RandomNoiseProceduralInput_RandomSeed = nameof(Input_RandomNoiseProceduralInput_RandomSeed);

        [LanguageKey, DefaultValue("進行")]
        public const string Input_RandomNoiseProceduralInput_Advance = nameof(Input_RandomNoiseProceduralInput_Advance);

        [DefaultValue("フラクタルノイズ")]
        public const string Input_FractalNoiseProceduralInput_Name = nameof(Input_FractalNoiseProceduralInput_Name);

        [DefaultValue("ノイズからパターンを生成します")]
        public const string Input_Input_FractalNoiseProceduralInput_Description = nameof(Input_Input_FractalNoiseProceduralInput_Description);

        [LanguageKey, DefaultValue("サイズ")]
        public const string Input_Input_FractalNoiseProceduralInput_ImageSize = nameof(Input_Input_FractalNoiseProceduralInput_ImageSize);

        [LanguageKey, DefaultValue("フラクタルの種類")]
        public const string Input_Input_FractalNoiseProceduralInput_FractalType = nameof(Input_Input_FractalNoiseProceduralInput_FractalType);

        [LanguageKey, DefaultValue("ノイズの種類")]
        public const string Input_Input_FractalNoiseProceduralInput_NoiseType = nameof(Input_Input_FractalNoiseProceduralInput_NoiseType);

        [LanguageKey, DefaultValue("反転")]
        public const string Input_Input_FractalNoiseProceduralInput_Invert = nameof(Input_Input_FractalNoiseProceduralInput_Invert);

        [LanguageKey, DefaultValue("コントラスト")]
        public const string Input_Input_FractalNoiseProceduralInput_Contrast = nameof(Input_Input_FractalNoiseProceduralInput_Contrast);

        [LanguageKey, DefaultValue("明るさ")]
        public const string Input_Input_FractalNoiseProceduralInput_Luminance = nameof(Input_Input_FractalNoiseProceduralInput_Luminance);

        [LanguageKey, DefaultValue("トランスフォーム")]
        public const string Input_Input_FractalNoiseProceduralInput_Transform = nameof(Input_Input_FractalNoiseProceduralInput_Transform);

        [LanguageKey, DefaultValue("位置")]
        public const string Input_Input_FractalNoiseProceduralInput_Transform_Position = nameof(Input_Input_FractalNoiseProceduralInput_Transform_Position);

        [LanguageKey, DefaultValue("スケール")]
        public const string Input_Input_FractalNoiseProceduralInput_Transform_Scale = nameof(Input_Input_FractalNoiseProceduralInput_Transform_Scale);

        [LanguageKey, DefaultValue("回転")]
        public const string Input_Input_FractalNoiseProceduralInput_Transform_Angle = nameof(Input_Input_FractalNoiseProceduralInput_Transform_Angle);

        [LanguageKey, DefaultValue("複雑度")]
        public const string Input_Input_FractalNoiseProceduralInput_Octave = nameof(Input_Input_FractalNoiseProceduralInput_Octave);

        [LanguageKey, DefaultValue("繰り返し設定")]
        public const string Input_Input_FractalNoiseProceduralInput_OctaveSetting = nameof(Input_Input_FractalNoiseProceduralInput_OctaveSetting);

        [LanguageKey, DefaultValue("影響度")]
        public const string Input_Input_FractalNoiseProceduralInput_OctaveSetting_Amount = nameof(Input_Input_FractalNoiseProceduralInput_OctaveSetting_Amount);

        [LanguageKey, DefaultValue("位置のオフセット")]
        public const string Input_Input_FractalNoiseProceduralInput_OctaveSetting_PositionOffset = nameof(Input_Input_FractalNoiseProceduralInput_OctaveSetting_PositionOffset);

        [LanguageKey, DefaultValue("スケール")]
        public const string Input_Input_FractalNoiseProceduralInput_OctaveSetting_Scale = nameof(Input_Input_FractalNoiseProceduralInput_OctaveSetting_Scale);

        [LanguageKey, DefaultValue("回転")]
        public const string Input_Input_FractalNoiseProceduralInput_OctaveSetting_Angle = nameof(Input_Input_FractalNoiseProceduralInput_OctaveSetting_Angle);

        [LanguageKey, DefaultValue("スケールの中心を合わせる")]
        public const string Input_Input_FractalNoiseProceduralInput_OctaveSetting_CenteringScale = nameof(Input_Input_FractalNoiseProceduralInput_OctaveSetting_CenteringScale);

        [LanguageKey, DefaultValue("展開")]
        public const string Input_Input_FractalNoiseProceduralInput_Evolution = nameof(Input_Input_FractalNoiseProceduralInput_Evolution);

        [LanguageKey, DefaultValue("ランダムシード")]
        public const string Input_Input_FractalNoiseProceduralInput_RandomSeed = nameof(Input_Input_FractalNoiseProceduralInput_RandomSeed);

        [LanguageKey, DefaultValue("不透明度")]
        public const string Input_Input_FractalNoiseProceduralInput_Opacity = nameof(Input_Input_FractalNoiseProceduralInput_Opacity);

        // Input setting views

        [ShowInMarkup, DefaultValue("アルファチャンネルの読み込み")]
        public const string DirectShowInputSettingView_Group_AlphaChannel = nameof(DirectShowInputSettingView_Group_AlphaChannel);

        [ShowInMarkup, DefaultValue("ストレート")]
        public const string DirectShowInputSettingView_AlphaChannel_Straight = nameof(DirectShowInputSettingView_AlphaChannel_Straight);

        [ShowInMarkup, DefaultValue("乗算済みアルファ")]
        public const string DirectShowInputSettingView_AlphaChannel_PreMultiply = nameof(DirectShowInputSettingView_AlphaChannel_PreMultiply);

        [ShowInMarkup, DefaultValue("無視")]
        public const string DirectShowInputSettingView_AlphaChannel_Ignore = nameof(DirectShowInputSettingView_AlphaChannel_Ignore);

        // Outputs

        [DefaultValue("AVI出力")]
        public const string Output_AviOutput_Name = nameof(Output_AviOutput_Name);

        [DefaultValue("動画をAVIで出力します")]
        public const string Output_AviOutput_Description = nameof(Output_AviOutput_Description);

        [DefaultValue("画像シーケンス出力")]
        public const string Output_SequentialImageOutput_Name = nameof(Output_SequentialImageOutput_Name);

        [DefaultValue("動画を連番画像として出力します")]
        public const string Output_SequentialImageOutput_Description = nameof(Output_SequentialImageOutput_Description);

        [DefaultValue("Wave出力")]
        public const string Output_WaveOutput_Name = nameof(Output_WaveOutput_Name);

        [DefaultValue("音声をPCM形式で出力します")]
        public const string Output_WaveOutput_Description = nameof(Output_WaveOutput_Description);

        // Output setting views

        [ShowInMarkup, DefaultValue("ビデオ出力")]
        public const string AviOutputSettingView_Group_Video = nameof(AviOutputSettingView_Group_Video);

        [ShowInMarkup, DefaultValue("チャンネル:")]
        public const string AviOutputSettingView_Group_Video_OutputChannel = nameof(AviOutputSettingView_Group_Video_OutputChannel);

        [ShowInMarkup, DefaultValue("コーデック:")]
        public const string AviOutputSettingView_Group_Video_Codec = nameof(AviOutputSettingView_Group_Video_Codec);

        [ShowInMarkup, DefaultValue("アルファ")]
        public const string AviOutputSettingView_Group_Video_AlphaMode = nameof(AviOutputSettingView_Group_Video_AlphaMode);

        [ShowInMarkup, DefaultValue("ストレート")]
        public const string AviOutputSettingView_Video_AlphaMode_Straight = nameof(AviOutputSettingView_Video_AlphaMode_Straight);

        [ShowInMarkup, DefaultValue("乗算済みアルファ")]
        public const string AviOutputSettingView_Video_AlphaMode_PreMultiply = nameof(AviOutputSettingView_Video_AlphaMode_PreMultiply);

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

        // Tone Mapper

        [DefaultValue("ACES Filmic")]
        public const string ToneMapper_ACESFilmicToneMapper_Name = nameof(ToneMapper_ACESFilmicToneMapper_Name);

        [DefaultValue("ACESに近似したトーンマッピングを行います")]
        public const string ToneMapper_ACESFilmicToneMapper_Description = nameof(ToneMapper_ACESFilmicToneMapper_Description);

        [DefaultValue("ACES")]
        public const string ToneMapper_ACESToneMapper_Name = nameof(ToneMapper_ACESToneMapper_Name);

        [DefaultValue("ACES色空間でトーンマッピングを行います")]
        public const string ToneMapper_ACESToneMapper_Description = nameof(ToneMapper_ACESToneMapper_Description);

        [DefaultValue("Reinhard Extended")]
        public const string ToneMapper_ReinhardExtendedToneMapper_Name = nameof(ToneMapper_ReinhardExtendedToneMapper_Name);

        [DefaultValue("拡張Reinhardの式でトーンマッピングを行います")]
        public const string ToneMapper_ReinhardExtendedToneMapper_Description = nameof(ToneMapper_ReinhardExtendedToneMapper_Description);

        // Tone Mapper setting views

        [ShowInMarkup, DefaultValue("最高輝度:")]
        public const string ReinhardExtendedToneMapperSettingView_MaxLuminance = nameof(ReinhardExtendedToneMapperSettingView_MaxLuminance);

        // Dialog

        [LanguageKey, DefaultValue("OK")]
        public const string Dialog_OK = nameof(Dialog_OK);

        [LanguageKey, DefaultValue("キャンセル")]
        public const string Dialog_Cancel = nameof(Dialog_Cancel);

        [LanguageKey, DefaultValue("背景色の選択")]
        public const string Dialog_ColorDialog_Title_BackgroundColor = nameof(Dialog_ColorDialog_Title_BackgroundColor);

        [LanguageKey, DefaultValue("色の選択")]
        public const string Dialog_ColorDialog_Title_Color = nameof(Dialog_ColorDialog_Title_Color);

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

        [DefaultValue("水平&垂直")]
        public const string GlowDirection_HorizontalAndVertical = nameof(GlowDirection_HorizontalAndVertical);

        [DefaultValue("水平")]
        public const string GlowDirection_Horizontal = nameof(GlowDirection_Horizontal);

        [DefaultValue("垂直")]
        public const string GlowDirection_Vertical = nameof(GlowDirection_Vertical);

        [DefaultValue("なし")]
        public const string EdgeRepeatMode_None = nameof(EdgeRepeatMode_None);

        [DefaultValue("エッジのみ繰り返し")]
        public const string EdgeRepeatMode_Wrap = nameof(EdgeRepeatMode_Wrap);

        [DefaultValue("繰り返し")]
        public const string EdgeRepeatMode_Repeat = nameof(EdgeRepeatMode_Repeat);

        [DefaultValue("鏡面繰り返し")]
        public const string EdgeRepeatMode_Mirror = nameof(EdgeRepeatMode_Mirror);

        [DefaultValue("エッジを拡張する")]
        public const string EdgeRepeatMode_AddAmount = nameof(EdgeRepeatMode_AddAmount);

        [DefaultValue("明るさ")]
        public const string ThresholdMode_Brightness = nameof(ThresholdMode_Brightness);

        [DefaultValue("暗さ")]
        public const string ThresholdMode_Darkness = nameof(ThresholdMode_Darkness);

        [DefaultValue("縦")]
        public const string SortMode_Vertical = nameof(SortMode_Vertical);

        [DefaultValue("横")]
        public const string SortMode_Horizontal = nameof(SortMode_Horizontal);

        [DefaultValue("昇順")]
        public const string SortOrder_Ascending = nameof(SortOrder_Ascending);

        [DefaultValue("降順")]
        public const string SortOrder_Descending = nameof(SortOrder_Descending);

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

        [DefaultValue("RGB")]
        public const string WithHSLChannelType_RGB = nameof(WithHSLChannelType_RGB);

        [DefaultValue("赤")]
        public const string WithHSLChannelType_R = nameof(WithHSLChannelType_R);

        [DefaultValue("緑")]
        public const string WithHSLChannelType_G = nameof(WithHSLChannelType_G);

        [DefaultValue("青")]
        public const string WithHSLChannelType_B = nameof(WithHSLChannelType_B);

        [DefaultValue("アルファ")]
        public const string WithHSLChannelType_A = nameof(WithHSLChannelType_A);

        [DefaultValue("色相")]
        public const string WithHSLChannelType_Hue = nameof(WithHSLChannelType_Hue);

        [DefaultValue("彩度")]
        public const string WithHSLChannelType_Saturation = nameof(WithHSLChannelType_Saturation);

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
        public const string SourceLayerPositionType_Center = nameof(SourceLayerPositionType_Center);

        [DefaultValue("リサイズ")]
        public const string SourceLayerPositionType_Stretch = nameof(SourceLayerPositionType_Stretch);

        [DefaultValue("ループ")]
        public const string SourceLayerPositionType_Loop = nameof(SourceLayerPositionType_Loop);

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

        [DefaultValue("前景")]
        public const string CompositeOrder_Front = nameof(CompositeOrder_Front);

        [DefaultValue("背景")]
        public const string CompositeOrder_Back = nameof(CompositeOrder_Back);

        [DefaultValue("サイン")]
        public const string WaveWarpType_Sin = nameof(WaveWarpType_Sin);

        [DefaultValue("矩形")]
        public const string WaveWarpType_Rectangle = nameof(WaveWarpType_Rectangle);

        [DefaultValue("三角")]
        public const string WaveWarpType_Triangle = nameof(WaveWarpType_Triangle);

        [DefaultValue("のこぎり")]
        public const string WaveWarpType_Saw = nameof(WaveWarpType_Saw);

        [DefaultValue("ノイズ")]
        public const string WaveWarpType_Noise = nameof(WaveWarpType_Noise);

        [DefaultValue("ノイズ(滑らか)")]
        public const string WaveWarpType_SmoothNoise = nameof(WaveWarpType_SmoothNoise);

        [DefaultValue("赤&青")]
        public const string ChromaticAberrationChannelType_RedAndBlue = nameof(ChromaticAberrationChannelType_RedAndBlue);

        [DefaultValue("赤&緑")]
        public const string ChromaticAberrationChannelType_RedAndGreen = nameof(ChromaticAberrationChannelType_RedAndGreen);

        [DefaultValue("緑&青")]
        public const string ChromaticAberrationChannelType_GreenAndBlue = nameof(ChromaticAberrationChannelType_GreenAndBlue);

        [DefaultValue("長方形から極座標")]
        public const string PolarDistortionMode_ToPolar = nameof(PolarDistortionMode_ToPolar);

        [DefaultValue("極座標から長方形")]
        public const string PolarDistortionMode_ToRect = nameof(PolarDistortionMode_ToRect);

        [DefaultValue("線形")]
        public const string GradientShapeType_Linear = nameof(GradientShapeType_Linear);

        [DefaultValue("放射状")]
        public const string GradientShapeType_Radial = nameof(GradientShapeType_Radial);

        [DefaultValue("最小")]
        public const string MinMaxMode_Min = nameof(MinMaxMode_Min);

        [DefaultValue("最大")]
        public const string MinMaxMode_Max = nameof(MinMaxMode_Max);

        [DefaultValue("幅")]
        public const string CheckerBoardGridSizeType_Width = nameof(CheckerBoardGridSizeType_Width);

        [DefaultValue("幅&高さ")]
        public const string CheckerBoardGridSizeType_WidthAndHeight = nameof(CheckerBoardGridSizeType_WidthAndHeight);

        [DefaultValue("コーナー")]
        public const string CheckerBoardGridSizeType_Corner = nameof(CheckerBoardGridSizeType_Corner);

        [DefaultValue("三角形")]
        public const string LensBlurIrisType_Triangle = nameof(LensBlurIrisType_Triangle);

        [DefaultValue("四角形")]
        public const string LensBlurIrisType_Rectangle = nameof(LensBlurIrisType_Rectangle);

        [DefaultValue("五角形")]
        public const string LensBlurIrisType_Pentagon = nameof(LensBlurIrisType_Pentagon);

        [DefaultValue("六角形")]
        public const string LensBlurIrisType_Hexagon = nameof(LensBlurIrisType_Hexagon);

        [DefaultValue("七角形")]
        public const string LensBlurIrisType_Heptagon = nameof(LensBlurIrisType_Heptagon);

        [DefaultValue("八角形")]
        public const string LensBlurIrisType_Octagon = nameof(LensBlurIrisType_Octagon);

        [DefaultValue("九角形")]
        public const string LensBlurIrisType_Nonagon = nameof(LensBlurIrisType_Nonagon);

        [DefaultValue("十角形")]
        public const string LensBlurIrisType_Decagon = nameof(LensBlurIrisType_Decagon);

        [DefaultValue("円")]
        public const string LensBlurIrisType_Circle = nameof(LensBlurIrisType_Circle);

        [DefaultValue("赤")]
        public const string LuminanceAndSingleChannelType_R = nameof(LuminanceAndSingleChannelType_R);

        [DefaultValue("緑")]
        public const string LuminanceAndSingleChannelType_G = nameof(LuminanceAndSingleChannelType_G);

        [DefaultValue("青")]
        public const string LuminanceAndSingleChannelType_B = nameof(LuminanceAndSingleChannelType_B);

        [DefaultValue("アルファ")]
        public const string LuminanceAndSingleChannelType_A = nameof(LuminanceAndSingleChannelType_A);

        [DefaultValue("輝度")]
        public const string LuminanceAndSingleChannelType_Luminance = nameof(LuminanceAndSingleChannelType_Luminance);

        [DefaultValue("128")]
        public const string AudioSpectrumAudioLengthType_Length128 = nameof(AudioSpectrumAudioLengthType_Length128);

        [DefaultValue("256")]
        public const string AudioSpectrumAudioLengthType_Length256 = nameof(AudioSpectrumAudioLengthType_Length256);

        [DefaultValue("512")]
        public const string AudioSpectrumAudioLengthType_Length512 = nameof(AudioSpectrumAudioLengthType_Length512);

        [DefaultValue("1024")]
        public const string AudioSpectrumAudioLengthType_Length1024 = nameof(AudioSpectrumAudioLengthType_Length1024);

        [DefaultValue("2048")]
        public const string AudioSpectrumAudioLengthType_Length2048 = nameof(AudioSpectrumAudioLengthType_Length2048);

        [DefaultValue("4096")]
        public const string AudioSpectrumAudioLengthType_Length4096 = nameof(AudioSpectrumAudioLengthType_Length4096);

        [DefaultValue("8192")]
        public const string AudioSpectrumAudioLengthType_Length8192 = nameof(AudioSpectrumAudioLengthType_Length8192);

        [DefaultValue("16384")]
        public const string AudioSpectrumAudioLengthType_Length16384 = nameof(AudioSpectrumAudioLengthType_Length16384);

        [DefaultValue("32768")]
        public const string AudioSpectrumAudioLengthType_Length32768 = nameof(AudioSpectrumAudioLengthType_Length32768);

        [DefaultValue("65536")]
        public const string AudioSpectrumAudioLengthType_Length65536 = nameof(AudioSpectrumAudioLengthType_Length65536);

        [DefaultValue("ハン窓")]
        public const string AudioSpectrumWindowFunctionType_Hann = nameof(AudioSpectrumWindowFunctionType_Hann);

        [DefaultValue("ハミング窓")]
        public const string AudioSpectrumWindowFunctionType_Hamming = nameof(AudioSpectrumWindowFunctionType_Hamming);

        [DefaultValue("ブラックマン-ハリス窓")]
        public const string AudioSpectrumWindowFunctionType_BlackmannHarris = nameof(AudioSpectrumWindowFunctionType_BlackmannHarris);

        [DefaultValue("上")]
        public const string AudioSpectrumDisplayMode_Up = nameof(AudioSpectrumDisplayMode_Up);

        [DefaultValue("下")]
        public const string AudioSpectrumDisplayMode_Down = nameof(AudioSpectrumDisplayMode_Down);

        [DefaultValue("両方")]
        public const string AudioSpectrumDisplayMode_Both = nameof(AudioSpectrumDisplayMode_Both);

        [DefaultValue("バー")]
        public const string AudioSpectrumShapeType_Bar = nameof(AudioSpectrumShapeType_Bar);

        [DefaultValue("ライン")]
        public const string AudioSpectrumShapeType_Line = nameof(AudioSpectrumShapeType_Line);

        [DefaultValue("ドット")]
        public const string AudioSpectrumShapeType_Dot = nameof(AudioSpectrumShapeType_Dot);

        [DefaultValue("線形")]
        public const string AudioSpectrumFrequencyScaleType_Linear = nameof(AudioSpectrumFrequencyScaleType_Linear);

        [DefaultValue("対数")]
        public const string AudioSpectrumFrequencyScaleType_Log = nameof(AudioSpectrumFrequencyScaleType_Log);

        [DefaultValue("メル")]
        public const string AudioSpectrumFrequencyScaleType_Mel = nameof(AudioSpectrumFrequencyScaleType_Mel);

        [DefaultValue("平行")]
        public const string LongShadowShapeType_Parallel = nameof(LongShadowShapeType_Parallel);

        [DefaultValue("放射状")]
        public const string LongShadowShapeType_Radiate = nameof(LongShadowShapeType_Radiate);

        [DefaultValue("逆放射状")]
        public const string LongShadowShapeType_InvertRadiate = nameof(LongShadowShapeType_InvertRadiate);

        [DefaultValue("三角形1")]
        public const string ShatterShapeType_Triangle1 = nameof(ShatterShapeType_Triangle1);

        [DefaultValue("三角形2")]
        public const string ShatterShapeType_Triangle2 = nameof(ShatterShapeType_Triangle2);

        [DefaultValue("四角形")]
        public const string ShatterShapeType_Rectangle = nameof(ShatterShapeType_Rectangle);

        [DefaultValue("六角形")]
        public const string ShatterShapeType_Hexagon = nameof(ShatterShapeType_Hexagon);

        [DefaultValue("レンガ")]
        public const string ShatterShapeType_Brick = nameof(ShatterShapeType_Brick);

        [DefaultValue("ひし形")]
        public const string ShatterShapeType_Rhombus = nameof(ShatterShapeType_Rhombus);

        [DefaultValue("ランダム")]
        public const string ShatterShapeType_Random = nameof(ShatterShapeType_Random);

        [DefaultValue("ワイヤーフレーム")]
        public const string ShatterDisplayType_Wireframe = nameof(ShatterDisplayType_Wireframe);

        [DefaultValue("ワイヤーフレーム+フォース")]
        public const string ShatterDisplayType_WireframeWithForce = nameof(ShatterDisplayType_WireframeWithForce);

        [DefaultValue("レンダリング")]
        public const string ShatterDisplayType_Rendering = nameof(ShatterDisplayType_Rendering);

        // unit

        [LanguageKey, DefaultValue("s")]
        public const string Unit_Second = nameof(Unit_Second);

        [LanguageKey, DefaultValue("ms")]
        public const string Unit_MilliSecond = nameof(Unit_MilliSecond);

        [LanguageKey, DefaultValue("px")]
        public const string Unit_Pixel = nameof(Unit_Pixel);

        [LanguageKey, DefaultValue("dB")]
        public const string Unit_Decibel = nameof(Unit_Decibel);

        [LanguageKey, DefaultValue("Hz")]
        public const string Unit_Hertz = nameof(Unit_Hertz);

        [LanguageKey, DefaultValue("%")]
        public const string Unit_Percent = nameof(Unit_Percent);

        [ShowInMarkup, LanguageKey, DefaultValue("°")]
        public const string Unit_Angle = nameof(Unit_Angle);

        [LanguageKey, DefaultValue("°/s")]
        public const string Unit_AnglePerSec = nameof(Unit_AnglePerSec);

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

        protected override void Reload(string? forceLangCode = null)
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
