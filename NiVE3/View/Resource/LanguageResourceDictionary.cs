using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Plugin.Resource;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;

namespace NiVE3.View.Resource
{
    [MarkupableResourceDictionary]
    class LanguageResourceDictionary : LanguageResourceDictionaryBase
    {
        public static LanguageResourceDictionary Dictionary { get; }

        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        [ShowInMarkup, DefaultValue("NicoVisualEffects 3")]
        public static readonly string MainWindow_Title = nameof(MainWindow_Title);

        [ShowInMarkup, DefaultValue("ファイル(_F)")]
        public static readonly string MainWindow_Menu_File = nameof(MainWindow_Menu_File);

        [ShowInMarkup, DefaultValue("プロジェクトを開く(_O)")]
        public static readonly string MainWindow_Menu_OpenProject = nameof(MainWindow_Menu_OpenProject);

        [ShowInMarkup, DefaultValue("終了(_X)")]
        public static readonly string MainWindow_Menu_Exit = nameof(MainWindow_Menu_Exit);

        [ShowInMarkup, DefaultValue("表示(_V)")]
        public static readonly string MainWindow_Menu_View = nameof(MainWindow_Menu_View);

        [ShowInMarkup, DefaultValue("再生コントロール")]
        public static readonly string PlayControlView_Title = nameof(PlayControlView_Title);

        [ShowInMarkup, DefaultValue("フッテージ")]
        public static readonly string FootageListView_Title = nameof(FootageListView_Title);

        [ShowInMarkup, DefaultValue("名前")]
        public static readonly string FootageListView_FootageName = nameof(FootageListView_FootageName);

        [ShowInMarkup, DefaultValue("サイズ")]
        public static readonly string FootageListView_FootageSize = nameof(FootageListView_FootageSize);

        [ShowInMarkup, DefaultValue("フレームレート")]
        public static readonly string FootageListView_FootageFrameRate = nameof(FootageListView_FootageFrameRate);

        [ShowInMarkup, DefaultValue("デュレーション")]
        public static readonly string FootageListView_FootageDuration = nameof(FootageListView_FootageDuration);

        [ShowInMarkup, DefaultValue("ファイルパス")]
        public static readonly string FootageListView_FootageFilePath = nameof(FootageListView_FootageFilePath);

        [ShowInMarkup, DefaultValue("拡張子")]
        public static readonly string FootageListView_FootageFileExtension = nameof(FootageListView_FootageFileExtension);

        [ShowInMarkup, DefaultValue("コメント")]
        public static readonly string FootageListView_FootageComment = nameof(FootageListView_FootageComment);

        [ShowInMarkup, DefaultValue("エフェクト")]
        public static readonly string EffectListView_Title = nameof(EffectListView_Title);

        [ShowInMarkup, DefaultValue("フッテージ")]
        public static readonly string PreviewView_FootageTitle = nameof(PreviewView_FootageTitle);

        [ShowInMarkup, DefaultValue("コンポジション")]
        public static readonly string PreviewView_CompositionTitle = nameof(PreviewView_CompositionTitle);

        [ShowInMarkup, DefaultValue("(なし)")]
        public static readonly string PreviewView_Title_ItemEmpty = nameof(PreviewView_Title_ItemEmpty);

        [ShowInMarkup, DefaultValue("全体表示")]
        public static readonly string PreviewView_StretchPreviewScale = nameof(PreviewView_StretchPreviewScale);

        [ShowInMarkup, DefaultValue("全体表示(最大100%)")]
        public static readonly string PreviewView_StretchPreviewScaleMax100 = nameof(PreviewView_StretchPreviewScaleMax100);

        [ShowInMarkup, DefaultValue("フル画質")]
        public static readonly string PreviewView_DownScaleRateFull = nameof(PreviewView_DownScaleRateFull);

        [ShowInMarkup, DefaultValue("1/{0}画質")]
        public static readonly string PreviewView_DownScaleRateFormat = nameof(PreviewView_DownScaleRateFormat);

        [ShowInMarkup, DefaultValue("タイムライン")]
        public static readonly string TimelineView_Title = nameof(TimelineView_Title);

        [ShowInMarkup, DefaultValue("入力設定")]
        public static readonly string InputSettingView_Title = nameof(InputSettingView_Title);

