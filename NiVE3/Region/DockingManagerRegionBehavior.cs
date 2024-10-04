using AvalonDock;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.View.Dock;
using System.Reflection;
using NiVE3.ViewModel;
using Prism.Navigation.Regions;
using Prism.Navigation.Regions.Behaviors;

namespace NiVE3.Region
{
    class DockingManagerRegionBehavior : RegionBehavior, IHostAwareRegionBehavior
    {
        public static string BehaviorName = nameof(DockingManagerRegionAdapter);

        public DockingManager? RegionTarget { get; set; }

        public DependencyObject? HostControl
        {
            get => RegionTarget;
            set { RegionTarget = value as DockingManager; }
        }

        ObservableCollection<object> Documents { get; } = [];

        ObservableCollection<object> Anchorable { get; } = [];

        protected override void OnAttach()
        {
            if (Region == null || RegionTarget == null)
            {
                return;
            }

            Region.Views.CollectionChanged += Views_CollectionChanged;
            RegionTarget.DocumentsSource = Documents;
            RegionTarget.AnchorablesSource = Anchorable;

            RegionTarget.DocumentClosed += RegionTarget_DocumentClosed;
        }

        private void Views_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                var item = e.NewItems?[0];
                if (item != null)
                {
                    if (item.GetType().GetCustomAttribute<PaneLocationAttribute>()?.Layout == PaneLocation.Document)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Documents.Add(item);
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Anchorable.Add(item);
                        });
                    }

                    if (item is PaneViewModelBase pane)
                    {
                        pane.OpenPaneRequest += PaneViewModel_OpenPaneRequest;
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
            {
                var item = e.OldItems?[0];
                if (item != null)
                {
                    if (item.GetType().GetCustomAttribute<PaneLocationAttribute>()?.Layout == PaneLocation.Document)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Documents.Remove(item);
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Anchorable.Remove(item);
                        });
                    }

                    if (item is PaneViewModelBase pane)
                    {
                        pane.OpenPaneRequest -= PaneViewModel_OpenPaneRequest;
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Documents.Clear();
                    Anchorable.Clear();
                });
            }
        }

        private void PaneViewModel_OpenPaneRequest(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                Region.Activate(sender);
            }
        }

        private void RegionTarget_DocumentClosed(object? sender, DocumentClosedEventArgs e)
        {
            Documents.Remove(e.Document.Content);
            if (Region?.Views.Contains(e.Document.Content) ?? false)
            {
                Region.Remove(e.Document.Content);
            }
        }
    }
}
