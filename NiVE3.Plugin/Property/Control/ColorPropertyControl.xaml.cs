using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
using Microsoft.Xaml.Behaviors.Core;
using NiVE3.UI.Dialog;

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// ColorPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorPropertyControl : PropertyControlBase
    {
        public string DialogTitle { get; set; } = "";

        public string OKButtonText { get; set; } = "";

        public string CancelButtonText { get; set; } = "";

        public ICommand OpenDialogCommand { get; }

        public ColorPropertyControl()
        {
            OpenDialogCommand = new ActionCommand(() =>
            {
                var viewModel = ViewModel;
                if (viewModel == null)
                {
                    return;
                }

                var dialog = new ColorPickerDialog((Vector4)(viewModel.CurrentTimeValue ?? Vector4.Zero))
                {
                    Owner = Application.Current.MainWindow,
                    Title = DialogTitle,
                    OKButtonText = OKButtonText,
                    CancelButtonText = CancelButtonText
                };
                if (dialog.ShowDialog() ?? false)
                {
                    viewModel.BeginEditCommand.Execute(null);
                    viewModel.CurrentTimeValue = dialog.VectorColor;
                    viewModel.EndEditCommand.Execute(null);
                }
            });

            InitializeComponent();
        }
    }
}
