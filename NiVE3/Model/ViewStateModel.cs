using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class ViewStateModel : BindableBase
    {
        private double timelineTagColumnWidth = 16.0;
        public double TimelineTagColumnWidth
        {
            get { return timelineTagColumnWidth; }
            set { SetProperty(ref timelineTagColumnWidth, value); }
        }

        private double timelineLayerNumberColumnWidth = 18.0;
        public double TimelineLayerNumberColumnWidth
        {
            get { return timelineLayerNumberColumnWidth; }
            set { SetProperty(ref timelineLayerNumberColumnWidth, value); }
        }

        private double timelineLayerNameColumnWidth = 150.0;
        public double TimelineLayerNameColumnWidth
        {
            get { return timelineLayerNameColumnWidth; }
            set { SetProperty(ref timelineLayerNameColumnWidth, value); }
        }

        private double timelineLayerCommentColumnWidth = 50.0;
        public double TimelineLayerCommentColumnWidth
        {
            get { return timelineLayerCommentColumnWidth; }
            set { SetProperty(ref timelineLayerCommentColumnWidth, value); }
        }

        private double timelineLayerSwitchColumnWidth = 114.0;
        public double TimelineLayerSwitchColumnWidth
        {
            get { return timelineLayerSwitchColumnWidth; }
            set { SetProperty(ref timelineLayerSwitchColumnWidth, value); }
        }

        private double timelineModeColumnWidth = 120.0;
        public double TimelineModeColumnWidth
        {
            get { return timelineModeColumnWidth; }
            set { SetProperty(ref timelineModeColumnWidth, value); }
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

        private bool timelineParentLayerColumnVisible = true;
        public bool TimelineParentLayerColumnVisible
        {
            get { return timelineParentLayerColumnVisible; }
            set { SetProperty(ref timelineParentLayerColumnVisible, value); }
        }
    }
}
