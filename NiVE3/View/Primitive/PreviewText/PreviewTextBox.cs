using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NiVE3.Numerics;
using NiVE3.Util;

namespace NiVE3.View.Primitive.PreviewText
{
    /// <summary>
    /// プレビューパネル上でテキストレイヤーのテキストを直接編集するためのテキストエディタコントロール。
    /// 標準 TextBox (AcceptsReturn=true) 相当のキーボード・マウス・IME・クリップボード・
    /// Undo/Redo 操作を提供する。
    ///
    /// レイアウト (文字位置⇔座標) は ITextLayout に分離してあり、CharacterGeometries が
    /// 設定されている場合はレンダリング結果に基づく GeometryTextLayout を、
    /// 未設定の場合は FormatterTextLayout (スタンドアロン描画) を使う。
    /// </summary>
    class PreviewTextBox : Control
    {
        #region 既定値のブラシ

        // 依存関係プロパティの既定値として使うため、静的初期化子の実行順 (ソース上の並び順) の都合で
        // DependencyProperty.Register より前に定義する必要がある

        static Brush DefaultSelectionBrush { get; } = CreateFrozenBrush(Color.FromArgb(0x66, 0x33, 0x99, 0xFF));

        static Brush DefaultBoundingBoxBrush { get; } = CreateFrozenBrush(Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF));

        static Brush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        #endregion 既定値のブラシ

