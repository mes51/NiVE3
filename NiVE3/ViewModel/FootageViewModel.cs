using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using Prism.Commands;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Left)]
    [CommandHandling(nameof(FootageViewModel.OpenFileCommand), nameof(ShortcutKeySetting.OpenFileGesture), IsGlobal = true)]
    class FootageViewModel : PaneViewModelBase
    {
        public ICommand OpenFileCommand { get; }

        public FootageViewModel()
        {
            Title = "フッテージ";

            OpenFileCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.OpenFileCommand is not implemented"));
        }
    }
}
