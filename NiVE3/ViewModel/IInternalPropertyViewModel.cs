using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using System.Windows.Input;
using NiVE3.Model.UI;
using Microsoft.Win32;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    interface IInternalPropertyViewModel : IPropertyViewModel, IViewModelShortcutCommand
    {
        string Name { get; }

        bool IsEnable { get; }

        PropertyViewState ViewState { get; }

        ObservableCollection<KeyFrame>? KeyFrames { get; }

        ObservableCollectionView<IPropertyModel, IInternalPropertyViewModel>? Children { get; }

        ICommand SelectItemCommand { get; }

        // NOTE: IViewModelShortcutCommand系のコマンドはRequerySuggestedCommandではないので2重になるが別で定義する
        ICommand AddKeyFrameToSelectedChildrenCommand { get; }

        ICommand ResetSelectedChildrenCommand { get; }

        ICommand CutSelectedChildrenCommand { get; }

        ICommand CopySelectedChildrenCommand { get; }

        ICommand PasteToSelectedChildrenCommand { get; }

        ICommand DeleteSelectedChildrenCommand { get; }

        ICommand DuplicateSelectedChildrenCommand { get; }

        ICommand SavePropertyPresetCommand { get; }

        ICommand LoadPropertyPresetCommand { get; }

        event EventHandler<SelectItemEventArgs> SelectItemChanged;

        event EventHandler<PropertyValueCommitedEventArgs> PropertyValueCommited;

        void DeSelect();
    }

    static class InternalPropertyViewModel
    {
        public static IInternalPropertyViewModel CreateViewModel(IPropertyModel model, ViewStateModel viewState)
        {
            if (model is PropertyGroupModel pg)
            {
                return new PropertyGroupViewModel(pg, viewState);
            }
            else if (model is AppendablePropertyModel ap)
            {
                return new AppendablePropertyViewModel(ap, viewState);
            }
            else
            {
                return new PropertyViewModel((PropertyModel)model, viewState);
            }
        }

        public static string? ShowPropertyPresetSaveDialog()
        {
            var save = new SaveFileDialog
            {
                Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSavePropertyPreset_Filter_PropertyPreset)}(*.nvpp3)|*.nvpp3"
            };

            if (save.ShowDialog() ?? false)
            {
                return save.FileName;
            }
            else
            {
                return null;
            }
        }

        public static string? ShowPropertyPresetOpenDialog()
        {
            var save = new OpenFileDialog
            {
                Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_OpenSavePropertyPreset_Filter_PropertyPreset)}(*.nvpp3)|*.nvpp3"
            };

            if (save.ShowDialog() ?? false)
            {
                return save.FileName;
            }
            else
            {
                return null;
            }
        }
    }
}
