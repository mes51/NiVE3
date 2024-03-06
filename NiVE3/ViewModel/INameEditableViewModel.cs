using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NiVE3.ViewModel
{
    interface INameEditableViewModel
    {
        ICommand BeginEditNameCommand { get; }
    }

    interface INameEditableParentViewModel
    {
        INameEditableViewModel? TargetChild { get; }
    }
}