        [ShowInMarkup, DefaultValue("平面")]
        public static readonly string SolidInputSettingView_DefaultName = nameof(SolidInputSettingView_DefaultName);

        [ShowInMarkup, DefaultValue("名前:")]
        public static readonly string SolidInputSettingView_FootageName = nameof(SolidInputSettingView_FootageName);

        [ShowInMarkup, DefaultValue("サイズ")]
        public static readonly string SolidInputSettingView_Group_Size = nameof(SolidInputSettingView_Group_Size);

        [ShowInMarkup, DefaultValue("カラー")]
        public static readonly string SolidInputSettingView_Group_Color = nameof(SolidInputSettingView_Group_Color);

        [ShowInMarkup, DefaultValue("幅:")]
        public static readonly string SolidInputSettingView_Width = nameof(SolidInputSettingView_Width);

        [ShowInMarkup, DefaultValue("高さ:")]
        public static readonly string SolidInputSettingView_Height = nameof(SolidInputSettingView_Height);

        [ShowInMarkup, DefaultValue("縦横比を固定する")]
        public static readonly string SolidInputSettingView_IsFixRatio = nameof(SolidInputSettingView_IsFixRatio);

        [ShowInMarkup, DefaultValue("コンポジションサイズに合わせる")]
        public static readonly string SolidInputSettingView_FitCompositionSize = nameof(SolidInputSettingView_FitCompositionSize);

        [ShowInMarkup, DefaultValue("{0}, {1}fps")]
        public static readonly string FootagePreviewView_TimeFormat = nameof(FootagePreviewView_TimeFormat);

        [ShowInMarkup, DefaultValue("{0}アイテム")]
        public static readonly string FootagePreviewView_FolderItemCount = nameof(FootagePreviewView_FolderItemCount);

        [ShowInMarkup, DefaultValue("{0}アイテム選択中")]
        public static readonly string FootagePreviewView_SelectedItemCount = nameof(FootagePreviewView_SelectedItemCount);

        [ShowInMarkup, DefaultValue("コンポ {0}")]
        public static readonly string CompositionSettingView_DefaultName = nameof(CompositionSettingView_DefaultName);

        [ShowInMarkup, DefaultValue("コンポジション設定")]
        public static readonly string CompositionSettingView_Title = nameof(CompositionSettingView_Title);

        [ShowInMarkup, DefaultValue("コンポジション名:")]
        public static readonly string CompositionSettingView_CompositionNameLabel = nameof(CompositionSettingView_CompositionNameLabel);

        [ShowInMarkup, DefaultValue("プリセット名:")]
        public static readonly string CompositionSettingView_PresetNameLabel = nameof(CompositionSettingView_PresetNameLabel);

        [ShowInMarkup, DefaultValue("基本")]
        public static readonly string CompositionSettingView_BasicTab = nameof(CompositionSettingView_BasicTab);

        [ShowInMarkup, DefaultValue("高度")]
        public static readonly string CompositionSettingView_AdvancedTab = nameof(CompositionSettingView_AdvancedTab);

        [ShowInMarkup, DefaultValue("コンポジション設定")]
        public static readonly string CompositionSettingView_CompositionSettingGroup = nameof(CompositionSettingView_CompositionSettingGroup);

        [ShowInMarkup, DefaultValue("モーションブラー設定")]
        public static readonly string CompositionSettingView_MotionBlurSettingGroup = nameof(CompositionSettingView_MotionBlurSettingGroup);

        [ShowInMarkup, DefaultValue("幅:")]
        public static readonly string CompositionSettingView_CompositionWidthLabel = nameof(CompositionSettingView_CompositionWidthLabel);

        [ShowInMarkup, DefaultValue("高さ:")]
        public static readonly string CompositionSettingView_CompositionHeightLabel = nameof(CompositionSettingView_CompositionHeightLabel);

        [ShowInMarkup, DefaultValue("px")]
        public static readonly string CompositionSettingView_SizeUnitLabel = nameof(CompositionSettingView_SizeUnitLabel);

        [ShowInMarkup, DefaultValue("フレームレート:")]
        public static readonly string CompositionSettingView_FrameRateLabel = nameof(CompositionSettingView_FrameRateLabel);

        [ShowInMarkup, DefaultValue("フレーム/秒")]
        public static readonly string CompositionSettingView_FrameRateUnitLabel = nameof(CompositionSettingView_FrameRateUnitLabel);

