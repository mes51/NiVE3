using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using Prism.Commands;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Left)]
    [CommandHandling(nameof(FootageListViewModel.OpenFileCommand), nameof(ShortcutKeySetting.OpenFileGesture), IsGlobal = true)]
    [CommandHandling(nameof(FootageListViewModel.DeleteFootageCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    class FootageListViewModel : PaneViewModelBase
    {
        public ICommand OpenFileCommand { get; }

        public ICommand DeleteFootageCommand { get; }

        FootageListModel FootageListModel { get; }

        public FootageListViewModel(FootageListModel footageListModel)
        {
            FootageListModel = footageListModel;

            Title = "フッテージ";

            OpenFileCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.OpenFileCommand is not implemented"));

            DeleteFootageCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.DeleteFootageCommand is not implemented"));
        }
    }
}
