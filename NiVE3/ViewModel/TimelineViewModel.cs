using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.View.Dock;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel
{
    [PaneLocation(PaneLocation.Bottom)]
    class TimelineViewModel : PaneViewModelBase
    {
        CompositionModel CompositionModel { get; }

        public TimelineViewModel(CompositionModel compositionModel)
        {
            CompositionModel = compositionModel;
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.TimelineView_Title);
        }
    }
}