        [ShowInMarkup, DefaultValue("デュレーション:")]
        public static readonly string CompositionSettingView_DurationLabel = nameof(CompositionSettingView_DurationLabel);

        [ShowInMarkup, DefaultValue("ネスト時にフレームレートを維持")]
        public static readonly string CompositionSettingView_RetentionFrameRate = nameof(CompositionSettingView_RetentionFrameRate);

        [ShowInMarkup, DefaultValue("シャッター角度:")]
        public static readonly string CompositionSettingView_ShutterAngleLabel = nameof(CompositionSettingView_ShutterAngleLabel);

        [ShowInMarkup, DefaultValue("シャッターフェーズ:")]
        public static readonly string CompositionSettingView_ShutterPhaseLabel = nameof(CompositionSettingView_ShutterPhaseLabel);

        [ShowInMarkup, DefaultValue("°")]
        public static readonly string CompositionSettingView_DegreeUnitLabel = nameof(CompositionSettingView_DegreeUnitLabel);

        [ShowInMarkup, DefaultValue("フレームあたりのサンプル数:")]
        public static readonly string CompositionSettingView_MotionBlurSampleCountLabel = nameof(CompositionSettingView_MotionBlurSampleCountLabel);

        [ShowInMarkup, DefaultValue("レンダラ:")]
        public static readonly string CompositionSettingView_RendererLabel = nameof(CompositionSettingView_RendererLabel);

        [ShowInMarkup, DefaultValue("色の選択")]
        public static readonly string ColorPickerWindow_Title = nameof(ColorPickerWindow_Title);

        [ShowInMarkup, DefaultValue("OK")]
        public static readonly string Dialog_OK = nameof(Dialog_OK);

        [ShowInMarkup, DefaultValue("キャンセル")]
        public static readonly string Dialog_Cancel = nameof(Dialog_Cancel);

        [ShowInMarkup, DefaultValue("A/V機能")]
        public static readonly string Timeline_AVSwitchColumn = nameof(Timeline_AVSwitchColumn);

        [ShowInMarkup, DefaultValue("タグ")]
        public static readonly string Timeline_TagColumn = nameof(Timeline_TagColumn);

        [ShowInMarkup, DefaultValue("レイヤー番号")]
        public static readonly string Timeline_LayerNumberColumn = nameof(Timeline_LayerNumberColumn);

        [ShowInMarkup, DefaultValue("レイヤー名")]
        public static readonly string Timeline_LayerNameColumn = nameof(Timeline_LayerNameColumn);

        [ShowInMarkup, DefaultValue("コメント")]
        public static readonly string Timeline_LayerCommentColumn = nameof(Timeline_LayerCommentColumn);

        [ShowInMarkup, DefaultValue("レイヤースイッチ")]
        public static readonly string Timeline_LayerSwitchColumn = nameof(Timeline_LayerSwitchColumn);

        [ShowInMarkup, DefaultValue("モード")]
        public static readonly string Timeline_ModeColumn = nameof(Timeline_ModeColumn);

        [ShowInMarkup, DefaultValue("トラックマット")]
        public static readonly string Timeline_TrackMatteColumn = nameof(Timeline_TrackMatteColumn);

        [ShowInMarkup, DefaultValue("親")]
        public static readonly string Timeline_ParentLayerColumn = nameof(Timeline_ParentLayerColumn);

        [ShowInMarkup, DefaultValue("(なし)")]
        public static readonly string Timeline_EmptyTitle = nameof(Timeline_EmptyTitle);

        [ShowInMarkup, DefaultValue("なし")]
        public static readonly string Layer_EmptyTrackMatte = nameof(Layer_EmptyTrackMatte);

        [ShowInMarkup, DefaultValue("なし")]
        public static readonly string Layer_EmptyParentLayer = nameof(Layer_EmptyParentLayer);

        [ShowInMarkup, DefaultValue("エフェクト")]
        public static readonly string Layer_Effects = nameof(Layer_Effects);

        [ShowInMarkup, DefaultValue("トランスフォーム")]
        public static readonly string Layer_Transform = nameof(Layer_Transform);

        [ShowInMarkup, DefaultValue("名前")]
        public static readonly string EffectList_Name = nameof(EffectList_Name);

        [ShowInMarkup, DefaultValue("カテゴリ")]
        public static readonly string EffectList_Category = nameof(EffectList_Category);

