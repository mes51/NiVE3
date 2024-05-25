using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Mvvm;
using NiVE3.ViewModel;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class ViewStateModel : BindableBase
    {
        private double timelineLayerNumberColumnWidth = 21.0;
        public double TimelineLayerNumberColumnWidth
        {
            get { return timelineLayerNumberColumnWidth; }
            set { SetProperty(ref timelineLayerNumberColumnWidth, value); }
        }

        private double timelineLayerNameColumnWidth = 153.0;
        public double TimelineLayerNameColumnWidth
        {
            get { return timelineLayerNameColumnWidth; }
            set { SetProperty(ref timelineLayerNameColumnWidth, value); }
        }

        private double timelineLayerCommentColumnWidth = 53.0;
        public double TimelineLayerCommentColumnWidth
        {
            get { return timelineLayerCommentColumnWidth; }
            set { SetProperty(ref timelineLayerCommentColumnWidth, value); }
        }

        private double timelineLayerSwitchColumnWidth = 133.0;
        public double TimelineLayerSwitchColumnWidth
        {
            get { return timelineLayerSwitchColumnWidth; }
            set { SetProperty(ref timelineLayerSwitchColumnWidth, value); }
        }

        private double timelineModeColumnWidth = 75.0;
        public double TimelineModeColumnWidth
        {
            get { return timelineModeColumnWidth; }
            set { SetProperty(ref timelineModeColumnWidth, value); }
        }

        private double timelineTrackMatteColumnWidth = 125.0;
        public double TimelineTrackMatteColumnWidth
        {
            get { return timelineTrackMatteColumnWidth; }
            set { SetProperty(ref timelineTrackMatteColumnWidth, value); }
        }

        private double timelineParentLayerColumnWidth = 75.0;
        public double TimelineParentLayerColumnWidth
        {
            get { return timelineParentLayerColumnWidth; }
            set { SetProperty(ref timelineParentLayerColumnWidth, value); }
        }

        private bool timelineAVSwitchColumnVisible = true;
        public bool TimelineAVSwitchColumnVisible
        {
            get { return timelineAVSwitchColumnVisible; }
            set { SetProperty(ref timelineAVSwitchColumnVisible, value); }
        }

        private bool timelineTagColumnVisible = true;
        public bool TimelineTagColumnVisible
        {
            get { return timelineTagColumnVisible; }
            set { SetProperty(ref timelineTagColumnVisible, value); }
        }

        private bool timelineLayerNumberColumnVisible = true;
        public bool TimelineLayerNumberColumnVisible
        {
            get { return timelineLayerNumberColumnVisible; }
            set { SetProperty(ref timelineLayerNumberColumnVisible, value); }
        }

        private bool timelineLayerCommentColumnVisible = true;
        public bool TimelineLayerCommentColumnVisible
        {
            get { return timelineLayerCommentColumnVisible; }
            set { SetProperty(ref timelineLayerCommentColumnVisible, value); }
        }

        private bool timelineLayerSwitchColumnVisible = true;
        public bool TimelineLayerSwitchColumnVisible
        {
            get { return timelineLayerSwitchColumnVisible; }
            set { SetProperty(ref timelineLayerSwitchColumnVisible, value); }
        }

        private bool timelineModeColumnVisible = true;
        public bool TimelineModeColumnVisible
        {
            get { return timelineModeColumnVisible; }
            set { SetProperty(ref timelineModeColumnVisible, value); }
        }

        private bool timelineTrackMatteColumnVisible = true;
        public bool TimelineTrackMatteColumnVisible
        {
            get { return timelineTrackMatteColumnVisible; }
            set { SetProperty(ref timelineTrackMatteColumnVisible, value); }
        }

        private bool timelineParentLayerColumnVisible = true;
        public bool TimelineParentLayerColumnVisible
        {
            get { return timelineParentLayerColumnVisible; }
            set { SetProperty(ref timelineParentLayerColumnVisible, value); }
        }

        private double propertyControllerLayerNameColumnWidth = 153.0;
        public double PropertyControllerLayerNameColumnWidth
        {
            get { return propertyControllerLayerNameColumnWidth; }
            set { SetProperty(ref propertyControllerLayerNameColumnWidth, value); }
        }

        private double propertyControllerLayerSwitchColumnWidth = 133.0;
        public double PropertyControllerLayerSwitchColumnWidth
        {
            get { return propertyControllerLayerSwitchColumnWidth; }
            set { SetProperty(ref propertyControllerLayerSwitchColumnWidth, value); }
        }

        private Guid? lastSelectedLayerId;
        public Guid? LastSelectedLayerId
        {
            get { return lastSelectedLayerId; }
            set { SetProperty(ref lastSelectedLayerId, value); }
        }

        private ObservableCollection<Guid>? selectedLayerIds;
        public ObservableCollection<Guid>? SelectedLayerIds
        {
            get { return selectedLayerIds; }
            set { SetProperty(ref selectedLayerIds, value); }
        }

        private Guid? currentEditingCompositionId;
        public Guid? CurrentEditingCompositionId
        {
            get { return currentEditingCompositionId; }
            set { SetProperty(ref currentEditingCompositionId, value); }
        }

        private bool isIgnoreUpdatePreview;
        public bool IsIgnoreUpdatePreview
        {
            get { return isIgnoreUpdatePreview; }
            set { SetProperty(ref isIgnoreUpdatePreview, value); }
        }

        WeakEventPublisher<SelectLayerEvent> SelectLayerRequestPublisher { get; } = new WeakEventPublisher<SelectLayerEvent>();
        public event EventHandler<SelectLayerEvent> SelectLayerRequest
        {
            add { SelectLayerRequestPublisher.Subscribe(value); }
            remove { SelectLayerRequestPublisher.Unsubscribe(value); }
        }

        public void NotifySelectLayer(Guid compositionId, Guid? layerId)
        {
            SelectLayerRequestPublisher.Publish(this, new SelectLayerEvent(compositionId, layerId));
        }
    }
}