        #region 依存関係プロパティ

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(
                "",
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextPropertyChanged,
                static (_, value) => value ?? ""
            )
        );

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(false, static (d, _) => ((PreviewTextBox)d).UpdateInputMethodState())
        );

        public static readonly DependencyProperty AcceptsReturnProperty = DependencyProperty.Register(
            nameof(AcceptsReturn),
            typeof(bool),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty AcceptsTabProperty = DependencyProperty.Register(
            nameof(AcceptsTab),
            typeof(bool),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(false)
        );

        public static readonly DependencyProperty SelectionBrushProperty = DependencyProperty.Register(
            nameof(SelectionBrush),
            typeof(Brush),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(DefaultSelectionBrush, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CaretBrushProperty = DependencyProperty.Register(
            nameof(CaretBrush),
            typeof(Brush),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CharacterGeometriesProperty = DependencyProperty.Register(
            nameof(CharacterGeometries),
            typeof(IReadOnlyList<CharacterGeometry>),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                static (d, _) => ((PreviewTextBox)d).InvalidateTextLayout()
            )
        );

        public static readonly DependencyProperty ShowBoundingBoxProperty = DependencyProperty.Register(
            nameof(ShowBoundingBox),
            typeof(bool),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty BoundingBoxBrushProperty = DependencyProperty.Register(
            nameof(BoundingBoxBrush),
            typeof(Brush),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(DefaultBoundingBoxBrush, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CompositionDisplayModeProperty = DependencyProperty.Register(
            nameof(CompositionDisplayMode),
            typeof(ImeCompositionDisplayMode),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(
                ImeCompositionDisplayMode.Renderer,
                FrameworkPropertyMetadataOptions.AffectsRender,
                static (d, _) => ((PreviewTextBox)d).OnCompositionDisplayModeChanged()
            )
        );

        public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty = DependencyProperty.Register(
            nameof(IsInactiveSelectionHighlightEnabled),
            typeof(bool),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty EmptyTextCaretGeometryProperty = DependencyProperty.Register(
            nameof(EmptyTextCaretGeometry),
            typeof(CharacterGeometry),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                static (d, _) => ((PreviewTextBox)d).InvalidateTextLayout()
            )
        );

        /// <summary>
        /// この添付プロパティが true の要素 (とその子孫) をクリックしても、編集中のテキストの選択解除とフォーカス解除を行わない。
        /// 選択範囲に対してフォントなどを変更するパネルのルート要素に指定する。
        /// </summary>
        public static readonly DependencyProperty KeepsSelectionOnClickProperty = DependencyProperty.RegisterAttached(
            "KeepsSelectionOnClick",
            typeof(bool),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits)
        );

        public static bool GetKeepsSelectionOnClick(DependencyObject element)
        {
            return (bool)element.GetValue(KeepsSelectionOnClickProperty);
        }

        public static void SetKeepsSelectionOnClick(DependencyObject element, bool value)
        {
            element.SetValue(KeepsSelectionOnClickProperty, value);
        }

        public static readonly DependencyProperty IsVerticalTextProperty = DependencyProperty.Register(
            nameof(IsVerticalText),
            typeof(bool),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                static (d, _) => ((PreviewTextBox)d).InvalidateTextLayout()
            )
        );

        public static readonly DependencyProperty OriginProperty = DependencyProperty.Register(
            nameof(Origin),
            typeof(Vector2d),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(
                Vector2d.Zero,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty PreviewImageScaleXProperty = DependencyProperty.Register(
            nameof(PreviewImageScaleX),
            typeof(double),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PreviewImageScaleYProperty = DependencyProperty.Register(
            nameof(PreviewImageScaleY),
            typeof(double),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PreviewImageLeftProperty = DependencyProperty.Register(
            nameof(PreviewImageLeft),
            typeof(double),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PreviewImageTopProperty = DependencyProperty.Register(
            nameof(PreviewImageTop),
            typeof(double),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender)
        );

        private static readonly DependencyPropertyKey SelectionRangePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SelectionRange),
            typeof(SelectionRange),
            typeof(PreviewTextBox),
            new FrameworkPropertyMetadata(new SelectionRange())
        );

        public static readonly DependencyProperty SelectionRangeProperty = SelectionRangePropertyKey.DependencyProperty;

        public SelectionRange SelectionRange
        {
            get { return (SelectionRange)GetValue(SelectionRangeProperty); }
            private set { SetValue(SelectionRangePropertyKey, value); }
        }

        public double PreviewImageTop
        {
            get { return (double)GetValue(PreviewImageTopProperty); }
            set { SetValue(PreviewImageTopProperty, value); }
        }

        public double PreviewImageLeft
        {
            get { return (double)GetValue(PreviewImageLeftProperty); }
            set { SetValue(PreviewImageLeftProperty, value); }
        }

        public double PreviewImageScaleY
        {
            get { return (double)GetValue(PreviewImageScaleYProperty); }
            set { SetValue(PreviewImageScaleYProperty, value); }
        }

        public double PreviewImageScaleX
        {
            get { return (double)GetValue(PreviewImageScaleXProperty); }
            set { SetValue(PreviewImageScaleXProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public bool AcceptsReturn
        {
            get { return (bool)GetValue(AcceptsReturnProperty); }
            set { SetValue(AcceptsReturnProperty, value); }
        }

        public bool AcceptsTab
        {
            get { return (bool)GetValue(AcceptsTabProperty); }
            set { SetValue(AcceptsTabProperty, value); }
        }

        public Brush SelectionBrush
        {
            get { return (Brush)GetValue(SelectionBrushProperty); }
            set { SetValue(SelectionBrushProperty, value); }
        }

        public Brush? CaretBrush
        {
            get { return (Brush?)GetValue(CaretBrushProperty); }
            set { SetValue(CaretBrushProperty, value); }
        }

        /// <summary>
        /// レンダラ側が生成したグリフごとのジオメトリ。
        /// 設定すると、テキスト本体の描画を行わないオーバーレイモード
        /// (GeometryTextLayout) に切り替わる。DisplayText の内容と対応している必要がある。
        /// </summary>
        public IReadOnlyList<CharacterGeometry>? CharacterGeometries
        {
            get { return (IReadOnlyList<CharacterGeometry>?)GetValue(CharacterGeometriesProperty); }
            set { SetValue(CharacterGeometriesProperty, value); }
        }

        /// <summary>
        /// テキスト全体のバウンディングボックスを表示するか (オーバーレイモードのみ)
        /// </summary>
        public bool ShowBoundingBox
        {
            get { return (bool)GetValue(ShowBoundingBoxProperty); }
            set { SetValue(ShowBoundingBoxProperty, value); }
        }

        public Brush BoundingBoxBrush
        {
            get { return (Brush)GetValue(BoundingBoxBrushProperty); }
            set { SetValue(BoundingBoxBrushProperty, value); }
        }

        /// <summary>
        /// IME 未確定文字列の表示方法。Renderer は DisplayText 経由の実レンダリング、
        /// Editor はエディタによるプレーンテキスト描画 (レンダラの再描画が不要)。
        /// オーバーレイ (ジオメトリ) モードのみ有効。スタンドアロンモードでは常にエディタが描画する。
        /// </summary>
        public ImeCompositionDisplayMode CompositionDisplayMode
        {
            get { return (ImeCompositionDisplayMode)GetValue(CompositionDisplayModeProperty); }
            set { SetValue(CompositionDisplayModeProperty, value); }
        }

        /// <summary>
        /// フォーカスが外れていても選択範囲のハイライトを表示するか (TextBox 互換)。
        /// フォント選択などのツールバー操作中に選択範囲を見せたい場合に有効にする。
        /// </summary>
        public bool IsInactiveSelectionHighlightEnabled
        {
            get { return (bool)GetValue(IsInactiveSelectionHighlightEnabledProperty); }
            set { SetValue(IsInactiveSelectionHighlightEnabledProperty, value); }
        }

        /// <summary>
        /// テキストが空でジオメトリが無いときに使うキャレット位置 (オーバーレイモード用)。
        /// Bounds はローカル空間でのキャレット矩形 (幅 0 でよい。X/Y/Height を使用)、
        /// GraphemeTransform はレイヤーのトランスフォーム。未設定の場合、空テキスト時はキャレットを表示しない。
        /// </summary>
        public CharacterGeometry? EmptyTextCaretGeometry
        {
            get { return (CharacterGeometry?)GetValue(EmptyTextCaretGeometryProperty); }
            set { SetValue(EmptyTextCaretGeometryProperty, value); }
        }

        /// <summary>
        /// 縦書きか (オーバーレイモードのみ)。
        /// true のとき CharacterGeometries は文字が下へ進み、列が右から左へ積まれる配置として解釈され、
        /// 上下キーで文字送り、左右キーで列の移動を行う。
        /// </summary>
        public bool IsVerticalText
        {
            get { return (bool)GetValue(IsVerticalTextProperty); }
            set { SetValue(IsVerticalTextProperty, value); }
        }

        /// <summary>
        /// プレビュー (レンダリング結果) の拡大縮小の中心 (オーバーレイモードのみ)。
        /// 仮想スクリーン座標 screen は control = Origin + (screen − Origin) × Scale + Offset で
        /// コントロール座標へ写像される (Scale = 1、Offset = 0 のとき仮想スクリーン座標 = コントロール座標)。
        /// PreviewTextCoordTransformer.ScreenCoordToLocalCoord の origin 引数にもこの値が渡される。
        /// Origin / Scale / Offset の変更ではジオメトリの再生成は不要 (レイアウトは再構築されない)。
        /// </summary>
        public Vector2d Origin
        {
            get { return (Vector2d)GetValue(OriginProperty); }
            set { SetValue(OriginProperty, value); }
        }

        /// <summary>
        /// Origin を中心としたプレビューの表示倍率 (オーバーレイモードのみ)。
        /// コントロール自体のサイズや LayoutTransform を変えずにプレビューの拡大率へ追従できる。
        /// PreviewTextCoordTransformer.ScreenCoordToLocalCoord の scale 引数にもこの値が渡される。
        /// </summary>
        public Vector2d Scale => new Vector2d(PreviewImageScaleX, PreviewImageScaleY);

        /// <summary>
        /// 拡大縮小後に加えるコントロール座標での平行移動 (オーバーレイモードのみ)。
        /// Scale = 1 のままでもプレビューを移動でき、移動量は倍率に依存しない。
        /// </summary>
        public Vector2d Offset => new Vector2d(PreviewImageLeft, PreviewImageTop);

        #endregion 依存関係プロパティ

        #region イベント

        public event EventHandler? TextChanged;

        public event EventHandler? SelectionChanged;

        /// <summary>
        /// テキスト編集の詳細 (位置・削除/挿入文字列) を通知する。
        /// DisplayTextChanged より先に発生するため、ホストはここでスタイル範囲などを
        /// 編集に追従させてから再レンダリングできる。
        /// </summary>
        public event EventHandler<TextEditedEventArgs>? TextEdited;

        /// <summary>
        /// DisplayText (確定テキスト+IME 未確定文字列) が変化したときに発生する。
        /// レンダラ側はこれを受けてテキストを再レンダリングし、CharacterGeometries を更新する。
        /// </summary>
        public event EventHandler? DisplayTextChanged;

        #endregion イベント

        #region 定数・既定値

        /// <summary>
        /// スタンドアロンモードでの行末キャレットの描画余白
        /// </summary>
        const double CaretMargin = 2.0;

        /// <summary>
        /// GetCaretBlinkTime が取得できないときの点滅間隔 (ミリ秒)
        /// </summary>
        const double DefaultCaretBlinkIntervalMilliseconds = 530.0;

        #endregion 定数・既定値

        #region 内部状態

        enum DragSelectionMode
        {
            None,
            Char,
            Word,
            Line
        }

        TextDocument Document { get; } = new();

        UndoStack UndoStack { get; } = new();

        DispatcherTimer CaretTimer { get; }

        /// <summary>
        /// 選択の起点
        /// </summary>
        int Anchor
        {
            get;
            set
            {
                field = value;
                SelectionRange = new SelectionRange(Math.Min(value, Caret), Math.Abs(value - Caret));
            }
        }

        /// <summary>
        /// キャレット位置 (選択の可動端)
        /// </summary>
        int Caret
        {
            get;
            set
            {
                field = value;
                SelectionRange = new SelectionRange(Math.Min(Anchor, value), Math.Abs(Anchor - value));
            }
        }

        /// <summary>
        /// 隣の行へ移動するときに維持する流れ方向の位置
        /// </summary>
        double? PreferredFlowPosition { get; set; }

        bool IsCaretBlinkVisible { get; set; } = true;

        bool IsSyncingTextDp { get; set; }

        (int Anchor, int Caret) LastRaisedSelection { get; set; }

        /// <summary>
        /// IME 未確定文字列 (ドキュメントには含めず、表示時にキャレット位置へ挿入する)
        /// </summary>
        string CompositionText { get; set; } = "";

        TextComposition? CurrentComposition { get; set; }

        /// <summary>
        /// IME プロキシ: キーボードフォーカスを持つ不可視の TextBox。
        /// WPF 標準の TSF テキストストアを持つため IME が完全に正常動作し、
        /// キャレット位置に配置することで候補ウィンドウがテキストレイヤーに追従する。
        /// キー・文字入力は Preview (トンネリング) 段階でエディタ側が処理する。
        /// </summary>
        TextBox ImeProxy { get; }

        // レイアウトキャッシュ

        ITextLayout? Layout { get; set; }

        string? LayoutText { get; set; }

        Typeface? LayoutTypeface { get; set; }

        double LayoutFontSize { get; set; }

        double LayoutPixelsPerDip { get; set; }

        Brush? LayoutForeground { get; set; }

        IReadOnlyList<CharacterGeometry>? LayoutGeometries { get; set; }

        CharacterGeometry? LayoutEmptyCaret { get; set; }

        bool LayoutIsVertical { get; set; }

        string? LastNotifiedDisplayText { get; set; }

        // マウスドラッグ選択

        DragSelectionMode DragMode { get; set; }

        (int Start, int End) DragOrigin { get; set; }

        #endregion 内部状態

        public PreviewTextBox()
        {
            Cursor = Cursors.IBeam;
            FocusVisualStyle = null;

            // 外側クリックの検出はルート要素で行うため、ビジュアルツリーへの接続に合わせて付け外しする
            Loaded += (_, _) => AttachOutsideClickHandler();
            Unloaded += (_, _) => DetachOutsideClickHandler();
            // 非表示になったとき (ツールの切り替えなど) は、WPF がキーボードフォーカスだけを他へ移して論理フォーカスが残るため、
            // 論理フォーカスも外して LostFocus を発生させる
            IsVisibleChanged += (_, e) =>
            {
                if (!(bool)e.NewValue)
                {
                    ClearLogicalFocus();
                }
            };

            CaretTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(GetCaretBlinkInterval()) };
            CaretTimer.Tick += (_, _) =>
            {
                IsCaretBlinkVisible = !IsCaretBlinkVisible;
                InvalidateVisual();
            };

            // IME プロキシからバブリングしてくる composition イベントを受け取る。
            // プロキシ TextBox 自身 (TextEditor) がイベントを Handled にするため、
            // handledEventsToo: true での登録が必須。
            AddHandler(TextCompositionManager.TextInputStartEvent, new TextCompositionEventHandler(OnTextCompositionStart), handledEventsToo: true);
            AddHandler(TextCompositionManager.TextInputUpdateEvent, new TextCompositionEventHandler(OnTextCompositionUpdate), handledEventsToo: true);
            // IME 確定文字列の回収
            AddHandler(TextCompositionManager.TextInputEvent, new TextCompositionEventHandler(OnTextInputBubbled), handledEventsToo: true);

            ImeProxy = CreateImeProxy();
            AddVisualChild(ImeProxy);
            AddLogicalChild(ImeProxy);

            SetupCommandBindings();
            ContextMenu = CreateDefaultContextMenu();
            ImeProxy.ContextMenu = ContextMenu;
            UpdateInputMethodState();
        }

        TextBox CreateImeProxy()
        {
            var proxy = new TextBox
            {
                // 不可視だがフォーカス・IME は機能する (Visibility を変えると機能しなくなる)
                Opacity = 0,
                IsHitTestVisible = false,
                IsTabStop = false,
                Focusable = true,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                MinWidth = 4,
                IsUndoEnabled = false,
            };
            proxy.TextChanged += (_, _) =>
            {
                ImeDebugLog.Log($"proxy.TextChanged text='{proxy.Text}' composing={CurrentComposition != null}");
                // IME 確定文字列は TextInput イベントを経由せずプロキシのバッファへ
                // 直接入ることがあるため、変換中でなければ回収する。
                // TSF の編集ロック中にバッファを変更しないよう Dispatcher 経由で行う。
                if (CurrentComposition == null && proxy.Text.Length > 0)
                {
                    Dispatcher.BeginInvoke(new Action(SyncProxyCommittedText), DispatcherPriority.Input);
                }
            };
            return proxy;
        }

        #region ビジュアルツリー (IME プロキシ)

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return ImeProxy;
        }

        #endregion ビジュアルツリー (IME プロキシ)

        #region 公開 API (TextBox 互換)

        public int CaretIndex
        {
            get { return Caret; }
            set { MoveCaretTo(value, extendSelection: false); }
        }

        public bool HasSelection => Anchor != Caret;

        public int SelectionStart => Math.Min(Anchor, Caret);

        public int SelectionLength => Math.Abs(Anchor - Caret);

        public string SelectedText
        {
            get
            {
                return Document.GetText(SelectionStart, SelectionLength);
            }
            set
            {
                if (IsReadOnly)
                {
                    return;
                }
                var start = SelectionStart;
                var inserted = TextDocument.Normalize(value);
                PerformEdit(start, SelectionLength, inserted, start + inserted.Length, EditKind.Other);
            }
        }

        public bool CanUndo => UndoStack.CanUndo;

        public bool CanRedo => UndoStack.CanRedo;

        public void Select(int start, int length)
        {
            start = Math.Clamp(start, 0, Document.Length);
            var end = Math.Clamp(start + Math.Max(0, length), 0, Document.Length);
            Anchor = start;
            Caret = end;
            PreferredFlowPosition = null;
            RestartCaretBlink();
            InvalidateVisual();
            RaiseSelectionChangedIfNeeded();
        }

        public void SelectAll()
        {
            Select(0, Document.Length);
        }

        public void Undo()
        {
            if (IsReadOnly)
            {
                return;
            }
            var operation = UndoStack.Undo();
            if (operation == null)
            {
                return;
            }
            Document.Replace(operation.Offset, operation.Inserted.Length, operation.Removed);
            Anchor = operation.AnchorBefore;
            Caret = operation.CaretBefore;
            AfterEditApplied(new TextEditedEventArgs(operation.Offset, operation.Inserted, operation.Removed));
        }

        public void Redo()
        {
            if (IsReadOnly)
            {
                return;
            }
            var operation = UndoStack.Redo();
            if (operation == null)
            {
                return;
            }
            Document.Replace(operation.Offset, operation.Removed.Length, operation.Inserted);
            Anchor = operation.AnchorAfter;
            Caret = operation.CaretAfter;
            AfterEditApplied(new TextEditedEventArgs(operation.Offset, operation.Removed, operation.Inserted));
        }

        #endregion 公開 API (TextBox 互換)

        #region レイアウト

        /// <summary>
        /// スタンドアロンモードのレイアウトを生成する。派生クラスで override して差し替えられる。
        /// </summary>
        protected virtual ITextLayout CreateLayout(string displayText, Typeface typeface, double fontSize, Brush foreground, double pixelsPerDip)
        {
            return new FormatterTextLayout(displayText, typeface, fontSize, foreground, pixelsPerDip);
        }

        /// <summary>
        /// レンダラ側でレンダリングすべきテキスト。
        /// CompositionDisplayMode が Renderer の場合は IME 未確定文字列をキャレット位置に
        /// 挿入したもの、Editor の場合は確定テキストのみ (未確定文字列はエディタが描画する)。
        /// オーバーレイモードでは、レンダラ側はこの文字列をレンダリングして
        /// CharacterGeometries を生成する。
        /// </summary>
        public string DisplayText
        {
            get
            {
                return CompositionText.Length == 0 || UseEditorCompositionDisplay
                    ? Document.Text
                    : Document.Text.Insert(Math.Min(Caret, Document.Length), CompositionText);
            }
        }

        /// <summary>
        /// 未確定文字列をエディタ側で直接描画するモードか
        /// </summary>
        bool UseEditorCompositionDisplay => CompositionDisplayMode == ImeCompositionDisplayMode.Editor && CharacterGeometries != null;

        /// <summary>
        /// 表示 (レイアウト) 座標系でのキャレットオフセット
        /// </summary>
        int DisplayCaretOffset => UseEditorCompositionDisplay ? Caret : Caret + CompositionText.Length;

        void NotifyDisplayTextChanged()
        {
            var display = DisplayText;
            if (display == LastNotifiedDisplayText)
            {
                return;
            }
            LastNotifiedDisplayText = display;
            DisplayTextChanged?.Invoke(this, EventArgs.Empty);
        }

        void OnCompositionDisplayModeChanged()
        {
            // 変換中にモードを切り替えられた場合も表示を整合させる
            NotifyDisplayTextChanged();
            InvalidateTextLayout();
        }

        /// <summary>
        /// 未確定文字列の変化後の再描画。エディタ側描画モードではレイアウト対象の
        /// テキストが変わらないため、レイアウトを作り直さず再描画のみ行う。
        /// </summary>
        void InvalidateAfterCompositionChange()
        {
            if (UseEditorCompositionDisplay)
            {
                InvalidateVisual();
            }
            else
            {
                InvalidateTextLayout();
            }
        }

        /// <summary>
        /// デモ・テスト用: IME 未確定文字列の表示状態を擬似的に設定する
        /// </summary>
        internal void SimulateComposition(string compositionText)
        {
            CompositionText = compositionText ?? "";
            NotifyDisplayTextChanged();
            InvalidateAfterCompositionChange();
        }

        ITextLayout EnsureLayout()
        {
            var display = DisplayText;
            var geometries = CharacterGeometries;

            // オーバーレイモード: 外部から渡されたジオメトリでレイアウトを構築する
            if (geometries != null)
            {
                var emptyCaret = EmptyTextCaretGeometry;
                var isVertical = IsVerticalText;
                if (Layout is not GeometryTextLayout cached ||
                    LayoutText != display ||
                    !ReferenceEquals(LayoutGeometries, geometries) ||
                    !Equals(LayoutEmptyCaret, emptyCaret) ||
                    LayoutIsVertical != isVertical)
                {
                    Layout?.Dispose();
                    cached = new GeometryTextLayout(display, geometries, emptyCaret, isVertical);
                    Layout = cached;
                    LayoutText = display;
                    LayoutGeometries = geometries;
                    LayoutEmptyCaret = emptyCaret;
                    LayoutIsVertical = isVertical;
                }
                // 表示配置 (Origin/Scale) はレイアウトの再構築なしに反映する
                cached.ViewOrigin = Origin;
                cached.ViewScale = Scale;
                return cached;
            }

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var foreground = Foreground ?? Brushes.Black;
            var pixelsPerDip = GetPixelsPerDip();

            if (Layout != null && Layout is not GeometryTextLayout &&
                LayoutText == display &&
                LayoutFontSize == FontSize &&
                LayoutPixelsPerDip == pixelsPerDip &&
                ReferenceEquals(LayoutForeground, foreground) &&
                typeface.Equals(LayoutTypeface))
            {
                return Layout;
            }

            Layout?.Dispose();
            Layout = CreateLayout(display, typeface, FontSize, foreground, pixelsPerDip);
            LayoutText = display;
            LayoutGeometries = null;
            LayoutTypeface = typeface;
            LayoutFontSize = FontSize;
            LayoutPixelsPerDip = pixelsPerDip;
            LayoutForeground = foreground;
            return Layout;
        }

        void InvalidateTextLayout()
        {
            // 次回 EnsureLayout で再構築させる
            LayoutText = null;
            InvalidateMeasure();
            InvalidateVisual();
        }

        double GetPixelsPerDip()
        {
            try
            {
                return VisualTreeHelper.GetDpi(this).PixelsPerDip;
            }
            catch
            {
                // ビジュアルツリー未接続時
                return 1.0;
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            InvalidateTextLayout();
        }

        /// <summary>
        /// レイアウト空間 → コントロール座標の変換。
        /// スタンドアロンモードでは Padding による平行移動、オーバーレイ (ジオメトリ) モードでは
        /// Origin を中心とした Scale 倍の拡大縮小+Offset の平行移動
        /// (control = Origin + (screen − Origin) × Scale + Offset。
        /// WPF の ScaleTransform(sx, sy, centerX, centerY) → TranslateTransform と同じ)。
        /// </summary>
        Matrix GetViewMatrix(ITextLayout layout)
        {
            if (layout.DrawsText)
            {
                return new Matrix(1, 0, 0, 1, Padding.Left, Padding.Top);
            }
            var origin = Origin;
            var scale = Scale;
            var offset = Offset;
            return new Matrix(
                scale.X, 0, 0, scale.Y,
                (origin.X * (1 - scale.X)) + offset.X,
                (origin.Y * (1 - scale.Y)) + offset.Y);
        }

        /// <summary>
        /// コントロール座標の点をレイアウト空間へ変換する (ヒットテスト用)
        /// </summary>
        Point ControlToLayoutPoint(ITextLayout layout, Point point)
        {
            var view = GetViewMatrix(layout);
            if (!view.HasInverse)
            {
                // Scale が 0 など縮退している場合
                return point;
            }
            view.Invert();
            return view.Transform(point);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            ImeProxy.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var layout = EnsureLayout();
            var extent = layout.Extent;
            if (layout.DrawsText)
            {
                return new Size(
                    extent.Width + Padding.Left + Padding.Right + CaretMargin,
                    extent.Height + Padding.Top + Padding.Bottom);
            }

            // オーバーレイモード: 表示配置 (Origin/Scale) 適用後の範囲の右下までを希望サイズとする
            // (実際にはプレビュー全体に配置され、ホスト側のサイズ指定が優先される想定)
            var view = GetViewMatrix(layout);
            var topLeft = view.Transform(new Point(0, 0));
            var bottomRight = view.Transform(new Point(extent.Width, extent.Height));
            return new Size(
                Math.Max(0, Math.Max(topLeft.X, bottomRight.X)),
                Math.Max(0, Math.Max(topLeft.Y, bottomRight.Y)));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            // IME プロキシを変換開始位置 (現在のキャレット) へ配置する。
            // 候補ウィンドウはプロキシ TextBox の TSF テキストストアが報告する
            // キャレット位置に表示されるため、これだけで候補がテキストレイヤーに追従する。
            var layout = EnsureLayout();
            var (top, bottom) = layout.GetCaretLine(Math.Clamp(Caret, 0, Document.Length));
            var rect = new Rect(new Point(0, 0), ImeProxy.DesiredSize);
            if (top != bottom)
            {
                var view = GetViewMatrix(layout);
                var topInControl = view.Transform(top);
                var bottomInControl = view.Transform(bottom);
                var caretHeight = (bottomInControl - topInControl).Length;
                var fontSize = Math.Clamp(caretHeight * 0.8, 8, 96);
                if (Math.Abs(ImeProxy.FontSize - fontSize) > 0.5)
                {
                    // 候補ウィンドウの縦オフセットを実サイズに近づける
                    ImeProxy.FontSize = fontSize;
                }
                rect = new Rect(topInControl, ImeProxy.DesiredSize);
            }
            ImeProxy.Arrange(rect);
            return arrangeBounds;
        }

        #endregion レイアウト

        #region 描画

        protected override void OnRender(DrawingContext drawingContext)
        {
            var layout = EnsureLayout();
            var view = GetViewMatrix(layout);

            // 背景 (Transparent でもヒットテストのために必ず塗る)
            drawingContext.DrawRectangle(Background ?? Brushes.Transparent, null, new Rect(RenderSize));

            // 選択ハイライト (フォーカス時のみ・TextBox と同じ挙動)
            if ((IsKeyboardFocusWithin || IsInactiveSelectionHighlightEnabled) && HasSelection && CompositionText.Length == 0)
            {
                var quads = layout.GetRangeQuads(SelectionStart, SelectionStart + SelectionLength);
                if (quads.Count > 0)
                {
                    drawingContext.DrawGeometry(SelectionBrush, null, BuildQuadGeometry(quads, view, closed: true));
                }
            }

            // テキスト本体 (スタンドアロンモードのみ。オーバーレイモードでは外部でレンダリング済み)
            if (layout.DrawsText)
            {
                layout.DrawText(drawingContext, new Point(Padding.Left, Padding.Top));
            }
            else if (ShowBoundingBox)
            {
                var boxQuads = layout.GetBoundingQuads();
                if (boxQuads.Count > 0)
                {
                    var boxPen = new Pen(BoundingBoxBrush, 1);
                    drawingContext.DrawGeometry(null, boxPen, BuildQuadGeometry(boxQuads, view, closed: true));
                }
            }

            // IME 未確定文字列
            if (CompositionText.Length > 0)
            {
                if (UseEditorCompositionDisplay)
                {
                    // エディタ側描画モード: キャレット位置にプレーンテキストで直接描画する
                    DrawCompositionOverlay(drawingContext, layout, view);
                }
                else
                {
                    // レンダラ経由モード: レイアウトに含まれる未確定範囲へ下線を引く
                    var pen = new Pen(CaretBrush ?? Foreground ?? Brushes.Black, 1);
                    foreach (var quad in layout.GetRangeQuads(Caret, Caret + CompositionText.Length))
                    {
                        drawingContext.DrawLine(pen, view.Transform(quad.BottomLeft), view.Transform(quad.BottomRight));
                    }
                }
            }

            // キャレット (トランスフォーム適用後の線分として描く)
            if (IsKeyboardFocusWithin && IsCaretBlinkVisible)
            {
                var (top, bottom) = GetCaretScreenLine(layout);
                if (top != bottom)
                {
                    var topInControl = view.Transform(top);
                    var bottomInControl = view.Transform(bottom);

                    // 垂直なキャレットはピクセル境界にスナップして滲みを防ぐ
                    if (Math.Abs(topInControl.X - bottomInControl.X) < 0.01)
                    {
                        var pixelsPerDip = GetPixelsPerDip();
                        var x = (Math.Floor(topInControl.X * pixelsPerDip) + 0.5) / pixelsPerDip;
                        topInControl = new Point(x, topInControl.Y);
                        bottomInControl = new Point(x, bottomInControl.Y);
                    }

                    var caretPen = new Pen(CaretBrush ?? Foreground ?? Brushes.Black, Math.Max(1, SystemParameters.CaretWidth));
                    drawingContext.DrawLine(caretPen, topInControl, bottomInControl);
                }
            }
        }

        /// <summary>
        /// 未確定文字列をキャレット位置へエディタ自身で描画する (エディタ側描画モード)。
        /// キャレット位置のローカル座標系にトランスフォームを適用してプレーンテキストを描く。
        /// フォントサイズは行の高さから近似するため、レンダラの描画と完全には一致しない。
        /// </summary>
        void DrawCompositionOverlay(DrawingContext drawingContext, ITextLayout layout, Matrix view)
        {
            if (!layout.TryGetCaretLocalFrame(Caret, out var localPosition, out var localHeight, out var transform))
            {
                return;
            }
            if (localHeight <= 0)
            {
                return;
            }

            drawingContext.PushTransform(new MatrixTransform(view));
            drawingContext.PushTransform(new MatrixTransform(transform));

            var pen = new Pen(Foreground ?? Brushes.Black, Math.Max(1, localHeight / 24));
            if (IsVerticalText)
            {
                // 縦書き: 列の上から下へ 1 文字ずつ並べ、未確定範囲の目印は列の右側に引く
                var length = DrawVerticalCompositionText(drawingContext, localPosition, localHeight);
                var lineX = localPosition.X + localHeight;
                drawingContext.DrawLine(pen,
                    new Point(lineX, localPosition.Y),
                    new Point(lineX, localPosition.Y + length));
            }
            else
            {
                var formattedText = CreateCompositionFormattedText(localHeight);

                // FormattedText の行高とローカル行高の差を吸収するため垂直方向はセンタリング
                var textY = localPosition.Y + ((localHeight - formattedText.Height) / 2);
                drawingContext.DrawText(formattedText, new Point(localPosition.X, textY));

                // 未確定範囲の下線
                var underlineY = localPosition.Y + localHeight;
                drawingContext.DrawLine(pen,
                    new Point(localPosition.X, underlineY),
                    new Point(localPosition.X + formattedText.WidthIncludingTrailingWhitespace, underlineY));
            }

            drawingContext.Pop();
            drawingContext.Pop();
        }

        /// <summary>
        /// 縦書き用に未確定文字列を 1 テキスト要素ずつ列の上から下へ並べて描く (drawingContext が null なら計測のみ)。
        /// 戻り値は並べた文字列全体の流れ方向の長さ。
        /// </summary>
        /// <param name="columnStart">列の左上 (レイアウトローカル空間)</param>
        /// <param name="columnWidth">列の幅 (文字サイズとして使う)</param>
        double DrawVerticalCompositionText(DrawingContext? drawingContext, Point columnStart, double columnWidth)
        {
            var y = columnStart.Y;
            var enumerator = StringInfo.GetTextElementEnumerator(CompositionText);
            while (enumerator.MoveNext())
            {
                var formattedText = CreateFormattedText(enumerator.GetTextElement(), columnWidth);
                // 列の中央に寄せる
                var x = columnStart.X + ((columnWidth - formattedText.WidthIncludingTrailingWhitespace) / 2);
                drawingContext?.DrawText(formattedText, new Point(x, y));
                y += formattedText.Height;
            }
            return y - columnStart.Y;
        }

        FormattedText CreateCompositionFormattedText(double localHeight)
        {
            return CreateFormattedText(CompositionText, localHeight);
        }

        FormattedText CreateFormattedText(string text, double fontSize)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                Math.Max(1.0, fontSize),
                Foreground ?? Brushes.Black,
                GetPixelsPerDip());
        }

        /// <summary>
        /// 実効的なキャレット線分 (レイアウト空間)。エディタ側描画モードで変換中は
        /// 未確定文字列の末尾にキャレットを置く。縮退線分 (Top == Bottom) は「位置不明」を表す。
        /// </summary>
        (Point Top, Point Bottom) GetCaretScreenLine(ITextLayout layout)
        {
            if (CompositionText.Length > 0 && UseEditorCompositionDisplay &&
                layout.TryGetCaretLocalFrame(Caret, out var localPosition, out var localHeight, out var transform) &&
                localHeight > 0)
            {
                if (IsVerticalText)
                {
                    // 縦書き: 未確定文字列の下端に、列の幅の水平なキャレットを置く
                    var y = localPosition.Y + DrawVerticalCompositionText(null, localPosition, localHeight);
                    return (transform.Transform(new Point(localPosition.X, y)),
                            transform.Transform(new Point(localPosition.X + localHeight, y)));
                }
                var x = localPosition.X + CreateCompositionFormattedText(localHeight).WidthIncludingTrailingWhitespace;
                return (transform.Transform(new Point(x, localPosition.Y)),
                        transform.Transform(new Point(x, localPosition.Y + localHeight)));
            }
            return layout.GetCaretLine(DisplayCaretOffset);
        }

        static StreamGeometry BuildQuadGeometry(IReadOnlyList<TextQuad> quads, Matrix view, bool closed)
        {
            var geometry = new StreamGeometry { FillRule = FillRule.Nonzero };
            using (var context = geometry.Open())
            {
                foreach (var quad in quads)
                {
                    context.BeginFigure(view.Transform(quad.TopLeft), true, closed);
                    context.LineTo(view.Transform(quad.TopRight), true, false);
                    context.LineTo(view.Transform(quad.BottomRight), true, false);
                    context.LineTo(view.Transform(quad.BottomLeft), true, false);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        #endregion 描画

        #region フォーカス・キャレット点滅

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            // コントロール自身がフォーカスを得たら、実際の入力先である IME プロキシへ移す
            if (ReferenceEquals(e.NewFocus, this))
            {
                var result = Keyboard.Focus(ImeProxy);
                ImeDebugLog.Log($"FocusRedirect result={result?.GetType().Name} proxyFocused={ImeProxy.IsKeyboardFocused} proxyVisible={ImeProxy.IsVisible}");
            }
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            ImeDebugLog.Log($"FocusWithinChanged {e.NewValue} focused={Keyboard.FocusedElement?.GetType().Name}");
            if ((bool)e.NewValue)
            {
                RestartCaretBlink();
                CaretTimer.Start();
            }
            else
            {
                CompleteComposition();
                SyncProxyCommittedText();
                CaretTimer.Stop();
            }
            InvalidateVisual();
        }

        void RestartCaretBlink()
        {
            IsCaretBlinkVisible = true;
            if (IsKeyboardFocusWithin)
            {
                CaretTimer.Stop();
                CaretTimer.Start();
            }
        }

        static double GetCaretBlinkInterval()
        {
            var milliseconds = NativeMethods.GetCaretBlinkTime();
            return milliseconds is 0 or uint.MaxValue ? DefaultCaretBlinkIntervalMilliseconds : milliseconds;
        }

        #endregion フォーカス・キャレット点滅

        #region キーボード

        /// <summary>
        /// フォーカスは IME プロキシ (TextBox) にあるため、プロキシ側で処理される前の
        /// トンネリング (Preview) 段階でエディタのキー操作を処理する。
        /// IME 変換中のキーは Key.ImeProcessed になるため、ここのどのケースにも一致せず
        /// IME 側の処理 (候補選択など) が優先される。
        /// </summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled)
            {
                return;
            }

            ImeDebugLog.Log($"PreviewKeyDown key={e.Key} imeProcessed={e.Key == Key.ImeProcessed} focused={Keyboard.FocusedElement?.GetType().Name} imeState={InputMethod.Current?.ImeState}");

            var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            var text = Document.Text;

            // クリップボード・Undo 系ショートカット
            // (プロキシ TextBox 自身のコマンドに処理させないよう、ここで処理する)
            if (ctrl && !shift)
            {
                switch (e.Key)
                {
                    case Key.C:
                    case Key.Insert:
                        CopySelection();
                        e.Handled = true;
                        return;
                    case Key.X:
                        CutSelection();
                        e.Handled = true;
                        return;
                    case Key.V:
                        PasteFromClipboard();
                        e.Handled = true;
                        return;
                    case Key.A:
                        SelectAll();
                        e.Handled = true;
                        return;
                    case Key.Z:
                        Undo();
                        e.Handled = true;
                        return;
                    case Key.Y:
                        Redo();
                        e.Handled = true;
                        return;
                }
            }
            if (shift && !ctrl)
            {
                if (e.Key == Key.Delete)
                {
                    CutSelection();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Insert)
                {
                    PasteFromClipboard();
                    e.Handled = true;
                    return;
                }
            }

            switch (e.Key)
            {
                // 横書き: 左右で文字送り、上下で行移動。
                // 縦書き: 上下で文字送り、左右で列移動 (列は右から左へ積まれるため、左が次の列)。
                case Key.Left:
                    if (IsVerticalText)
                    {
                        MoveCaretToAdjacentLine(+1, shift);
                    }
                    else
                    {
                        MoveCaretBackward(text, ctrl, shift);
                    }
                    e.Handled = true;
                    break;

                case Key.Right:
                    if (IsVerticalText)
                    {
                        MoveCaretToAdjacentLine(-1, shift);
                    }
                    else
                    {
                        MoveCaretForward(text, ctrl, shift);
                    }
                    e.Handled = true;
                    break;

                case Key.Up:
                    if (IsVerticalText)
                    {
                        MoveCaretBackward(text, ctrl, shift);
                    }
                    else
                    {
                        MoveCaretToAdjacentLine(-1, shift);
                    }
                    e.Handled = true;
                    break;

                case Key.Down:
                    if (IsVerticalText)
                    {
                        MoveCaretForward(text, ctrl, shift);
                    }
                    else
                    {
                        MoveCaretToAdjacentLine(+1, shift);
                    }
                    e.Handled = true;
                    break;

                case Key.Home:
                    {
                        var layout = EnsureLayout();
                        var target = ctrl ? 0 : layout.GetLineRange(layout.GetLineIndexFromOffset(Caret)).Start;
                        MoveCaretTo(target, shift);
                        e.Handled = true;
                        break;
                    }

                case Key.End:
                    {
                        var layout = EnsureLayout();
                        var (start, length) = layout.GetLineRange(layout.GetLineIndexFromOffset(Caret));
                        MoveCaretTo(ctrl ? Document.Length : start + length, shift);
                        e.Handled = true;
                        break;
                    }

                case Key.Back:
                    HandleBackspace(ctrl);
                    e.Handled = true;
                    break;

                case Key.Delete:
                    HandleDelete(ctrl);
                    e.Handled = true;
                    break;

                case Key.Space:
                    // TextBox は Space を TextInput ではなく KeyDown で処理するため、
                    // プロキシに渡すと挿入順序が狂う。ここで直接挿入する。
                    if (!IsReadOnly)
                    {
                        InsertText(" ", typing: true);
                    }
                    e.Handled = true;
                    break;

                case Key.Return:
                    if (AcceptsReturn && !IsReadOnly)
                    {
                        InsertText("\n", typing: false);
                    }
                    e.Handled = true;
                    break;

                case Key.Tab:
                    if (AcceptsTab && !ctrl)
                    {
                        if (!IsReadOnly)
                        {
                            InsertText("\t", typing: true);
                        }
                        e.Handled = true;
                    }
                    break;
            }
        }

        void HandleBackspace(bool word)
        {
            if (IsReadOnly)
            {
                return;
            }
            if (HasSelection)
            {
                PerformEdit(SelectionStart, SelectionLength, "", SelectionStart, EditKind.Other);
                return;
            }
            if (Caret == 0)
            {
                return;
            }
            var text = Document.Text;
            var start = word ? TextNavigation.PrevWord(text, Caret) : TextNavigation.PrevElement(text, Caret);
            if (start >= Caret)
            {
                return;
            }
            PerformEdit(start, Caret - start, "", start, word ? EditKind.Other : EditKind.Backspace);
        }

        void HandleDelete(bool word)
        {
            if (IsReadOnly)
            {
                return;
            }
            if (HasSelection)
            {
                PerformEdit(SelectionStart, SelectionLength, "", SelectionStart, EditKind.Other);
                return;
            }
            if (Caret >= Document.Length)
            {
                return;
            }
            var text = Document.Text;
            var end = word ? TextNavigation.NextWord(text, Caret) : TextNavigation.NextElement(text, Caret);
            if (end <= Caret)
            {
                return;
            }
            PerformEdit(Caret, end - Caret, "", Caret, word ? EditKind.Other : EditKind.DeleteForward);
        }

        /// <summary>
        /// 文字列順で 1 つ手前 (Ctrl で 1 単語手前) へキャレットを移動する。
        /// 選択がある状態で Shift なしの場合は選択の先頭へ寄せる (TextBox と同じ)。
        /// </summary>
        void MoveCaretBackward(string text, bool word, bool extendSelection)
        {
            if (!extendSelection && !word && HasSelection)
            {
                MoveCaretTo(SelectionStart, extendSelection: false);
            }
            else
            {
                MoveCaretTo(word ? TextNavigation.PrevWord(text, Caret) : TextNavigation.PrevElement(text, Caret), extendSelection);
            }
        }

        /// <summary>
        /// 文字列順で 1 つ奥 (Ctrl で 1 単語奥) へキャレットを移動する。
        /// 選択がある状態で Shift なしの場合は選択の末尾へ寄せる (TextBox と同じ)。
        /// </summary>
        void MoveCaretForward(string text, bool word, bool extendSelection)
        {
            if (!extendSelection && !word && HasSelection)
            {
                MoveCaretTo(SelectionStart + SelectionLength, extendSelection: false);
            }
            else
            {
                MoveCaretTo(word ? TextNavigation.NextWord(text, Caret) : TextNavigation.NextElement(text, Caret), extendSelection);
            }
        }

        /// <summary>
        /// 隣の行 (縦書きでは隣の列) へ、流れ方向の位置を維持してキャレットを移動する
        /// </summary>
        /// <param name="direction">+1 で文字列順で次の行、-1 で前の行</param>
        void MoveCaretToAdjacentLine(int direction, bool extendSelection)
        {
            var layout = EnsureLayout();

            // 選択がある状態で Shift なしの行移動は、選択の端へ寄せてから移動する (TextBox と同じ)
            var baseOffset = extendSelection
                ? Caret
                : HasSelection ? (direction < 0 ? SelectionStart : SelectionStart + SelectionLength) : Caret;

            var line = layout.GetLineIndexFromOffset(baseOffset);
            PreferredFlowPosition ??= layout.GetCaretFlowPosition(baseOffset);

            var targetLine = line + direction;
            var target = targetLine < 0
                ? 0
                : targetLine >= layout.LineCount ? Document.Length : layout.GetOffsetAtFlowPosition(targetLine, PreferredFlowPosition.Value);

            MoveCaretTo(target, extendSelection, keepPreferredFlowPosition: true);
        }

        void MoveCaretTo(int offset, bool extendSelection, bool keepPreferredFlowPosition = false)
        {
            offset = Math.Clamp(offset, 0, Document.Length);
            Caret = offset;
            if (!extendSelection)
            {
                Anchor = offset;
            }
            if (!keepPreferredFlowPosition)
            {
                PreferredFlowPosition = null;
            }
            RestartCaretBlink();
            InvalidateVisual();
            // IME プロキシをキャレット位置へ追従させる
            InvalidateArrange();
            RaiseSelectionChangedIfNeeded();
            BringCaretIntoView();
        }

        #endregion キーボード

        #region 文字入力・IME

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);
            if (e.Handled)
            {
                return;
            }

            ImeDebugLog.Log($"PreviewTextInput text='{e.Text}' system='{e.SystemText}' compType={e.TextComposition?.GetType().Name}");

            // IME 確定はここでは処理しない: プロキシ TextBox に挿入させ、
            // バブリングの TextInput → SyncProxyCommittedText でバッファごと回収する。
            // (ここで挿入するとプロキシのバッファに変換文字列が残留し、二重挿入の原因になる)
            if (CurrentComposition != null || e.TextComposition is FrameworkTextComposition)
            {
                return;
            }

            // 通常のキー入力。トンネリング段階で処理することでプロキシ側には入力させない。
            EndCompositionDisplay();

            var text = e.Text;
            if (string.IsNullOrEmpty(text))
            {
                text = e.SystemText;
            }
            text = FilterInputText(text);
            if (text.Length > 0 && !IsReadOnly)
            {
                InsertText(text, typing: true);
            }
            e.Handled = true;
        }

        /// <summary>
        /// TextInput のバブリング段階 (handledEventsToo で登録)。
        /// IME 確定文字列は TextInput を経由せずプロキシ TextBox のバッファへ直接
        /// 入ることがあるため、変換終了のタイミングでバッファを回収する。
        /// </summary>
        void OnTextInputBubbled(object sender, TextCompositionEventArgs e)
        {
            ImeDebugLog.Log($"TextInput(bubbled) text='{e.Text}' handled={e.Handled} compType={e.TextComposition?.GetType().Name} proxyText='{ImeProxy.Text}'");
            EndCompositionDisplay();
            // TSF の編集ロックが解けてからバッファを回収する
            Dispatcher.BeginInvoke(new Action(SyncProxyCommittedText), DispatcherPriority.Input);
        }

        /// <summary>
        /// プロキシ TextBox のバッファに残った確定文字列をドキュメントへ移す
        /// </summary>
        void SyncProxyCommittedText()
        {
            ImeDebugLog.Log($"SyncProxy proxyText='{ImeProxy.Text}' composing={CurrentComposition != null}");
            if (CurrentComposition != null)
            {
                // まだ変換中
                return;
            }
            var text = ImeProxy.Text;
            if (text.Length == 0)
            {
                return;
            }

            // クリアできない状態 (変換処理中など) では回収しない。
            // 先にクリアすることで、失敗時も二重挿入にはならない。
            try
            {
                ImeProxy.Clear();
            }
            catch
            {
                return;
            }

            text = FilterInputText(TextDocument.Normalize(text));
            if (text.Length > 0 && !IsReadOnly)
            {
                InsertText(text, typing: false);
            }
        }

        void OnTextCompositionStart(object sender, TextCompositionEventArgs e)
        {
            var composition = e.TextComposition;
            if (composition == null)
            {
                return;
            }
            var compositionString = GetCompositionString(composition);
            ImeDebugLog.Log($"TextInputStart compType={composition.GetType().Name} comp='{compositionString}' text='{composition.Text}' handled={e.Handled}");
            // IME 由来の TextComposition のみ処理する。WPF の DefaultTextStore が生成する
            // TextComposition は FrameworkTextComposition とは限らないため、
            // 未確定文字列の有無でも判定する。通常のキー入力は OnPreviewTextInput に任せる。
            if (composition is not FrameworkTextComposition && compositionString.Length == 0)
            {
                return;
            }

            CurrentComposition = composition;
            if (HasSelection && !IsReadOnly)
            {
                // IME 入力開始時に選択範囲を削除する (TextBox と同じ)
                PerformEdit(SelectionStart, SelectionLength, "", SelectionStart, EditKind.Other);
            }
            CompositionText = compositionString;
            // 注意: e.Handled は設定しない。TextInputStart を Handled にすると
            // TextStore が変換を拒否し、IME 入力が一切できなくなる。
            RestartCaretBlink();
            NotifyDisplayTextChanged();
            InvalidateAfterCompositionChange();
        }

        void OnTextCompositionUpdate(object sender, TextCompositionEventArgs e)
        {
            var composition = e.TextComposition;
            if (composition == null)
            {
                return;
            }
            var compositionString = GetCompositionString(composition);
            ImeDebugLog.Log($"TextInputUpdate compType={composition.GetType().Name} comp='{compositionString}' text='{composition.Text}' handled={e.Handled}");
            // 変換中 (CurrentComposition あり) は空文字列への更新 (全削除) も受け付ける
            if (composition is not FrameworkTextComposition && compositionString.Length == 0 && CurrentComposition == null)
            {
                return;
            }

            CurrentComposition = composition;
            CompositionText = compositionString;
            // 注意: e.Handled は設定しない (TextInputStart と同様、変換の拒否につながる)
            RestartCaretBlink();
            NotifyDisplayTextChanged();
            InvalidateAfterCompositionChange();
            BringCaretIntoView();
        }

        static string GetCompositionString(TextComposition composition)
        {
            if (!string.IsNullOrEmpty(composition.CompositionText))
            {
                return composition.CompositionText;
            }
            return composition.SystemCompositionText ?? "";
        }

        /// <summary>
        /// 制御文字 (WM_CHAR 由来の '\r'、Ctrl 系コードなど) を除去する
        /// </summary>
        static string FilterInputText(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            if (text.All(IsAcceptableInputChar))
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                if (IsAcceptableInputChar(character))
                {
                    builder.Append(character);
                }
            }
            return builder.ToString();
        }

        static bool IsAcceptableInputChar(char character)
        {
            return character >= ' ' || character == '\n';
        }

        /// <summary>
        /// IME の未確定入力を確定させる (マウスクリック・フォーカス喪失時)
        /// </summary>
        void CompleteComposition()
        {
            var composition = CurrentComposition;
            if (composition != null)
            {
                try
                {
                    composition.Complete();
                }
                catch
                {
                    // 既に完了している場合
                }
            }
            EndCompositionDisplay();
            // Complete() の結果がバッファに残っていれば回収する
            SyncProxyCommittedText();
        }

        void EndCompositionDisplay()
        {
            CurrentComposition = null;
            if (CompositionText.Length == 0)
            {
                return;
            }
            CompositionText = "";
            NotifyDisplayTextChanged();
            InvalidateAfterCompositionChange();
        }

        void UpdateInputMethodState()
        {
            InputMethod.SetIsInputMethodEnabled(this, !IsReadOnly);
            if (ImeProxy != null)
            {
                InputMethod.SetIsInputMethodEnabled(ImeProxy, !IsReadOnly);
                ImeProxy.IsReadOnly = IsReadOnly;
            }
        }

        #endregion 文字入力・IME

        #region 編集の中核

        void InsertText(string text, bool typing)
        {
            if (IsReadOnly || text.Length == 0)
            {
                return;
            }
            if (!AcceptsReturn)
            {
                var newline = text.IndexOf('\n');
                if (newline >= 0)
                {
                    text = text[..newline];
                }
                if (text.Length == 0)
                {
                    return;
                }
            }
            var start = SelectionStart;
            var length = SelectionLength;
            var kind = typing && length == 0 && !text.Contains('\n') ? EditKind.Typing : EditKind.Other;
            PerformEdit(start, length, text, start + text.Length, kind);
        }

        void PerformEdit(int offset, int length, string inserted, int caretAfter, EditKind kind)
        {
            if (IsReadOnly)
            {
                return;
            }
            inserted = TextDocument.Normalize(inserted);

            var operation = new EditOperation
            {
                Offset = offset,
                Removed = Document.GetText(offset, length),
                Inserted = inserted,
                AnchorBefore = Anchor,
                CaretBefore = Caret,
                Kind = kind,
            };

            Document.Replace(offset, length, inserted);
            Caret = Math.Clamp(caretAfter, 0, Document.Length);
            Anchor = Caret;
            operation.AnchorAfter = Caret;
            operation.CaretAfter = Caret;
            UndoStack.Push(operation);

            AfterEditApplied(new TextEditedEventArgs(offset, operation.Removed, inserted));
        }

        void AfterEditApplied(TextEditedEventArgs edit)
        {
            SyncTextToDependencyProperty();
            PreferredFlowPosition = null;
            // 編集詳細を先に通知し (ホストがスタイル範囲などを追従させる)、
            // その後レンダラに再レンダリングさせてから (同期的に CharacterGeometries が
            // 更新される想定) レイアウトを無効化する
            TextEdited?.Invoke(this, edit);
            NotifyDisplayTextChanged();
            InvalidateTextLayout();
            RestartCaretBlink();
            TextChanged?.Invoke(this, EventArgs.Empty);
            RaiseSelectionChangedIfNeeded();
            BringCaretIntoView();
        }

        void SyncTextToDependencyProperty()
        {
            IsSyncingTextDp = true;
            try
            {
                SetCurrentValue(TextProperty, Document.Text);
            }
            finally
            {
                IsSyncingTextDp = false;
            }
        }

        static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (PreviewTextBox)d;
            if (editor.IsSyncingTextDp)
            {
                return;
            }

            // 外部 (バインディング/コード) からの設定: TextBox と同様に Undo 履歴を破棄する
            var newText = TextDocument.Normalize((string?)e.NewValue);
            var oldText = editor.Document.Text;
            if (newText == oldText)
            {
                return;
            }

            editor.EndCompositionDisplay();
            editor.Document.SetText(newText);
            editor.UndoStack.Clear();
            editor.Anchor = 0;
            editor.Caret = 0;
            editor.PreferredFlowPosition = null;

            // 正規化した値を DP へ反映 (再入は IsSyncingTextDp で防止)
            if (newText != (string?)e.NewValue)
            {
                editor.SyncTextToDependencyProperty();
            }

            editor.TextEdited?.Invoke(editor, new TextEditedEventArgs(0, oldText, newText));
            editor.NotifyDisplayTextChanged();
            editor.InvalidateTextLayout();
            editor.TextChanged?.Invoke(editor, EventArgs.Empty);
            editor.RaiseSelectionChangedIfNeeded();
        }

        void RaiseSelectionChangedIfNeeded()
        {
            var current = (Anchor, Caret);
            if (current == LastRaisedSelection)
            {
                return;
            }
            LastRaisedSelection = current;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        void BringCaretIntoView()
        {
            if (!IsLoaded)
            {
                return;
            }
            var layout = EnsureLayout();
            var (top, bottom) = GetCaretScreenLine(layout);
            if (top == bottom)
            {
                return;
            }
            var view = GetViewMatrix(layout);
            var rect = new Rect(view.Transform(top), view.Transform(bottom));
            // 左右に少し余白を持たせてスクロールする
            rect.Inflate(FontSize, 0);
            BringIntoView(rect);
        }

        #endregion 編集の中核

        #region マウス

        /// <summary>
        /// 点がテキストのバウンディングボックス内かを返す。
        /// スタンドアロンモードでは常に true (コントロール全域が対象)。
        /// オーバーレイモードでジオメトリが無い場合 (空テキストなど) は常に false
        /// (クリックではフォーカスせず、ホスト側が Focus() で明示的にフォーカスを与える)。
        /// </summary>
        bool IsPointInTextBounds(ITextLayout layout, Point point)
        {
            if (layout.DrawsText)
            {
                return true;
            }

            var layoutPoint = ControlToLayoutPoint(layout, point);
            foreach (var quad in layout.GetBoundingQuads())
            {
                if (quad.Contains(layoutPoint))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// オーバーレイモードではテキストのバウンディングボックス内だけをヒットさせ、
        /// 外側のマウスイベント (各ボタンのクリック・ホイール・移動など) がこのコントロールに捕まらず、
        /// 下に重なっているコントロールへ届くようにする。
        /// </summary>
        protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
        {
            var layout = EnsureLayout();
            if (!layout.DrawsText && !IsPointInTextBounds(layout, hitTestParameters.HitPoint))
            {
                return null;
            }
            return base.HitTestCore(hitTestParameters);
        }

        /// <summary>
        /// 外側クリックの監視先 (ルート要素)。バウンディングボックス外のクリックはヒットテストで
        /// このコントロールに届かないため、ルート要素の PreviewMouseDown で検出する。
        /// </summary>
        UIElement? OutsideClickSource { get; set; }

        void AttachOutsideClickHandler()
        {
            var root = PresentationSource.FromVisual(this)?.RootVisual as UIElement;
            if (root == null || ReferenceEquals(root, OutsideClickSource))
            {
                return;
            }
            DetachOutsideClickHandler();
            root.AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(OnRootPreviewMouseDown), handledEventsToo: true);
            OutsideClickSource = root;
        }

        void DetachOutsideClickHandler()
        {
            OutsideClickSource?.RemoveHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(OnRootPreviewMouseDown));
            OutsideClickSource = null;
        }

        /// <summary>
        /// 編集中にバウンディングボックス外を左クリックしたら、変換を確定し、選択を解除してフォーカスを外す。
        /// e.Handled は設定せず、クリック先のコントロールがそのまま処理できるようにする。
        /// </summary>
        void OnRootPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || IsMouseCaptured || !IsVisible)
            {
                return;
            }
            // KeepsSelectionOnClick のパネルを操作するとフォーカスはそちらへ移るが、選択範囲は残る。
            // その後に別の場所をクリックしたときにも選択を解除できるよう、フォーカスが無くても選択が残っていれば処理する。
            if (!IsKeyboardFocusWithin && !HasSelection)
            {
                return;
            }
            // 選択範囲を対象に操作するパネル (KeepsSelectionOnClick) 内のクリックでは、選択もフォーカスもそのままにする。
            // クリック先がフォーカスを取る場合は通常通りフォーカスが移るが、選択範囲は保持される。
            if (e.OriginalSource is DependencyObject source && GetKeepsSelectionOnClick(source))
            {
                return;
            }
            var layout = EnsureLayout();
            if (layout.DrawsText || IsPointInTextBounds(layout, e.GetPosition(this)))
            {
                // 内側のクリックは OnMouseLeftButtonDown で処理する
                return;
            }

            CompleteComposition();
            // 選択解除
            MoveCaretTo(Caret, extendSelection: false);
            ReleaseFocus();
        }

        /// <summary>
        /// 編集を終えるためにフォーカスを手放す。
        /// Keyboard.ClearFocus() だけでは論理フォーカスが IME プロキシに残るため、LostFocus が発生せず、
        /// フォーカススコープ (ウィンドウなど) がキーボードフォーカスを受け取り直したときに IME プロキシへ戻されてしまう。
        /// そのため、フォーカススコープの論理フォーカスも外す。
        /// </summary>
        void ReleaseFocus()
        {
            ClearLogicalFocus();
            if (IsKeyboardFocusWithin)
            {
                Keyboard.ClearFocus();
            }
        }

        /// <summary>
        /// フォーカススコープの論理フォーカスがこのコントロール (IME プロキシ) にあれば外す。LostFocus が発生する。
        /// </summary>
        void ClearLogicalFocus()
        {
            var scope = FocusManager.GetFocusScope(this);
            if (scope == null)
            {
                return;
            }
            var focused = FocusManager.GetFocusedElement(scope);
            if (ReferenceEquals(focused, this) || ReferenceEquals(focused, ImeProxy))
            {
                FocusManager.SetFocusedElement(scope, null);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

#if NIVE3_PREVIEW_TEXT_DIAGNOSTICS
            var position = e.GetPosition(this);
            var boundingQuads = EnsureLayout().GetBoundingQuads();
            var originScreen = PointToScreen(new Point(0, 0));
            ImeDebugLog.Log($"MouseDown pos=({position.X:F0},{position.Y:F0}) inBounds={IsPointInTextBounds(EnsureLayout(), position)} " +
                            $"quads={boundingQuads.Count} bounds={(boundingQuads.Count > 0 ? boundingQuads[0].GetBounds().ToString() : "-")} " +
                            $"originScreen=({originScreen.X:F0},{originScreen.Y:F0})");
#endif

            // バウンディングボックス外は HitTestCore でヒットしないため、ここには届かない
            // (外側のクリックによるフォーカス解除は OnRootPreviewMouseDown で行う)
            if (!IsPointInTextBounds(EnsureLayout(), e.GetPosition(this)))
            {
                return;
            }

            Keyboard.Focus(ImeProxy);
            CompleteComposition();

            var offset = OffsetFromPoint(e.GetPosition(this));
            var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

            if (e.ClickCount == 2)
            {
                var (start, end) = TextNavigation.WordRange(Document.Text, offset);
                DragMode = DragSelectionMode.Word;
                DragOrigin = (start, end);
                SetSelection(start, end);
            }
            else if (e.ClickCount >= 3)
            {
                var (start, end) = GetLineSelectionRange(offset);
                DragMode = DragSelectionMode.Line;
                DragOrigin = (start, end);
                SetSelection(start, end);
            }
            else
            {
                DragMode = DragSelectionMode.Char;
                MoveCaretTo(offset, shift);
            }

            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // バウンディングボックス外は HitTestCore でヒットしないため、ドラッグ中以外にここへ届くのは内側の移動だけ
            if (DragMode == DragSelectionMode.None || !IsMouseCaptured)
            {
                return;
            }

            var offset = OffsetFromPoint(e.GetPosition(this));
            switch (DragMode)
            {
                case DragSelectionMode.Char:
                    MoveCaretTo(offset, extendSelection: true);
                    break;

                case DragSelectionMode.Word:
                    {
                        var (wordStart, wordEnd) = TextNavigation.WordRange(Document.Text, offset);
                        if (wordStart < DragOrigin.Start)
                        {
                            SetSelection(DragOrigin.End, wordStart);
                        }
                        else
                        {
                            SetSelection(DragOrigin.Start, Math.Max(wordEnd, DragOrigin.End));
                        }
                        break;
                    }

                case DragSelectionMode.Line:
                    {
                        var (lineStart, lineEnd) = GetLineSelectionRange(offset);
                        if (lineStart < DragOrigin.Start)
                        {
                            SetSelection(DragOrigin.End, lineStart);
                        }
                        else
                        {
                            SetSelection(DragOrigin.Start, Math.Max(lineEnd, DragOrigin.End));
                        }
                        break;
                    }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (DragMode != DragSelectionMode.None)
            {
                DragMode = DragSelectionMode.None;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            DragMode = DragSelectionMode.None;
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            base.OnContextMenuOpening(e);
            if (e.Handled)
            {
                return;
            }

            // バウンディングボックス外ではコンテキストメニューを表示しない
            // (CursorLeft < 0 はキーボード (アプリケーションキー) からの表示なので許可)
            if (e.CursorLeft >= 0 && !IsPointInTextBounds(EnsureLayout(), Mouse.GetPosition(this)))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// anchor→caret の向きを保って選択を設定する (ドラッグ選択用)
        /// </summary>
        void SetSelection(int anchor, int caret)
        {
            Anchor = Math.Clamp(anchor, 0, Document.Length);
            Caret = Math.Clamp(caret, 0, Document.Length);
            PreferredFlowPosition = null;
            RestartCaretBlink();
            InvalidateVisual();
            // IME プロキシをキャレット位置へ追従させる
            InvalidateArrange();
            RaiseSelectionChangedIfNeeded();
            BringCaretIntoView();
        }

        /// <summary>
        /// トリプルクリック用: 行全体 (末尾の改行を含む) の範囲
        /// </summary>
        (int Start, int End) GetLineSelectionRange(int offset)
        {
            var line = Document.GetLineIndexFromOffset(offset);
            var start = Document.GetLineStart(line);
            var end = start + Document.GetLineLength(line);
            if (end < Document.Length)
            {
                // 改行を含める
                end++;
            }
            return (start, end);
        }

        int OffsetFromPoint(Point point)
        {
            var layout = EnsureLayout();
            var display = layout.GetOffsetAt(ControlToLayoutPoint(layout, point));
            return DisplayToDocumentOffset(display);
        }

        int DisplayToDocumentOffset(int displayOffset)
        {
            // エディタ側描画モードではレイアウトに未確定文字列が含まれないためそのまま
            if (CompositionText.Length == 0 || UseEditorCompositionDisplay)
            {
                return displayOffset;
            }
            if (displayOffset <= Caret)
            {
                return displayOffset;
            }
            return Math.Max(Caret, displayOffset - CompositionText.Length);
        }

        #endregion マウス

        #region クリップボード・コマンド

        void SetupCommandBindings()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,
                (_, _) => CopySelection(),
                (_, e) => e.CanExecute = HasSelection));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut,
                (_, _) => CutSelection(),
                (_, e) => e.CanExecute = HasSelection && !IsReadOnly));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste,
                (_, _) => PasteFromClipboard(),
                (_, e) => e.CanExecute = !IsReadOnly && ClipboardContainsText()));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll,
                (_, _) => SelectAll(),
                (_, e) => e.CanExecute = true));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo,
                (_, _) => Undo(),
                (_, e) => e.CanExecute = CanUndo && !IsReadOnly));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo,
                (_, _) => Redo(),
                (_, e) => e.CanExecute = CanRedo && !IsReadOnly));
        }

        ContextMenu CreateDefaultContextMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Command = ApplicationCommands.Cut, CommandTarget = this });
            menu.Items.Add(new MenuItem { Command = ApplicationCommands.Copy, CommandTarget = this });
            menu.Items.Add(new MenuItem { Command = ApplicationCommands.Paste, CommandTarget = this });
            menu.Items.Add(new Separator());
            menu.Items.Add(new MenuItem { Command = ApplicationCommands.SelectAll, CommandTarget = this });
            return menu;
        }

        void CopySelection()
        {
            if (!HasSelection)
            {
                return;
            }
            try
            {
                Clipboard.SetText(SelectedText.Replace("\n", "\r\n"));
            }
            catch
            {
                // クリップボードが他プロセスにロックされている場合など
            }
        }

        void CutSelection()
        {
            if (!HasSelection || IsReadOnly)
            {
                return;
            }
            CopySelection();
            PerformEdit(SelectionStart, SelectionLength, "", SelectionStart, EditKind.Other);
        }

        void PasteFromClipboard()
        {
            if (IsReadOnly)
            {
                return;
            }
            var text = "";
            try
            {
                text = Clipboard.GetText();
            }
            catch
            {
                return;
            }
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            InsertText(TextDocument.Normalize(text), typing: false);
        }

        static bool ClipboardContainsText()
        {
            try
            {
                return Clipboard.ContainsText();
            }
            catch
            {
                return false;
            }
        }

        #endregion クリップボード・コマンド
    }

    readonly record struct SelectionRange(int Start, int Length);
}