        [ShowInMarkup, DefaultValue("ヒストリ")]
        public static readonly string HistoryList_Title = nameof(HistoryList_Title);

        // History Command

        [ShowInMarkup, DefaultValue("プロジェクトの新規作成/開く")]
        public static readonly string History_NewProject = nameof(History_NewProject);

        [ShowInMarkup, DefaultValue("フォルダの追加")]
        public static readonly string History_AddFolder = nameof(History_AddFolder);

        [ShowInMarkup, DefaultValue("ファイルの読み込み")]
        public static readonly string History_LoadFootageFile = nameof(History_LoadFootageFile);

        [ShowInMarkup, DefaultValue("フッテージの移動")]
        public static readonly string History_MoveFootage = nameof(History_MoveFootage);

        [ShowInMarkup, DefaultValue("フッテージの名前変更")]
        public static readonly string History_ChangeFootageName = nameof(History_ChangeFootageName);

        [ShowInMarkup, DefaultValue("フッテージのコメント変更")]
        public static readonly string History_ChangeFootageComment = nameof(History_ChangeFootageComment);

        [ShowInMarkup, DefaultValue("フッテージの削除")]
        public static readonly string History_DeleteFootages = nameof(History_DeleteFootages);

        [ShowInMarkup, DefaultValue("コンポジションの追加")]
        public static readonly string History_AddComposition = nameof(History_AddComposition);

        [ShowInMarkup, DefaultValue("コンポジションの削除")]
        public static readonly string History_RemoveComposition = nameof(History_RemoveComposition);

        [ShowInMarkup, DefaultValue("レイヤーの追加")]
        public static readonly string History_AddLayers = nameof(History_AddLayers);

        [ShowInMarkup, DefaultValue("レイヤーの移動")]
        public static readonly string History_MoveLayers = nameof(History_MoveLayers);

        [ShowInMarkup, DefaultValue("レイヤーの削除")]
        public static readonly string History_DeleteLayers = nameof(History_DeleteLayers);

        [ShowInMarkup, DefaultValue("平面の追加")]
        public static readonly string History_AddSolid = nameof(History_AddSolid);

        [ShowInMarkup, DefaultValue("レイヤー時間変更")]
        public static readonly string History_EditLayerDuration = nameof(History_EditLayerDuration);

        [ShowInMarkup, DefaultValue("レイヤースイッチの切り替え")]
        public static readonly string History_ChangeLayerSwitch = nameof(History_ChangeLayerSwitch);

        [ShowInMarkup, DefaultValue("レイヤーの名前変更")]
        public static readonly string History_ChangeLayerName = nameof(History_ChangeLayerName);

        [ShowInMarkup, DefaultValue("レイヤーのコメント変更")]
        public static readonly string History_ChangeLayerComment = nameof(History_ChangeLayerComment);

        [ShowInMarkup, DefaultValue("合成モードの変更")]
        public static readonly string History_ChangeBlendMode = nameof(History_ChangeBlendMode);

        [ShowInMarkup, DefaultValue("トラックマットの変更")]
        public static readonly string History_ChangeTrackMatteLayer = nameof(History_ChangeTrackMatteLayer);

        [ShowInMarkup, DefaultValue("トラックマットのモード変更")]
        public static readonly string History_ChangeTrackMatteMode = nameof(History_ChangeTrackMatteMode);

        [ShowInMarkup, DefaultValue("親レイヤーの変更")]
        public static readonly string History_ChangeParentLayer = nameof(History_ChangeParentLayer);

        [ShowInMarkup, DefaultValue("プロパティの変更")]
        public static readonly string History_ChangePropertyValue = nameof(History_ChangePropertyValue);

        [ShowInMarkup, DefaultValue("キーフレームの追加")]
        public static readonly string History_AddKeyFrame = nameof(History_AddKeyFrame);

        [ShowInMarkup, DefaultValue("キーフレームの削除")]
        public static readonly string History_RemoveKeyFrame = nameof(History_RemoveKeyFrame);

        [ShowInMarkup, DefaultValue("キーフレームの移動")]
        public static readonly string History_MoveKeyFrame = nameof(History_MoveKeyFrame);

        [ShowInMarkup, DefaultValue("キーフレームの補間法の変更")]
        public static readonly string History_ChangeKeyFrameInterpolationType = nameof(History_ChangeKeyFrameInterpolationType);

