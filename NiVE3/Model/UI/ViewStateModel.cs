using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;

namespace NiVE3.Model.UI
{
    [UseReactiveProperty]
    partial class ViewStateModel : BindableBase
    {
        [ReactiveProperty]
        public partial double TimelineLayerNumberColumnWidth { get; set; } = 21.0;

        [ReactiveProperty]
        public partial double TimelineLayerNameColumnWidth { get; set; } = 153.0;

        [ReactiveProperty]
        public partial double TimelineLayerCommentColumnWidth { get; set; } = 53.0;

        [ReactiveProperty]
        public partial double TimelineLayerSwitchColumnWidth { get; set; } = 133.0;

        [ReactiveProperty]
        public partial double TimelineModeColumnWidth { get; set; } = 75.0;

        [ReactiveProperty]
        public partial double TimelineTrackMatteColumnWidth { get; set; } = 125.0;

        [ReactiveProperty]
        public partial double TimelineParentLayerColumnWidth { get; set; } = 75.0;

        [ReactiveProperty]
        public partial bool TimelineAVSwitchColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial bool TimelineTagColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial bool TimelineLayerNumberColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial bool TimelineLayerCommentColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial bool TimelineLayerSwitchColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial bool TimelineModeColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial bool TimelineTrackMatteColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial bool TimelineParentLayerColumnVisible { get; set; } = true;

        [ReactiveProperty]
        public partial double PropertyControllerLayerNameColumnWidth { get; set; } = 153.0;

        [ReactiveProperty]
        public partial double PropertyControllerLayerSwitchColumnWidth { get; set; } = 133.0;

        [ReactiveProperty]
        public partial Guid? LastSelectedLayerId { get; set; }

        [ReactiveProperty]
        public partial ObservableCollection<Guid>? SelectedLayerIds { get; set; }

        [ReactiveProperty]
        public partial Guid? CurrentEditingCompositionId { get; set; }

        [ReactiveProperty]
        public partial int LastSelectedObjectHashCode { get; set; }

        [ReactiveProperty]
        public partial bool IsIgnoreUpdatePreview { get; set; }

        [ReactiveProperty]
        public partial bool IsPreviewPlaying { get; set; }
    }
}
