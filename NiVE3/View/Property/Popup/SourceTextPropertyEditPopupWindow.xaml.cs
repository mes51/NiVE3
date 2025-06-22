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
using System.Windows.Shapes;
using NiVE3.Text;
using Prism.Commands;

namespace NiVE3.View.Property.Popup
{
    /// <summary>
    /// SourceTextPropertyEditPopupWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SourceTextPropertyEditPopupWindow : Window
    {
        public static readonly DependencyProperty SourceTextProperty = DependencyProperty.Register(
            nameof(SourceText),
            typeof(string),
            typeof(SourceTextPropertyEditPopupWindow),
            new FrameworkPropertyMetadata("")
        );

        public string SourceText
        {
            get { return (string)GetValue(SourceTextProperty); }
            set { SetValue(SourceTextProperty, value); }
        }

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public SourceTextPropertyEditPopupWindow()
        {
            OKCommand = new DelegateCommand(() =>
            {
                Deactivated -= RootWindow_Deactivated;
                DialogResult = true;
                Close();
            });

            CancelCommand = new DelegateCommand(() =>
            {
                Deactivated -= RootWindow_Deactivated;
                DialogResult = false;
                Close();
            });

            InitializeComponent();
        }

        private void RootWindow_Deactivated(object? sender, EventArgs e)
        {
            CancelCommand.Execute(null);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.ImeProcessedKey == Key.None)
            {
                OKCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                CancelCommand.Execute(null);
            }
        }
    }
}