        [ShowInMarkup, DefaultValue("エフェクトの追加")]
        public static readonly string History_AddEffects = nameof(History_AddEffects);

        [ShowInMarkup, DefaultValue("エフェクトの移動")]
        public static readonly string History_MoveEffects = nameof(History_MoveEffects);

        [ShowInMarkup, DefaultValue("エフェクトの有効・無効切り替え")]
        public static readonly string History_ChangeEffectsEnable = nameof(History_ChangeEffectsEnable);

        [ShowInMarkup, DefaultValue("エフェクトの削除")]
        public static readonly string History_DeleteEffects = nameof(History_DeleteEffects);

        // Dialog

        [ShowInMarkup, DefaultValue("フッテージの削除")]
        public static readonly string Dialog_ConfirmDeleteFootage_Title = nameof(Dialog_ConfirmDeleteFootage_Title);

        [ShowInMarkup, DefaultValue("フッテージを削除すると、各コンポジションからこのフッテージを使用しているレイヤーが削除されます。このフッテージを削除しますか?")]
        public static readonly string Dialog_ConfirmDeleteFootage_Text = nameof(Dialog_ConfirmDeleteFootage_Text);

        [ShowInMarkup, DefaultValue("フォルダを削除すると、中に含まれているフッテージも一緒に削除され、各コンポジションからも含まれているフッテージを使用しているレイヤーが削除されます。このフォルダを削除しますか?")]
        public static readonly string Dialog_ConfirmDeleteFootageFolder_Text = nameof(Dialog_ConfirmDeleteFootageFolder_Text);

        // Property

        [ShowInMarkup, DefaultValue("アンカーポイント")]
        public static readonly string TransformProperty_AnchorPoint = nameof(TransformProperty_AnchorPoint);

        [ShowInMarkup, DefaultValue("位置")]
        public static readonly string TransformProperty_Translate = nameof(TransformProperty_Translate);

        [ShowInMarkup, DefaultValue("方向")]
        public static readonly string TransformProperty_Direction = nameof(TransformProperty_Direction);

        [ShowInMarkup, DefaultValue("回転")]
        public static readonly string TransformProperty_ZAngle2D = nameof(TransformProperty_ZAngle2D);

        [ShowInMarkup, DefaultValue("X回転")]
        public static readonly string TransformProperty_XAngle3D = nameof(TransformProperty_XAngle3D);

        [ShowInMarkup, DefaultValue("Y回転")]
        public static readonly string TransformProperty_YAngle3D = nameof(TransformProperty_YAngle3D);

        [ShowInMarkup, DefaultValue("Z回転")]
        public static readonly string TransformProperty_ZAngle3D = nameof(TransformProperty_ZAngle3D);

        [ShowInMarkup, DefaultValue("スケール")]
        public static readonly string TransformProperty_Scale = nameof(TransformProperty_Scale);

        [ShowInMarkup, DefaultValue("不透明度")]
        public static readonly string TransformProperty_Opacity = nameof(TransformProperty_Opacity);

        // Enum

        [ShowInMarkup, DefaultValue("RGB")]
        public static readonly string PreviewColorChannel_Rgb = nameof(PreviewColorChannel_Rgb);

        [ShowInMarkup, DefaultValue("赤")]
        public static readonly string PreviewColorChannel_R = nameof(PreviewColorChannel_R);

        [ShowInMarkup, DefaultValue("緑")]
        public static readonly string PreviewColorChannel_G = nameof(PreviewColorChannel_G);

        [ShowInMarkup, DefaultValue("青")]
        public static readonly string PreviewColorChannel_B = nameof(PreviewColorChannel_B);

        [ShowInMarkup, DefaultValue("アルファ")]
        public static readonly string PreviewColorChannel_Alpha = nameof(PreviewColorChannel_Alpha);

        [ShowInMarkup, DefaultValue("RGBストレート")]
        public static readonly string PreviewColorChannel_RgbStraight = nameof(PreviewColorChannel_RgbStraight);

        [ShowInMarkup, DefaultValue("通常")]
        public static readonly string BlendMode_Normal = nameof(BlendMode_Normal);

        [ShowInMarkup, DefaultValue("置換")]
        public static readonly string BlendMode_Replace = nameof(BlendMode_Replace);

        [ShowInMarkup, DefaultValue("加算")]
        public static readonly string BlendMode_Add = nameof(BlendMode_Add);

