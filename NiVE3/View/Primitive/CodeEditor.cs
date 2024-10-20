using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using Acornima;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Jint;
using NiVE3.Expression;
using NiVE3.View.Primitive.Editor;

namespace NiVE3.View.Primitive
{
    class CodeEditor : TextEditor
    {
        static readonly TimeSpan FoldingUpdateInterval = new TimeSpan(0, 0, 0, 0, 500);

        public static readonly DependencyProperty CodeErrorMessageProperty = DependencyProperty.Register(
            nameof(CodeErrorMessage),
            typeof(string),
            typeof(CodeEditor),
            new FrameworkPropertyMetadata("", CodeErrorChanged)
        );

        public static readonly DependencyProperty CodeErrorLocationProperty = DependencyProperty.Register(
            nameof(CodeErrorLocation),
            typeof(SourceLocation),
            typeof(CodeEditor),
            new FrameworkPropertyMetadata(new SourceLocation())
        );

        public SourceLocation CodeErrorLocation
        {
            get { return (SourceLocation)GetValue(CodeErrorLocationProperty); }
            set { SetValue(CodeErrorLocationProperty, value); }
        }

        public string CodeErrorMessage
        {
            get { return (string)GetValue(CodeErrorMessageProperty); }
            set { SetValue(CodeErrorMessageProperty, value); }
        }

        FoldingManager? CurrentFoldingManager { get; set; }

        DispatcherTimer UpdateTimer { get; }

        CurrentLineBackgroundRenderer CurrentLineBackgroundRenderer { get; set; }

        ErrorDisplayService? ErrorDisplayService { get; set; }

        ToolTip ErrorToolTip { get; }

        static CodeEditor()
        {
            VisibilityProperty.OverrideMetadata(typeof(CodeEditor), new FrameworkPropertyMetadata(VisibilityProperty.DefaultMetadata.DefaultValue, FrameworkPropertyMetadataOptions.Inherits, VisibilityChanged));
        }

        public CodeEditor()
        {
            UpdateTimer = new DispatcherTimer { Interval = FoldingUpdateInterval };
            CurrentLineBackgroundRenderer = new CurrentLineBackgroundRenderer(this);
            ErrorToolTip = new ToolTip
            {
                PlacementTarget = this
            };

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            SyntaxLoader.Load();

            UpdateTimer.Tick += UpdateTimer_Tick;
            TextChanged += CodeEditor_TextChanged;
            Loaded += CodeEditor_Loaded;
            DocumentChanged += CodeEditor_DocumentChanged;

            TextArea.TextView.MouseHover += TextView_MouseHover;
            TextArea.TextView.MouseHoverStopped += TextView_MouseHoverStopped;
            TextArea.TextView.VisualLinesChanged += TextView_VisualLinesChanged;
        }

        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var backgroundRenderers = TextArea.TextView.BackgroundRenderers;
            if (backgroundRenderers.OfType<CurrentLineBackgroundRenderer>().FirstOrDefault() == null)
            {
                backgroundRenderers.Add(CurrentLineBackgroundRenderer);
            }

            if (ErrorDisplayService != null)
            {
                backgroundRenderers.Remove(ErrorDisplayService);
            }
            if (Document != null)
            {
                ErrorDisplayService = new ErrorDisplayService(Document);
                backgroundRenderers.Add(ErrorDisplayService);
            }
            else
            {
                ErrorDisplayService = null;
            }
        }

        private void CodeEditor_DocumentChanged(object? sender, EventArgs e)
        {
            InstallFoldingManager();

            var backgroundRenderers = TextArea.TextView.BackgroundRenderers;

            if (ErrorDisplayService != null)
            {
                backgroundRenderers.Remove(ErrorDisplayService);
            }
            if (Document != null)
            {
                ErrorDisplayService = new ErrorDisplayService(Document);
                backgroundRenderers.Add(ErrorDisplayService);
            }
            else
            {
                ErrorDisplayService = null;
            }
        }

        void InstallFoldingManager()
        {
            if (CurrentFoldingManager != null)
            {
                FoldingManager.Uninstall(CurrentFoldingManager);
            }

            if (Document == null)
            {
                CurrentFoldingManager = null;
            }
            else
            {
                CurrentFoldingManager = FoldingManager.Install(TextArea);
                CurrentFoldingManager.UpdateFoldings(FoldingStrategy.CreateNewFolding(Document), -1);
            }
        }

