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
        bool IsNameEditing { get; }

        ICommand BeginEditNameCommand { get; }

        ICommand EndEditNameCommand { get; }
    }
}
