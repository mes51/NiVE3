using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Prism.Dialogs;

namespace NiVE3.View.Dialog.CustomWindow
{
    /// <summary>
    /// CommandPaletteDialogWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CommandPaletteDialogWindow : Window, IDialogWindow
    {
        public CommandPaletteDialogWindow()
        {
            InitializeComponent();
        }

        public IDialogResult Result { get; set; } = new DialogResult(ButtonResult.Cancel);

        bool IsClosed { get; set; }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!IsClosed)
            {
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosed = true;
        }
    }
}
