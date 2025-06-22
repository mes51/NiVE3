using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Text;
using NiVE3.View.Property.Popup;
using Prism.Commands;

namespace NiVE3.View.Property
{
    /// <summary>
    /// SourceTextPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SourceTextPropertyControl : PropertyControlBase
    {
        public ICommand OpenDialogCommand { get; }

        public SourceTextPropertyControl()
        {
            OpenDialogCommand = new DelegateCommand(() =>
            {
                var viewModel = ViewModel;
                if (viewModel == null || viewModel.CurrentTimeRawValue is not StyledText d)
                {
                    return;
                }

                var windowLocation = EditButton.PointToScreen(new Point(0.0, EditButton.Height));
                var dpi = VisualTreeHelper.GetDpi(EditButton);

                var dialog = new SourceTextPropertyEditPopupWindow
                {
                    Owner = Application.Current.MainWindow,
                    Left = windowLocation.X / dpi.DpiScaleX,
                    Top = windowLocation.Y / dpi.DpiScaleY,
                    SourceText = d.Text
                };
                if (dialog.ShowDialog() ?? false)
                {
                    viewModel.BeginEditCommand.Execute(null);
                    viewModel.CurrentTimeRawValue = d.ChangeText(dialog.SourceText);
                    viewModel.EndEditCommand.Execute(null);
                }
            });

            InitializeComponent();
        }
    }
}
