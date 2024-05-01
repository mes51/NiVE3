using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class RenderQueueModel : BindableBase
    {
        private ObservableCollection<RenderQueueItemModel> items = [];
        public ObservableCollection<RenderQueueItemModel> Items
        {
            get { return items; }
            set { SetProperty(ref items, value); }
        }

        private double progress;
        public double Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        private bool isRendering;
        public bool IsRendering
        {
            get { return isRendering; }
            set { SetProperty(ref isRendering, value); }
        }

        private bool isPaused;
        public bool IsPaused
        {
            get { return isPaused; }
            set { SetProperty(ref isPaused, value); }
        }

        private bool isAborting;
        public bool IsAborting
        {
            get { return isAborting; }
            set { SetProperty(ref isAborting, value); }
        }

        private TimeSpan eta;
        public TimeSpan Eta
        {
            get { return eta; }
            set { SetProperty(ref eta, value); }
        }

        Lazy<ProjectModel> ProjectModel { get; }

        public RenderQueueModel(Lazy<ProjectModel> projectModel)
        {
            ProjectModel = projectModel;
        }
    }
}
