using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.View.Dock;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Document)]
    class PreviewViewModel : PaneViewModelBase
    {
        PreviewModel PreviewModel { get; }

        public PreviewViewModel(PreviewModel previewModel)
        {
            Title = "プレビュー";

            PreviewModel = previewModel;
        }
    }
}
