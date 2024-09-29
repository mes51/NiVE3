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
using NiVE3.Data;
using NiVE3.UI.Dialog;
using NiVE3.View.Resource;
using NiVE3.ViewModel;
using NiVE3.ViewModel.Input;

namespace NiVE3.View.Pane
{
    /// <summary>
    /// TextPropertyView.xaml の相互作用ロジック
    /// </summary>
    public partial class TextPropertyView : UserControl
    {
        public TextPropertyView()
        {
            InitializeComponent();
        }

        TextPropertyViewModel? ViewModel => DataContext as TextPropertyViewModel;

        private void FillColorChangeButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            var dialog = new ColorPickerDialog(viewModel.FillColor.ToByteColor())
            {
                Owner = Application.Current.MainWindow,
                Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ColorPickerDialog_Title),
                OKButtonText = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OK),
                CancelButtonText = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_Cancel)
            };
            if (dialog.ShowDialog() ?? false)
            {
                viewModel.FillColor = FloatColor.FromColor(dialog.Color);
            }
        }

        private void TextLineColorChangeButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            var dialog = new ColorPickerDialog(viewModel.TextLineColor.ToByteColor())
            {
                Owner = Application.Current.MainWindow,
                Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ColorPickerDialog_Title),
                OKButtonText = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OK),
                CancelButtonText = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_Cancel)
            };
            if (dialog.ShowDialog() ?? false)
            {
                viewModel.TextLineColor = FloatColor.FromColor(dialog.Color);
            }
        }

        private void FontListComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.CreateFontSampleGeometryCommand.Execute(null);
        }

        private void FontSubFamilyListComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.CreateSubFamilySampleGeometryCommaned.Execute(null);
        }
    }
}
