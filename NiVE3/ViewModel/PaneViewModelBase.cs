using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Mvvm;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [UseReactiveProperty]
    abstract partial class PaneViewModelBase : BindableBase
    {
        public string ContentId { get; private set; } = "";

        [ReactiveProperty]
        public partial string Title { get; set; } = "";

        public bool IsActive
        {
            get;
            set
            {
                SetProperty(ref field, value);
                if (value)
                {
                    PaneSelectedPublisher.Publish(this, EventArgs.Empty);
                }
            }
        }

        public bool IsSelected
        {
            get;
            set
            {
                SetProperty(ref field, value);
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

        protected PaneViewModelBase()
        {
            ContentId = GetType().Name;
        }

        public virtual void OpenPane()
        {
            IsSelected = true;
            IsActive = true;
            OpenPaneRequestPublisher.Publish(this, EventArgs.Empty);
        }
    }

    [UseReactiveProperty]
    abstract partial class SingletonePaneViewModelBase : PaneViewModelBase
    {
        [ReactiveProperty]
        public partial Visibility Visibility { get; set; }

        public override void OpenPane()
        {
            Visibility = Visibility.Visible;
            base.OpenPane();
        }
    }
}
