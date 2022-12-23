using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvalonDock.Layout;
using NiVE3.ViewModel;
using System.Windows.Controls;
using System.Windows;

namespace NiVE3.View.Dock
{
    // NOTE: DependencyObjectだとバインディング時にエラーになるためFreezableにする
    // SEE: https://qiita.com/ugaya40/items/58e9e3c3340cc1f61b4f
    class LayoutInitializer : Freezable, ILayoutUpdateStrategy
    {
        public static readonly string PanelNamePrefix = "MainLayoutPanel_";

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(object),
            typeof(LayoutInitializer),
            new PropertyMetadata(null)
        );

        public object? ViewModel
        {
            get { return (object)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {
            anchorableShown.CanClose = true;
            if (anchorableShown.Content is PaneViewModelBase vm)
            {
                anchorableShown.ContentId = vm.ContentId;
                anchorableShown.Title = vm.Title;
            }

            EventHandler? closed = null;
            closed = (object? sender, EventArgs e) =>
            {
                // TODO: Remove ViewModel from Region
                anchorableShown.Closed -= closed;
            };

            anchorableShown.Closed += closed;
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
        {
            if (anchorableShown.Content is PaneViewModelBase vm)
            {
                anchorableShown.ContentId = vm.ContentId;
                anchorableShown.Title = vm.Title;
            }
        }

        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            var location = anchorableToShow.Content.GetType().GetCustomAttributes(typeof(PaneLocationAttribute), true).OfType<PaneLocationAttribute>().FirstOrDefault()?.Layout;
            if (location != null)
            {
                var pane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(p => p.Name == location.ToString());
                if (pane == null)
                {
                    pane = CreateAnchorablePane(layout, location.Value);
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

        LayoutAnchorablePane CreateAnchorablePane(LayoutRoot layout, PaneLocation location)
        {
            var orientation = location == PaneLocation.Top || location == PaneLocation.Bottom ? Orientation.Vertical : Orientation.Horizontal;
            //var parent = layout.Descendents().OfType<LayoutPanel>().First(p => p.Orientation == orientation);
            var parent = (LayoutPanel)layout.Manager.FindName(PanelNamePrefix + orientation.ToString());
            var pane = new LayoutAnchorablePane { Name = location.ToString() };

            if (location == PaneLocation.Top || location == PaneLocation.Left)
            {
                parent.InsertChildAt(0, pane);
            }
            else if (location == PaneLocation.Document)
            {
                var prev = parent.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(p => p.Name == PaneLocation.Left.ToString());
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
    }
}