        void UpdateErrorMaker()
        {
            if (ErrorDisplayService == null)
            {
                return;
            }

            var (offset, length) = CalcCodePosition(Document.Text, CodeErrorLocation);
            ErrorDisplayService.SetError(CodeErrorMessage, offset, length);
            TextArea.TextView.Redraw();
        }

        void SetCurrentCompileError(ScriptPreparationException ex)
        {
            if (ErrorDisplayService == null)
            {
                return;
            }

            ErrorDisplayService.Clear();
            if (ex.InnerException is ParseErrorException pex)
            {
                var (offset, length) = CalcCodePosition(Document.Text, SourceLocation.From(pex.Error.Position, pex.Error.Position));
                ErrorDisplayService.SetError(pex.Message, offset, length);
            }
            else
            {
                ErrorDisplayService.SetError(ex.Message, 0, 1);
            }

            TextArea.TextView.Redraw();
        }

        static (int offset, int length) CalcCodePosition(string code, SourceLocation location)
        {
            var lines = code.Split("\n");
            var offset = lines.Take(location.Start.Line - 1).Sum(l => l.Length + 1) + location.Start.Column;
            var length = lines.Take(location.End.Line - 1).Sum(l => l.Length + 1) + location.End.Column - offset;

            if (length < 1)
            {
                length = 1;
                if (offset >= code.Length)
                {
                    offset--;
                }
            }

            return (offset, length);
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTimer.Stop();

            CurrentFoldingManager?.UpdateFoldings(FoldingStrategy.CreateNewFolding(Document), -1);

            if (ErrorDisplayService != null && !string.IsNullOrEmpty(Document.Text))
            {
                try
                {
                    using var _ = ExpressionEngine.Compile(Document.Text);
                }
                catch (ScriptPreparationException ex)
                {
                    SetCurrentCompileError(ex);
                }
            }
        }

        private void CodeEditor_TextChanged(object? sender, EventArgs e)
        {
            UpdateTimer.Stop();

            if (ErrorDisplayService != null && ErrorDisplayService.HasErrorMaker)
            {
                ErrorDisplayService.Clear();
                TextArea.TextView.Redraw();
            }

            UpdateTimer.Start();
        }

        private void TextView_VisualLinesChanged(object? sender, EventArgs e)
        {
            ErrorToolTip.IsOpen = false;
        }

        private void TextView_MouseHoverStopped(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ErrorToolTip.IsOpen = false;
        }

        private void TextView_MouseHover(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ErrorDisplayService == null)
            {
                return;
            }

            var textView = TextArea.TextView;
            var mousePos = e.GetPosition(textView);
            var pos = textView.GetPositionFloor(new Point(mousePos.X + textView.ScrollOffset.X, mousePos.Y + textView.ScrollOffset.Y));
            if (!pos.HasValue)
            {
                return;
            }

            var offset = Document.GetOffset(pos.Value.Location);
            var marker = ErrorDisplayService.GetMarker(offset);
            if (marker != null)
            {
                ErrorToolTip.Content = marker.ErrorMessage;
                ErrorToolTip.IsOpen = true;
            }
        }

        private static void CodeErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeEditor editor)
            {
                editor.UpdateErrorMaker();
            }
        }

        private static void VisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeEditor editor)
            {
                editor.UpdateErrorMaker();
            }
        }
    }

    file static class SyntaxLoader
    {
        static bool Loaded { get; set; }

        static readonly object SyncObject = new object();

        public static void Load()
        {
            if (Loaded)
            {
                return;
            }

            lock (SyncObject)
            {
                if (Loaded)
                {
                    return;
                }

                var expressionSyntaxDefinitionStream = Application.GetResourceStream(new Uri("/Resources/ExpressionSyntax.xshd", UriKind.Relative));
                using var reader = new XmlTextReader(expressionSyntaxDefinitionStream.Stream);

                var manager = HighlightingManager.Instance;
                manager.RegisterHighlighting("ExpressionJavaScript", null, HighlightingLoader.Load(reader, manager));

                Loaded = true;
            }
        }
    }
}
