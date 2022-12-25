using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Dock;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Document)]
    class PreviewViewModel : PaneViewModelBase
    {
        public PreviewViewModel()
        {
            Title = "プレビュー";
        }
    }
}
