using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using NiVE3.ViewModel.Dialog;

namespace NiVE3.View.Dialog
{
    /// <summary>
    /// OptionView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionView : UserControl
    {
        public OptionView()
        {
            InitializeComponent();
        }

        private void DefaultTagChangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not OptionViewModel viewModel || sender is not Button button || button.Tag is not string propertyName)
            {
                return;
            }

            var targetColor = viewModel.GetDefaultTag(propertyName);
            if (!targetColor.HasValue)
            {
                return;
            }

            var dialog = new ColorPickerDialog(targetColor.Value)
            {
                Owner = Application.Current.MainWindow,
                Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ColorPickerDialog_Title),
                OKButtonText = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OK),
                CancelButtonText = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_Cancel)
            };
            if (dialog.ShowDialog() ?? false)
            {
                viewModel.SetDefaultTag(propertyName, dialog.Color);
            }
        }
    }

    file static class OptionViewModelExtensions
    {
        static Dictionary<string, PropertyInfo> Properties { get; } = [];

        public static Color? GetDefaultTag(this OptionViewModel viewModel, string name)
        {
            if (!Properties.ContainsKey(name))
            {
                var p = typeof(OptionViewModel).GetProperty(name);
                if (p != null && p.PropertyType == typeof(Color))
                {
                    Properties.Add(name, p);
                }
                else
                {
                    return null;
                }
            }
            var property = Properties[name];
            if (property.GetValue(viewModel) is not Color result)
            {
                return null;
            }
            return result;
        }

        public static void SetDefaultTag(this OptionViewModel viewModel, string name, Color value)
        {
            if (!Properties.ContainsKey(name))
            {
                var p = typeof(OptionViewModel).GetProperty(name);
                if (p != null && p.PropertyType == typeof(Color))
                {
                    Properties.Add(name, p);
                }
                else
                {
                    return;
                }
            }

            Properties[name].SetValue(viewModel, value);
        }
    }
}
