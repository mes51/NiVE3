using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Plugin.Resource;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;
using NiVE3.SourceGenerator.LanguageResourceGenerator;

namespace NiVE3.View.Resource
{
    [MarkupableResourceDictionary, HasLanguageKey]
    partial class LanguageResourceDictionary : LanguageResourceDictionaryBase
    {
        public static LanguageResourceDictionary Dictionary { get; }

        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        [ShowInMarkup, LanguageKey, DefaultValue("NicoVisualEffects 3{0}")]
        public static readonly string MainWindow_Title = nameof(MainWindow_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("{0} - NicoVisualEffects 3{1}")]
        public static readonly string MainWindow_TitleWithPath = nameof(MainWindow_TitleWithPath);

        [ShowInMarkup, LanguageKey, DefaultValue("ファイル(_F)")]
        public static readonly string MainWindow_Menu_File = nameof(MainWindow_Menu_File);

        [ShowInMarkup, LanguageKey, DefaultValue("プロジェクトを開く(_O)")]
        public static readonly string MainWindow_Menu_OpenProject = nameof(MainWindow_Menu_OpenProject);

        [ShowInMarkup, LanguageKey, DefaultValue("終了(_X)")]
        public static readonly string MainWindow_Menu_Exit = nameof(MainWindow_Menu_Exit);

        [ShowInMarkup, LanguageKey, DefaultValue("表示(_V)")]
        public static readonly string MainWindow_Menu_View = nameof(MainWindow_Menu_View);

