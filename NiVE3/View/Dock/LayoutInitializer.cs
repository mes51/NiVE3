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
using System.ComponentModel;

namespace NiVE3.View.Dock
{
    // NOTE: DependencyObjectだとバインディング時にエラーになるためFreezableにする
    // SEE: https://qiita.com/ugaya40/items/58e9e3c3340cc1f61b4f
    class LayoutInitializer : Freezable, ILayoutUpdateStrategy
    {
        static readonly GridLength InitialSidePaneSize = new GridLength(300);

        static readonly GridLength PanelSize = new GridLength(1.0, GridUnitType.Star);

        public static readonly string PanelNamePrefix = "MainLayoutPanel_";

        private static readonly DependencyProperty DefaultHeightProperty = DependencyProperty.RegisterAttached("DefaultHeight", typeof(double), typeof(LayoutInitializer), new PropertyMetadata(0.0));

        private static double GetDefaultHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(DefaultHeightProperty);
        }

        private static void SetDefaultHeight(DependencyObject obj, double value)
        {
            obj.SetValue(DefaultHeightProperty, value);
        }

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

            BindClosed(() => ViewModel, anchorableShown);
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
        {
            if (anchorableShown.Content is SingletonePaneViewModelBase)
            {
                return;
            }

            BindClosed(() => ViewModel, anchorableShown);
        }

        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            var attr = anchorableToShow.Content.GetType().GetCustomAttributes(typeof(PaneLocationAttribute), true).OfType<PaneLocationAttribute>().FirstOrDefault();
            if (attr != null)
            {
                var location = attr.Layout;
                var pane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(p => p.Name == location.ToString()) ?? CreateAnchorablePane(layout, location, attr.Size);
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

        static LayoutAnchorablePane CreateAnchorablePane(LayoutRoot layout, PaneLocation location, double size)
        {
            var parent = (LayoutAnchorablePaneGroup)layout.Manager.FindName(GetPaneName(location)) ?? CreatePaneGroup(layout, location);
            var pane = new LayoutAnchorablePane
            {
                Name = location.ToString(),
                DockHeight = size != 0.0 ? new GridLength(size, GridUnitType.Star) : PanelSize
            };
            //pane.PropertyChanged += Pane_PropertyChanged;
            if (size != 0.0)
            {
                SetDefaultHeight(pane, size);
            }

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

        public static void BindClosed(Func<MainWindowViewModel?> viewModelGetter, LayoutContent layoutContent)
        {
            void closed(object? sender, EventArgs e)
            {
                viewModelGetter()?.RemoveViewModelCommand?.Execute(layoutContent.Content);
                layoutContent.Closed -= closed;
            }

            layoutContent.Closed += closed;
        }

        // NOTE: (多分LayoutRoot.CollectGarbageが呼ばれるせいで)XAML上でLayoutAnchorablePaneGroupを定義してもいなくなってしまうため、必要になったら生成する
        static LayoutAnchorablePaneGroup CreatePaneGroup(LayoutRoot layout, PaneLocation location)
        {
            var name = GetPaneName(location);
            var pane = new LayoutAnchorablePaneGroup { Orientation = Orientation.Vertical };
            if (location.HasFlag(PaneLocation.Vertical))
            {
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
                var panel = (LayoutPanel)layout.Manager.FindName(PanelNamePrefix + Orientation.Horizontal.ToString());
                pane.DockWidth = InitialSidePaneSize;
                var index = 0;
                if (location.HasFlag(PaneLocation.LeftArea))
                {
                    if (location.HasFlag(PaneLocation.SecondPanel) && layout.Manager.FindName(GetPaneName(PaneLocation.Horizontal | PaneLocation.LeftArea | PaneLocation.FirstPanel)) != null)
                    {
                        index = 1;
                    }
                }
                else
                {
                    if (location.HasFlag(PaneLocation.SecondPanel) && layout.Manager.FindName(GetPaneName(PaneLocation.Horizontal | PaneLocation.RightArea | PaneLocation.FirstPanel)) != null)
                    {
                        index = panel.Children.Count - 1;
                    }
                    else
                    {
                        index = panel.Children.Count;
                    }
                }
                panel.InsertChildAt(index, pane);
            }

            layout.Manager.RegisterName(name, pane);
            return pane;
        }

        // NOTE: AvalonDockの中で起動時にDockHeightが強制的に"*"に変えられてしまうため、書き換わったタイミングで設定したい値に戻す
        // NOTE: レイアウトの保存時、DockHeightがStarでない場合にちゃんとレイアウトが保存されないため、固定サイズでのレイアウトをやめる
        // TODO: どうにかして抜け穴を探す
        //private void Pane_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        //{
        //    if (sender is LayoutAnchorablePane pane && e.PropertyName == nameof(LayoutAnchorablePane.DockHeight))
        //    {
        //        var defaultHeight = GetDefaultHeight(pane);
        //        if (defaultHeight != 0.0)
        //        {
        //            pane.DockHeight = new GridLength(defaultHeight);
        //        }
        //        pane.PropertyChanged -= Pane_PropertyChanged;
        //    }
        //}

        static string GetPaneName(PaneLocation location)
        {
            return PanelNamePrefix + location switch
            {
                _ when location.HasFlag(PaneLocation.Vertical) && location.HasFlag(PaneLocation.TopArea) => $"{Orientation.Vertical}_Top",
                _ when location.HasFlag(PaneLocation.Vertical) && location.HasFlag(PaneLocation.Bottom) => $"{Orientation.Vertical}_Bottom",
                _ when location.HasFlag(PaneLocation.Horizontal) && location.HasFlag(PaneLocation.LeftArea) => $"{Orientation.Horizontal}_Left",
                _ when location.HasFlag(PaneLocation.Horizontal) && location.HasFlag(PaneLocation.RightArea) => $"{Orientation.Horizontal}_Right",
                _ => $"{Orientation.Vertical}_Top"
            } + "_" + location switch
            {
                _ when location.HasFlag(PaneLocation.SecondPanel) => "Second",
                _ => "First"
            };
        }
    }
}
