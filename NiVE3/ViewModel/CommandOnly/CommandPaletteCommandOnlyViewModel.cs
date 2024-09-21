using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.View.Command;
using Prism.Commands;
using Prism.Events;

namespace NiVE3.ViewModel.CommandOnly
{
    // NOTE: コマンドパレットは別Regionの都合上、MainWindowViewModelから取れないため、CommandOnlyViewModel経由で開くショートカットを操作する
    [CommandHandling(nameof(OpenCommandPaletteCommand), nameof(ShortcutKeySetting.OpenCommandPaletteGesture), IsGlobal = true)]
    class CommandPaletteCommandOnlyViewModel : CommandOnlyViewModelBase
    {
        public ICommand OpenCommandPaletteCommand { get; }

        public CommandPaletteCommandOnlyViewModel(IEventAggregator eventAggregator)
        {
            OpenCommandPaletteCommand = new DelegateCommand(() => eventAggregator.GetEvent<OpenCommandPaletteEvent>().Publish());
        }
    }

    class OpenCommandPaletteEvent : PubSubEvent { }
}
