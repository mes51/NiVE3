using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using Prism.Commands;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Left)]
    [CommandHandling(nameof(OpenFileCommand), nameof(ShortcutKeySetting.OpenFileGesture), IsGlobal = true)]
    [CommandHandling(nameof(AddSolidCommand), nameof(ShortcutKeySetting.AddSolidGesture), IsGlobal = true)]
    [CommandHandling(nameof(AddFootageFolderCommand), nameof(ShortcutKeySetting.AddFootageFolderGesture), IsGlobal = true)]
    [CommandHandling(nameof(DeleteFootageCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    class FootageListViewModel : PaneViewModelBase
    {
        private ObservableCollectionView<IFootageModel, IFootageViewModel> footages;
        public ObservableCollectionView<IFootageModel, IFootageViewModel> Footages
        {
            get { return footages; }
            set { SetProperty(ref footages, value); }
        }

        private IFootageViewModel? editingFootage;
        public IFootageViewModel? EditingFootage
        {
            get { return editingFootage; }
            private set { SetProperty(ref editingFootage, value); }
        }

        public ICommand OpenFileCommand { get; }

        public ICommand DeleteFootageCommand { get; }

        public ICommand AddSolidCommand { get; }

        public ICommand AddFootageFolderCommand { get; }

        public ICommand BeginEditPropertyCommand { get; }

        public ICommand EndEditPropertyCommand { get; }

        FootageListModel FootageListModel { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public FootageListViewModel(FootageListModel footageListModel)
#pragma warning restore CS8618
        {
            FootageListModel = footageListModel;
            Footages = footageListModel.Footages.CreateViewCollection<IFootageModel, IFootageViewModel>(m => m is FootageModel ? new FootageViewModel((FootageModel)m) : new FootageFolderViewModel((FootageFolderModel)m));

            Title = "フッテージ";

            OpenFileCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.OpenFileCommand is not implemented"));

            DeleteFootageCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.DeleteFootageCommand is not implemented"));

            AddSolidCommand = new DelegateCommand(() => FootageListModel.AddSolid());

            AddFootageFolderCommand = new DelegateCommand(() => FootageListModel.AddFolder());

            BeginEditPropertyCommand = new DelegateCommand<Tuple<IFootageViewModel, string>>(t =>
            {
                if (EditingFootage != null)
                {
                    EditingFootage.EndEditProperty();
                }

                EditingFootage = t.Item1;
                EditingFootage.BeginEditProperty(t.Item2);
            });

            EndEditPropertyCommand = new DelegateCommand(() =>
            {
                if (EditingFootage != null)
                {
                    EditingFootage.EndEditProperty();
                    EditingFootage = null;
                }
            });
        }
    }
}
