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
using NiVE3.Extension;
using NiVE3.ValueObject;
using NiVE3.View.Converter;
using NiVE3.View.Primitive;
using NiVE3.View.Resource;
using NiVE3.ViewModel.Dialog;
using NiVE3.Wpf.Input;

namespace NiVE3.View.Dialog
{
    /// <summary>
    /// ShortcutKeySettingView.xaml の相互作用ロジック
    /// </summary>
    public partial class ShortcutKeySettingView : UserControl
    {
        public static readonly IMultiValueConverter ShortcutKeysIndexerConverter = new DelegateMultiConverter<IDictionary<ShortcutKeyName, InputGesture>, ShortcutKeyName, InputGesture>((dictionary, key) => dictionary[key]);

        public static readonly IMultiValueConverter DuplicatedKeysIndexerConverter = new DelegateMultiConverter<IDictionary<ShortcutKeyName, string>, ShortcutKeyName, string>((dictionary, key) => dictionary[key]);

        ShortcutKeySettingViewModel? ViewModel => DataContext as ShortcutKeySettingViewModel;

        public ShortcutKeySettingView()
        {
            InitializeComponent();
        }

        private void ShortcutKeyDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Cancel)
            {
                return;
            }

            var shortcutKeyInput = e.EditingElement.FindVisualChild<ShortcutKeyInput>(true);
            var viewModel = ViewModel;
            if (shortcutKeyInput == null || !shortcutKeyInput.IsCompleted || e.Row.DataContext is not ShortcutKeyName name || viewModel == null)
            {
                return;
            }

            var modifier = shortcutKeyInput.Modifier;
            try
            {
                if (modifier == ModifierKeys.None || modifier == ModifierKeys.Shift)
                {
                    viewModel.ChangeShortcutKeyCommand.Execute(Tuple.Create<ShortcutKeyName, InputGesture>(name, new SingleKeyGesture(shortcutKeyInput.Key, modifier == ModifierKeys.Shift)));
                }
                else
                {
                    viewModel.ChangeShortcutKeyCommand.Execute(Tuple.Create<ShortcutKeyName, InputGesture>(name, new KeyGesture(shortcutKeyInput.Key, modifier)));
                }
            }
            catch (InvalidEnumArgumentException)
            {
                var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_InvalidShortcutKeyCombination_Title);
                var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_InvalidShortcutKeyCombination_Text);
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (NotSupportedException)
            {
                var title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_InvalidShortcutKeyCombination_Title);
                var text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_InvalidShortcutKeyCombination_Text);
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShortcutKeyInput_ShortcutKeyInputCompleted(object sender, RoutedEventArgs e)
        {
            ShortcutKeysDataGrid.CommitEdit();
        }

        private void EditShortcutKeyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShortcutKeysDataGrid.BeginEdit();
        }

        private void ShortcutKeysDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            var shortcutKeyInput = e.EditingElement.FindVisualChild<ShortcutKeyInput>(true);
            if (shortcutKeyInput != null)
            {
                shortcutKeyInput.Focus();
            }
        }
    }
}
