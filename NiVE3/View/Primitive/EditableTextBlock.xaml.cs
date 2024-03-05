using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.ViewModel;

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// EditableTextBlock.xaml の相互作用ロジック
    /// </summary>
    // NOTE: 親のスタイルを継承するためにUserControlではなくDecoratorを使用する(FindImplicitStyleResourceでControlの派生が無視されるため)
    // NOTE: 本来のDecoratorの用途では無いが、自前で子を持てるFrameworkElementのクラスを用意して使用するとなぜかドラッグの受け付けが出来なくなるため代用する
    public partial class EditableTextBlock : Decorator
    {
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(Brushes.Transparent)
        );

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(EditableTextBlock),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
            nameof(IsEditing),
            typeof(bool),
            typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(false)
        );

        public static readonly DependencyProperty EndEditCommandProperty = DependencyProperty.Register(
            nameof(EndEditCommand),
            typeof(ICommand),
            typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(EditableTextBlock));

        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(EditableTextBlock));

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(EditableTextBlock));

        public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(EditableTextBlock));

        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(EditableTextBlock));

        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(EditableTextBlock));

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public FontStretch FontStretch
        {
            get { return (FontStretch)GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public Brush? Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public ICommand? EndEditCommand
        {
            get { return (ICommand)GetValue(EndEditCommandProperty); }
            set { SetValue(EndEditCommandProperty, value); }
        }

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public Brush? Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public EditableTextBlock()
        {
            InitializeComponent();
        }

        static bool IsClickSameControl(FrameworkElement fe, MouseButtonEventArgs e)
        {
            return new Rect(0.0, 0.0, fe.ActualWidth, fe.ActualHeight).Contains(e.GetPosition(fe));
        }

        private void EditTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsEditing)
            {
                EditTextBox.Focus();
                EditTextBox.SelectAll();
                EditTextBox.CaptureMouse();
            }
        }

        private void EditTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Tab || (e.Key == Key.Enter && e.ImeProcessedKey == Key.None)) && (EndEditCommand?.CanExecute(true) ?? false))
            {
                EndEditCommand.Execute(true);
                EditTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && (EndEditCommand?.CanExecute(false) ?? false))
            {
                EndEditCommand.Execute(false);
                EditTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void EditTextBox_PreviewMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            if (!IsClickSameControl(EditTextBox, e) && (EndEditCommand?.CanExecute(true) ?? false))
            {
                EndEditCommand.Execute(true);
                EditTextBox.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
    }
}
