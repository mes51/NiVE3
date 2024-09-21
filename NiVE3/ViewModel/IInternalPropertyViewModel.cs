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

        event EventHandler<SelectItemEventArgs> SelectItemChanged;

        event EventHandler<PropertyValueCommitedEventArgs> PropertyValueCommited;

        void DeSelect();
    }

    static class InternalPropertyViewModel
    {
        public static IInternalPropertyViewModel CreateViewModel(IPropertyModel model)
        {
            if (model is PropertyGroupModel pg)
            {
                return new PropertyGroupViewModel(pg);
            }
            else if (model is AppendablePropertyModel ap)
            {
                return new AppendablePropertyViewModel(ap);
            }
            else
            {
                return new PropertyViewModel((PropertyModel)model);
            }
        }
    }
}
