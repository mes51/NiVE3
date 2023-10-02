using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;

namespace NiVE3.ViewModel
{
    interface IViewModelShortcutCommand
    {
        DelegateCommand<SelectItemType?> DeleteCommand { get; }
    }
}
