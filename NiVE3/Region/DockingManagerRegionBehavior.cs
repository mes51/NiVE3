using AvalonDock;
using Prism.Regions.Behaviors;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.ViewModel;

namespace NiVE3.Region
{
    internal class DockingManagerRegionBehavior : RegionBehavior, IHostAwareRegionBehavior
    {
        public static string BehaviorName = nameof(DockingManagerRegionAdapter);

        public DockingManager? RegionTarget { get; set; }

        public DependencyObject? HostControl
        {
            get => RegionTarget;
            set { RegionTarget = value as DockingManager; }
        }

        ObservableCollection<object> Documents { get; } = new ObservableCollection<object>();

        ObservableCollection<object> Anchorable { get; } = new ObservableCollection<object>();

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
                    if (item.GetType().GetCustomAttributes(typeof(DocumentViewModelAttribute), true)?.Length > 0)
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
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
            {
                var item = e.OldItems?[0];
                if (item != null)
                {
                    if (item.GetType().GetCustomAttributes(typeof(DocumentViewModelAttribute), true)?.Length > 0)
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
