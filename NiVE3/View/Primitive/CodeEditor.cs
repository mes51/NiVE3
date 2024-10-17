using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NiVE3.View.Primitive.Editor;

namespace NiVE3.View.Primitive
{
    class CodeEditor : TextEditor
    {
        static readonly TimeSpan FoldingUpdateInterval = new TimeSpan(0, 0, 0, 0, 500);

        FoldingManager? CurrentFoldingManager { get; set; }

        DispatcherTimer UpdateTimer { get; }

        CurrentLineBackgroundRenderer CurrentLineBackgroundRenderer { get; set; }

        static CodeEditor()
        {
            DocumentProperty.OverrideMetadata(typeof(CodeEditor), new FrameworkPropertyMetadata(DocumentProperty.DefaultMetadata.DefaultValue, FrameworkPropertyMetadataOptions.Inherits, DocumentPropertyChanged));

            var expressionSyntaxDefinitionStream = Application.GetResourceStream(new Uri("/Resources/ExpressionSyntax.xshd", UriKind.Relative));
            using var reader = new XmlTextReader(expressionSyntaxDefinitionStream.Stream);

            var manager = HighlightingManager.Instance;
            manager.RegisterHighlighting("ExpressionJavaScript", null, HighlightingLoader.Load(reader, manager));
        }

        public CodeEditor()
        {
            UpdateTimer = new DispatcherTimer { Interval = FoldingUpdateInterval };
            CurrentLineBackgroundRenderer = new CurrentLineBackgroundRenderer(this);

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            UpdateTimer.Tick += UpdateTimer_Tick;
            TextChanged += CodeEditor_TextChanged;
            Loaded += CodeEditor_Loaded;
        }

        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var backgroundRenderers = TextArea.TextView.BackgroundRenderers;
            if (backgroundRenderers.OfType<CurrentLineBackgroundRenderer>().FirstOrDefault() != null)
            {
                backgroundRenderers.Add(CurrentLineBackgroundRenderer);
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

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTimer.Stop();
            if (CurrentFoldingManager == null)
            {
                return;
            }

            CurrentFoldingManager.UpdateFoldings(FoldingStrategy.CreateNewFolding(Document), -1);
        }

        private void CodeEditor_TextChanged(object? sender, EventArgs e)
        {
            UpdateTimer.Stop();
            UpdateTimer.Start();
        }

        private static void DocumentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor editor)
            {
                return;
            }

            editor.InstallFoldingManager();
        }
    }
}
