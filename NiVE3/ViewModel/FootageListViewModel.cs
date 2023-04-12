using System;
using System.Collections.Generic;
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
    [CommandHandling(nameof(DeleteFootageCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    class FootageListViewModel : PaneViewModelBase
    {
        private ObservableCollectionView<FootageModel, FootageViewModel> footages;
        public ObservableCollectionView<FootageModel, FootageViewModel> Footages
        {
            get { return footages; }
            set { SetProperty(ref footages, value); }
        }

        public ICommand OpenFileCommand { get; }

        public ICommand DeleteFootageCommand { get; }

        FootageListModel FootageListModel { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public FootageListViewModel(FootageListModel footageListModel)
#pragma warning restore CS8618
        {
            FootageListModel = footageListModel;
            Footages = footageListModel.Footages.CreateViewCollection(m => new FootageViewModel(m));

            Title = "フッテージ";

            OpenFileCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.OpenFileCommand is not implemented"));

            DeleteFootageCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.DeleteFootageCommand is not implemented"));
        }
    }
}
