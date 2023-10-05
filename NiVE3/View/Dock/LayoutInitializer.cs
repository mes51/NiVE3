using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvalonDock.Layout;
using NiVE3.ViewModel;
using System.Windows.Controls;
using System.Windows;
using NiVE3.Extension;
using System.Numerics;

namespace NiVE3.View.Dock
{
    // NOTE: DependencyObjectだとバインディング時にエラーになるためFreezableにする
    // SEE: https://qiita.com/ugaya40/items/58e9e3c3340cc1f61b4f
    class LayoutInitializer : Freezable, ILayoutUpdateStrategy
    {
        static readonly GridLength InitialSidePaneSize = new GridLength(300);

        static readonly GridLength PanelSize = new GridLength(1.0, GridUnitType.Star);

        public static readonly string PanelNamePrefix = "MainLayoutPanel_";

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(MainWindowViewModel),
            typeof(LayoutInitializer),
            new PropertyMetadata(null)
        );

        public MainWindowViewModel? ViewModel
        {
            get { return (MainWindowViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {
            if (anchorableShown.Content is SingletonePaneViewModelBase)
            {
                return;
            }

            EventHandler? closed = null;
            closed = (object? sender, EventArgs e) =>
            {
                ViewModel?.RemoveViewModelCommand?.Execute(anchorableShown.Content);
                anchorableShown.Closed -= closed;
            };

            anchorableShown.Closed += closed;
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
        {
            if (anchorableShown.Content is SingletonePaneViewModelBase)
            {
                return;
            }

            EventHandler? closed = null;
            closed += (object? sender, EventArgs e) =>
            {
                ViewModel?.RemoveViewModelCommand?.Execute(anchorableShown.Content);
                anchorableShown.Closed -= closed;
            };

            anchorableShown.Closed += closed;
        }

        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            var attr = anchorableToShow.Content.GetType().GetCustomAttributes(typeof(PaneLocationAttribute), true).OfType<PaneLocationAttribute>().FirstOrDefault();
            if (attr != null)
            {
                var location = attr.Layout;
                var pane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(p => p.Name == location.ToString());
                if (pane == null)
                {
                    pane = CreateAnchorablePane(layout, location, attr.Size);
                }

                pane.Children.Add(anchorableToShow);
                return true;
            }
            return false;
        }

        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
        {
            return false;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LayoutInitializer();
        }

        LayoutAnchorablePane CreateAnchorablePane(LayoutRoot layout, PaneLocation location, int size)
        {
            var paneName = location switch
            {
                _ when location.HasFlag(PaneLocation.Vertical) && location.HasFlag(PaneLocation.TopArea) => $"{Orientation.Vertical}_Top",
                _ when location.HasFlag(PaneLocation.Vertical) && location.HasFlag(PaneLocation.Bottom) => $"{Orientation.Vertical}_Bottom",
                _ when location.HasFlag(PaneLocation.Horizontal) && location.HasFlag(PaneLocation.LeftArea) => $"{Orientation.Horizontal}_Left",
                _ when location.HasFlag(PaneLocation.Horizontal) && location.HasFlag(PaneLocation.RightArea) => $"{Orientation.Horizontal}_Right",
                _ => $"{Orientation.Vertical}_Top"
            };
            var parent = (LayoutAnchorablePaneGroup)layout.Manager.FindName(PanelNamePrefix + paneName);
            if (parent == null)
            {
                parent = CreatePaneGroup(layout, location);
            }
            var pane = new LayoutAnchorablePane
            {
                Name = location.ToString(),
                DockHeight = size != 0 ? new GridLength(size) : PanelSize
            };

            if (location.HasFlag(PaneLocation.TopArea))
            {
                parent.InsertChildAt(0, pane);
            }
            else if (location == PaneLocation.Document || location.HasFlag(PaneLocation.CenterArea))
            {
                var prev = parent.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(p => p.Name.Contains("Left"));
                if (prev != null)
                {
                    parent.InsertChildAt(1, pane);
                }
                else
                {
                    parent.InsertChildAt(0, pane);
                }
            }
            else
            {
                parent.Children.Add(pane);
            }

            return pane;
        }

        // NOTE: (多分LayoutRoot.CollectGarbageが呼ばれるせいで)XAML上でLayoutAnchorablePaneGroupを定義してもいなくなってしまうため、必要になったら生成する
        LayoutAnchorablePaneGroup CreatePaneGroup(LayoutRoot layout, PaneLocation location)
        {
            var name = PanelNamePrefix;
            var pane = new LayoutAnchorablePaneGroup { Orientation = Orientation.Vertical };
            if (location.HasFlag(PaneLocation.Vertical))
            {
                name += $"{Orientation.Vertical}_{(location.HasFlag(PaneLocation.TopArea) ? "Top" : "Bottom")}";
                var panel = (LayoutPanel)layout.Manager.FindName(PanelNamePrefix + Orientation.Vertical.ToString());
                pane.DockHeight = InitialSidePaneSize;
                if (location.HasFlag(PaneLocation.TopArea))
                {
                    panel.InsertChildAt(0, pane);
                }
                else
                {
                    panel.Children.Add(pane);
                }
            }
            else
            {
                name += $"{Orientation.Horizontal}_{(location.HasFlag(PaneLocation.LeftArea) ? "Left" : "Right")}";
                var panel = (LayoutPanel)layout.Manager.FindName(PanelNamePrefix + Orientation.Horizontal.ToString());
                pane.DockWidth = InitialSidePaneSize;
                if (location.HasFlag(PaneLocation.LeftArea))
                {
                    panel.InsertChildAt(0, pane);
                }
                else
                {
                    panel.Children.Add(pane);
                }
            }

            layout.Manager.RegisterName(name, pane);
            return pane;
        }
    }
}