        [ShowInMarkup, LanguageKey, DefaultValue("再生コントロール")]
        public static readonly string PlayControlView_Title = nameof(PlayControlView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージ")]
        public static readonly string FootageListView_Title = nameof(FootageListView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("名前")]
        public static readonly string FootageListView_FootageName = nameof(FootageListView_FootageName);

        [ShowInMarkup, LanguageKey, DefaultValue("サイズ")]
        public static readonly string FootageListView_FootageSize = nameof(FootageListView_FootageSize);

        [ShowInMarkup, LanguageKey, DefaultValue("フレームレート")]
        public static readonly string FootageListView_FootageFrameRate = nameof(FootageListView_FootageFrameRate);

        [ShowInMarkup, LanguageKey, DefaultValue("デュレーション")]
        public static readonly string FootageListView_FootageDuration = nameof(FootageListView_FootageDuration);

        [ShowInMarkup, LanguageKey, DefaultValue("ファイルパス")]
        public static readonly string FootageListView_FootageFilePath = nameof(FootageListView_FootageFilePath);

        [ShowInMarkup, LanguageKey, DefaultValue("拡張子")]
        public static readonly string FootageListView_FootageFileExtension = nameof(FootageListView_FootageFileExtension);

        [ShowInMarkup, LanguageKey, DefaultValue("コメント")]
        public static readonly string FootageListView_FootageComment = nameof(FootageListView_FootageComment);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクト")]
        public static readonly string EffectListView_Title = nameof(EffectListView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージ")]
        public static readonly string PreviewView_FootageTitle = nameof(PreviewView_FootageTitle);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジション")]
        public static readonly string PreviewView_CompositionTitle = nameof(PreviewView_CompositionTitle);

        [ShowInMarkup, LanguageKey, DefaultValue("(なし)")]
        public static readonly string PreviewView_Title_ItemEmpty = nameof(PreviewView_Title_ItemEmpty);

        [ShowInMarkup, LanguageKey, DefaultValue("全体表示")]
        public static readonly string PreviewView_StretchPreviewScale = nameof(PreviewView_StretchPreviewScale);

        [ShowInMarkup, LanguageKey, DefaultValue("全体表示(最大100%)")]
        public static readonly string PreviewView_StretchPreviewScaleMax100 = nameof(PreviewView_StretchPreviewScaleMax100);

        [ShowInMarkup, LanguageKey, DefaultValue("フル画質")]
        public static readonly string PreviewView_DownScaleRateFull = nameof(PreviewView_DownScaleRateFull);

        [ShowInMarkup, LanguageKey, DefaultValue("1/{0}画質")]
        public static readonly string PreviewView_DownScaleRateFormat = nameof(PreviewView_DownScaleRateFormat);

        [ShowInMarkup, LanguageKey, DefaultValue("タイムライン")]
        public static readonly string TimelineView_Title = nameof(TimelineView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("入力設定")]
        public static readonly string InputSettingView_Title = nameof(InputSettingView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("平面")]
        public static readonly string SolidInputSettingView_DefaultName = nameof(SolidInputSettingView_DefaultName);

        [ShowInMarkup, LanguageKey, DefaultValue("名前:")]
        public static readonly string SolidInputSettingView_FootageName = nameof(SolidInputSettingView_FootageName);

        [ShowInMarkup, LanguageKey, DefaultValue("サイズ")]
        public static readonly string SolidInputSettingView_Group_Size = nameof(SolidInputSettingView_Group_Size);

        [ShowInMarkup, LanguageKey, DefaultValue("カラー")]
        public static readonly string SolidInputSettingView_Group_Color = nameof(SolidInputSettingView_Group_Color);

        [ShowInMarkup, LanguageKey, DefaultValue("幅:")]
        public static readonly string SolidInputSettingView_Width = nameof(SolidInputSettingView_Width);

        [ShowInMarkup, LanguageKey, DefaultValue("高さ:")]
        public static readonly string SolidInputSettingView_Height = nameof(SolidInputSettingView_Height);

        [ShowInMarkup, LanguageKey, DefaultValue("縦横比を固定する")]
        public static readonly string SolidInputSettingView_IsFixRatio = nameof(SolidInputSettingView_IsFixRatio);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジションサイズに合わせる")]
        public static readonly string SolidInputSettingView_FitCompositionSize = nameof(SolidInputSettingView_FitCompositionSize);

        [ShowInMarkup, LanguageKey, DefaultValue("{0}, {1}fps")]
        public static readonly string FootagePreviewView_TimeFormat = nameof(FootagePreviewView_TimeFormat);

        [ShowInMarkup, LanguageKey, DefaultValue("{0}アイテム")]
        public static readonly string FootagePreviewView_FolderItemCount = nameof(FootagePreviewView_FolderItemCount);

        [ShowInMarkup, LanguageKey, DefaultValue("{0}アイテム選択中")]
        public static readonly string FootagePreviewView_SelectedItemCount = nameof(FootagePreviewView_SelectedItemCount);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポ {0}")]
        public static readonly string CompositionSettingView_DefaultName = nameof(CompositionSettingView_DefaultName);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジション設定")]
        public static readonly string CompositionSettingView_Title = nameof(CompositionSettingView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジション名:")]
        public static readonly string CompositionSettingView_CompositionNameLabel = nameof(CompositionSettingView_CompositionNameLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("プリセット名:")]
        public static readonly string CompositionSettingView_PresetNameLabel = nameof(CompositionSettingView_PresetNameLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("基本")]
        public static readonly string CompositionSettingView_BasicTab = nameof(CompositionSettingView_BasicTab);

        [ShowInMarkup, LanguageKey, DefaultValue("高度")]
        public static readonly string CompositionSettingView_AdvancedTab = nameof(CompositionSettingView_AdvancedTab);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジション設定")]
        public static readonly string CompositionSettingView_CompositionSettingGroup = nameof(CompositionSettingView_CompositionSettingGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("モーションブラー設定")]
        public static readonly string CompositionSettingView_MotionBlurSettingGroup = nameof(CompositionSettingView_MotionBlurSettingGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("幅:")]
        public static readonly string CompositionSettingView_CompositionWidthLabel = nameof(CompositionSettingView_CompositionWidthLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("高さ:")]
        public static readonly string CompositionSettingView_CompositionHeightLabel = nameof(CompositionSettingView_CompositionHeightLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("px")]
        public static readonly string CompositionSettingView_SizeUnitLabel = nameof(CompositionSettingView_SizeUnitLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("フレームレート:")]
        public static readonly string CompositionSettingView_FrameRateLabel = nameof(CompositionSettingView_FrameRateLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("フレーム/秒")]
        public static readonly string CompositionSettingView_FrameRateUnitLabel = nameof(CompositionSettingView_FrameRateUnitLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("デュレーション:")]
        public static readonly string CompositionSettingView_DurationLabel = nameof(CompositionSettingView_DurationLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("ネスト時にフレームレートを維持")]
        public static readonly string CompositionSettingView_RetentionFrameRate = nameof(CompositionSettingView_RetentionFrameRate);

        [ShowInMarkup, LanguageKey, DefaultValue("ネスト時にトーンマッピングを適用")]
        public static readonly string CompositionSettingView_ApplyToneMappingWhenNested = nameof(CompositionSettingView_ApplyToneMappingWhenNested);

        [ShowInMarkup, LanguageKey, DefaultValue("シャッター角度:")]
        public static readonly string CompositionSettingView_ShutterAngleLabel = nameof(CompositionSettingView_ShutterAngleLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("シャッターフェーズ:")]
        public static readonly string CompositionSettingView_ShutterPhaseLabel = nameof(CompositionSettingView_ShutterPhaseLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("°")]
        public static readonly string CompositionSettingView_DegreeUnitLabel = nameof(CompositionSettingView_DegreeUnitLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("フレームあたりのサンプル数:")]
        public static readonly string CompositionSettingView_MotionBlurSampleCountLabel = nameof(CompositionSettingView_MotionBlurSampleCountLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダラ:")]
        public static readonly string CompositionSettingView_RendererLabel = nameof(CompositionSettingView_RendererLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("トーンマッパー:")]
        public static readonly string CompositionSettingView_ToneMapperLabel = nameof(CompositionSettingView_ToneMapperLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("色の選択")]
        public static readonly string ColorPickerDialog_Title = nameof(ColorPickerDialog_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("OK")]
        public static readonly string Dialog_OK = nameof(Dialog_OK);

        [ShowInMarkup, LanguageKey, DefaultValue("キャンセル")]
        public static readonly string Dialog_Cancel = nameof(Dialog_Cancel);

        [ShowInMarkup, LanguageKey, DefaultValue("A/V機能")]
        public static readonly string Timeline_AVSwitchColumn = nameof(Timeline_AVSwitchColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("タグ")]
        public static readonly string Timeline_TagColumn = nameof(Timeline_TagColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤー番号")]
        public static readonly string Timeline_LayerNumberColumn = nameof(Timeline_LayerNumberColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤー名")]
        public static readonly string Timeline_LayerNameColumn = nameof(Timeline_LayerNameColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("コメント")]
        public static readonly string Timeline_LayerCommentColumn = nameof(Timeline_LayerCommentColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤースイッチ")]
        public static readonly string Timeline_LayerSwitchColumn = nameof(Timeline_LayerSwitchColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("モード")]
        public static readonly string Timeline_ModeColumn = nameof(Timeline_ModeColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("トラックマット")]
        public static readonly string Timeline_TrackMatteColumn = nameof(Timeline_TrackMatteColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("親")]
        public static readonly string Timeline_ParentLayerColumn = nameof(Timeline_ParentLayerColumn);

        [ShowInMarkup, LanguageKey, DefaultValue("(なし)")]
        public static readonly string Timeline_EmptyTitle = nameof(Timeline_EmptyTitle);

        [ShowInMarkup, LanguageKey, DefaultValue("シェイプの追加(_S)")]
        public static readonly string Timeline_ContextMenu_AddShape = nameof(Timeline_ContextMenu_AddShape);

        [ShowInMarkup, LanguageKey, DefaultValue("カメラの追加(_C)")]
        public static readonly string Timeline_ContextMenu_AddCamera = nameof(Timeline_ContextMenu_AddCamera);

        [ShowInMarkup, LanguageKey, DefaultValue("ライトの追加(_L)")]
        public static readonly string Timeline_ContextMenu_AddLight = nameof(Timeline_ContextMenu_AddLight);

        [ShowInMarkup, LanguageKey, DefaultValue("ヌルオブジェクトの追加(_N)")]
        public static readonly string Timeline_ContextMenu_AddNullObject = nameof(Timeline_ContextMenu_AddNullObject);

        [ShowInMarkup, LanguageKey, DefaultValue("テキストの追加(_T)")]
        public static readonly string Timeline_ContextMenu_AddText = nameof(Timeline_ContextMenu_AddText);

        [ShowInMarkup, LanguageKey, DefaultValue("なし")]
        public static readonly string Layer_EmptyTrackMatte = nameof(Layer_EmptyTrackMatte);

        [ShowInMarkup, LanguageKey, DefaultValue("なし")]
        public static readonly string Layer_EmptyParentLayer = nameof(Layer_EmptyParentLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクト")]
        public static readonly string Layer_Effects = nameof(Layer_Effects);

        [ShowInMarkup, LanguageKey, DefaultValue("オーディオ")]
        public static readonly string Layer_Audio = nameof(Layer_Audio);

        [ShowInMarkup, LanguageKey, DefaultValue("ウェーブフォーム")]
        public static readonly string Layer_Audio_WaveForm = nameof(Layer_Audio_WaveForm);

        [ShowInMarkup, LanguageKey, DefaultValue("トランスフォーム")]
        public static readonly string Layer_Transform = nameof(Layer_Transform);

        [ShowInMarkup, LanguageKey, DefaultValue("マテリアルオプション")]
        public static readonly string Layer_LayerOptions_Layer = nameof(Layer_LayerOptions_Layer);

        [ShowInMarkup, LanguageKey, DefaultValue("カメラオプション")]
        public static readonly string Layer_LayerOptions_Camera = nameof(Layer_LayerOptions_Camera);

        [ShowInMarkup, LanguageKey, DefaultValue("ライトオプション")]
        public static readonly string Layer_LayerOptions_Light = nameof(Layer_LayerOptions_Light);

        [ShowInMarkup, LanguageKey, DefaultValue("テキスト")]
        public static readonly string Layer_TextOption = nameof(Layer_TextOption);

        [ShowInMarkup, LanguageKey, DefaultValue("シェイプ")]
        public static readonly string Layer_ShapeOption = nameof(Layer_ShapeOption);

        [ShowInMarkup, LanguageKey, DefaultValue("ソースのオプション")]
        public static readonly string Layer_SourceOption = nameof(Layer_SourceOption);

        [ShowInMarkup, LanguageKey, DefaultValue("オーディオ設定")]
        public static readonly string Layer_AudioOption = nameof(Layer_AudioOption);

        [ShowInMarkup, LanguageKey, DefaultValue("音声レベル")]
        public static readonly string Layer_AudioOption_AudioLevel = nameof(Layer_AudioOption_AudioLevel);

        [ShowInMarkup, LanguageKey, DefaultValue("名前")]
        public static readonly string EffectList_Name = nameof(EffectList_Name);

        [ShowInMarkup, LanguageKey, DefaultValue("カテゴリ")]
        public static readonly string EffectList_Category = nameof(EffectList_Category);

        [ShowInMarkup, LanguageKey, DefaultValue("ヒストリ")]
        public static readonly string HistoryList_Title = nameof(HistoryList_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティコントロール - {0}")]
        public static readonly string LayerPropertyControllerView_Title = nameof(LayerPropertyControllerView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティコントロール - なし")]
        public static readonly string LayerPropertyControllerView_Title_Empty = nameof(LayerPropertyControllerView_Title_Empty);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクト")]
        public static readonly string LayerPropertyControllerView_Effects = nameof(LayerPropertyControllerView_Effects);

        [ShowInMarkup, LanguageKey, DefaultValue("テキスト")]
        public static readonly string TextPropertyView_Title = nameof(TextPropertyView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("サイズ")]
        public static readonly string TextPropertyView_Property_Size = nameof(TextPropertyView_Property_Size);

        [ShowInMarkup, LanguageKey, DefaultValue("行送り")]
        public static readonly string TextPropertyView_Property_LineHeight = nameof(TextPropertyView_Property_LineHeight);

        [ShowInMarkup, LanguageKey, DefaultValue("垂直比率")]
        public static readonly string TextPropertyView_Property_VerticalScale = nameof(TextPropertyView_Property_VerticalScale);

        [ShowInMarkup, LanguageKey, DefaultValue("水平比率")]
        public static readonly string TextPropertyView_Property_HorizontalScale = nameof(TextPropertyView_Property_HorizontalScale);

        [ShowInMarkup, LanguageKey, DefaultValue("字間(未実装)")]
        public static readonly string TextPropertyView_Property_LetterSpacing = nameof(TextPropertyView_Property_LetterSpacing);

        [ShowInMarkup, LanguageKey, DefaultValue("線の太さ")]
        public static readonly string TextPropertyView_Property_LineWidth = nameof(TextPropertyView_Property_LineWidth);

        [ShowInMarkup, LanguageKey, DefaultValue("レベルメーター")]
        public static readonly string AudioInformationView_Title = nameof(AudioInformationView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダーキュー")]
        public static readonly string RenderQueueView_Title = nameof(RenderQueueView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング状況:")]
        public static readonly string RenderQueueView_Progress = nameof(RenderQueueView_Progress);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング")]
        public static readonly string RenderQueueView_Execute = nameof(RenderQueueView_Execute);

        [ShowInMarkup, LanguageKey, DefaultValue("中止")]
        public static readonly string RenderQueueView_Stop = nameof(RenderQueueView_Stop);

        [ShowInMarkup, LanguageKey, DefaultValue("一時停止")]
        public static readonly string RenderQueueView_Pause = nameof(RenderQueueView_Pause);

        [ShowInMarkup, LanguageKey, DefaultValue("一時停止")]
        public static readonly string RenderQueueView_Continue = nameof(RenderQueueView_Continue);

        [ShowInMarkup, LanguageKey, DefaultValue("削除")]
        public static readonly string RenderQueueView_ContextMenu_Delete = nameof(RenderQueueView_ContextMenu_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("出力プラグイン:")]
        public static readonly string RenderQueueItemView_OutputPlugin = nameof(RenderQueueItemView_OutputPlugin);

        [ShowInMarkup, LanguageKey, DefaultValue("出力先:")]
        public static readonly string RenderQueueItemView_OutputFilePath = nameof(RenderQueueItemView_OutputFilePath);

        [ShowInMarkup, LanguageKey, DefaultValue("参照")]
        public static readonly string RenderQueueItemView_OutputFilePath_ChangePath = nameof(RenderQueueItemView_OutputFilePath_ChangePath);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリングする範囲を指定する")]
        public static readonly string RenderQueueItemView_UseRenderQueueItemTimeRange = nameof(RenderQueueItemView_UseRenderQueueItemTimeRange);

        [ShowInMarkup, LanguageKey, DefaultValue("開始:")]
        public static readonly string RenderQueueItemView_TimeRange_Begin = nameof(RenderQueueItemView_TimeRange_Begin);

        [ShowInMarkup, LanguageKey, DefaultValue("終了:")]
        public static readonly string RenderQueueItemView_TimeRange_End = nameof(RenderQueueItemView_TimeRange_End);

        [ShowInMarkup, LanguageKey, DefaultValue("出力ソース")]
        public static readonly string RenderQueueItemView_OutputSources = nameof(RenderQueueItemView_OutputSources);

        [ShowInMarkup, LanguageKey, DefaultValue("ビデオ")]
        public static readonly string RenderQueueItemView_OutputSources_Video = nameof(RenderQueueItemView_OutputSources_Video);

        [ShowInMarkup, LanguageKey, DefaultValue("オーディオ(存在する場合)")]
        public static readonly string RenderQueueItemView_OutputSources_Audio = nameof(RenderQueueItemView_OutputSources_Audio);

        [ShowInMarkup, LanguageKey, DefaultValue("出力プラグインの設定")]
        public static readonly string RenderQueueItemView_OpenOutputSetting = nameof(RenderQueueItemView_OpenOutputSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("出力設定")]
        public static readonly string OutputSettingView_Title = nameof(OutputSettingView_Title);

        // History Command

        [ShowInMarkup, LanguageKey, DefaultValue("プロジェクトの新規作成/開く")]
        public static readonly string History_NewProject = nameof(History_NewProject);

        [ShowInMarkup, LanguageKey, DefaultValue("フォルダの追加")]
        public static readonly string History_AddFolder = nameof(History_AddFolder);

        [ShowInMarkup, LanguageKey, DefaultValue("ファイルの読み込み")]
        public static readonly string History_LoadFootageFile = nameof(History_LoadFootageFile);

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージの移動")]
        public static readonly string History_MoveFootage = nameof(History_MoveFootage);

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージの名前変更")]
        public static readonly string History_ChangeFootageName = nameof(History_ChangeFootageName);

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージのコメント変更")]
        public static readonly string History_ChangeFootageComment = nameof(History_ChangeFootageComment);

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージの削除")]
        public static readonly string History_DeleteFootages = nameof(History_DeleteFootages);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジションの追加")]
        public static readonly string History_AddComposition = nameof(History_AddComposition);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジションの削除")]
        public static readonly string History_RemoveComposition = nameof(History_RemoveComposition);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーの追加")]
        public static readonly string History_AddLayers = nameof(History_AddLayers);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーの移動")]
        public static readonly string History_MoveLayers = nameof(History_MoveLayers);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーの削除")]
        public static readonly string History_DeleteLayers = nameof(History_DeleteLayers);

        [ShowInMarkup, LanguageKey, DefaultValue("平面の追加")]
        public static readonly string History_AddSolid = nameof(History_AddSolid);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤー時間変更")]
        public static readonly string History_EditLayerDuration = nameof(History_EditLayerDuration);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤースイッチの切り替え")]
        public static readonly string History_ChangeLayerSwitch = nameof(History_ChangeLayerSwitch);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーの名前変更")]
        public static readonly string History_ChangeLayerName = nameof(History_ChangeLayerName);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーのコメント変更")]
        public static readonly string History_ChangeLayerComment = nameof(History_ChangeLayerComment);

        [ShowInMarkup, LanguageKey, DefaultValue("合成モードの変更")]
        public static readonly string History_ChangeBlendMode = nameof(History_ChangeBlendMode);

        [ShowInMarkup, LanguageKey, DefaultValue("トラックマットの変更")]
        public static readonly string History_ChangeTrackMatteLayer = nameof(History_ChangeTrackMatteLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("トラックマットのモード変更")]
        public static readonly string History_ChangeTrackMatteMode = nameof(History_ChangeTrackMatteMode);

        [ShowInMarkup, LanguageKey, DefaultValue("親レイヤーの変更")]
        public static readonly string History_ChangeParentLayer = nameof(History_ChangeParentLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーのタグの変更")]
        public static readonly string History_ChangeTagColor = nameof(History_ChangeTagColor);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティの変更")]
        public static readonly string History_ChangePropertyValue = nameof(History_ChangePropertyValue);

        [ShowInMarkup, LanguageKey, DefaultValue("キーフレームの追加")]
        public static readonly string History_AddKeyFrame = nameof(History_AddKeyFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("キーフレームの削除")]
        public static readonly string History_RemoveKeyFrame = nameof(History_RemoveKeyFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("キーフレームの移動")]
        public static readonly string History_MoveKeyFrame = nameof(History_MoveKeyFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("キーフレームの補間法の変更")]
        public static readonly string History_ChangeKeyFrameInterpolationType = nameof(History_ChangeKeyFrameInterpolationType);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトの追加")]
        public static readonly string History_AddEffects = nameof(History_AddEffects);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトの移動")]
        public static readonly string History_MoveEffects = nameof(History_MoveEffects);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトの有効・無効切り替え")]
        public static readonly string History_ChangeEffectsEnable = nameof(History_ChangeEffectsEnable);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトの削除")]
        public static readonly string History_DeleteEffects = nameof(History_DeleteEffects);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトの名前変更")]
        public static readonly string History_ChangeEffectName = nameof(History_ChangeEffectName);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトのコメント変更")]
        public static readonly string History_ChangeEffectComment = nameof(History_ChangeEffectComment);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダーキューの追加")]
        public static readonly string History_EnqueueRendering = nameof(History_EnqueueRendering);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダーキューの削除")]
        public static readonly string History_DeleteRenderQueue = nameof(History_DeleteRenderQueue);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダーキューの設定変更")]
        public static readonly string History_ChangeRenderQueueSetting = nameof(History_ChangeRenderQueueSetting);

        // Dialog

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージの削除")]
        public static readonly string Dialog_ConfirmDeleteFootage_Title = nameof(Dialog_ConfirmDeleteFootage_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("フッテージを削除すると、各コンポジションからこのフッテージを使用しているレイヤーが削除されます。このフッテージを削除しますか?")]
        public static readonly string Dialog_ConfirmDeleteFootage_Text = nameof(Dialog_ConfirmDeleteFootage_Text);

        [ShowInMarkup, LanguageKey, DefaultValue("フォルダを削除すると、中に含まれているフッテージも一緒に削除され、各コンポジションからも含まれているフッテージを使用しているレイヤーが削除されます。このフォルダを削除しますか?")]
        public static readonly string Dialog_ConfirmDeleteFootageFolder_Text = nameof(Dialog_ConfirmDeleteFootageFolder_Text);

        [ShowInMarkup, LanguageKey, DefaultValue("プロジェクトファイル")]
        public static readonly string Dialog_OpenSaveProject_Filter_Project = nameof(Dialog_OpenSaveProject_Filter_Project);

        // Property

        [ShowInMarkup, LanguageKey, DefaultValue("アンカーポイント")]
        public static readonly string TransformProperty_AnchorPoint = nameof(TransformProperty_AnchorPoint);

        [ShowInMarkup, LanguageKey, DefaultValue("位置")]
        public static readonly string TransformProperty_Translate = nameof(TransformProperty_Translate);

        [ShowInMarkup, LanguageKey, DefaultValue("方向")]
        public static readonly string TransformProperty_Direction = nameof(TransformProperty_Direction);

        [ShowInMarkup, LanguageKey, DefaultValue("回転")]
        public static readonly string TransformProperty_ZAngle2D = nameof(TransformProperty_ZAngle2D);

        [ShowInMarkup, LanguageKey, DefaultValue("X回転")]
        public static readonly string TransformProperty_XAngle3D = nameof(TransformProperty_XAngle3D);

        [ShowInMarkup, LanguageKey, DefaultValue("Y回転")]
        public static readonly string TransformProperty_YAngle3D = nameof(TransformProperty_YAngle3D);

        [ShowInMarkup, LanguageKey, DefaultValue("Z回転")]
        public static readonly string TransformProperty_ZAngle3D = nameof(TransformProperty_ZAngle3D);

        [ShowInMarkup, LanguageKey, DefaultValue("スケール")]
        public static readonly string TransformProperty_Scale = nameof(TransformProperty_Scale);

        [ShowInMarkup, LanguageKey, DefaultValue("不透明度")]
        public static readonly string TransformProperty_Opacity = nameof(TransformProperty_Opacity);

        [ShowInMarkup, LanguageKey, DefaultValue("影を落とす")]
        public static readonly string LayerOptionsProperty_IsCastShadow = nameof(LayerOptionsProperty_IsCastShadow);

        [ShowInMarkup, LanguageKey, DefaultValue("ライトを透過")]
        public static readonly string LayerOptionsProperty_LightTransmission = nameof(LayerOptionsProperty_LightTransmission);

        [ShowInMarkup, LanguageKey, DefaultValue("影を受ける")]
        public static readonly string LayerOptionsProperty_IsAcceptShadow = nameof(LayerOptionsProperty_IsAcceptShadow);

        [ShowInMarkup, LanguageKey, DefaultValue("ライトを受ける")]
        public static readonly string LayerOptionsProperty_IsAcceptLight = nameof(LayerOptionsProperty_IsAcceptLight);

        [ShowInMarkup, LanguageKey, DefaultValue("アンビエント")]
        public static readonly string LayerOptionsProperty_Ambient = nameof(LayerOptionsProperty_Ambient);

        [ShowInMarkup, LanguageKey, DefaultValue("拡散")]
        public static readonly string LayerOptionsProperty_Diffuse = nameof(LayerOptionsProperty_Diffuse);

        [ShowInMarkup, LanguageKey, DefaultValue("鏡面強度")]
        public static readonly string LayerOptionsProperty_SpecularIntensity = nameof(LayerOptionsProperty_SpecularIntensity);

        [ShowInMarkup, LanguageKey, DefaultValue("鏡面光沢")]
        public static readonly string LayerOptionsProperty_SpecularShininess = nameof(LayerOptionsProperty_SpecularShininess);

        [ShowInMarkup, LanguageKey, DefaultValue("金属")]
        public static readonly string LayerOptionsProperty_Metal = nameof(LayerOptionsProperty_Metal);

        [ShowInMarkup, LanguageKey, DefaultValue("目標点")]
        public static readonly string TransformProperty_CameraPointOfInterest = nameof(TransformProperty_CameraPointOfInterest);

        [ShowInMarkup, LanguageKey, DefaultValue("ズーム")]
        public static readonly string LayerOptionsProperty_CameraZoom = nameof(LayerOptionsProperty_CameraZoom);

        [ShowInMarkup, LanguageKey, DefaultValue("ライトの種類")]
        public static readonly string LayerOptionsProperty_LightType = nameof(LayerOptionsProperty_LightType);

        [ShowInMarkup, LanguageKey, DefaultValue("色")]
        public static readonly string LayerOptionsProperty_Color = nameof(LayerOptionsProperty_Color);

        [ShowInMarkup, LanguageKey, DefaultValue("強度")]
        public static readonly string LayerOptionsProperty_Intensity = nameof(LayerOptionsProperty_Intensity);

        [ShowInMarkup, LanguageKey, DefaultValue("円錐頂角")]
        public static readonly string LayerOptionsProperty_ConeAngle = nameof(LayerOptionsProperty_ConeAngle);

        [ShowInMarkup, LanguageKey, DefaultValue("円錐ぼかし")]
        public static readonly string LayerOptionsProperty_ConeAttenuation = nameof(LayerOptionsProperty_ConeAttenuation);

        [ShowInMarkup, LanguageKey, DefaultValue("フォールオフの種類")]
        public static readonly string LayerOptionsProperty_FalloffType = nameof(LayerOptionsProperty_FalloffType);

        [ShowInMarkup, LanguageKey, DefaultValue("フォールオフの開始")]
        public static readonly string LayerOptionsProperty_FalloffStart = nameof(LayerOptionsProperty_FalloffStart);

        [ShowInMarkup, LanguageKey, DefaultValue("フォールオフの距離")]
        public static readonly string LayerOptionsProperty_FalloffLength = nameof(LayerOptionsProperty_FalloffLength);

        [ShowInMarkup, LanguageKey, DefaultValue("影を落とす")]
        public static readonly string LayerOptionsProperty_EnableShadow = nameof(LayerOptionsProperty_EnableShadow);

        [ShowInMarkup, LanguageKey, DefaultValue("影の濃さ")]
        public static readonly string LayerOptionsProperty_ShadowStrength = nameof(LayerOptionsProperty_ShadowStrength);

        [ShowInMarkup, LanguageKey, DefaultValue("影のぼかし")]
        public static readonly string LayerOptionsProperty_ShadowScatterSize = nameof(LayerOptionsProperty_ShadowScatterSize);

        [ShowInMarkup, LanguageKey, DefaultValue("ソーステキスト")]
        public static readonly string TextProperty_SourceText = nameof(TextProperty_SourceText);

        [ShowInMarkup, LanguageKey, DefaultValue("詳細")]
        public static readonly string TextProperty_TextMoreOptions = nameof(TextProperty_TextMoreOptions);

        [ShowInMarkup, LanguageKey, DefaultValue("アンカーポイントの基準")]
        public static readonly string TextProperty_TextMoreOptions_BaseAnchorPointRate = nameof(TextProperty_TextMoreOptions_BaseAnchorPointRate);

        [ShowInMarkup, LanguageKey, DefaultValue("テキストボックスのサイズ")]
        public static readonly string TextProperty_TextMoreOptions_TextBoxSize = nameof(TextProperty_TextMoreOptions_TextBoxSize);

        [ShowInMarkup, LanguageKey, DefaultValue("文字間のブレンドモード")]
        public static readonly string TextProperty_TextMoreOptions_InterCharacterBlendMode = nameof(TextProperty_TextMoreOptions_InterCharacterBlendMode);

        [ShowInMarkup, LanguageKey, DefaultValue("テキストアニメータ")]
        public static readonly string TextProperty_TextAnimator = nameof(TextProperty_TextAnimator);

        [ShowInMarkup, LanguageKey, DefaultValue("アニメータ")]
        public static readonly string TextProperty_TextAnimator_Animator = nameof(TextProperty_TextAnimator_Animator);

        [ShowInMarkup, LanguageKey, DefaultValue("範囲セレクタ")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector = nameof(TextProperty_TextAnimator_Animator_Selector);

        [ShowInMarkup, LanguageKey, DefaultValue("セレクタ")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Selector = nameof(TextProperty_TextAnimator_Animator_Selector_Selector);

        [ShowInMarkup, LanguageKey, DefaultValue("開始")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Begin = nameof(TextProperty_TextAnimator_Animator_Selector_Begin);

        [ShowInMarkup, LanguageKey, DefaultValue("終了")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_End = nameof(TextProperty_TextAnimator_Animator_Selector_End);

        [ShowInMarkup, LanguageKey, DefaultValue("オフセット")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Offset = nameof(TextProperty_TextAnimator_Animator_Selector_Offset);

        [ShowInMarkup, LanguageKey, DefaultValue("詳細")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_MoreOption = nameof(TextProperty_TextAnimator_Animator_Selector_MoreOption);

        [ShowInMarkup, LanguageKey, DefaultValue("基準")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Criteria = nameof(TextProperty_TextAnimator_Animator_Selector_Criteria);

        [ShowInMarkup, LanguageKey, DefaultValue("モード")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_BlendMode = nameof(TextProperty_TextAnimator_Animator_Selector_BlendMode);

        [ShowInMarkup, LanguageKey, DefaultValue("量")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Amount = nameof(TextProperty_TextAnimator_Animator_Selector_Amount);

        [ShowInMarkup, LanguageKey, DefaultValue("シェイプ")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Shape = nameof(TextProperty_TextAnimator_Animator_Selector_Shape);

        [ShowInMarkup, LanguageKey, DefaultValue("ランダム")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_EnableRandom = nameof(TextProperty_TextAnimator_Animator_Selector_EnableRandom);

        [ShowInMarkup, LanguageKey, DefaultValue("ランダムシード")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_RandomSeed = nameof(TextProperty_TextAnimator_Animator_Selector_RandomSeed);

        [ShowInMarkup, LanguageKey, DefaultValue("値")]
        public static readonly string TextProperty_TextAnimator_Animator_Value = nameof(TextProperty_TextAnimator_Animator_Value);

        [ShowInMarkup, LanguageKey, DefaultValue("アンカーポイント")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_AnchorPoint = nameof(TextProperty_TextAnimator_Animator_Value_AnchorPoint);

        [ShowInMarkup, LanguageKey, DefaultValue("位置")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Position = nameof(TextProperty_TextAnimator_Animator_Value_Position);

        [ShowInMarkup, LanguageKey, DefaultValue("スケール")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Scale = nameof(TextProperty_TextAnimator_Animator_Value_Scale);

        [ShowInMarkup, LanguageKey, DefaultValue("回転")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Angle = nameof(TextProperty_TextAnimator_Animator_Value_Angle);

        [ShowInMarkup, LanguageKey, DefaultValue("歪曲")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Skew = nameof(TextProperty_TextAnimator_Animator_Value_Skew);

        [ShowInMarkup, LanguageKey, DefaultValue("歪曲軸")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_SkewAxis = nameof(TextProperty_TextAnimator_Animator_Value_SkewAxis);

        [ShowInMarkup, LanguageKey, DefaultValue("不透明度")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Opacity = nameof(TextProperty_TextAnimator_Animator_Value_Opacity);

        [ShowInMarkup, LanguageKey, DefaultValue("フォントサイズ")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_FontSize = nameof(TextProperty_TextAnimator_Animator_Value_FontSize);

        [ShowInMarkup, LanguageKey, DefaultValue("塗りの色")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_FillColor = nameof(TextProperty_TextAnimator_Animator_Value_FillColor);

        [ShowInMarkup, LanguageKey, DefaultValue("塗りの不透明度")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_FillColorOpacity = nameof(TextProperty_TextAnimator_Animator_Value_FillColorOpacity);

        [ShowInMarkup, LanguageKey, DefaultValue("線の色")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_TextLineColor = nameof(TextProperty_TextAnimator_Animator_Value_TextLineColor);

        [ShowInMarkup, LanguageKey, DefaultValue("線の不透明度")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_TextLineColorOpacity = nameof(TextProperty_TextAnimator_Animator_Value_TextLineColorOpacity);

        [ShowInMarkup, LanguageKey, DefaultValue("線の太さ")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_TextLineWidth = nameof(TextProperty_TextAnimator_Animator_Value_TextLineWidth);

        [ShowInMarkup, LanguageKey, DefaultValue("文字のオフセット")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_CharacterOffset = nameof(TextProperty_TextAnimator_Animator_Value_CharacterOffset);

        [ShowInMarkup, LanguageKey, DefaultValue("存在しない文字は空白にする")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_CharacterOffset_WhiteSpaceReplacementChar = nameof(TextProperty_TextAnimator_Animator_Value_CharacterOffset_WhiteSpaceReplacementChar);

        [ShowInMarkup, LanguageKey, DefaultValue("ASCIIの範囲内に収める")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_CharacterOffset_RestrictAscii = nameof(TextProperty_TextAnimator_Animator_Value_CharacterOffset_RestrictAscii);

        [ShowInMarkup, LanguageKey, DefaultValue("ブラー")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Blur = nameof(TextProperty_TextAnimator_Animator_Value_Blur);

        [ShowInMarkup, LanguageKey, DefaultValue("コンテンツ")]
        public static readonly string ShapeProperty_Content = nameof(ShapeProperty_Content);

        [ShowInMarkup, LanguageKey, DefaultValue("グループ")]
        public static readonly string ShapeProperty_Group = nameof(ShapeProperty_Group);

        [ShowInMarkup, LanguageKey, DefaultValue("コンテンツ")]
        public static readonly string ShapeProperty_Group_Content = nameof(ShapeProperty_Group_Content);

        [ShowInMarkup, LanguageKey, DefaultValue("トランスフォーム")]
        public static readonly string ShapeProperty_Transform = nameof(ShapeProperty_Transform);

        [ShowInMarkup, LanguageKey, DefaultValue("アンカーポイント")]
        public static readonly string ShapeProperty_Transform_AnchorPoint = nameof(ShapeProperty_Transform_AnchorPoint);

        [ShowInMarkup, LanguageKey, DefaultValue("位置")]
        public static readonly string ShapeProperty_Transform_Position = nameof(ShapeProperty_Transform_Position);

        [ShowInMarkup, LanguageKey, DefaultValue("スケール")]
        public static readonly string ShapeProperty_Transform_Scale = nameof(ShapeProperty_Transform_Scale);

        [ShowInMarkup, LanguageKey, DefaultValue("回転")]
        public static readonly string ShapeProperty_Transform_Angle = nameof(ShapeProperty_Transform_Angle);

        [ShowInMarkup, LanguageKey, DefaultValue("歪曲")]
        public static readonly string ShapeProperty_Transform_Skew = nameof(ShapeProperty_Transform_Skew);

        [ShowInMarkup, LanguageKey, DefaultValue("歪曲軸")]
        public static readonly string ShapeProperty_Transform_SkewAxis = nameof(ShapeProperty_Transform_SkewAxis);

        [ShowInMarkup, LanguageKey, DefaultValue("不透明度")]
        public static readonly string ShapeProperty_Transform_Opacity = nameof(ShapeProperty_Transform_Opacity);

        [ShowInMarkup, LanguageKey, DefaultValue("長方形")]
        public static readonly string ShapeProperty_RectangleGroup = nameof(ShapeProperty_RectangleGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("角丸")]
        public static readonly string ShapeProperty_RectangleGroup_CornerRounded = nameof(ShapeProperty_RectangleGroup_CornerRounded);

        [ShowInMarkup, LanguageKey, DefaultValue("円")]
        public static readonly string ShapeProperty_CircleGroup = nameof(ShapeProperty_CircleGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("多角形")]
        public static readonly string ShapeProperty_RegularPolygonGroup = nameof(ShapeProperty_RegularPolygonGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("半径")]
        public static readonly string ShapeProperty_RegularPolygonGroup_Radius = nameof(ShapeProperty_RegularPolygonGroup_Radius);

        [ShowInMarkup, LanguageKey, DefaultValue("角の丸み")]
        public static readonly string ShapeProperty_RegularPolygonGroup_Rounded = nameof(ShapeProperty_RegularPolygonGroup_Rounded);

        [ShowInMarkup, LanguageKey, DefaultValue("星")]
        public static readonly string ShapeProperty_StarGroup = nameof(ShapeProperty_StarGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("内半径")]
        public static readonly string ShapeProperty_StarGroup_InnerRadius = nameof(ShapeProperty_StarGroup_InnerRadius);

        [ShowInMarkup, LanguageKey, DefaultValue("外半径")]
        public static readonly string ShapeProperty_StarGroup_OuterRadius = nameof(ShapeProperty_StarGroup_OuterRadius);

        [ShowInMarkup, LanguageKey, DefaultValue("内側の丸み")]
        public static readonly string ShapeProperty_StarGroup_InnerRounded = nameof(ShapeProperty_StarGroup_InnerRounded);

        [ShowInMarkup, LanguageKey, DefaultValue("外側の丸み")]
        public static readonly string ShapeProperty_StarGroup_OuterRounded = nameof(ShapeProperty_StarGroup_OuterRounded);

        [ShowInMarkup, LanguageKey, DefaultValue("頂点数")]
        public static readonly string ShapeProperty_PolygonGroup_Points = nameof(ShapeProperty_PolygonGroup_Points);

        [ShowInMarkup, LanguageKey, DefaultValue("サイズ")]
        public static readonly string ShapeProperty_ShapeObjectGroup_Size = nameof(ShapeProperty_ShapeObjectGroup_Size);

        [ShowInMarkup, LanguageKey, DefaultValue("位置")]
        public static readonly string ShapeProperty_ShapeObjectGroup_Position = nameof(ShapeProperty_ShapeObjectGroup_Position);

        [ShowInMarkup, LanguageKey, DefaultValue("回転")]
        public static readonly string ShapeProperty_ShapeObjectGroup_Angle = nameof(ShapeProperty_ShapeObjectGroup_Angle);

        [ShowInMarkup, LanguageKey, DefaultValue("塗り")]
        public static readonly string ShapeProperty_SolidFillGroup = nameof(ShapeProperty_SolidFillGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("グラデーションの塗り")]
        public static readonly string ShapeProperty_GradientFillGroup = nameof(ShapeProperty_GradientFillGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("規則")]
        public static readonly string ShapeProperty_FillGroup_FillRule = nameof(ShapeProperty_FillGroup_FillRule);

        [ShowInMarkup, LanguageKey, DefaultValue("線")]
        public static readonly string ShapeProperty_SolidStrokeGroup = nameof(ShapeProperty_SolidStrokeGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("グラデーションの線")]
        public static readonly string ShapeProperty_GradientStrokeGroup = nameof(ShapeProperty_GradientStrokeGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("線幅")]
        public static readonly string ShapeProperty_StrokeGroup_Width = nameof(ShapeProperty_StrokeGroup_Width);

        [ShowInMarkup, LanguageKey, DefaultValue("線端")]
        public static readonly string ShapeProperty_StrokeGroup_EndCapStyleType = nameof(ShapeProperty_StrokeGroup_EndCapStyleType);

        [ShowInMarkup, LanguageKey, DefaultValue("線の結合")]
        public static readonly string ShapeProperty_StrokeGroup_JoinStyleType = nameof(ShapeProperty_StrokeGroup_JoinStyleType);

        [ShowInMarkup, LanguageKey, DefaultValue("グラデーションの種類")]
        public static readonly string ShapeProperty_GradientGroup_Type = nameof(ShapeProperty_GradientGroup_Type);

        [ShowInMarkup, LanguageKey, DefaultValue("グラデーションの編集")]
        public static readonly string ShapeProperty_GradientGroup_Color_Edit = nameof(ShapeProperty_GradientGroup_Color_Edit);

        [ShowInMarkup, LanguageKey, DefaultValue("OKLab色空間で補間する")]
        public static readonly string ShapeProperty_GradientGroup_UseOkLabInterpolation = nameof(ShapeProperty_GradientGroup_UseOkLabInterpolation);

        [ShowInMarkup, LanguageKey, DefaultValue("開始点")]
        public static readonly string ShapeProperty_GradientGroup_BeginPosition = nameof(ShapeProperty_GradientGroup_BeginPosition);

        [ShowInMarkup, LanguageKey, DefaultValue("終了点")]
        public static readonly string ShapeProperty_GradientGroup_EndPosition = nameof(ShapeProperty_GradientGroup_EndPosition);

        [ShowInMarkup, LanguageKey, DefaultValue("色")]
        public static readonly string ShapeProperty_Drawing_Color = nameof(ShapeProperty_Drawing_Color);

        [ShowInMarkup, LanguageKey, DefaultValue("不透明度")]
        public static readonly string ShapeProperty_Drawing_Opacity = nameof(ShapeProperty_Drawing_Opacity);

        [ShowInMarkup, LanguageKey, DefaultValue("ブレンドモード")]
        public static readonly string ShapeProperty_Drawing_BlendMode = nameof(ShapeProperty_Drawing_BlendMode);

        [ShowInMarkup, LanguageKey, DefaultValue("リピータ")]
        public static readonly string ShapeProperty_RepeaterGroup = nameof(ShapeProperty_RepeaterGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("数")]
        public static readonly string ShapeProperty_RepeaterGroup_Count = nameof(ShapeProperty_RepeaterGroup_Count);

        [ShowInMarkup, LanguageKey, DefaultValue("オフセット")]
        public static readonly string ShapeProperty_RepeaterGroup_Offset = nameof(ShapeProperty_RepeaterGroup_Offset);

        [ShowInMarkup, LanguageKey, DefaultValue("開始点の不透明度")]
        public static readonly string ShapeProperty_RepeaterGroup_Transform_BeginPointOpacity = nameof(ShapeProperty_RepeaterGroup_Transform_BeginPointOpacity);

        [ShowInMarkup, LanguageKey, DefaultValue("終了点の不透明度")]
        public static readonly string ShapeProperty_RepeaterGroup_Transform_EndPointOpacity = nameof(ShapeProperty_RepeaterGroup_Transform_EndPointOpacity);

        [ShowInMarkup, LanguageKey, DefaultValue("パスの結合")]
        public static readonly string ShapeProperty_CombineGroup = nameof(ShapeProperty_CombineGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("種類")]
        public static readonly string ShapeProperty_CombineGroup_CombineType = nameof(ShapeProperty_CombineGroup_CombineType);

        // Property Control

        [ShowInMarkup, LanguageKey, DefaultValue("編集")]
        public static readonly string SourceTextPropertyControl_Edit = nameof(SourceTextPropertyControl_Edit);

        // Unit

        [ShowInMarkup, LanguageKey, DefaultValue("%")]
        public static readonly string Unit_Percent = nameof(Unit_Percent);

        [ShowInMarkup, LanguageKey, DefaultValue("°")]
        public static readonly string Unit_Angle = nameof(Unit_Angle);

        [ShowInMarkup, LanguageKey, DefaultValue("px")]
        public static readonly string Unit_Pixel = nameof(Unit_Pixel);

        [ShowInMarkup, LanguageKey, DefaultValue("dB")]
        public static readonly string Unit_Decibel = nameof(Unit_Decibel);

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

        [ShowInMarkup, DefaultValue("ポイント")]
        public static readonly string LightType_Point = nameof(LightType_Point);

        [ShowInMarkup, DefaultValue("スポット")]
        public static readonly string LightType_Spot = nameof(LightType_Spot);

        [ShowInMarkup, DefaultValue("平行")]
        public static readonly string LightType_Parallel = nameof(LightType_Parallel);

        [ShowInMarkup, DefaultValue("アンビエント")]
        public static readonly string LightType_Ambient = nameof(LightType_Ambient);

        [ShowInMarkup, DefaultValue("なし")]
        public static readonly string LightFalloffType_None = nameof(LightFalloffType_None);

        [ShowInMarkup, DefaultValue("リニア")]
        public static readonly string LightFalloffType_Linear = nameof(LightFalloffType_Linear);

        [ShowInMarkup, DefaultValue("逆二乗クランプ")]
        public static readonly string LightFalloffType_Exponential = nameof(LightFalloffType_Exponential);

        [ShowInMarkup, DefaultValue("線の上に塗り")]
        public static readonly string TextLineDrawOrder_BeforeFill = nameof(TextLineDrawOrder_BeforeFill);

        [ShowInMarkup, DefaultValue("塗りの上に線")]
        public static readonly string TextLineDrawOrder_AfterFill = nameof(TextLineDrawOrder_AfterFill);

        [ShowInMarkup, DefaultValue("文字")]
        public static readonly string SelectorCriteria_Charactor = nameof(SelectorCriteria_Charactor);

        [ShowInMarkup, DefaultValue("空白を除いた文字")]
        public static readonly string SelectorCriteria_CharactorWithoutSpace = nameof(SelectorCriteria_CharactorWithoutSpace);

        [ShowInMarkup, DefaultValue("単語")]
        public static readonly string SelectorCriteria_Word = nameof(SelectorCriteria_Word);

        [ShowInMarkup, DefaultValue("行")]
        public static readonly string SelectorCriteria_Line = nameof(SelectorCriteria_Line);

        [ShowInMarkup, DefaultValue("加算")]
        public static readonly string SelectorBlendMode_Add = nameof(SelectorBlendMode_Add);

        [ShowInMarkup, DefaultValue("減算")]
        public static readonly string SelectorBlendMode_Subtract = nameof(SelectorBlendMode_Subtract);

        [ShowInMarkup, DefaultValue("乗算")]
        public static readonly string SelectorBlendMode_Multiply = nameof(SelectorBlendMode_Multiply);

        [ShowInMarkup, DefaultValue("最小")]
        public static readonly string SelectorBlendMode_Min = nameof(SelectorBlendMode_Min);

        [ShowInMarkup, DefaultValue("最大")]
        public static readonly string SelectorBlendMode_Max = nameof(SelectorBlendMode_Max);

        [ShowInMarkup, DefaultValue("差")]
        public static readonly string SelectorBlendMode_Difference = nameof(SelectorBlendMode_Difference);

        [ShowInMarkup, DefaultValue("四角")]
        public static readonly string SelectorShape_Rectangle = nameof(SelectorShape_Rectangle);

        [ShowInMarkup, DefaultValue("上へ傾斜")]
        public static readonly string SelectorShape_RampUp = nameof(SelectorShape_RampUp);

        [ShowInMarkup, DefaultValue("下へ傾斜")]
        public static readonly string SelectorShape_RampDown = nameof(SelectorShape_RampDown);

        [ShowInMarkup, DefaultValue("三角形")]
        public static readonly string SelectorShape_Triangle = nameof(SelectorShape_Triangle);

        [ShowInMarkup, DefaultValue("円")]
        public static readonly string SelectorShape_Circle = nameof(SelectorShape_Circle);

        [ShowInMarkup, DefaultValue("非ゼロ規則")]
        public static readonly string ShapeFillRule_NonZero = nameof(ShapeFillRule_NonZero);

        [ShowInMarkup, DefaultValue("奇偶規則")]
        public static readonly string ShapeFillRule_EvenOdd = nameof(ShapeFillRule_EvenOdd);

        [ShowInMarkup, DefaultValue("線形")]
        public static readonly string GradientType_Linear = nameof(GradientType_Linear);

        [ShowInMarkup, DefaultValue("円形")]
        public static readonly string GradientType_Radial = nameof(GradientType_Radial);

        [ShowInMarkup, DefaultValue("バット")]
        public static readonly string EndCapStyle_Butt = nameof(EndCapStyle_Butt);

        [ShowInMarkup, DefaultValue("丸型")]
        public static readonly string EndCapStyle_Round = nameof(EndCapStyle_Round);

        [ShowInMarkup, DefaultValue("四角")]
        public static readonly string EndCapStyle_Square = nameof(EndCapStyle_Square);

        [ShowInMarkup, DefaultValue("ポリゴン")]
        public static readonly string EndCapStyle_Polygon = nameof(EndCapStyle_Polygon);

        [ShowInMarkup, DefaultValue("結合")]
        public static readonly string EndCapStyle_Joined = nameof(EndCapStyle_Joined);

        [ShowInMarkup, DefaultValue("四角")]
        public static readonly string JointStyle_Square = nameof(JointStyle_Square);

        [ShowInMarkup, DefaultValue("丸型")]
        public static readonly string JointStyle_Round = nameof(JointStyle_Round);

        [ShowInMarkup, DefaultValue("マイター")]
        public static readonly string JointStyle_Miter = nameof(JointStyle_Miter);

        [ShowInMarkup, DefaultValue("結合")]
        public static readonly string ClippingOperation_None = nameof(ClippingOperation_None);

        [ShowInMarkup, DefaultValue("交差")]
        public static readonly string ClippingOperation_Intersection = nameof(ClippingOperation_Intersection);

        [ShowInMarkup, DefaultValue("追加")]
        public static readonly string ClippingOperation_Union = nameof(ClippingOperation_Union);

        [ShowInMarkup, DefaultValue("差")]
        public static readonly string ClippingOperation_Difference = nameof(ClippingOperation_Difference);

        [ShowInMarkup, DefaultValue("中マド")]
        public static readonly string ClippingOperation_Xor = nameof(ClippingOperation_Xor);

        [ShowInMarkup, DefaultValue("ステレオ")]
        public static readonly string WaveFormType_Stereo = nameof(WaveFormType_Stereo);

        [ShowInMarkup, DefaultValue("モノラル")]
        public static readonly string WaveFormType_Monaural = nameof(WaveFormType_Monaural);

        [ShowInMarkup, DefaultValue("左チャンネル")]
        public static readonly string WaveFormType_Left = nameof(WaveFormType_Left);

        [ShowInMarkup, DefaultValue("右チャンネル")]
        public static readonly string WaveFormType_Right = nameof(WaveFormType_Right);

        // Inner Plugin

        [DefaultValue("なし")]
        public const string ToneMapper_NoOpToneMapper_Name = nameof(ToneMapper_NoOpToneMapper_Name);

        [DefaultValue("トーンマッピングを行いません")]
        public const string ToneMapper_NoOpToneMapper_Description = nameof(ToneMapper_NoOpToneMapper_Description);

        static LanguageResourceDictionary()
        {
            LanguageKeys = typeof(LanguageResourceDictionary).GetFields(BindingFlags.Static | BindingFlags.Public)
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
