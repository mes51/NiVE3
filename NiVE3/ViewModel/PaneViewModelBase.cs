using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Mvvm;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    abstract class PaneViewModelBase : BindableBase
    {
        public string ContentId { get; private set; } = "";

        private string title = "";
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private bool isActive;
        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                SetProperty(ref isSelected, value);
                if (value)
                {
                    PaneSelectedPublisher.Publish(this, EventArgs.Empty);
                }
            }
        }

        WeakEventPublisher<EventArgs> PaneSelectedPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> PaneSelected
        {
            add { PaneSelectedPublisher.Subscribe(value); }
            remove { PaneSelectedPublisher.Unsubscribe(value); }
        }

        WeakEventPublisher<EventArgs> OpenPaneRequestPublisher { get; } = new WeakEventPublisher<EventArgs>();
        public event EventHandler<EventArgs> OpenPaneRequest
        {
            add { OpenPaneRequestPublisher.Subscribe(value); }
            remove { OpenPaneRequestPublisher.Unsubscribe(value); }
        }

        public virtual void OpenPane()
        {
            IsSelected = true;
            IsActive = true;
            OpenPaneRequestPublisher.Publish(this, EventArgs.Empty);
        }
    }

    abstract class SingletonePaneViewModelBase : PaneViewModelBase
    {
        private Visibility visibility;
        public Visibility Visibility
        {
            get { return visibility; }
            set { SetProperty(ref visibility, value); }
        }

        public override void OpenPane()
        {
            Visibility = Visibility.Visible;
            base.OpenPane();
        }
    }
}