        [ShowInMarkup, DefaultValue("減算")]
        public static readonly string BlendMode_Subtract = nameof(BlendMode_Subtract);

        [ShowInMarkup, DefaultValue("乗算")]
        public static readonly string BlendMode_Multiply = nameof(BlendMode_Multiply);

        [ShowInMarkup, DefaultValue("スクリーン")]
        public static readonly string BlendMode_Screen = nameof(BlendMode_Screen);

        [ShowInMarkup, DefaultValue("オーバーレイ")]
        public static readonly string BlendMode_Overlay = nameof(BlendMode_Overlay);

        [ShowInMarkup, DefaultValue("ハードライト")]
        public static readonly string BlendMode_HardLight = nameof(BlendMode_HardLight);

        [ShowInMarkup, DefaultValue("ソフトライト")]
        public static readonly string BlendMode_SoftLight = nameof(BlendMode_SoftLight);

        [ShowInMarkup, DefaultValue("ビビッドライト")]
        public static readonly string BlendMode_VividLight = nameof(BlendMode_VividLight);

        [ShowInMarkup, DefaultValue("リニアライト")]
        public static readonly string BlendMode_LinearLight = nameof(BlendMode_LinearLight);

        [ShowInMarkup, DefaultValue("ピンライト")]
        public static readonly string BlendMode_PinLight = nameof(BlendMode_PinLight);

        [ShowInMarkup, DefaultValue("覆い焼きカラー")]
        public static readonly string BlendMode_ColorDodge = nameof(BlendMode_ColorDodge);

        [ShowInMarkup, DefaultValue("覆い焼きリニア")]
        public static readonly string BlendMode_LinearDodge = nameof(BlendMode_LinearDodge);

        [ShowInMarkup, DefaultValue("焼き込みカラー")]
        public static readonly string BlendMode_ColorBurn = nameof(BlendMode_ColorBurn);

        [ShowInMarkup, DefaultValue("焼き込みリニア")]
        public static readonly string BlendMode_LinearBurn = nameof(BlendMode_LinearBurn);

        [ShowInMarkup, DefaultValue("比較(暗)")]
        public static readonly string BlendMode_Darken = nameof(BlendMode_Darken);

        [ShowInMarkup, DefaultValue("比較(明)")]
        public static readonly string BlendMode_Lighten = nameof(BlendMode_Lighten);

        [ShowInMarkup, DefaultValue("差")]
        public static readonly string BlendMode_Difference = nameof(BlendMode_Difference);

        [ShowInMarkup, DefaultValue("除外")]
        public static readonly string BlendMode_Exclusion = nameof(BlendMode_Exclusion);

        [ShowInMarkup, DefaultValue("色相")]
        public static readonly string BlendMode_Hue = nameof(BlendMode_Hue);

        [ShowInMarkup, DefaultValue("彩度")]
        public static readonly string BlendMode_Saturation = nameof(BlendMode_Saturation);

        [ShowInMarkup, DefaultValue("カラー")]
        public static readonly string BlendMode_Color = nameof(BlendMode_Color);

        [ShowInMarkup, DefaultValue("明度")]
        public static readonly string BlendMode_Luminance = nameof(BlendMode_Luminance);

        [ShowInMarkup, DefaultValue("アルファ")]
        public static readonly string TrackMatteMode_Alpha = nameof(TrackMatteMode_Alpha);

        [ShowInMarkup, DefaultValue("反転アルファ")]
        public static readonly string TrackMatteMode_InvertAlpha = nameof(TrackMatteMode_InvertAlpha);

        [ShowInMarkup, DefaultValue("明度")]
        public static readonly string TrackMatteMode_Luminance = nameof(TrackMatteMode_Luminance);

        [ShowInMarkup, DefaultValue("反転明度")]
        public static readonly string TrackMatteMode_InvertLuminance = nameof(TrackMatteMode_InvertLuminance);

        static LanguageResourceDictionary()
        {
            LanguageKeys = typeof(LanguageResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(f => (f.Name, f.GetCustomAttribute<DefaultValueAttribute>()))
                .Where(t => t.Item2 != null)
                .ToDictionary(t => t.Name, t => Tuple.Create(t.Item2!.DefaultValue, Version.Parse(t.Item2!.FromVersion)));

            Dictionary = new LanguageResourceDictionary();
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
