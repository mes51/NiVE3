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

        public static LanguageResourceDictionary JPDictionary { get; }

        static Dictionary<string, Tuple<string, Version>> LanguageKeys { get; }

        [ShowInMarkup, LanguageKey, DefaultValue("NicoVisualEffects 3{0}")]
        public static readonly string MainWindow_Title = nameof(MainWindow_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("{0} - NicoVisualEffects 3{1}")]
        public static readonly string MainWindow_TitleWithPath = nameof(MainWindow_TitleWithPath);

        [ShowInMarkup, LanguageKey, DefaultValue("ファイル(_F)")]
        public static readonly string MainWindow_Menu_File = nameof(MainWindow_Menu_File);

        [ShowInMarkup, LanguageKey, DefaultValue("編集(_E)")]
        public static readonly string MainWindow_Menu_Edit = nameof(MainWindow_Menu_Edit);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクト(_P)")]
        public static readonly string MainWindow_Menu_Effect = nameof(MainWindow_Menu_Effect);

        [ShowInMarkup, LanguageKey, DefaultValue("表示(_V)")]
        public static readonly string MainWindow_Menu_View = nameof(MainWindow_Menu_View);

        [ShowInMarkup, LanguageKey, DefaultValue("ツール(_T)")]
        public static readonly string MainWindow_Menu_Tool = nameof(MainWindow_Menu_Tool);

        [ShowInMarkup, LanguageKey, DefaultValue("ヘルプ(_H)")]
        public static readonly string MainWindow_Menu_Help = nameof(MainWindow_Menu_Help);

        [ShowInMarkup, LanguageKey, DefaultValue("新規(_N)")]
        public static readonly string MainWindow_Menu_File_New = nameof(MainWindow_Menu_File_New);

        [ShowInMarkup, LanguageKey, DefaultValue("新規プロジェクト(_P)")]
        public static readonly string MainWindow_MenuItem_File_NewProject = nameof(MainWindow_MenuItem_File_NewProject);

        [ShowInMarkup, LanguageKey, DefaultValue("新規フォルダ(_F)")]
        public static readonly string MainWindow_MenuItem_File_NewFolder = nameof(MainWindow_MenuItem_File_NewFolder);

        [ShowInMarkup, LanguageKey, DefaultValue("新規コンポジション...(_C)")]
        public static readonly string MainWindow_MenuItem_File_NewComposition = nameof(MainWindow_MenuItem_File_NewComposition);

        [ShowInMarkup, LanguageKey, DefaultValue("プロジェクトを開く(_O)")]
        public static readonly string MainWindow_MenuItem_File_OpenProject = nameof(MainWindow_MenuItem_File_OpenProject);

        [ShowInMarkup, LanguageKey, DefaultValue("プロジェクトを保存(_S)")]
        public static readonly string MainWindow_MenuItem_File_SaveProject = nameof(MainWindow_MenuItem_File_SaveProject);

        [ShowInMarkup, LanguageKey, DefaultValue("別名でプロジェクトを保存(_V)")]
        public static readonly string MainWindow_MenuItem_File_SaveProjectAsNewName = nameof(MainWindow_MenuItem_File_SaveProjectAsNewName);

        [ShowInMarkup, LanguageKey, DefaultValue("読み込み(_L)")]
        public static readonly string MainWindow_MenuItem_File_Load = nameof(MainWindow_MenuItem_File_Load);

        [ShowInMarkup, LanguageKey, DefaultValue("ファイル...(_F)")]
        public static readonly string MainWindow_MenuItem_File_LoadFile = nameof(MainWindow_MenuItem_File_LoadFile);

        [ShowInMarkup, LanguageKey, DefaultValue("平面...(_S)")]
        public static readonly string MainWindow_MenuItem_File_LoadSolid = nameof(MainWindow_MenuItem_File_LoadSolid);

        [ShowInMarkup, LanguageKey, DefaultValue("終了(_X)")]
        public static readonly string MainWindow_MenuItem_File_Exit = nameof(MainWindow_MenuItem_File_Exit);

        [ShowInMarkup, LanguageKey, DefaultValue("元に戻す(_U)")]
        public static readonly string MainWindow_MenuItem_Edit_Undo = nameof(MainWindow_MenuItem_Edit_Undo);

        [ShowInMarkup, LanguageKey, DefaultValue("やり直し(_R)")]
        public static readonly string MainWindow_MenuItem_Edit_Redo = nameof(MainWindow_MenuItem_Edit_Redo);

        [ShowInMarkup, LanguageKey, DefaultValue("切り取り(_X)")]
        public static readonly string MainWindow_MenuItem_Edit_Cut = nameof(MainWindow_MenuItem_Edit_Cut);

        [ShowInMarkup, LanguageKey, DefaultValue("コピー(_C)")]
        public static readonly string MainWindow_MenuItem_Edit_Copy = nameof(MainWindow_MenuItem_Edit_Copy);

        [ShowInMarkup, LanguageKey, DefaultValue("ペースト(_P)")]
        public static readonly string MainWindow_MenuItem_Edit_Paste = nameof(MainWindow_MenuItem_Edit_Paste);

        [ShowInMarkup, LanguageKey, DefaultValue("複製(_L)")]
        public static readonly string MainWindow_MenuItem_Edit_Duplicate = nameof(MainWindow_MenuItem_Edit_Duplicate);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーを分割(_S)")]
        public static readonly string MainWindow_MenuItem_Edit_SplitLayer = nameof(MainWindow_MenuItem_Edit_SplitLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("削除(_D)")]
        public static readonly string MainWindow_MenuItem_Edit_Delete = nameof(MainWindow_MenuItem_Edit_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("すべて選択(_A)")]
        public static readonly string MainWindow_MenuItem_Edit_SelectAll = nameof(MainWindow_MenuItem_Edit_SelectAll);

        [ShowInMarkup, LanguageKey, DefaultValue("環境設定(_O)...")]
        public static readonly string MainWindow_MenuItem_Tool_OpenSetting = nameof(MainWindow_MenuItem_Tool_OpenSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("ショートカットキー設定(_S)...")]
        public static readonly string MainWindow_MenuItem_Tool_OpenShortcutKeySetting = nameof(MainWindow_MenuItem_Tool_OpenShortcutKeySetting);

        [ShowInMarkup, LanguageKey, DefaultValue("NicoVisualEffectsについて(_A)...")]
        public static readonly string MainWindow_MenuItem_Help_About = nameof(MainWindow_MenuItem_Help_About);

        [ShowInMarkup, LanguageKey, DefaultValue("再生コントロール")]
        public static readonly string PlayControlView_Title = nameof(PlayControlView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("RAMプレビュー")]
        public static readonly string PlayControlView_UseRamPreview = nameof(PlayControlView_UseRamPreview);

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

        [ShowInMarkup, LanguageKey, DefaultValue("削除(_D)")]
        public static readonly string FootageListView_ContextMenu_Delete = nameof(FootageListView_ContextMenu_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("新規(_N)")]
        public static readonly string FootageListView_ContextMenu_New = nameof(FootageListView_ContextMenu_New);

        [ShowInMarkup, LanguageKey, DefaultValue("新規コンポジション...(_C)")]
        public static readonly string FootageListView_ContextMenu_NewComposition = nameof(FootageListView_ContextMenu_NewComposition);

        [ShowInMarkup, LanguageKey, DefaultValue("新規フォルダ(_F)")]
        public static readonly string FootageListView_ContextMenu_NewFolder = nameof(FootageListView_ContextMenu_NewFolder);

        [ShowInMarkup, LanguageKey, DefaultValue("読み込み(_L)")]
        public static readonly string FootageListView_ContextMenu_Load = nameof(FootageListView_ContextMenu_Load);

        [ShowInMarkup, LanguageKey, DefaultValue("ファイル...(_F)")]
        public static readonly string FootageListView_ContextMenu_LoadFile = nameof(FootageListView_ContextMenu_LoadFile);

        [ShowInMarkup, LanguageKey, DefaultValue("平面...(_S)")]
        public static readonly string FootageListView_ContextMenu_LoadSolid = nameof(FootageListView_ContextMenu_LoadSolid);

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

        [DefaultValue("入力設定")]
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

        [ShowInMarkup, LanguageKey, DefaultValue("プラグイン")]
        public static readonly string CompositionSettingView_PluginTab = nameof(CompositionSettingView_PluginTab);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジション設定")]
        public static readonly string CompositionSettingView_CompositionSettingGroup = nameof(CompositionSettingView_CompositionSettingGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("モーションブラー設定")]
        public static readonly string CompositionSettingView_MotionBlurSettingGroup = nameof(CompositionSettingView_MotionBlurSettingGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("プラグイン設定")]
        public static readonly string CompositionSettingView_PluginSettingGroup = nameof(CompositionSettingView_PluginSettingGroup);

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

        [ShowInMarkup, LanguageKey, DefaultValue("フレームあたりのサンプル数:")]
        public static readonly string CompositionSettingView_MotionBlurSampleCountLabel = nameof(CompositionSettingView_MotionBlurSampleCountLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダラ:")]
        public static readonly string CompositionSettingView_RendererLabel = nameof(CompositionSettingView_RendererLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("トーンマッパー:")]
        public static readonly string CompositionSettingView_ToneMapperLabel = nameof(CompositionSettingView_ToneMapperLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダラの設定")]
        public static readonly string CompositionSettingView_RendererSetting = nameof(CompositionSettingView_RendererSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("トーンマッパーの設定")]
        public static readonly string CompositionSettingView_ToneMapperSetting = nameof(CompositionSettingView_ToneMapperSetting);

        [DefaultValue("レンダラ設定")]
        public static readonly string RendererSettingView_Title = nameof(RendererSettingView_Title);

        [DefaultValue("トーンマッパー設定")]
        public static readonly string ToneMapperSettingView_Title = nameof(ToneMapperSettingView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("カスタム")]
        public static readonly string CompositionSettingView_PresetName_Custom = nameof(CompositionSettingView_PresetName_Custom);

        [ShowInMarkup, LanguageKey, DefaultValue("色の選択")]
        public static readonly string ColorPickerDialog_Title = nameof(ColorPickerDialog_Title);

        [ShowInMarkup, DefaultValue("エラー情報")]
        public static readonly string DebugMessageBox_ExceptionInfo_Header = nameof(DebugMessageBox_ExceptionInfo_Header);

        [ShowInMarkup, DefaultValue("予期されなかったエラー")]
        public static readonly string UnhandledExceptionWindow_Title = nameof(UnhandledExceptionWindow_Title);

        [ShowInMarkup, DefaultValue("予期されなかったエラーが発生したため、動作を停止しました。申し訳ありません。")]
        public static readonly string UnhandledExceptionWindow_Message = nameof(UnhandledExceptionWindow_Message);

        [ShowInMarkup, DefaultValue("例外情報:")]
        public static readonly string UnhandledExceptionWindow_ExceptionInfo = nameof(UnhandledExceptionWindow_ExceptionInfo);

        [ShowInMarkup, LanguageKey, DefaultValue("適用")]
        public static readonly string Dialog_Apply = nameof(Dialog_Apply);

        [ShowInMarkup, LanguageKey, DefaultValue("OK")]
        public static readonly string Dialog_OK = nameof(Dialog_OK);

        [ShowInMarkup, LanguageKey, DefaultValue("キャンセル")]
        public static readonly string Dialog_Cancel = nameof(Dialog_Cancel);

        [ShowInMarkup, LanguageKey, DefaultValue("はい")]
        public static readonly string Dialog_Yes = nameof(Dialog_Yes);

        [ShowInMarkup, LanguageKey, DefaultValue("いいえ")]
        public static readonly string Dialog_No = nameof(Dialog_No);

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

        [ShowInMarkup, LanguageKey, DefaultValue("追加(_A)")]
        public static readonly string Timeline_ContextMenu_AddLayer = nameof(Timeline_ContextMenu_AddLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("シェイプ(_G)")]
        public static readonly string Timeline_ContextMenu_AddLayer_Shape = nameof(Timeline_ContextMenu_AddLayer_Shape);

        [ShowInMarkup, LanguageKey, DefaultValue("カメラ(_C)")]
        public static readonly string Timeline_ContextMenu_AddLayer_Camera = nameof(Timeline_ContextMenu_AddLayer_Camera);

        [ShowInMarkup, LanguageKey, DefaultValue("ライト(_L)")]
        public static readonly string Timeline_ContextMenu_AddLayer_Light = nameof(Timeline_ContextMenu_AddLayer_Light);

        [ShowInMarkup, LanguageKey, DefaultValue("ヌルオブジェクト(_N)")]
        public static readonly string Timeline_ContextMenu_AddLayer_NullObject = nameof(Timeline_ContextMenu_AddLayer_NullObject);

        [ShowInMarkup, LanguageKey, DefaultValue("平面(_S)...")]
        public static readonly string Timeline_ContextMenu_AddLayer_LoadSolid = nameof(Timeline_ContextMenu_AddLayer_LoadSolid);

        [ShowInMarkup, LanguageKey, DefaultValue("プロシージャル(_P)")]
        public static readonly string Timeline_ContextMenu_AddLayer_Procedural = nameof(Timeline_ContextMenu_AddLayer_Procedural);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクト(_E)")]
        public static readonly string Timeline_ContextMenu_Effect = nameof(Timeline_ContextMenu_Effect);

        [ShowInMarkup, LanguageKey, DefaultValue("マスク(_M)")]
        public static readonly string Timeline_ContextMenu_Mask = nameof(Timeline_ContextMenu_Mask);

        [ShowInMarkup, LanguageKey, DefaultValue("長方形を追加(_R)")]
        public static readonly string Timeline_ContextMenu_Mask_AddRectangle = nameof(Timeline_ContextMenu_Mask_AddRectangle);

        [ShowInMarkup, LanguageKey, DefaultValue("楕円形を追加(_E)")]
        public static readonly string Timeline_ContextMenu_Mask_AddEllipse = nameof(Timeline_ContextMenu_Mask_AddEllipse);

        [ShowInMarkup, LanguageKey, DefaultValue("パスを追加(_P)")]
        public static readonly string Timeline_ContextMenu_Mask_AddBezier = nameof(Timeline_ContextMenu_Mask_AddBezier);

        [ShowInMarkup, LanguageKey, DefaultValue("テキスト(_T)")]
        public static readonly string Timeline_ContextMenu_AddLayer_Text = nameof(Timeline_ContextMenu_AddLayer_Text);

        [ShowInMarkup, LanguageKey, DefaultValue("切り取り(_X)")]
        public static readonly string Timeline_ContextMenu_Cut = nameof(Timeline_ContextMenu_Cut);

        [ShowInMarkup, LanguageKey, DefaultValue("コピー(_C)")]
        public static readonly string Timeline_ContextMenu_Copy = nameof(Timeline_ContextMenu_Copy);

        [ShowInMarkup, LanguageKey, DefaultValue("ペースト(_P)")]
        public static readonly string Timeline_ContextMenu_Paste = nameof(Timeline_ContextMenu_Paste);

        [ShowInMarkup, LanguageKey, DefaultValue("複製(_L)")]
        public static readonly string Timeline_ContextMenu_Duplicate = nameof(Timeline_ContextMenu_Duplicate);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーを分割(_S)")]
        public static readonly string Timeline_ContextMenu_SplitLayer = nameof(Timeline_ContextMenu_SplitLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("削除(_D)")]
        public static readonly string Timeline_ContextMenu_Delete = nameof(Timeline_ContextMenu_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("時間(_T)")]
        public static readonly string Timeline_ContextMenu_Time = nameof(Timeline_ContextMenu_Time);

        [ShowInMarkup, LanguageKey, DefaultValue("インポイントを基準にインジケーターの位置にレイヤー時間を移動(_B)")]
        public static readonly string Timeline_ContextMenu_MoveSourceStartPointToIndicatorBaseInPoint = nameof(Timeline_ContextMenu_MoveSourceStartPointToIndicatorBaseInPoint);

        [ShowInMarkup, LanguageKey, DefaultValue("アウトポイントを基準にインジケーターの位置にレイヤー時間を移動(_E)")]
        public static readonly string Timeline_ContextMenu_MoveSourceStartPointToIndicatorBaseOutPoint = nameof(Timeline_ContextMenu_MoveSourceStartPointToIndicatorBaseOutPoint);

        [ShowInMarkup, LanguageKey, DefaultValue("インポイントをインジケーターの位置に移動(_I)")]
        public static readonly string Timeline_ContextMenu_MoveInPointToIndicator = nameof(Timeline_ContextMenu_MoveInPointToIndicator);

        [ShowInMarkup, LanguageKey, DefaultValue("アウトポイントをインジケーターの位置に移動(_O)")]
        public static readonly string Timeline_ContextMenu_MoveOutPointToIndicator = nameof(Timeline_ContextMenu_MoveOutPointToIndicator);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーを1フレーム後ろにシフト(_N)")]
        public static readonly string Timeline_ContextMenu_ShiftSourceStartPointToNextFrame = nameof(Timeline_ContextMenu_ShiftSourceStartPointToNextFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーを1フレーム前にシフト(_P)")]
        public static readonly string Timeline_ContextMenu_ShiftSourceStartPointToPreviousFrame= nameof(Timeline_ContextMenu_ShiftSourceStartPointToPreviousFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーを10フレーム後ろにシフト(_A)")]
        public static readonly string Timeline_ContextMenu_ShiftSourceStartPointToNext10Frame = nameof(Timeline_ContextMenu_ShiftSourceStartPointToNext10Frame);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーを10フレーム前にシフト(_R)")]
        public static readonly string Timeline_ContextMenu_ShiftSourceStartPointToPrevious10Frame = nameof(Timeline_ContextMenu_ShiftSourceStartPointToPrevious10Frame);

        [ShowInMarkup, LanguageKey, DefaultValue("再生速度を変更...(_S)")]
        public static readonly string Timeline_ContextMenu_ChangePlayRate = nameof(Timeline_ContextMenu_ChangePlayRate);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーの表示フレームを現在時刻で固定(_F)")]
        public static readonly string Timeline_ContextMenu_ChangeLayerFreezeFrame = nameof(Timeline_ContextMenu_ChangeLayerFreezeFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーの順番を1つ上げる(_U)")]
        public static readonly string Timeline_ContextMenu_MoveLayerOrderUp = nameof(Timeline_ContextMenu_MoveLayerOrderUp);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーの順番を1つ下げる(_W)")]
        public static readonly string Timeline_ContextMenu_MoveLayerOrderDown = nameof(Timeline_ContextMenu_MoveLayerOrderDown);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーのタグをランダムに変更(_R)")]
        public static readonly string Timeline_ContextMenu_ChangeLayerTagsRandomly = nameof(Timeline_ContextMenu_ChangeLayerTagsRandomly);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクト/マスクプリセットの適用(_G)...")]
        public static readonly string Timeline_ContextMenu_LoadEffectOrMaskPreset = nameof(Timeline_ContextMenu_LoadEffectOrMaskPreset);

        [ShowInMarkup, LanguageKey, DefaultValue("コンポジションの設定...(_O)")]
        public static readonly string Timeline_ContextMenu_CompositionSetting = nameof(Timeline_ContextMenu_CompositionSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("シャイが有効なレイヤーの表示/非表示")]
        public static readonly string Timeline_ToolTip_IsEnableShy = nameof(Timeline_ToolTip_IsEnableShy);

        [ShowInMarkup, LanguageKey, DefaultValue("フレームブレンドの有効/無効")]
        public static readonly string Timeline_ToolTip_IsEnableFrameBlend = nameof(Timeline_ToolTip_IsEnableFrameBlend);

        [ShowInMarkup, LanguageKey, DefaultValue("モーションブラーの有効/無効")]
        public static readonly string Timeline_ToolTip_IsEnableMotionBlur = nameof(Timeline_ToolTip_IsEnableMotionBlur);

        [ShowInMarkup, LanguageKey, DefaultValue("画像・映像の表示/非表示")]
        public static readonly string Timeline_ToolTip_IsEnableVideo = nameof(Timeline_ToolTip_IsEnableVideo);

        [ShowInMarkup, LanguageKey, DefaultValue("音声の有効/無効")]
        public static readonly string Timeline_ToolTip_IsEnableAudio = nameof(Timeline_ToolTip_IsEnableAudio);

        [ShowInMarkup, LanguageKey, DefaultValue("ソロの有効/無効")]
        public static readonly string Timeline_ToolTip_IsEnableSolo = nameof(Timeline_ToolTip_IsEnableSolo);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーのロック")]
        public static readonly string Timeline_ToolTip_IsLock = nameof(Timeline_ToolTip_IsLock);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーのタグの色")]
        public static readonly string Timeline_ToolTip_LayerTagColor = nameof(Timeline_ToolTip_LayerTagColor);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤー番号")]
        public static readonly string Timeline_ToolTip_LayerNumber = nameof(Timeline_ToolTip_LayerNumber);

        [ShowInMarkup, LanguageKey, DefaultValue("シャイの有効/無効")]
        public static readonly string Timeline_ToolTip_LayerSwitch_IsEnableShy = nameof(Timeline_ToolTip_LayerSwitch_IsEnableShy);

        [ShowInMarkup, LanguageKey, DefaultValue("未実装")]
        public static readonly string Timeline_ToolTip_LayerSwitch_IsEnableCollapse = nameof(Timeline_ToolTip_LayerSwitch_IsEnableCollapse);

        [ShowInMarkup, LanguageKey, DefaultValue("画像の補間品質")]
        public static readonly string Timeline_ToolTip_LayerSwitch_InterpolationQuality = nameof(Timeline_ToolTip_LayerSwitch_InterpolationQuality);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトの有効/無効")]
        public static readonly string Timeline_ToolTip_LayerSwitch_IsEnableEffect = nameof(Timeline_ToolTip_LayerSwitch_IsEnableEffect);

        [ShowInMarkup, LanguageKey, DefaultValue("フレームブレンドの有効/無効")]
        public static readonly string Timeline_ToolTip_LayerSwitch_IsEnableFrameBlend = nameof(Timeline_ToolTip_LayerSwitch_IsEnableFrameBlend);

        [ShowInMarkup, LanguageKey, DefaultValue("モーションブラーの有効/無効")]
        public static readonly string Timeline_ToolTip_LayerSwitch_IsEnableMotionBlur = nameof(Timeline_ToolTip_LayerSwitch_IsEnableMotionBlur);

        [ShowInMarkup, LanguageKey, DefaultValue("調整レイヤー化する/しない")]
        public static readonly string Timeline_ToolTip_LayerSwitch_IsEnableAdjustmentLayer = nameof(Timeline_ToolTip_LayerSwitch_IsEnableAdjustmentLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("3Dレイヤー化する/しない")]
        public static readonly string Timeline_ToolTip_LayerSwitch_IsEnable3D = nameof(Timeline_ToolTip_LayerSwitch_IsEnable3D);

        [ShowInMarkup, LanguageKey, DefaultValue("ワークエリアの範囲指定")]
        public static readonly string Timeline_ToolTip_WorkareaBar = nameof(Timeline_ToolTip_WorkareaBar);

        [ShowInMarkup, LanguageKey, DefaultValue("タイムラインの時間軸のスクロール、拡大/縮小")]
        public static readonly string Timeline_ToolTip_RangeScrollBar = nameof(Timeline_ToolTip_RangeScrollBar);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクト(_E)")]
        public static readonly string LayerProperty_ContextMenu_Effect = nameof(LayerProperty_ContextMenu_Effect);

        [ShowInMarkup, LanguageKey, DefaultValue("マスク(_M)")]
        public static readonly string LayerProperty_ContextMenu_Mask = nameof(LayerProperty_ContextMenu_Mask);

        [ShowInMarkup, LanguageKey, DefaultValue("長方形を追加(_R)")]
        public static readonly string LayerProperty_ContextMenu_Mask_AddRectangle = nameof(LayerProperty_ContextMenu_Mask_AddRectangle);

        [ShowInMarkup, LanguageKey, DefaultValue("楕円形を追加(_E)")]
        public static readonly string LayerProperty_ContextMenu_Mask_AddEllipse = nameof(LayerProperty_ContextMenu_Mask_AddEllipse);

        [ShowInMarkup, LanguageKey, DefaultValue("パスを追加(_P)")]
        public static readonly string LayerProperty_ContextMenu_Mask_AddBezier = nameof(LayerProperty_ContextMenu_Mask_AddBezier);

        [ShowInMarkup, LanguageKey, DefaultValue("レイヤーのタグをランダムに変更(_R)")]
        public static readonly string LayerProperty_ContextMenu_ChangeLayerTagsRandomly = nameof(LayerProperty_ContextMenu_ChangeLayerTagsRandomly);

        [ShowInMarkup, LanguageKey, DefaultValue("なし")]
        public static readonly string Layer_EmptyTrackMatte = nameof(Layer_EmptyTrackMatte);

        [ShowInMarkup, LanguageKey, DefaultValue("なし")]
        public static readonly string Layer_EmptyParentLayer = nameof(Layer_EmptyParentLayer);

        [ShowInMarkup, LanguageKey, DefaultValue("マスク")]
        public static readonly string Layer_Masks = nameof(Layer_Masks);

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

        [ShowInMarkup, LanguageKey, DefaultValue("切り取り(_X)")]
        public static readonly string Layer_Effect_ContextMenu_Cut = nameof(Layer_Effect_ContextMenu_Cut);

        [ShowInMarkup, LanguageKey, DefaultValue("コピー(_C)")]
        public static readonly string Layer_Effect_ContextMenu_Copy = nameof(Layer_Effect_ContextMenu_Copy);

        [ShowInMarkup, LanguageKey, DefaultValue("ペースト(_P)")]
        public static readonly string Layer_Effect_ContextMenu_Paste = nameof(Layer_Effect_ContextMenu_Paste);

        [ShowInMarkup, LanguageKey, DefaultValue("複製(_L)")]
        public static readonly string Layer_Effect_ContextMenu_Duplicate = nameof(Layer_Effect_ContextMenu_Duplicate);

        [ShowInMarkup, LanguageKey, DefaultValue("削除(_D)")]
        public static readonly string Layer_Effect_ContextMenu_Delete = nameof(Layer_Effect_ContextMenu_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトプリセットとして保存(_S)...")]
        public static readonly string Layer_Effect_ContextMenu_SaveEffectPreset = nameof(Layer_Effect_ContextMenu_SaveEffectPreset);

        [ShowInMarkup, LanguageKey, DefaultValue("エフェクトプリセットを適用(_G)...")]
        public static readonly string Layer_Effect_ContextMenu_LoadEffectPreset = nameof(Layer_Effect_ContextMenu_LoadEffectPreset);

        [ShowInMarkup, LanguageKey, DefaultValue("切り取り(_X)")]
        public static readonly string Layer_Mask_ContextMenu_Cut = nameof(Layer_Mask_ContextMenu_Cut);

        [ShowInMarkup, LanguageKey, DefaultValue("コピー(_C)")]
        public static readonly string Layer_Mask_ContextMenu_Copy = nameof(Layer_Mask_ContextMenu_Copy);

        [ShowInMarkup, LanguageKey, DefaultValue("ペースト(_P)")]
        public static readonly string Layer_Mask_ContextMenu_Paste = nameof(Layer_Mask_ContextMenu_Paste);

        [ShowInMarkup, LanguageKey, DefaultValue("複製(_L)")]
        public static readonly string Layer_Mask_ContextMenu_Duplicate = nameof(Layer_Mask_ContextMenu_Duplicate);

        [ShowInMarkup, LanguageKey, DefaultValue("削除(_D)")]
        public static readonly string Layer_Mask_ContextMenu_Delete = nameof(Layer_Mask_ContextMenu_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("マスクプリセットとして保存(_S)...")]
        public static readonly string Layer_Mask_ContextMenu_SaveMaskPreset = nameof(Layer_Mask_ContextMenu_SaveMaskPreset);

        [ShowInMarkup, LanguageKey, DefaultValue("マスクプリセットを適用(_K)...")]
        public static readonly string Layer_Mask_ContextMenu_LoadMaskPreset = nameof(Layer_Mask_ContextMenu_LoadMaskPreset);

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

        [ShowInMarkup, LanguageKey, DefaultValue("マスク")]
        public static readonly string LayerPropertyControllerView_Masks = nameof(LayerPropertyControllerView_Masks);

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

        [ShowInMarkup, LanguageKey, DefaultValue("キーフレームの追加(_A)")]
        public static readonly string PropertyCollection_ContextMenu_AddKeyFrame = nameof(PropertyCollection_ContextMenu_AddKeyFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティをリセット(_R)")]
        public static readonly string PropertyCollection_ContextMenu_ResetProperty = nameof(PropertyCollection_ContextMenu_ResetProperty);

        [ShowInMarkup, LanguageKey, DefaultValue("キーフレームを切り取り(_X)")]
        public static readonly string PropertyCollection_ContextMenu_CutKeyFrame = nameof(PropertyCollection_ContextMenu_CutKeyFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティを切り取り(_X)")]
        public static readonly string PropertyCollection_ContextMenu_CutPropertyGroup = nameof(PropertyCollection_ContextMenu_CutPropertyGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("コピー(_C)")]
        public static readonly string PropertyCollection_ContextMenu_CopyProperty = nameof(PropertyCollection_ContextMenu_CopyProperty);

        [ShowInMarkup, LanguageKey, DefaultValue("ペースト(_P)")]
        public static readonly string PropertyCollection_ContextMenu_PasteProperty = nameof(PropertyCollection_ContextMenu_PasteProperty);

        [ShowInMarkup, LanguageKey, DefaultValue("エクスプレッションのみペースト(_E)")]
        public static readonly string PropertyCollection_ContextMenu_PasteExpressionOnly = nameof(PropertyCollection_ContextMenu_PasteExpressionOnly);

        [ShowInMarkup, LanguageKey, DefaultValue("キーフレームを削除(_D)")]
        public static readonly string PropertyCollection_ContextMenu_DeleteKeyFrame = nameof(PropertyCollection_ContextMenu_DeleteKeyFrame);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティを削除(_D)")]
        public static readonly string PropertyCollection_ContextMenu_DeletePropertyGroup = nameof(PropertyCollection_ContextMenu_DeletePropertyGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティを複製(_E)")]
        public static readonly string PropertyCollection_ContextMenu_DuplicatePropertyGroup = nameof(PropertyCollection_ContextMenu_DuplicatePropertyGroup);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティプリセットとして保存(_S)...")]
        public static readonly string PropertyCollection_ContextMenu_SavePropertyPreset = nameof(PropertyCollection_ContextMenu_SavePropertyPreset);

        [ShowInMarkup, LanguageKey, DefaultValue("プロパティプリセットを適用(_L)...")]
        public static readonly string PropertyCollection_ContextMenu_LoadPropertyPreset = nameof(PropertyCollection_ContextMenu_LoadPropertyPreset);

        [ShowInMarkup, LanguageKey, DefaultValue("エクスプレッション")]
        public static readonly string PropertyView_Expression = nameof(PropertyView_Expression);

        [ShowInMarkup, LanguageKey, DefaultValue("切り取り(_X)")]
        public static readonly string PropertyView_ContextMenu_ExpressionCode_Cut = nameof(PropertyView_ContextMenu_ExpressionCode_Cut);

        [ShowInMarkup, LanguageKey, DefaultValue("コピー(_C)")]
        public static readonly string PropertyView_ContextMenu_ExpressionCode_Copy = nameof(PropertyView_ContextMenu_ExpressionCode_Copy);

        [ShowInMarkup, LanguageKey, DefaultValue("ペースト(_P)")]
        public static readonly string PropertyView_ContextMenu_ExpressionCode_Paste = nameof(PropertyView_ContextMenu_ExpressionCode_Paste);

        [ShowInMarkup, LanguageKey, DefaultValue("元に戻す(_U)")]
        public static readonly string PropertyView_ContextMenu_ExpressionCode_Undo = nameof(PropertyView_ContextMenu_ExpressionCode_Undo);

        [ShowInMarkup, LanguageKey, DefaultValue("やり直す(_R)")]
        public static readonly string PropertyView_ContextMenu_ExpressionCode_Redo = nameof(PropertyView_ContextMenu_ExpressionCode_Redo);

        [ShowInMarkup, LanguageKey, DefaultValue("レベルメーター")]
        public static readonly string AudioInformationView_Title = nameof(AudioInformationView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング設定")]
        public static readonly string RenderSettingView_Title = nameof(RenderSettingView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("出力プラグイン:")]
        public static readonly string RenderSettingView_OutputPlugin = nameof(RenderSettingView_OutputPlugin);

        [ShowInMarkup, LanguageKey, DefaultValue("出力先:")]
        public static readonly string RenderSettingView_OutputFilePath = nameof(RenderSettingView_OutputFilePath);

        [ShowInMarkup, LanguageKey, DefaultValue("参照")]
        public static readonly string RenderSettingView_OutputFilePath_ChangePath = nameof(RenderSettingView_OutputFilePath_ChangePath);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング範囲")]
        public static readonly string RenderSettingView_RenderRange = nameof(RenderSettingView_RenderRange);

        [ShowInMarkup, LanguageKey, DefaultValue("開始:")]
        public static readonly string RenderSettingView_TimeRange_Begin = nameof(RenderSettingView_TimeRange_Begin);

        [ShowInMarkup, LanguageKey, DefaultValue("終了:")]
        public static readonly string RenderSettingView_TimeRange_End = nameof(RenderSettingView_TimeRange_End);

        [ShowInMarkup, LanguageKey, DefaultValue("出力ソース")]
        public static readonly string RenderSettingView_OutputSources = nameof(RenderSettingView_OutputSources);

        [ShowInMarkup, LanguageKey, DefaultValue("ビデオ")]
        public static readonly string RenderSettingView_OutputSources_Video = nameof(RenderSettingView_OutputSources_Video);

        [ShowInMarkup, LanguageKey, DefaultValue("オーディオ")]
        public static readonly string RenderSettingView_OutputSources_Audio = nameof(RenderSettingView_OutputSources_Audio);

        [ShowInMarkup, LanguageKey, DefaultValue("出力プラグインの設定")]
        public static readonly string RenderSettingView_OpenOutputSetting = nameof(RenderSettingView_OpenOutputSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("キューに追加")]
        public static readonly string RenderSettingView_Button_Enqueue = nameof(RenderSettingView_Button_Enqueue);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング")]
        public static readonly string RenderSettingView_Button_StartRender= nameof(RenderSettingView_Button_StartRender);

        [DefaultValue("出力設定")]
        public static readonly string OutputSettingView_Title = nameof(OutputSettingView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング")]
        public static readonly string RenderingProgressView_Title = nameof(RenderingProgressView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング状況:")]
        public static readonly string RenderingProgressView_Progress = nameof(RenderingProgressView_Progress);

        [ShowInMarkup, LanguageKey, DefaultValue("フレーム")]
        public static readonly string RenderingProgressView_Progress_Frame = nameof(RenderingProgressView_Progress_Frame);

        [ShowInMarkup, LanguageKey, DefaultValue("推定残り時間: {0:dd\\.hh\\:mm\\:ss}")]
        public static readonly string RenderingProgressView_Eta = nameof(RenderingProgressView_Eta);

        [ShowInMarkup, LanguageKey, DefaultValue("一時停止")]
        public static readonly string RenderingProgressView_Button_Pause = nameof(RenderingProgressView_Button_Pause);

        [ShowInMarkup, LanguageKey, DefaultValue("再開")]
        public static readonly string RenderingProgressView_Button_Continue = nameof(RenderingProgressView_Button_Continue);

        [ShowInMarkup, LanguageKey, DefaultValue("中止")]
        public static readonly string RenderingProgressView_Button_Abort = nameof(RenderingProgressView_Button_Abort);

        [ShowInMarkup, LanguageKey, DefaultValue("進捗:")]
        public static readonly string RenderQueueView_Progress = nameof(RenderQueueView_Progress);

        [ShowInMarkup, LanguageKey, DefaultValue("{0} フレーム")]
        public static readonly string RenderQueueView_RenderedFrameCount = nameof(RenderQueueView_RenderedFrameCount);

        [ShowInMarkup, LanguageKey, DefaultValue("推定残り時間: {0:dd\\.hh\\:mm\\:ss}")]
        public static readonly string RenderQueueView_Eta = nameof(RenderQueueView_Eta);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング")]
        public static readonly string RenderQueueView_Button_Rendering = nameof(RenderQueueView_Button_Rendering);

        [ShowInMarkup, LanguageKey, DefaultValue("一時停止")]
        public static readonly string RenderQueueView_Button_Pause = nameof(RenderQueueView_Button_Pause);

        [ShowInMarkup, LanguageKey, DefaultValue("再開")]
        public static readonly string RenderQueueView_Button_Continue = nameof(RenderQueueView_Button_Continue);

        [ShowInMarkup, LanguageKey, DefaultValue("中止")]
        public static readonly string RenderQueueView_Button_Abort = nameof(RenderingProgressView_Button_Abort);

        [ShowInMarkup, LanguageKey, DefaultValue("複製(_L)")]
        public static readonly string RenderQueueView_ContextMenu_Duplicate = nameof(RenderQueueView_ContextMenu_Duplicate);

        [ShowInMarkup, LanguageKey, DefaultValue("削除(_D)")]
        public static readonly string RenderQueueView_ContextMenu_Delete = nameof(RenderQueueView_ContextMenu_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("出力パス:")]
        public static readonly string RenderQueueItemView_FilePath = nameof(RenderQueueItemView_FilePath);

        [ShowInMarkup, LanguageKey, DefaultValue("ステータス:")]
        public static readonly string RenderQueueItemView_State = nameof(RenderQueueItemView_State);

        [ShowInMarkup, LanguageKey, DefaultValue("出力プラグイン:")]
        public static readonly string RenderQueueItemView_OutputPluginName = nameof(RenderQueueItemView_OutputPluginName);

        [ShowInMarkup, LanguageKey, DefaultValue("(なし)")]
        public static readonly string RenderQueueItemView_OutputPluginName_Empty = nameof(RenderQueueItemView_OutputPluginName_Empty);

        [ShowInMarkup, LanguageKey, DefaultValue("範囲:")]
        public static readonly string RenderQueueItemView_RenderRange = nameof(RenderQueueItemView_RenderRange);

        [ShowInMarkup, LanguageKey, DefaultValue("出力ソース:")]
        public static readonly string RenderQueueItemView_OutputSources = nameof(RenderQueueItemView_OutputSources);

        [ShowInMarkup, LanguageKey, DefaultValue("ビデオ")]
        public static readonly string RenderQueueItemView_OutputSources_Video = nameof(RenderQueueItemView_OutputSources_Video);

        [ShowInMarkup, LanguageKey, DefaultValue("オーディオ")]
        public static readonly string RenderQueueItemView_OutputSources_Audio = nameof(RenderQueueItemView_OutputSources_Audio);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング時間:")]
        public static readonly string RenderQueueItemView_RenderingTime = nameof(RenderQueueItemView_RenderingTime);

        [ShowInMarkup, LanguageKey, DefaultValue("参照")]
        public static readonly string RenderQueueItemView_Button_Reference = nameof(RenderQueueItemView_Button_Reference);

        [ShowInMarkup, LanguageKey, DefaultValue("レンダリング設定の変更")]
        public static readonly string RenderQueueItemView_Button_EditSetting = nameof(RenderQueueItemView_Button_EditSetting);

        [DefaultValue("環境設定")]
        public static readonly string OptionView_Title = nameof(OptionView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("全般")]
        public static readonly string OptionView_Category_General = nameof(OptionView_Category_General);

        [ShowInMarkup, LanguageKey, DefaultValue("パフォーマンス")]
        public static readonly string OptionView_Category_Performance = nameof(OptionView_Category_Performance);

        [ShowInMarkup, LanguageKey, DefaultValue("オートセーブ")]
        public static readonly string OptionView_Category_AutoSave = nameof(OptionView_Category_AutoSave);

        [ShowInMarkup, LanguageKey, DefaultValue("平面のフォルダ名")]
        public static readonly string OptionView_General_SolidFilderName = nameof(OptionView_General_SolidFilderName);

        [ShowInMarkup, LanguageKey, DefaultValue("GPU設定")]
        public static readonly string OptionView_Performance_GpuSetting = nameof(OptionView_Performance_GpuSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("GPUを使用しない")]
        public static readonly string OptionView_Performance_ForceUseCpu = nameof(OptionView_Performance_ForceUseCpu);

        [ShowInMarkup, LanguageKey, DefaultValue("使用するGPU:")]
        public static readonly string OptionView_Performance_UseGpuDevice_Label = nameof(OptionView_Performance_UseGpuDevice_Label);

        [ShowInMarkup, LanguageKey, DefaultValue("(デフォルト)")]
        public static readonly string OptionView_Performance_UseGpuDevice_Default = nameof(OptionView_Performance_UseGpuDevice_Default);

        [ShowInMarkup, LanguageKey, DefaultValue("キャッシュ設定 (合計最大: {0}MiB)")]
        public static readonly string OptionView_Performance_CacheSetting = nameof(OptionView_Performance_CacheSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("キャッシュサイズ:")]
        public static readonly string OptionView_Performance_ImageCacheLimit_Label = nameof(OptionView_Performance_ImageCacheLimit_Label);

        [ShowInMarkup, LanguageKey, DefaultValue("RAMプレビューサイズ:")]
        public static readonly string OptionView_Performance_RamPreviewCacheLimit_Label = nameof(OptionView_Performance_RamPreviewCacheLimit_Label);

        [ShowInMarkup, LanguageKey, DefaultValue("(最大: {0}MiB)")]
        public static readonly string OptionView_Performance_CacheLimit_MaxSizeLabel = nameof(OptionView_Performance_CacheLimit_MaxSizeLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("キャッシュを圧縮する(※変更時キャッシュがクリアされます)")]
        public static readonly string OptionView_Performance_IsCompressCache = nameof(OptionView_Performance_IsCompressCache);

        [ShowInMarkup, LanguageKey, DefaultValue("GPUキャッシュ設定")]
        public static readonly string OptionView_Performance_GpuCacheSetting = nameof(OptionView_Performance_GpuCacheSetting);

        [ShowInMarkup, LanguageKey, DefaultValue("GPUキャッシュを使用する")]
        public static readonly string OptionView_Performance_UseGpuCache = nameof(OptionView_Performance_UseGpuCache);

        [ShowInMarkup, LanguageKey, DefaultValue("キャッシュサイズの上限:")]
        public static readonly string OptionView_Performance_GpuCacheLimitRate_Label = nameof(OptionView_Performance_GpuCacheLimitRate_Label);

        [ShowInMarkup, LanguageKey, DefaultValue("※キャッシュサイズは、占有メモリと共有メモリの合計値から割合で算出されます")]
        public static readonly string OptionView_Performance_GpuCacheLimitRate_Note = nameof(OptionView_Performance_GpuCacheLimitRate_Note);

        [ShowInMarkup, LanguageKey, DefaultValue("オートセーブを使用する")]
        public static readonly string OptionView_AutoSave_UseAutoSave = nameof(OptionView_AutoSave_UseAutoSave);

        [ShowInMarkup, LanguageKey, DefaultValue("オートセーブ間隔:")]
        public static readonly string OptionView_AutoSave_Interval_Label = nameof(OptionView_AutoSave_Interval_Label);

        [ShowInMarkup, LanguageKey, DefaultValue("オートセーブの個数:")]
        public static readonly string OptionView_AutoSave_Count_Labell = nameof(OptionView_AutoSave_Count_Labell);

        [ShowInMarkup, LanguageKey, DefaultValue("ショートカットキー設定")]
        public static readonly string ShortcutKeySettingView_Title = nameof(ShortcutKeySettingView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("検索")]
        public static readonly string ShortcutKeySettingView_FilterLabel = nameof(ShortcutKeySettingView_FilterLabel);

        [ShowInMarkup, LanguageKey, DefaultValue("名前")]
        public static readonly string ShortcutKeySettingView_Column_Name = nameof(ShortcutKeySettingView_Column_Name);

        [ShowInMarkup, LanguageKey, DefaultValue("キー")]
        public static readonly string ShortcutKeySettingView_Column_Key = nameof(ShortcutKeySettingView_Column_Key);

        [ShowInMarkup, LanguageKey, DefaultValue("重複")]
        public static readonly string ShortcutKeySettingView_Column_Duplicated = nameof(ShortcutKeySettingView_Column_Duplicated);

        [ShowInMarkup, LanguageKey, DefaultValue("ショートカットキーの変更(_E)")]
        public static readonly string ShortcutKeySettingView_ContextMenu_Edit = nameof(ShortcutKeySettingView_ContextMenu_Edit);

        [ShowInMarkup, LanguageKey, DefaultValue("ショートカットキーの削除(_D)")]
        public static readonly string ShortcutKeySettingView_ContextMenu_Delete = nameof(ShortcutKeySettingView_ContextMenu_Delete);

        [ShowInMarkup, LanguageKey, DefaultValue("コマンドを検索")]
        public static readonly string CommandPalettePopup_FilterText_Placeholder = nameof(CommandPalettePopup_FilterText_Placeholder);

        [DefaultValue("エフェクト")]
        public static readonly string CommandPalettePopup_DisplayName_EffectCategory = nameof(CommandPalettePopup_DisplayName_EffectCategory);

        [DefaultValue("再生速度設定")]
        public static readonly string PlayRateSettingView_Title = nameof(PlayRateSettingView_Title);

        [ShowInMarkup, LanguageKey, DefaultValue("ソースの長さ:")]
        public static readonly string PlayRateSettingView_SourceDuration = nameof(PlayRateSettingView_SourceDuration);

        [ShowInMarkup, LanguageKey, DefaultValue("変更後の長さ:")]
        public static readonly string PlayRateSettingView_Duration = nameof(PlayRateSettingView_Duration);

        [ShowInMarkup, LanguageKey, DefaultValue("再生速度:")]
        public static readonly string PlayRateSettingView_PlayRate = nameof(PlayRateSettingView_PlayRate);

        [ShowInMarkup, LanguageKey, DefaultValue("マーカーを追加(_A)")]
        public static readonly string MarkerCollection_ContextMenu_AddCompositionMarker = nameof(MarkerCollection_ContextMenu_AddCompositionMarker);

        [ShowInMarkup, LanguageKey, DefaultValue("マーカーを削除(_D)")]
        public static readonly string MarkerCollection_ContextMenu_DeleteCompositionMarker = nameof(MarkerCollection_ContextMenu_DeleteCompositionMarker);

        // Effect category names

        [DefaultValue("オーディオ")]
        public static readonly string EffectCategory_Audio = DefaultLanguageResourceNames.EffectCategory_Audio;

        [DefaultValue("ブラー")]
        public static readonly string EffectCategory_Blur = DefaultLanguageResourceNames.EffectCategory_Blur;

        [DefaultValue("チャンネル")]
        public static readonly string EffectCategory_Channel = DefaultLanguageResourceNames.EffectCategory_Channel;

        [DefaultValue("カラー補正")]
        public static readonly string EffectCategory_ColorCollection = DefaultLanguageResourceNames.EffectCategory_ColorCollection;

        [DefaultValue("ディストーション")]
        public static readonly string EffectCategory_Distortion = DefaultLanguageResourceNames.EffectCategory_Distortion;

        [DefaultValue("エクスプレッション制御")]
        public static readonly string EffectCategory_ExpressionControl = DefaultLanguageResourceNames.EffectCategory_ExpressionControl;

        [DefaultValue("描画")]
        public static readonly string EffectCategory_Generate = DefaultLanguageResourceNames.EffectCategory_Generate;

        [DefaultValue("キーイング")]
        public static readonly string EffectCategory_Keying = DefaultLanguageResourceNames.EffectCategory_Keying;

        [DefaultValue("ノイズ")]
        public static readonly string EffectCategory_Noise = DefaultLanguageResourceNames.EffectCategory_Noise;

        [DefaultValue("シミュレーション")]
        public static readonly string EffectCategory_Simulation = DefaultLanguageResourceNames.EffectCategory_Simulation;

        [DefaultValue("スタイライズ")]
        public static readonly string EffectCategory_Stylize = DefaultLanguageResourceNames.EffectCategory_Stylize;

        [DefaultValue("ユーティリティ")]
        public static readonly string EffectCategory_Utility = DefaultLanguageResourceNames.EffectCategory_Utility;

        // History Command

        [DefaultValue("プロジェクトの新規作成/開く")]
        public static readonly string History_NewProject = nameof(History_NewProject);

        [DefaultValue("フォルダの追加")]
        public static readonly string History_AddFolder = nameof(History_AddFolder);

        [DefaultValue("ファイルの読み込み")]
        public static readonly string History_LoadFootageFile = nameof(History_LoadFootageFile);

        [DefaultValue("フッテージの移動")]
        public static readonly string History_MoveFootage = nameof(History_MoveFootage);

        [DefaultValue("フッテージの名前変更")]
        public static readonly string History_ChangeFootageName = nameof(History_ChangeFootageName);

        [DefaultValue("フッテージのコメント変更")]
        public static readonly string History_ChangeFootageComment = nameof(History_ChangeFootageComment);

        [DefaultValue("フッテージの削除")]
        public static readonly string History_DeleteFootages = nameof(History_DeleteFootages);

        [DefaultValue("コンポジションの追加")]
        public static readonly string History_AddComposition = nameof(History_AddComposition);

        [DefaultValue("コンポジションの削除")]
        public static readonly string History_RemoveComposition = nameof(History_RemoveComposition);

        [DefaultValue("コンポジションの設定変更")]
        public static readonly string History_ChangeCompositionSetting = nameof(History_ChangeCompositionSetting);

        [DefaultValue("シャイの有効・無効の切り替え")]
        public static readonly string History_ChangeEnableShy = nameof(History_ChangeEnableShy);

        [DefaultValue("フレームブレンドの有効・無効の切り替え")]
        public static readonly string History_ChangeEnableFrameBlend = nameof(History_ChangeEnableFrameBlend);

        [DefaultValue("モーションブラーの有効・無効の切り替え")]
        public static readonly string History_ChangeEnableMotionBlur = nameof(History_ChangeEnableMotionBlur);

        [DefaultValue("ワークエリアの変更")]
        public static readonly string History_ChangeWorkarea = nameof(History_ChangeWorkarea);

        [DefaultValue("レイヤーの追加")]
        public static readonly string History_AddLayers = nameof(History_AddLayers);

        [DefaultValue("レイヤーの移動")]
        public static readonly string History_MoveLayers = nameof(History_MoveLayers);

        [DefaultValue("レイヤーの削除")]
        public static readonly string History_RemoveLayers = nameof(History_RemoveLayers);

        [DefaultValue("レイヤーの切り取り")]
        public static readonly string History_CutLayers = nameof(History_CutLayers);

        [DefaultValue("レイヤーのペースト")]
        public static readonly string History_PasteLayers = nameof(History_PasteLayers);

        [DefaultValue("レイヤーの複製")]
        public static readonly string History_DuplicateLayers = nameof(History_DuplicateLayers);

        [DefaultValue("レイヤーの分割")]
        public static readonly string History_SplitLayers = nameof(History_SplitLayers);

        [DefaultValue("平面の追加")]
        public static readonly string History_AddSolid = nameof(History_AddSolid);

        [DefaultValue("レイヤー時間変更")]
        public static readonly string History_EditLayerDuration = nameof(History_EditLayerDuration);

        [DefaultValue("レイヤースイッチの切り替え")]
        public static readonly string History_ChangeLayerSwitch = nameof(History_ChangeLayerSwitch);

        [DefaultValue("レイヤーの名前変更")]
        public static readonly string History_ChangeLayerName = nameof(History_ChangeLayerName);

        [DefaultValue("レイヤーのコメント変更")]
        public static readonly string History_ChangeLayerComment = nameof(History_ChangeLayerComment);

        [DefaultValue("合成モードの変更")]
        public static readonly string History_ChangeBlendMode = nameof(History_ChangeBlendMode);

        [DefaultValue("トラックマットの変更")]
        public static readonly string History_ChangeTrackMatteLayer = nameof(History_ChangeTrackMatteLayer);

        [DefaultValue("トラックマットのモード変更")]
        public static readonly string History_ChangeTrackMatteMode = nameof(History_ChangeTrackMatteMode);

        [DefaultValue("親レイヤーの変更")]
        public static readonly string History_ChangeParentLayer = nameof(History_ChangeParentLayer);

        [DefaultValue("レイヤーのタグの変更")]
        public static readonly string History_ChangeTagColor = nameof(History_ChangeTagColor);

        [DefaultValue("レイヤーの再生時間の変更")]
        public static readonly string History_ChangeLayerPlayRate = nameof(History_ChangeLayerPlayRate);

        [DefaultValue("レイヤーのフレーム固定の変更")]
        public static readonly string History_ChangeFreezeFrame = nameof(History_ChangeFreezeFrame);

        [DefaultValue("プロパティの変更")]
        public static readonly string History_ChangePropertyValue = nameof(History_ChangePropertyValue);

        [DefaultValue("プロパティのリセット")]
        public static readonly string History_ResetPropertyValue = nameof(History_ResetPropertyValue);

        [DefaultValue("プロパティの切り取り")]
        public static readonly string History_CutProperty = nameof(History_CutProperty);

        [DefaultValue("プロパティのペースト")]
        public static readonly string History_PasteProperty = nameof(History_PasteProperty);

        [DefaultValue("プロパティの複製")]
        public static readonly string History_DuplicateProperty = nameof(History_DuplicateProperty);

        [DefaultValue("プロパティプリセットの適用")]
        public static readonly string History_LoadPropertyPreset = nameof(History_LoadPropertyPreset);

        [DefaultValue("エクスプレッションの変更")]
        public static readonly string History_ChangeExpression = nameof(History_ChangeExpression);

        [DefaultValue("エクスプレッションの有効・無効の切り替え")]
        public static readonly string History_ChangeUseExpression = nameof(History_ChangeUseExpression);

        [DefaultValue("キーフレームの追加")]
        public static readonly string History_AddKeyFrame = nameof(History_AddKeyFrame);

        [DefaultValue("キーフレームの削除")]
        public static readonly string History_RemoveKeyFrame = nameof(History_RemoveKeyFrame);

        [DefaultValue("キーフレームの切り取り")]
        public static readonly string History_CutKeyFrame = nameof(History_CutKeyFrame);

        [DefaultValue("キーフレームの移動")]
        public static readonly string History_MoveKeyFrame = nameof(History_MoveKeyFrame);

        [DefaultValue("キーフレームの補間法の変更")]
        public static readonly string History_ChangeKeyFrameInterpolationType = nameof(History_ChangeKeyFrameInterpolationType);

        [DefaultValue("キーフレームのペースト")]
        public static readonly string History_PasteKeyFrames = nameof(History_PasteKeyFrames);

        [DefaultValue("エフェクトの追加")]
        public static readonly string History_AddEffects = nameof(History_AddEffects);

        [DefaultValue("エフェクトの移動")]
        public static readonly string History_MoveEffects = nameof(History_MoveEffects);

        [DefaultValue("エフェクトの有効・無効切り替え")]
        public static readonly string History_ChangeEffectsEnable = nameof(History_ChangeEffectsEnable);

        [DefaultValue("エフェクトの削除")]
        public static readonly string History_RemoveEffects = nameof(History_RemoveEffects);

        [DefaultValue("エフェクトの切り取り")]
        public static readonly string History_CutEffects = nameof(History_CutEffects);

        [DefaultValue("エフェクトのペースト")]
        public static readonly string History_PasteEffects = nameof(History_PasteEffects);

        [DefaultValue("エフェクトの複製")]
        public static readonly string History_DuplicateEffects = nameof(History_DuplicateEffects);

        [DefaultValue("エフェクトの名前変更")]
        public static readonly string History_ChangeEffectName = nameof(History_ChangeEffectName);

        [DefaultValue("エフェクトのコメント変更")]
        public static readonly string History_ChangeEffectComment = nameof(History_ChangeEffectComment);

        [DefaultValue("エフェクトプリセットの適用")]
        public static readonly string History_LoadEffectPreset = nameof(History_LoadEffectPreset);

        [DefaultValue("レンダーキューに追加")]
        public static readonly string History_EnqueueRender = nameof(History_EnqueueRender);

        [DefaultValue("レンダリング設定の変更")]
        public static readonly string History_ChangeRenderQueueItemSetting = nameof(History_ChangeRenderQueueItemSetting);

        [DefaultValue("レンダーキューの削除")]
        public static readonly string History_RemoveRenderQueues = nameof(History_RemoveRenderQueues);

        [DefaultValue("レンダーキューの複製")]
        public static readonly string History_DuplicateRenderQueues = nameof(History_DuplicateRenderQueues);

        [DefaultValue("レンダリング")]
        public static readonly string History_ExecuteRendering = nameof(History_ExecuteRendering);

        [DefaultValue("コンポジション更新に伴うプロパティの更新")]
        public static readonly string History_UpdateValueByCompositionStateChanged = nameof(History_UpdateValueByCompositionStateChanged);

        [DefaultValue("レイヤー更新に伴うプロパティの更新")]
        public static readonly string History_UpdateValueByLayerStateChanged = nameof(History_UpdateValueByLayerStateChanged);

        [DefaultValue("マスクの追加")]
        public static readonly string History_AddMask = nameof(History_AddMask);

        [DefaultValue("マスクの移動")]
        public static readonly string History_MoveMasks = nameof(History_MoveMasks);

        [DefaultValue("マスクの有効・無効切り替え")]
        public static readonly string History_ChangeMasksEnable = nameof(History_ChangeMasksEnable);

        [DefaultValue("マスクの削除")]
        public static readonly string History_RemoveMasks = nameof(History_RemoveMasks);

        [DefaultValue("マスクの切り取り")]
        public static readonly string History_CutMasks = nameof(History_CutMasks);

        [DefaultValue("マスクのペースト")]
        public static readonly string History_PasteMasks = nameof(History_PasteMasks);

        [DefaultValue("マスクの複製")]
        public static readonly string History_DuplicateMasks = nameof(History_DuplicateMasks);

        [DefaultValue("マスクの名前変更")]
        public static readonly string History_ChangeMaskName = nameof(History_ChangeMaskName);

        [DefaultValue("マスクプリセットの適用")]
        public static readonly string History_LoadMaskPreset = nameof(History_LoadMaskPreset);

        [DefaultValue("マーカーの移動")]
        public static readonly string History_MoveCompositionMarker = nameof(History_MoveCompositionMarker);

        [DefaultValue("マーカーの追加")]
        public static readonly string History_AddCompositionMarker = nameof(History_AddCompositionMarker);

        [DefaultValue("マーカーの削除")]
        public static readonly string History_DeleteCompositionMarker = nameof(History_DeleteCompositionMarker);

        // Dialog

        [DefaultValue("フッテージの削除")]
        public static readonly string Dialog_ConfirmDeleteFootage_Title = nameof(Dialog_ConfirmDeleteFootage_Title);

        [DefaultValue("フッテージを削除すると、各コンポジションからこのフッテージを使用しているレイヤーが削除されます。このフッテージを削除しますか?")]
        public static readonly string Dialog_ConfirmDeleteFootage_Text = nameof(Dialog_ConfirmDeleteFootage_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_LoadFootageCannotLoad_Title = nameof(Dialog_LoadFootageCannotLoad_Title);

        [DefaultValue("ファイルを読み込めませんでした。ファイルが破損しているか、プラグインが対応していない可能性があります。")]
        public static readonly string Dialog_LoadFootageCannotLoad_Text = nameof(Dialog_LoadFootageCannotLoad_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_LoadFootageNotSupported_Title = nameof(Dialog_LoadFootageNotSupported_Title);

        [DefaultValue("ファイルを読み込めませんでした。現在読み込まれているプラグインでは対応していないファイル形式です。")]
        public static readonly string Dialog_LoadFootageNotSupported_Text = nameof(Dialog_LoadFootageNotSupported_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_LoadFootageCancel_Title = nameof(Dialog_LoadFootageCancel_Title);

        [DefaultValue("ファイルを読みの読み込みをキャンセルしました。")]
        public static readonly string Dialog_LoadFootageCancel_Text = nameof(Dialog_LoadFootageCancel_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_LoadFootageCannotLoadMultiple_Title = nameof(Dialog_LoadFootageCannotLoadMultiple_Title);

        [DefaultValue("いくつかのファイルの読み込みに失敗しました。読み込めなかったファイルはファイルが破損しているか、プラグインが対応していない可能性があります。")]
        public static readonly string Dialog_LoadFootageCannotLoadMultiple_Text = nameof(Dialog_LoadFootageCannotLoadMultiple_Text);

        [DefaultValue("フォルダを削除すると、中に含まれているフッテージも一緒に削除され、各コンポジションからも含まれているフッテージを使用しているレイヤーが削除されます。このフォルダを削除しますか?")]
        public static readonly string Dialog_ConfirmDeleteFootageFolder_Text = nameof(Dialog_ConfirmDeleteFootageFolder_Text);

        [DefaultValue("レンダリング開始")]
        public static readonly string Dialog_ConfirmRenderOverwriteByQueueingItem_Title = nameof(Dialog_ConfirmRenderOverwriteByQueueingItem_Title);

        [DefaultValue("レンダーキューに同じファイル名、または命名規則のパスが存在します。レンダリングを行うと後続のキューによってファイルが上書きされる可能性があります。よろしいですか?")]
        public static readonly string Dialog_ConfirmRenderOverwriteByQueueingItem_Text = nameof(Dialog_ConfirmRenderOverwriteByQueueingItem_Text);

        [DefaultValue("プロジェクトファイル")]
        public static readonly string Dialog_OpenSaveProject_Filter_Project = nameof(Dialog_OpenSaveProject_Filter_Project);

        [DefaultValue("サポートしている全てのファイル形式")]
        public static readonly string Dialog_LoadFile_Filter_SupportedAllTypes = nameof(Dialog_LoadFile_Filter_SupportedAllTypes);

        [DefaultValue("プリセットの削除")]
        public static readonly string Dialog_ConfirmDeleteCompositionPreset_Title = nameof(Dialog_ConfirmDeleteCompositionPreset_Title);

        [DefaultValue("選択しているプリセットを削除しますか?")]
        public static readonly string Dialog_ConfirmDeleteCompositionPreset_Text = nameof(Dialog_ConfirmDeleteCompositionPreset_Text);

        [DefaultValue("「{0}」はすでに使用されています。上書きしますか?")]
        public static readonly string Dialog_NameSettingView_ConfirmOverwrite_Text = nameof(Dialog_NameSettingView_ConfirmOverwrite_Text);

        [DefaultValue("新しいプリセット")]
        public static readonly string Dialog_CompositionPresetName_Title = nameof(Dialog_CompositionPresetName_Title);

        [DefaultValue("プリセット名:")]
        public static readonly string Dialog_CompositionPresetName_Label = nameof(Dialog_CompositionPresetName_Label);

        [DefaultValue("エラー")]
        public static readonly string Dialog_RaiseGPUException_Title = nameof(Dialog_RaiseGPUException_Title);

        [DefaultValue("レンダリング中にエラーが発生しました。GPUでの処理中に問題が発生した可能性があるため、CPUでの処理に切り替えます。")]
        public static readonly string Dialog_RaiseGPUException_Text = nameof(Dialog_RaiseGPUException_Text);

        [DefaultValue("確認")]
        public static readonly string Dialog_NotSaveEditedWhenClose_Title = nameof(Dialog_NotSaveEditedWhenClose_Title);

        [DefaultValue("未保存の編集内容があります。アプリケーション終了前に保存しますか?")]
        public static readonly string Dialog_NotSaveEditedWhenClose_Text = nameof(Dialog_NotSaveEditedWhenClose_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_RaiseSaveProjectError_Title = nameof(Dialog_RaiseSaveProjectError_Title);

        [DefaultValue("プロジェクト保存中にエラーが発生しました。ファイルパスを変えるなど、他の場所に保存してください。")]
        public static readonly string Dialog_RaiseSaveProjectError_Text = nameof(Dialog_RaiseSaveProjectError_Text);

        [DefaultValue("確認")]
        public static readonly string Dialog_StopRenderingWhenClose_Title = nameof(Dialog_StopRenderingWhenClose_Title);

        [DefaultValue("現在レンダリング実行中です。レンダリングを停止してアプリケーションを終了しますか?")]
        public static readonly string Dialog_StopRenderingWhenClose_Text = nameof(Dialog_StopRenderingWhenClose_Text);

        [DefaultValue("確認")]
        public static readonly string Dialog_NotSaveEditedWhenCloseProject_Title = nameof(Dialog_NotSaveEditedWhenCloseProject_Title);

        [DefaultValue("未保存の編集内容があります。プロジェクトを閉じる前に保存しますか?")]
        public static readonly string Dialog_NotSaveEditedWhenCloseProject_Text = nameof(Dialog_NotSaveEditedWhenCloseProject_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_InvalidShortcutKeyCombination_Title = nameof(Dialog_InvalidShortcutKeyCombination_Title);

        [DefaultValue("設定しようとしたキーの組み合わせは使用できません。他のキーの組み合わせを使用してください。")]
        public static readonly string Dialog_InvalidShortcutKeyCombination_Text = nameof(Dialog_InvalidShortcutKeyCombination_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_RaiseAutoSaveError_Title = nameof(Dialog_RaiseAutoSaveError_Title);

        [DefaultValue("プロジェクトのオートセーブに失敗したため、オートセーブ機能を一時停止しました。オートセーブ用ディレクトリの中に使用中のプロジェクトなど、ファイルをロックしているものがないか確認してください。")]
        public static readonly string Dialog_RaiseAutoSaveError_Text = nameof(Dialog_RaiseAutoSaveError_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_SavePresetError_Title = nameof(Dialog_SavePresetError_Title);

        [DefaultValue("プリセット保存中にエラーが発生しました。ファイルパスを変えるなど、他の場所に保存してください。")]
        public static readonly string Dialog_SavePresetError_Text = nameof(Dialog_SavePresetError_Text);

        [DefaultValue("エラー")]
        public static readonly string Dialog_LoadPresetError_Title = nameof(Dialog_LoadPresetError_Title);

        [DefaultValue("プリセット適用中にエラーが発生しました。ファイルが破損していないか、またはプロパティの適用先が正しいか確認してください。")]
        public static readonly string Dialog_LoadPresetError_Text = nameof(Dialog_LoadPresetError_Text);

        [DefaultValue("予期されなかったエラー")]
        public static readonly string Dialog_SaveChanceWhenThrownUnhandledException_Title = nameof(Dialog_SaveChanceWhenThrownUnhandledException_Title);

        [DefaultValue("終了前に、1度だけ保存にチャレンジできます。\r\n(正常に保存できる保証はないため、上書き保存はしないでください)")]
        public static readonly string Dialog_SaveChanceWhenThrownUnhandledException_Text = nameof(Dialog_SaveChanceWhenThrownUnhandledException_Text);

        [DefaultValue("プロパティプリセット")]
        public static readonly string Dialog_OpenSavePropertyPreset_Filter_PropertyPreset = nameof(Dialog_OpenSavePropertyPreset_Filter_PropertyPreset);

        [DefaultValue("エフェクトプリセット")]
        public static readonly string Dialog_OpenSaveEffectPreset_Filter_EffectPreset = nameof(Dialog_OpenSaveEffectPreset_Filter_EffectPreset);

        [DefaultValue("マスクプリセット")]
        public static readonly string Dialog_OpenSaveMaskPreset_Filter_MaskPreset = nameof(Dialog_OpenSaveMaskPreset_Filter_MaskPreset);

        [DefaultValue("エフェクトプリセット、マスクプリセット")]
        public static readonly string Dialog_OpenEffectOrMaskPreset_Filter_EffectPreset = nameof(Dialog_OpenEffectOrMaskPreset_Filter_EffectPreset);

        // ValidationRule

        [DefaultValue("入力が文字列ではありません")]
        public static readonly string ValidationRule_General_NotString = nameof(ValidationRule_General_NotString);

        [DefaultValue("文字数は{0}文字以上である必要があります")]
        public static readonly string ValidationRule_StringLength_LessLength = nameof(ValidationRule_StringLength_LessLength);

        [DefaultValue("文字数は{0}文字以下である必要があります")]
        public static readonly string ValidationRule_StringLength_OverLength = nameof(ValidationRule_StringLength_OverLength);

        [DefaultValue("文字数は{0}文字以上{1}文字以下である必要があります")]
        public static readonly string ValidationRule_StringLength_OutOfRange = nameof(ValidationRule_StringLength_OutOfRange);

        [DefaultValue("「{0}」はすでに使用されています")]
        public static readonly string ValidationRule_RegisteredNames_AlreadyUsed = nameof(ValidationRule_RegisteredNames_AlreadyUsed);

        // Property

        [LanguageKey, DefaultValue("アンカーポイント")]
        public static readonly string TransformProperty_AnchorPoint = nameof(TransformProperty_AnchorPoint);

        [LanguageKey, DefaultValue("位置")]
        public static readonly string TransformProperty_Translate = nameof(TransformProperty_Translate);

        [LanguageKey, DefaultValue("方向")]
        public static readonly string TransformProperty_Direction = nameof(TransformProperty_Direction);

        [LanguageKey, DefaultValue("回転")]
        public static readonly string TransformProperty_ZAngle2D = nameof(TransformProperty_ZAngle2D);

        [LanguageKey, DefaultValue("X回転")]
        public static readonly string TransformProperty_XAngle3D = nameof(TransformProperty_XAngle3D);

        [LanguageKey, DefaultValue("Y回転")]
        public static readonly string TransformProperty_YAngle3D = nameof(TransformProperty_YAngle3D);

        [LanguageKey, DefaultValue("Z回転")]
        public static readonly string TransformProperty_ZAngle3D = nameof(TransformProperty_ZAngle3D);

        [LanguageKey, DefaultValue("スケール")]
        public static readonly string TransformProperty_Scale = nameof(TransformProperty_Scale);

        [LanguageKey, DefaultValue("不透明度")]
        public static readonly string TransformProperty_Opacity = nameof(TransformProperty_Opacity);

        [LanguageKey, DefaultValue("影を落とす")]
        public static readonly string LayerOptionsProperty_IsCastShadow = nameof(LayerOptionsProperty_IsCastShadow);

        [LanguageKey, DefaultValue("ライトを透過")]
        public static readonly string LayerOptionsProperty_LightTransmission = nameof(LayerOptionsProperty_LightTransmission);

        [LanguageKey, DefaultValue("影を受ける")]
        public static readonly string LayerOptionsProperty_IsAcceptShadow = nameof(LayerOptionsProperty_IsAcceptShadow);

        [LanguageKey, DefaultValue("ライトを受ける")]
        public static readonly string LayerOptionsProperty_IsAcceptLight = nameof(LayerOptionsProperty_IsAcceptLight);

        [LanguageKey, DefaultValue("アンビエント")]
        public static readonly string LayerOptionsProperty_Ambient = nameof(LayerOptionsProperty_Ambient);

        [LanguageKey, DefaultValue("拡散")]
        public static readonly string LayerOptionsProperty_Diffuse = nameof(LayerOptionsProperty_Diffuse);

        [LanguageKey, DefaultValue("鏡面強度")]
        public static readonly string LayerOptionsProperty_SpecularIntensity = nameof(LayerOptionsProperty_SpecularIntensity);

        [LanguageKey, DefaultValue("鏡面光沢")]
        public static readonly string LayerOptionsProperty_SpecularShininess = nameof(LayerOptionsProperty_SpecularShininess);

        [LanguageKey, DefaultValue("金属")]
        public static readonly string LayerOptionsProperty_Metal = nameof(LayerOptionsProperty_Metal);

        [LanguageKey, DefaultValue("目標点")]
        public static readonly string TransformProperty_CameraPointOfInterest = nameof(TransformProperty_CameraPointOfInterest);

        [LanguageKey, DefaultValue("ズーム")]
        public static readonly string LayerOptionsProperty_CameraZoom = nameof(LayerOptionsProperty_CameraZoom);

        [LanguageKey, DefaultValue("ライトの種類")]
        public static readonly string LayerOptionsProperty_LightType = nameof(LayerOptionsProperty_LightType);

        [LanguageKey, DefaultValue("色")]
        public static readonly string LayerOptionsProperty_Color = nameof(LayerOptionsProperty_Color);

        [LanguageKey, DefaultValue("強度")]
        public static readonly string LayerOptionsProperty_Intensity = nameof(LayerOptionsProperty_Intensity);

        [LanguageKey, DefaultValue("円錐頂角")]
        public static readonly string LayerOptionsProperty_ConeAngle = nameof(LayerOptionsProperty_ConeAngle);

        [LanguageKey, DefaultValue("円錐ぼかし")]
        public static readonly string LayerOptionsProperty_ConeAttenuation = nameof(LayerOptionsProperty_ConeAttenuation);

        [LanguageKey, DefaultValue("フォールオフの種類")]
        public static readonly string LayerOptionsProperty_FalloffType = nameof(LayerOptionsProperty_FalloffType);

        [LanguageKey, DefaultValue("フォールオフの開始")]
        public static readonly string LayerOptionsProperty_FalloffStart = nameof(LayerOptionsProperty_FalloffStart);

        [LanguageKey, DefaultValue("フォールオフの距離")]
        public static readonly string LayerOptionsProperty_FalloffLength = nameof(LayerOptionsProperty_FalloffLength);

        [LanguageKey, DefaultValue("影を落とす")]
        public static readonly string LayerOptionsProperty_EnableShadow = nameof(LayerOptionsProperty_EnableShadow);

        [LanguageKey, DefaultValue("影の濃さ")]
        public static readonly string LayerOptionsProperty_ShadowStrength = nameof(LayerOptionsProperty_ShadowStrength);

        [LanguageKey, DefaultValue("影のぼかし")]
        public static readonly string LayerOptionsProperty_ShadowScatterSize = nameof(LayerOptionsProperty_ShadowScatterSize);

        [LanguageKey, DefaultValue("ソーステキスト")]
        public static readonly string TextProperty_SourceText = nameof(TextProperty_SourceText);

        [LanguageKey, DefaultValue("詳細")]
        public static readonly string TextProperty_TextMoreOptions = nameof(TextProperty_TextMoreOptions);

        [LanguageKey, DefaultValue("アンカーポイントの基準")]
        public static readonly string TextProperty_TextMoreOptions_BaseAnchorPointRate = nameof(TextProperty_TextMoreOptions_BaseAnchorPointRate);

        [LanguageKey, DefaultValue("テキストボックスのサイズ")]
        public static readonly string TextProperty_TextMoreOptions_TextBoxSize = nameof(TextProperty_TextMoreOptions_TextBoxSize);

        [LanguageKey, DefaultValue("文字間のブレンドモード")]
        public static readonly string TextProperty_TextMoreOptions_InterCharacterBlendMode = nameof(TextProperty_TextMoreOptions_InterCharacterBlendMode);

        [LanguageKey, DefaultValue("縦書き")]
        public static readonly string TextProperty_TextMoreOptions_IsEnableVerticalMode = nameof(TextProperty_TextMoreOptions_IsEnableVerticalMode);

        [LanguageKey, DefaultValue("パス")]
        public static readonly string TextProperty_TextPathOptions = nameof(TextProperty_TextPathOptions);

        [LanguageKey, DefaultValue("マスク")]
        public static readonly string TextProperty_TextPath_TargetMask = nameof(TextProperty_TextPath_TargetMask);

        [LanguageKey, DefaultValue("テキストの開始位置を反転")]
        public static readonly string TextProperty_TextPath_IsInvert = nameof(TextProperty_TextPath_IsInvert);

        [LanguageKey, DefaultValue("文字を回転しない")]
        public static readonly string TextProperty_TextPath_NotRotateCharacter = nameof(TextProperty_TextPath_NotRotateCharacter);

        [LanguageKey, DefaultValue("開始位置のマージン")]
        public static readonly string TextProperty_TextPath_BeginMargin = nameof(TextProperty_TextPath_BeginMargin);

        [LanguageKey, DefaultValue("テキストアニメータ")]
        public static readonly string TextProperty_TextAnimator = nameof(TextProperty_TextAnimator);

        [LanguageKey, DefaultValue("アニメータ")]
        public static readonly string TextProperty_TextAnimator_Animator = nameof(TextProperty_TextAnimator_Animator);

        [LanguageKey, DefaultValue("範囲セレクタ")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector = nameof(TextProperty_TextAnimator_Animator_Selector);

        [LanguageKey, DefaultValue("セレクタ")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Selector = nameof(TextProperty_TextAnimator_Animator_Selector_Selector);

        [LanguageKey, DefaultValue("開始")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Begin = nameof(TextProperty_TextAnimator_Animator_Selector_Begin);

        [LanguageKey, DefaultValue("終了")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_End = nameof(TextProperty_TextAnimator_Animator_Selector_End);

        [LanguageKey, DefaultValue("オフセット")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Offset = nameof(TextProperty_TextAnimator_Animator_Selector_Offset);

        [LanguageKey, DefaultValue("詳細")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_MoreOption = nameof(TextProperty_TextAnimator_Animator_Selector_MoreOption);

        [LanguageKey, DefaultValue("基準")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Criteria = nameof(TextProperty_TextAnimator_Animator_Selector_Criteria);

        [LanguageKey, DefaultValue("モード")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_BlendMode = nameof(TextProperty_TextAnimator_Animator_Selector_BlendMode);

        [LanguageKey, DefaultValue("量")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Amount = nameof(TextProperty_TextAnimator_Animator_Selector_Amount);

        [LanguageKey, DefaultValue("シェイプ")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_Shape = nameof(TextProperty_TextAnimator_Animator_Selector_Shape);

        [LanguageKey, DefaultValue("ランダム")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_EnableRandom = nameof(TextProperty_TextAnimator_Animator_Selector_EnableRandom);

        [LanguageKey, DefaultValue("ランダムシード")]
        public static readonly string TextProperty_TextAnimator_Animator_Selector_RandomSeed = nameof(TextProperty_TextAnimator_Animator_Selector_RandomSeed);

        [LanguageKey, DefaultValue("値")]
        public static readonly string TextProperty_TextAnimator_Animator_Value = nameof(TextProperty_TextAnimator_Animator_Value);

        [LanguageKey, DefaultValue("アンカーポイント")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_AnchorPoint = nameof(TextProperty_TextAnimator_Animator_Value_AnchorPoint);

        [LanguageKey, DefaultValue("位置")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Position = nameof(TextProperty_TextAnimator_Animator_Value_Position);

        [LanguageKey, DefaultValue("スケール")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Scale = nameof(TextProperty_TextAnimator_Animator_Value_Scale);

        [LanguageKey, DefaultValue("回転")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Angle = nameof(TextProperty_TextAnimator_Animator_Value_Angle);

        [LanguageKey, DefaultValue("歪曲")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Skew = nameof(TextProperty_TextAnimator_Animator_Value_Skew);

        [LanguageKey, DefaultValue("歪曲軸")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_SkewAxis = nameof(TextProperty_TextAnimator_Animator_Value_SkewAxis);

        [LanguageKey, DefaultValue("不透明度")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Opacity = nameof(TextProperty_TextAnimator_Animator_Value_Opacity);

        [LanguageKey, DefaultValue("フォントサイズ")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_FontSize = nameof(TextProperty_TextAnimator_Animator_Value_FontSize);

        [LanguageKey, DefaultValue("塗りの色")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_FillColor = nameof(TextProperty_TextAnimator_Animator_Value_FillColor);

        [LanguageKey, DefaultValue("塗りの不透明度")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_FillColorOpacity = nameof(TextProperty_TextAnimator_Animator_Value_FillColorOpacity);

        [LanguageKey, DefaultValue("線の色")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_TextLineColor = nameof(TextProperty_TextAnimator_Animator_Value_TextLineColor);

        [LanguageKey, DefaultValue("線の不透明度")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_TextLineColorOpacity = nameof(TextProperty_TextAnimator_Animator_Value_TextLineColorOpacity);

        [LanguageKey, DefaultValue("線の太さ")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_TextLineWidth = nameof(TextProperty_TextAnimator_Animator_Value_TextLineWidth);

        [LanguageKey, DefaultValue("文字のオフセット")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_CharacterOffset = nameof(TextProperty_TextAnimator_Animator_Value_CharacterOffset);

        [LanguageKey, DefaultValue("存在しない文字は空白にする")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_CharacterOffset_WhiteSpaceReplacementChar = nameof(TextProperty_TextAnimator_Animator_Value_CharacterOffset_WhiteSpaceReplacementChar);

        [LanguageKey, DefaultValue("ASCIIの範囲内に収める")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_CharacterOffset_RestrictAscii = nameof(TextProperty_TextAnimator_Animator_Value_CharacterOffset_RestrictAscii);

        [LanguageKey, DefaultValue("ブラー")]
        public static readonly string TextProperty_TextAnimator_Animator_Value_Blur = nameof(TextProperty_TextAnimator_Animator_Value_Blur);

        [LanguageKey, DefaultValue("コンテンツ")]
        public static readonly string ShapeProperty_Content = nameof(ShapeProperty_Content);

        [LanguageKey, DefaultValue("グループ")]
        public static readonly string ShapeProperty_Group = nameof(ShapeProperty_Group);

        [LanguageKey, DefaultValue("コンテンツ")]
        public static readonly string ShapeProperty_Group_Content = nameof(ShapeProperty_Group_Content);

        [LanguageKey, DefaultValue("トランスフォーム")]
        public static readonly string ShapeProperty_Transform = nameof(ShapeProperty_Transform);

        [LanguageKey, DefaultValue("アンカーポイント")]
        public static readonly string ShapeProperty_Transform_AnchorPoint = nameof(ShapeProperty_Transform_AnchorPoint);

        [LanguageKey, DefaultValue("位置")]
        public static readonly string ShapeProperty_Transform_Position = nameof(ShapeProperty_Transform_Position);

        [LanguageKey, DefaultValue("スケール")]
        public static readonly string ShapeProperty_Transform_Scale = nameof(ShapeProperty_Transform_Scale);

        [LanguageKey, DefaultValue("回転")]
        public static readonly string ShapeProperty_Transform_Angle = nameof(ShapeProperty_Transform_Angle);

        [LanguageKey, DefaultValue("歪曲")]
        public static readonly string ShapeProperty_Transform_Skew = nameof(ShapeProperty_Transform_Skew);

        [LanguageKey, DefaultValue("歪曲軸")]
        public static readonly string ShapeProperty_Transform_SkewAxis = nameof(ShapeProperty_Transform_SkewAxis);

        [LanguageKey, DefaultValue("不透明度")]
        public static readonly string ShapeProperty_Transform_Opacity = nameof(ShapeProperty_Transform_Opacity);

        [LanguageKey, DefaultValue("長方形")]
        public static readonly string ShapeProperty_RectangleGroup = nameof(ShapeProperty_RectangleGroup);

        [LanguageKey, DefaultValue("角丸")]
        public static readonly string ShapeProperty_RectangleGroup_CornerRounded = nameof(ShapeProperty_RectangleGroup_CornerRounded);

        [LanguageKey, DefaultValue("円")]
        public static readonly string ShapeProperty_CircleGroup = nameof(ShapeProperty_CircleGroup);

        [LanguageKey, DefaultValue("多角形")]
        public static readonly string ShapeProperty_RegularPolygonGroup = nameof(ShapeProperty_RegularPolygonGroup);

        [LanguageKey, DefaultValue("半径")]
        public static readonly string ShapeProperty_RegularPolygonGroup_Radius = nameof(ShapeProperty_RegularPolygonGroup_Radius);

        [LanguageKey, DefaultValue("角の丸み")]
        public static readonly string ShapeProperty_RegularPolygonGroup_Rounded = nameof(ShapeProperty_RegularPolygonGroup_Rounded);

        [LanguageKey, DefaultValue("星")]
        public static readonly string ShapeProperty_StarGroup = nameof(ShapeProperty_StarGroup);

        [LanguageKey, DefaultValue("内半径")]
        public static readonly string ShapeProperty_StarGroup_InnerRadius = nameof(ShapeProperty_StarGroup_InnerRadius);

        [LanguageKey, DefaultValue("外半径")]
        public static readonly string ShapeProperty_StarGroup_OuterRadius = nameof(ShapeProperty_StarGroup_OuterRadius);

        [LanguageKey, DefaultValue("内側の丸み")]
        public static readonly string ShapeProperty_StarGroup_InnerRounded = nameof(ShapeProperty_StarGroup_InnerRounded);

        [LanguageKey, DefaultValue("外側の丸み")]
        public static readonly string ShapeProperty_StarGroup_OuterRounded = nameof(ShapeProperty_StarGroup_OuterRounded);

        [LanguageKey, DefaultValue("頂点数")]
        public static readonly string ShapeProperty_PolygonGroup_Points = nameof(ShapeProperty_PolygonGroup_Points);

        [LanguageKey, DefaultValue("パス")]
        public static readonly string ShapeProperty_PathGroup = nameof(ShapeProperty_PathGroup);

        [LanguageKey, DefaultValue("パス")]
        public static readonly string ShapeProperty_PathGroup_BezierPath = nameof(ShapeProperty_PathGroup_BezierPath);

        [LanguageKey, DefaultValue("サイズ")]
        public static readonly string ShapeProperty_ShapeObjectGroup_Size = nameof(ShapeProperty_ShapeObjectGroup_Size);

        [LanguageKey, DefaultValue("位置")]
        public static readonly string ShapeProperty_ShapeObjectGroup_Position = nameof(ShapeProperty_ShapeObjectGroup_Position);

        [LanguageKey, DefaultValue("回転")]
        public static readonly string ShapeProperty_ShapeObjectGroup_Angle = nameof(ShapeProperty_ShapeObjectGroup_Angle);

        [LanguageKey, DefaultValue("塗り")]
        public static readonly string ShapeProperty_SolidFillGroup = nameof(ShapeProperty_SolidFillGroup);

        [LanguageKey, DefaultValue("グラデーションの塗り")]
        public static readonly string ShapeProperty_GradientFillGroup = nameof(ShapeProperty_GradientFillGroup);

        [LanguageKey, DefaultValue("規則")]
        public static readonly string ShapeProperty_FillGroup_FillRule = nameof(ShapeProperty_FillGroup_FillRule);

        [LanguageKey, DefaultValue("線")]
        public static readonly string ShapeProperty_SolidStrokeGroup = nameof(ShapeProperty_SolidStrokeGroup);

        [LanguageKey, DefaultValue("グラデーションの線")]
        public static readonly string ShapeProperty_GradientStrokeGroup = nameof(ShapeProperty_GradientStrokeGroup);

        [LanguageKey, DefaultValue("線幅")]
        public static readonly string ShapeProperty_StrokeGroup_Width = nameof(ShapeProperty_StrokeGroup_Width);

        [LanguageKey, DefaultValue("線端")]
        public static readonly string ShapeProperty_StrokeGroup_EndCapStyleType = nameof(ShapeProperty_StrokeGroup_EndCapStyleType);

        [LanguageKey, DefaultValue("線の結合")]
        public static readonly string ShapeProperty_StrokeGroup_JoinStyleType = nameof(ShapeProperty_StrokeGroup_JoinStyleType);

        [LanguageKey, DefaultValue("グラデーションの種類")]
        public static readonly string ShapeProperty_GradientGroup_Type = nameof(ShapeProperty_GradientGroup_Type);

        [LanguageKey, DefaultValue("グラデーションの編集")]
        public static readonly string ShapeProperty_GradientGroup_Color_Edit = nameof(ShapeProperty_GradientGroup_Color_Edit);

        [LanguageKey, DefaultValue("OKLab色空間で補間する")]
        public static readonly string ShapeProperty_GradientGroup_UseOkLabInterpolation = nameof(ShapeProperty_GradientGroup_UseOkLabInterpolation);

        [LanguageKey, DefaultValue("開始点")]
        public static readonly string ShapeProperty_GradientGroup_BeginPosition = nameof(ShapeProperty_GradientGroup_BeginPosition);

        [LanguageKey, DefaultValue("終了点")]
        public static readonly string ShapeProperty_GradientGroup_EndPosition = nameof(ShapeProperty_GradientGroup_EndPosition);

        [LanguageKey, DefaultValue("色")]
        public static readonly string ShapeProperty_Drawing_Color = nameof(ShapeProperty_Drawing_Color);

        [LanguageKey, DefaultValue("不透明度")]
        public static readonly string ShapeProperty_Drawing_Opacity = nameof(ShapeProperty_Drawing_Opacity);

        [LanguageKey, DefaultValue("ブレンドモード")]
        public static readonly string ShapeProperty_Drawing_BlendMode = nameof(ShapeProperty_Drawing_BlendMode);

        [LanguageKey, DefaultValue("リピータ")]
        public static readonly string ShapeProperty_RepeaterGroup = nameof(ShapeProperty_RepeaterGroup);

        [LanguageKey, DefaultValue("数")]
        public static readonly string ShapeProperty_RepeaterGroup_Count = nameof(ShapeProperty_RepeaterGroup_Count);

        [LanguageKey, DefaultValue("オフセット")]
        public static readonly string ShapeProperty_RepeaterGroup_Offset = nameof(ShapeProperty_RepeaterGroup_Offset);

        [LanguageKey, DefaultValue("開始点の不透明度")]
        public static readonly string ShapeProperty_RepeaterGroup_Transform_BeginPointOpacity = nameof(ShapeProperty_RepeaterGroup_Transform_BeginPointOpacity);

        [LanguageKey, DefaultValue("終了点の不透明度")]
        public static readonly string ShapeProperty_RepeaterGroup_Transform_EndPointOpacity = nameof(ShapeProperty_RepeaterGroup_Transform_EndPointOpacity);

        [LanguageKey, DefaultValue("パスの結合")]
        public static readonly string ShapeProperty_CombineGroup = nameof(ShapeProperty_CombineGroup);

        [LanguageKey, DefaultValue("種類")]
        public static readonly string ShapeProperty_CombineGroup_CombineType = nameof(ShapeProperty_CombineGroup_CombineType);

        [LanguageKey, DefaultValue("パスのトリミング")]
        public static readonly string ShapeProperty_TrimmingGroup = nameof(ShapeProperty_TrimmingGroup);

        [LanguageKey, DefaultValue("開始点")]
        public static readonly string ShapeProperty_TrimmingGroup_Begin = nameof(ShapeProperty_TrimmingGroup_Begin);

        [LanguageKey, DefaultValue("終了点")]
        public static readonly string ShapeProperty_TrimmingGroup_End = nameof(ShapeProperty_TrimmingGroup_End);

        [LanguageKey, DefaultValue("オフセット")]
        public static readonly string ShapeProperty_TrimmingGroup_Offset = nameof(ShapeProperty_TrimmingGroup_Offset);

        [ShowInMarkup, LanguageKey, DefaultValue("マスク設定")]
        public static readonly string MaskProperty_Setting = nameof(MaskProperty_Setting);

        [ShowInMarkup, LanguageKey, DefaultValue("パス")]
        public static readonly string MaskProperty_Setting_BezierPath = nameof(MaskProperty_Setting_BezierPath);

        [ShowInMarkup, LanguageKey, DefaultValue("形状")]
        public static readonly string MaskProperty_Setting_ShapeType = nameof(MaskProperty_Setting_ShapeType);

        [ShowInMarkup, LanguageKey, DefaultValue("サイズ")]
        public static readonly string MaskProperty_Setting_Size = nameof(MaskProperty_Setting_Size);

        [ShowInMarkup, LanguageKey, DefaultValue("位置")]
        public static readonly string MaskProperty_Setting_Position = nameof(MaskProperty_Setting_Position);

        [ShowInMarkup, LanguageKey, DefaultValue("マスクのぼかし")]
        public static readonly string MaskProperty_Setting_Blur = nameof(MaskProperty_Setting_Blur);

        [ShowInMarkup, LanguageKey, DefaultValue("マスクの不透明度")]
        public static readonly string MaskProperty_Setting_Opacity = nameof(MaskProperty_Setting_Opacity);

        [ShowInMarkup, LanguageKey, DefaultValue("マスクのブレンドモード")]
        public static readonly string MaskProperty_Setting_BlendMode = nameof(MaskProperty_Setting_BlendMode);

        [ShowInMarkup, LanguageKey, DefaultValue("反転")]
        public static readonly string MaskProperty_Setting_IsInvert = nameof(MaskProperty_Setting_IsInvert);

        // Property Control

        [ShowInMarkup, LanguageKey, DefaultValue("編集")]
        public static readonly string SourceTextPropertyControl_Edit = nameof(SourceTextPropertyControl_Edit);

        // Model Data

        [DefaultValue("マスク {0}")]
        public static readonly string LayerModel_NewMaskTemplate = nameof(LayerModel_NewMaskTemplate);

        // ShortcutKeyNames

        [DefaultValue("プリセットを適用")]
        public static readonly string ShortcutKeyName_LoadPropertyPresetGesture = nameof(ShortcutKeyName_LoadPropertyPresetGesture);

        [DefaultValue("プリセットとして保存")]
        public static readonly string ShortcutKeyName_SavePropertyPresetGesture = nameof(ShortcutKeyName_SavePropertyPresetGesture);

        [DefaultValue("再生・停止")]
        public static readonly string ShortcutKeyName_PlayOrStopGesture = nameof(ShortcutKeyName_PlayOrStopGesture);

        [DefaultValue("長方形のマスクを追加")]
        public static readonly string ShortcutKeyName_AddRectangleMaskGesture = nameof(ShortcutKeyName_AddRectangleMaskGesture);

        [DefaultValue("楕円形のマスクを追加")]
        public static readonly string ShortcutKeyName_AddEllipseMaskGesture = nameof(ShortcutKeyName_AddEllipseMaskGesture);

        [DefaultValue("バスのマスクを追加")]
        public static readonly string ShortcutKeyName_AddBezierMaskGesture = nameof(ShortcutKeyName_AddBezierMaskGesture);

        [DefaultValue("ワークエリアの終了をインジケーターの位置に移動")]
        public static readonly string ShortcutKeyName_MoveWorkareaEndToIndicatorGesture = nameof(ShortcutKeyName_MoveWorkareaEndToIndicatorGesture);

        [DefaultValue("ワークエリアの開始をインジケーターの位置に移動")]
        public static readonly string ShortcutKeyName_MoveWorkareaBeginToIndicatorGesture = nameof(ShortcutKeyName_MoveWorkareaBeginToIndicatorGesture);

        [DefaultValue("レイヤーの表示フレームを現在時刻で固定")]
        public static readonly string ShortcutKeyName_ChangeLayerFreezeFrameGesture = nameof(ShortcutKeyName_ChangeLayerFreezeFrameGesture);

        [DefaultValue("再生速度を変更")]
        public static readonly string ShortcutKeyName_ChangeLayerPlayRateGesture = nameof(ShortcutKeyName_ChangeLayerPlayRateGesture);

        [DefaultValue("インジケーターを選択レイヤーのアウトポイントに移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToSelectLayerOutPointGesture = nameof(ShortcutKeyName_MoveIndicatorToSelectLayerOutPointGesture);

        [DefaultValue("インジケーターを選択レイヤーのインポイントに移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToSelectLayerInPointGesture = nameof(ShortcutKeyName_MoveIndicatorToSelectLayerInPointGesture);

        [DefaultValue("インジケーターをコンポジションの最後に移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToCompositionEndGesture = nameof(ShortcutKeyName_MoveIndicatorToCompositionEndGesture);

        [DefaultValue("インジケーターをコンポジションの最初に移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToCompositionBeginGesture = nameof(ShortcutKeyName_MoveIndicatorToCompositionBeginGesture);

        [DefaultValue("インジケーターを10フレーム前に移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToPrevious10FrameGesture = nameof(ShortcutKeyName_MoveIndicatorToPrevious10FrameGesture);

        [DefaultValue("インジケーターを10フレーム後ろに移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToNext10FrameGesture = nameof(ShortcutKeyName_MoveIndicatorToNext10FrameGesture);

        [DefaultValue("インジケーターを1フレーム前に移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToPreviousFrameGesture = nameof(ShortcutKeyName_MoveIndicatorToPreviousFrameGesture);

        [DefaultValue("インジケーターを1フレーム後ろに移動")]
        public static readonly string ShortcutKeyName_MoveIndicatorToNextFrameGesture = nameof(ShortcutKeyName_MoveIndicatorToNextFrameGesture);

        [DefaultValue("レイヤーを10フレーム前にシフト")]
        public static readonly string ShortcutKeyName_ShiftSourceStartPointToPrevious10FrameGesture = nameof(ShortcutKeyName_ShiftSourceStartPointToPrevious10FrameGesture);

        [DefaultValue("レイヤーを10フレーム後ろにシフト")]
        public static readonly string ShortcutKeyName_ShiftSourceStartPointToNext10FrameGesture = nameof(ShortcutKeyName_ShiftSourceStartPointToNext10FrameGesture);

        [DefaultValue("レイヤーを1フレーム前にシフト")]
        public static readonly string ShortcutKeyName_ShiftSourceStartPointToPreviousFrameGesture = nameof(ShortcutKeyName_ShiftSourceStartPointToPreviousFrameGesture);

        [DefaultValue("レイヤーを1フレーム後ろにシフト")]
        public static readonly string ShortcutKeyName_ShiftSourceStartPointToNextFrameGesture = nameof(ShortcutKeyName_ShiftSourceStartPointToNextFrameGesture);

        [DefaultValue("インジケーターの位置にアウトポイントを移動")]
        public static readonly string ShortcutKeyName_MoveOutPointToIndicatorGesture = nameof(ShortcutKeyName_MoveOutPointToIndicatorGesture);

        [DefaultValue("インジケーターの位置にインポイントを移動")]
        public static readonly string ShortcutKeyName_MoveInPointToIndicatorGesture = nameof(ShortcutKeyName_MoveInPointToIndicatorGesture);

        [DefaultValue("アウトポイントを基準にインジケーターの位置にレイヤー時間を移動")]
        public static readonly string ShortcutKeyName_MoveSourceStartPointToIndicatorBaseOutPointGesture = nameof(ShortcutKeyName_MoveSourceStartPointToIndicatorBaseOutPointGesture);

        [DefaultValue("インポイントを基準にインジケーターの位置にレイヤー時間を移動")]
        public static readonly string ShortcutKeyName_MoveSourceStartPointToIndicatorBaseInPointGesture = nameof(ShortcutKeyName_MoveSourceStartPointToIndicatorBaseInPointGesture);

        [DefaultValue("レイヤーの順番を1つ下げる")]
        public static readonly string ShortcutKeyName_MoveLayerOrderDownGesture = nameof(ShortcutKeyName_MoveLayerOrderDownGesture);

        [DefaultValue("レイヤーの順番を1つ上げる")]
        public static readonly string ShortcutKeyName_MoveLayerOrderUpGesture = nameof(ShortcutKeyName_MoveLayerOrderUpGesture);

        [DefaultValue("レイヤーのタグをランダムに変更")]
        public static readonly string ShortcutKeyName_ChangeLayerTagsRandomlyGesture = nameof(ShortcutKeyName_ChangeLayerTagsRandomlyGesture);

        [DefaultValue("コマンドパレットを開く")]
        public static readonly string ShortcutKeyName_OpenCommandPaletteGesture = nameof(ShortcutKeyName_OpenCommandPaletteGesture);

        [DefaultValue("プロパティをリセット")]
        public static readonly string ShortcutKeyName_ResetPropertyGesture = nameof(ShortcutKeyName_ResetPropertyGesture);

        [DefaultValue("キーフレームを追加")]
        public static readonly string ShortcutKeyName_AddKeyFrameGesture = nameof(ShortcutKeyName_AddKeyFrameGesture);

        [DefaultValue("ファイルの読み込み")]
        public static readonly string ShortcutKeyName_LoadFileGesture = nameof(ShortcutKeyName_LoadFileGesture);

        [DefaultValue("平面の読み込み")]
        public static readonly string ShortcutKeyName_LoadSolidGesture = nameof(ShortcutKeyName_LoadSolidGesture);

        [DefaultValue("コンポジション設定")]
        public static readonly string ShortcutKeyName_OpenCompositionSettingGesture = nameof(ShortcutKeyName_OpenCompositionSettingGesture);

        [DefaultValue("テキストの追加")]
        public static readonly string ShortcutKeyName_AddTextLayerGesture = nameof(ShortcutKeyName_AddTextLayerGesture);

        [DefaultValue("ヌルオブジェクトの追加")]
        public static readonly string ShortcutKeyName_AddNullObjectLayerGesture = nameof(ShortcutKeyName_AddNullObjectLayerGesture);

        [DefaultValue("ライトの追加")]
        public static readonly string ShortcutKeyName_AddLightLayerGesture = nameof(ShortcutKeyName_AddLightLayerGesture);

        [DefaultValue("カメラの追加")]
        public static readonly string ShortcutKeyName_AddCameraLayerGesture = nameof(ShortcutKeyName_AddCameraLayerGesture);

        [DefaultValue("シェイプの追加")]
        public static readonly string ShortcutKeyName_AddShapeLayerGesture = nameof(ShortcutKeyName_AddShapeLayerGesture);

        [DefaultValue("全て選択")]
        public static readonly string ShortcutKeyName_SelectAllGesture = nameof(ShortcutKeyName_SelectAllGesture);

        [DefaultValue("カメラツールに切り替え")]
        public static readonly string ShortcutKeyName_SelectCameraToolGesture = nameof(ShortcutKeyName_SelectCameraToolGesture);

        [DefaultValue("拡大縮小ツールに切り替え")]
        public static readonly string ShortcutKeyName_SelectScaleGestureGesture = nameof(ShortcutKeyName_SelectScaleGestureGesture);

        [DefaultValue("回転ツールに切り替え")]
        public static readonly string ShortcutKeyName_SelectRotateToolGesture = nameof(ShortcutKeyName_SelectRotateToolGesture);

        [DefaultValue("選択ツールに切り替え")]
        public static readonly string ShortcutKeyName_SelectSelectToolGesture = nameof(ShortcutKeyName_SelectSelectToolGesture);

        [DefaultValue("手のひらツールに切り替え")]
        public static readonly string ShortcutKeyName_SelectHandToolGesture = nameof(ShortcutKeyName_SelectHandToolGesture);

        [DefaultValue("レンダリング設定・レンダーキューに追加")]
        public static readonly string ShortcutKeyName_OpenRenderSettingGesture = nameof(ShortcutKeyName_OpenRenderSettingGesture);

        [DefaultValue("名前の変更")]
        public static readonly string ShortcutKeyName_BeginEditNameGesture = nameof(ShortcutKeyName_BeginEditNameGesture);

        [DefaultValue("やり直し")]
        public static readonly string ShortcutKeyName_RedoGesture = nameof(ShortcutKeyName_RedoGesture);

        [DefaultValue("元に戻す")]
        public static readonly string ShortcutKeyName_UndoGesture = nameof(ShortcutKeyName_UndoGesture);

        [DefaultValue("フォルダを追加")]
        public static readonly string ShortcutKeyName_NewFootageFolderGesture = nameof(ShortcutKeyName_NewFootageFolderGesture);

        [DefaultValue("平面の追加")]
        public static readonly string ShortcutKeyName_AddSolidLayerGesture = nameof(ShortcutKeyName_AddSolidLayerGesture);

        [DefaultValue("別名でプロジェクトを保存")]
        public static readonly string ShortcutKeyName_SaveProjectAsNewNameGesture = nameof(ShortcutKeyName_SaveProjectAsNewNameGesture);

        [DefaultValue("プロジェクトを保存")]
        public static readonly string ShortcutKeyName_SaveProjectGesture = nameof(ShortcutKeyName_SaveProjectGesture);

        [DefaultValue("プロジェクトを開く")]
        public static readonly string ShortcutKeyName_OpenProjectGesture = nameof(ShortcutKeyName_OpenProjectGesture);

        [DefaultValue("新規プロジェクト")]
        public static readonly string ShortcutKeyName_NewProjectGesture = nameof(ShortcutKeyName_NewProjectGesture);

        [DefaultValue("終了")]
        public static readonly string ShortcutKeyName_ExitGesture = nameof(ShortcutKeyName_ExitGesture);

        [DefaultValue("レイヤーを分割")]
        public static readonly string ShortcutKeyName_SplitLayerGesture = nameof(ShortcutKeyName_SplitLayerGesture);

        [DefaultValue("複製")]
        public static readonly string ShortcutKeyName_DuplicateItemGesture = nameof(ShortcutKeyName_DuplicateItemGesture);

        [DefaultValue("ペースト")]
        public static readonly string ShortcutKeyName_PasteItemGesture = nameof(ShortcutKeyName_PasteItemGesture);

        [DefaultValue("コピー")]
        public static readonly string ShortcutKeyName_CopyItemGesture = nameof(ShortcutKeyName_CopyItemGesture);

        [DefaultValue("切り取り")]
        public static readonly string ShortcutKeyName_CutItemGesture = nameof(ShortcutKeyName_CutItemGesture);

        [DefaultValue("削除")]
        public static readonly string ShortcutKeyName_DeleteItemGesture = nameof(ShortcutKeyName_DeleteItemGesture);

        [DefaultValue("新規コンポジション")]
        public static readonly string ShortcutKeyName_NewCompositionGesture = nameof(ShortcutKeyName_NewCompositionGesture);

        // Unit

        [ShowInMarkup, LanguageKey, DefaultValue("%")]
        public static readonly string Unit_Percent = nameof(Unit_Percent);

        [ShowInMarkup, LanguageKey, DefaultValue("°")]
        public static readonly string Unit_Angle = nameof(Unit_Angle);

        [ShowInMarkup, LanguageKey, DefaultValue("px")]
        public static readonly string Unit_Pixel = nameof(Unit_Pixel);

        [ShowInMarkup, LanguageKey, DefaultValue("dB")]
        public static readonly string Unit_Decibel = nameof(Unit_Decibel);

        [ShowInMarkup, LanguageKey, DefaultValue("分")]
        public static readonly string Unit_Minute = nameof(Unit_Minute);

        [ShowInMarkup, LanguageKey, DefaultValue("個")]
        public static readonly string Unit_Pieces = nameof(Unit_Pieces);

        // Enum

        [DefaultValue("RGB")]
        public static readonly string PreviewColorChannel_Rgb = nameof(PreviewColorChannel_Rgb);

        [DefaultValue("赤")]
        public static readonly string PreviewColorChannel_R = nameof(PreviewColorChannel_R);

        [DefaultValue("緑")]
        public static readonly string PreviewColorChannel_G = nameof(PreviewColorChannel_G);

        [DefaultValue("青")]
        public static readonly string PreviewColorChannel_B = nameof(PreviewColorChannel_B);

        [DefaultValue("アルファ")]
        public static readonly string PreviewColorChannel_Alpha = nameof(PreviewColorChannel_Alpha);

        [DefaultValue("RGBストレート")]
        public static readonly string PreviewColorChannel_RgbStraight = nameof(PreviewColorChannel_RgbStraight);

        [DefaultValue("通常")]
        public static readonly string BlendMode_Normal = nameof(BlendMode_Normal);

        [DefaultValue("置換")]
        public static readonly string BlendMode_Replace = nameof(BlendMode_Replace);

        [DefaultValue("加算")]
        public static readonly string BlendMode_Add = nameof(BlendMode_Add);

        [DefaultValue("減算")]
        public static readonly string BlendMode_Subtract = nameof(BlendMode_Subtract);

        [DefaultValue("乗算")]
        public static readonly string BlendMode_Multiply = nameof(BlendMode_Multiply);

        [DefaultValue("スクリーン")]
        public static readonly string BlendMode_Screen = nameof(BlendMode_Screen);

        [DefaultValue("オーバーレイ")]
        public static readonly string BlendMode_Overlay = nameof(BlendMode_Overlay);

        [DefaultValue("ハードライト")]
        public static readonly string BlendMode_HardLight = nameof(BlendMode_HardLight);

        [DefaultValue("ソフトライト")]
        public static readonly string BlendMode_SoftLight = nameof(BlendMode_SoftLight);

        [DefaultValue("ビビッドライト")]
        public static readonly string BlendMode_VividLight = nameof(BlendMode_VividLight);

        [DefaultValue("リニアライト")]
        public static readonly string BlendMode_LinearLight = nameof(BlendMode_LinearLight);

        [DefaultValue("ピンライト")]
        public static readonly string BlendMode_PinLight = nameof(BlendMode_PinLight);

        [DefaultValue("覆い焼きカラー")]
        public static readonly string BlendMode_ColorDodge = nameof(BlendMode_ColorDodge);

        [DefaultValue("覆い焼きリニア")]
        public static readonly string BlendMode_LinearDodge = nameof(BlendMode_LinearDodge);

        [DefaultValue("焼き込みカラー")]
        public static readonly string BlendMode_ColorBurn = nameof(BlendMode_ColorBurn);

        [DefaultValue("焼き込みリニア")]
        public static readonly string BlendMode_LinearBurn = nameof(BlendMode_LinearBurn);

        [DefaultValue("比較(暗)")]
        public static readonly string BlendMode_Darken = nameof(BlendMode_Darken);

        [DefaultValue("比較(明)")]
        public static readonly string BlendMode_Lighten = nameof(BlendMode_Lighten);

        [DefaultValue("差")]
        public static readonly string BlendMode_Difference = nameof(BlendMode_Difference);

        [DefaultValue("除外")]
        public static readonly string BlendMode_Exclusion = nameof(BlendMode_Exclusion);

        [DefaultValue("色相")]
        public static readonly string BlendMode_Hue = nameof(BlendMode_Hue);

        [DefaultValue("彩度")]
        public static readonly string BlendMode_Saturation = nameof(BlendMode_Saturation);

        [DefaultValue("カラー")]
        public static readonly string BlendMode_Color = nameof(BlendMode_Color);

        [DefaultValue("輝度")]
        public static readonly string BlendMode_Luminance = nameof(BlendMode_Luminance);

        [DefaultValue("アルファ")]
        public static readonly string TrackMatteMode_Alpha = nameof(TrackMatteMode_Alpha);

        [DefaultValue("反転アルファ")]
        public static readonly string TrackMatteMode_InvertAlpha = nameof(TrackMatteMode_InvertAlpha);

        [DefaultValue("明度")]
        public static readonly string TrackMatteMode_Luminance = nameof(TrackMatteMode_Luminance);

        [DefaultValue("反転明度")]
        public static readonly string TrackMatteMode_InvertLuminance = nameof(TrackMatteMode_InvertLuminance);

        [DefaultValue("オフ")]
        public static readonly string ShadowCastMode_None = nameof(ShadowCastMode_None);

        [DefaultValue("オン")]
        public static readonly string ShadowCastMode_Cast = nameof(ShadowCastMode_Cast);

        [DefaultValue("影のみ")]
        public static readonly string ShadowCastMode_ShadowOnly = nameof(ShadowCastMode_ShadowOnly);

        [DefaultValue("オフ")]
        public static readonly string ShadowAcceptMode_None = nameof(ShadowAcceptMode_None);

        [DefaultValue("オン")]
        public static readonly string ShadowAcceptMode_Accept = nameof(ShadowAcceptMode_Accept);

        [DefaultValue("影のみ")]
        public static readonly string ShadowAcceptMode_ShadowOnly = nameof(ShadowAcceptMode_ShadowOnly);

        [DefaultValue("ポイント")]
        public static readonly string LightType_Point = nameof(LightType_Point);

        [DefaultValue("スポット")]
        public static readonly string LightType_Spot = nameof(LightType_Spot);

        [DefaultValue("平行")]
        public static readonly string LightType_Parallel = nameof(LightType_Parallel);

        [DefaultValue("アンビエント")]
        public static readonly string LightType_Ambient = nameof(LightType_Ambient);

        [DefaultValue("なし")]
        public static readonly string LightFalloffType_None = nameof(LightFalloffType_None);

        [DefaultValue("リニア")]
        public static readonly string LightFalloffType_Linear = nameof(LightFalloffType_Linear);

        [DefaultValue("逆二乗クランプ")]
        public static readonly string LightFalloffType_Exponential = nameof(LightFalloffType_Exponential);

        [DefaultValue("線の上に塗り")]
        public static readonly string TextLineDrawOrder_BeforeFill = nameof(TextLineDrawOrder_BeforeFill);

        [DefaultValue("塗りの上に線")]
        public static readonly string TextLineDrawOrder_AfterFill = nameof(TextLineDrawOrder_AfterFill);

        [DefaultValue("文字")]
        public static readonly string SelectorCriteria_Charactor = nameof(SelectorCriteria_Charactor);

        [DefaultValue("空白を除いた文字")]
        public static readonly string SelectorCriteria_CharactorWithoutSpace = nameof(SelectorCriteria_CharactorWithoutSpace);

        [DefaultValue("単語")]
        public static readonly string SelectorCriteria_Word = nameof(SelectorCriteria_Word);

        [DefaultValue("行")]
        public static readonly string SelectorCriteria_Line = nameof(SelectorCriteria_Line);

        [DefaultValue("加算")]
        public static readonly string SelectorBlendMode_Add = nameof(SelectorBlendMode_Add);

        [DefaultValue("減算")]
        public static readonly string SelectorBlendMode_Subtract = nameof(SelectorBlendMode_Subtract);

        [DefaultValue("乗算")]
        public static readonly string SelectorBlendMode_Multiply = nameof(SelectorBlendMode_Multiply);

        [DefaultValue("最小")]
        public static readonly string SelectorBlendMode_Min = nameof(SelectorBlendMode_Min);

        [DefaultValue("最大")]
        public static readonly string SelectorBlendMode_Max = nameof(SelectorBlendMode_Max);

        [DefaultValue("差")]
        public static readonly string SelectorBlendMode_Difference = nameof(SelectorBlendMode_Difference);

        [DefaultValue("四角")]
        public static readonly string SelectorShape_Rectangle = nameof(SelectorShape_Rectangle);

        [DefaultValue("上へ傾斜")]
        public static readonly string SelectorShape_RampUp = nameof(SelectorShape_RampUp);

        [DefaultValue("下へ傾斜")]
        public static readonly string SelectorShape_RampDown = nameof(SelectorShape_RampDown);

        [DefaultValue("三角形")]
        public static readonly string SelectorShape_Triangle = nameof(SelectorShape_Triangle);

        [DefaultValue("円")]
        public static readonly string SelectorShape_Circle = nameof(SelectorShape_Circle);

        [DefaultValue("非ゼロ規則")]
        public static readonly string ShapeFillRule_NonZero = nameof(ShapeFillRule_NonZero);

        [DefaultValue("奇偶規則")]
        public static readonly string ShapeFillRule_EvenOdd = nameof(ShapeFillRule_EvenOdd);

        [DefaultValue("線形")]
        public static readonly string GradientType_Linear = nameof(GradientType_Linear);

        [DefaultValue("円形")]
        public static readonly string GradientType_Radial = nameof(GradientType_Radial);

        [DefaultValue("バット")]
        public static readonly string EndCapStyle_Butt = nameof(EndCapStyle_Butt);

        [DefaultValue("丸型")]
        public static readonly string EndCapStyle_Round = nameof(EndCapStyle_Round);

        [DefaultValue("四角")]
        public static readonly string EndCapStyle_Square = nameof(EndCapStyle_Square);

        [DefaultValue("ポリゴン")]
        public static readonly string EndCapStyle_Polygon = nameof(EndCapStyle_Polygon);

        [DefaultValue("結合")]
        public static readonly string EndCapStyle_Joined = nameof(EndCapStyle_Joined);

        [DefaultValue("四角")]
        public static readonly string JointStyle_Square = nameof(JointStyle_Square);

        [DefaultValue("丸型")]
        public static readonly string JointStyle_Round = nameof(JointStyle_Round);

        [DefaultValue("マイター")]
        public static readonly string JointStyle_Miter = nameof(JointStyle_Miter);

        [DefaultValue("結合")]
        public static readonly string ClippingOperation_None = nameof(ClippingOperation_None);

        [DefaultValue("交差")]
        public static readonly string ClippingOperation_Intersection = nameof(ClippingOperation_Intersection);

        [DefaultValue("追加")]
        public static readonly string ClippingOperation_Union = nameof(ClippingOperation_Union);

        [DefaultValue("差")]
        public static readonly string ClippingOperation_Difference = nameof(ClippingOperation_Difference);

        [DefaultValue("中マド")]
        public static readonly string ClippingOperation_Xor = nameof(ClippingOperation_Xor);

        [DefaultValue("長方形")]
        public static readonly string MaskShapeType_Rectangle = nameof(MaskShapeType_Rectangle);

        [DefaultValue("楕円形")]
        public static readonly string MaskShapeType_Ellipse = nameof(MaskShapeType_Ellipse);

        [DefaultValue("加算")]
        public static readonly string MaskBlendMode_Add = nameof(MaskBlendMode_Add);

        [DefaultValue("減算")]
        public static readonly string MaskBlendMode_Subtract = nameof(MaskBlendMode_Subtract);

        [DefaultValue("乗算")]
        public static readonly string MaskBlendMode_Multiply = nameof(MaskBlendMode_Multiply);

        [DefaultValue("比較(暗)")]
        public static readonly string MaskBlendMode_Darken = nameof(MaskBlendMode_Darken);

        [DefaultValue("比較(明)")]
        public static readonly string MaskBlendMode_Lighten = nameof(MaskBlendMode_Lighten);

        [DefaultValue("差")]
        public static readonly string MaskBlendMode_Difference = nameof(MaskBlendMode_Difference);

        [DefaultValue("ステレオ")]
        public static readonly string WaveFormType_Stereo = nameof(WaveFormType_Stereo);

        [DefaultValue("モノラル")]
        public static readonly string WaveFormType_Monaural = nameof(WaveFormType_Monaural);

        [DefaultValue("左チャンネル")]
        public static readonly string WaveFormType_Left = nameof(WaveFormType_Left);

        [DefaultValue("右チャンネル")]
        public static readonly string WaveFormType_Right = nameof(WaveFormType_Right);

        [DefaultValue("すべて")]
        public static readonly string RenderRangeType_All = nameof(RenderRangeType_All);

        [DefaultValue("コンポジションのワークエリア")]
        public static readonly string RenderRangeType_Workarea = nameof(RenderRangeType_Workarea);

        [DefaultValue("指定範囲")]
        public static readonly string RenderRangeType_Specific = nameof(RenderRangeType_Specific);

        [DefaultValue("設定不足")]
        public static readonly string RenderQueueItemState_NotReady = nameof(RenderQueueItemState_NotReady);

        [DefaultValue("レンダリング可能")]
        public static readonly string RenderQueueItemState_Ready = nameof(RenderQueueItemState_Ready);

        [DefaultValue("レンダリング中")]
        public static readonly string RenderQueueItemState_Rendering = nameof(RenderQueueItemState_Rendering);

        [DefaultValue("レンダリング済み")]
        public static readonly string RenderQueueItemState_Completed = nameof(RenderQueueItemState_Completed);

        [DefaultValue("中断")]
        public static readonly string RenderQueueItemState_Aborted = nameof(RenderQueueItemState_Aborted);

        [DefaultValue("エラー")]
        public static readonly string RenderQueueItemState_Error = nameof(RenderQueueItemState_Error);

        [DefaultValue("手のひらツール")]
        public static readonly string ToolType_Hand = nameof(ToolType_Hand);

        [DefaultValue("選択ツール")]
        public static readonly string ToolType_Select = nameof(ToolType_Select);

        [DefaultValue("回転ツール")]
        public static readonly string ToolType_RotateAll = nameof(ToolType_RotateAll);

        [DefaultValue("X回転ツール")]
        public static readonly string ToolType_RotateX = nameof(ToolType_RotateX);

        [DefaultValue("Y回転ツール")]
        public static readonly string ToolType_RotateY = nameof(ToolType_RotateY);

        [DefaultValue("Z回転ツール")]
        public static readonly string ToolType_RotateZ = nameof(ToolType_RotateZ);

        [DefaultValue("拡大縮小ツール")]
        public static readonly string ToolType_Scale = nameof(ToolType_Scale);

        [DefaultValue("カメラ目標点周回ツール")]
        public static readonly string ToolType_CameraOrbit = nameof(ToolType_CameraOrbit);

        [DefaultValue("カメラパンツール")]
        public static readonly string ToolType_CameraPan = nameof(ToolType_CameraPan);

        [DefaultValue("カメラドリーツール")]
        public static readonly string ToolType_CameraDolly = nameof(ToolType_CameraDolly);

        [DefaultValue("ファイル")]
        public static readonly string ShortcutKeyCategoryType_File = nameof(ShortcutKeyCategoryType_File);

        [DefaultValue("編集")]
        public static readonly string ShortcutKeyCategoryType_Edit = nameof(ShortcutKeyCategoryType_Edit);

        [DefaultValue("フッテージ")]
        public static readonly string ShortcutKeyCategoryType_Footage = nameof(ShortcutKeyCategoryType_Footage);

        [DefaultValue("コンポジション")]
        public static readonly string ShortcutKeyCategoryType_Composition = nameof(ShortcutKeyCategoryType_Composition);

        [DefaultValue("レイヤー")]
        public static readonly string ShortcutKeyCategoryType_Layer = nameof(ShortcutKeyCategoryType_Layer);

        [DefaultValue("ツール")]
        public static readonly string ShortcutKeyCategoryType_Tool = nameof(ShortcutKeyCategoryType_Tool);

        [DefaultValue("プレビュー")]
        public static readonly string ShortcutKeyCategoryType_Preview = nameof(ShortcutKeyCategoryType_Preview);

        [DefaultValue("その他")]
        public static readonly string ShortcutKeyCategoryType_Other = nameof(ShortcutKeyCategoryType_Other);

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
            JPDictionary = new LanguageResourceDictionary("ja-JP");
        }

        public LanguageResourceDictionary() : this(null) { }

        public LanguageResourceDictionary(string? forceLangCode)
        {
            Reload(forceLangCode);
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
