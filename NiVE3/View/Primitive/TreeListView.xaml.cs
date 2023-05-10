using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// TreeListView.xaml の相互作用ロジック
    /// </summary>
    public partial class TreeListView : TreeView
    {
        // SEE: https://stackoverflow.com/a/18751667
        public static readonly DependencyProperty IsSelectedItemProperty = DependencyProperty.RegisterAttached(
            "IsSelectedItem",
            typeof(bool),
            typeof(TreeListView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static bool GetIsSelectedItem(UIElement target)
        {
            return (bool)target.GetValue(IsSelectedItemProperty);
        }

        public static void SetIsSelectedItem(UIElement target, bool value)
        {
            target.SetValue(IsSelectedItemProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            nameof(Columns),
            typeof(GridViewColumnCollection),
            typeof(TreeListView),
            new FrameworkPropertyMetadata(new GridViewColumnCollection(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            nameof(SelectedItems),
            typeof(ObservableCollection<TreeListViewItem>),
            typeof(TreeListView),
            new FrameworkPropertyMetadata(new ObservableCollection<TreeListViewItem>(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty HeaderContextMenuProperty = DependencyProperty.Register(
            nameof(HeaderContextMenu),
            typeof(ContextMenu),
            typeof(TreeListView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public ContextMenu? HeaderContextMenu
        {
            get { return (ContextMenu)GetValue(HeaderContextMenuProperty); }
            set { SetValue(HeaderContextMenuProperty, value); }
        }

        public ObservableCollection<TreeListViewItem> SelectedItems
        {
            get { return (ObservableCollection<TreeListViewItem>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public GridViewColumnCollection Columns
        {
            get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        TreeListViewItem? LastSelected { get; set; }

        public TreeListView()
        {
            InitializeComponent();
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeListViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            var source = e.OriginalSource as DependencyObject;
            if (source == null)
            {
                return;
            }

            var item = GetContainedTreeListViewItem(source);
            if (item == null)
            {
                return;
            }
            if (item.IsToggleButtonElement(source))
            {
                return;
            }

            SelectItem(item);
        }

        void SelectItem(TreeListViewItem item)
        {
            if (IsKeyDownControl())
            {
                if (SelectedItems.Contains(item))
                {
                    SetIsSelectedItem(item, false);
                    SelectedItems.Remove(item);
                }
                else
                {
                    SetIsSelectedItem(item, true);
                    SelectedItems.Add(item);
                }

                LastSelected = item;
            }
            else if (IsKeyDownShift() && SelectedItems.Count > 0)
            {
                var items = GetTreeListViewItems(this, false);
                if (items.Count < 1)
                {
                    return;
                }

                var oldSelectedItems = SelectedItems.ToArray();
                var startIndex = items.IndexOf(LastSelected ?? items[0]);
                var endIndex = items.IndexOf(item);
                if (startIndex == endIndex)
                {
                    foreach (var i in oldSelectedItems)
                    {
                        if (i != item)
                        {
                            SetIsSelectedItem(i, false);
                            SelectedItems.Remove(i);
                        }
                    }
                    if (!SelectedItems.Contains(item))
                    {
                        SetIsSelectedItem(item, true);
                        SelectedItems.Add(item);
                    }
                    return;
                }
                else if (startIndex > endIndex)
                {
                    var temp = startIndex;
                    startIndex = endIndex;
                    endIndex = temp;
                }

                var targets = items.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
                foreach (var i in oldSelectedItems.Except(targets))
                {
                    SetIsSelectedItem(i, false);
                    SelectedItems.Remove(i);
                }

                foreach (var i in targets.Except(oldSelectedItems))
                {
                    SetIsSelectedItem(i, true);
                    SelectedItems.Add(i);
                }
            }
            else if (SelectedItems.Contains(item))
            {
                foreach (var i in SelectedItems.ToArray())
                {
                    if (i != item)
                    {
                        SetIsSelectedItem(i, false);
                        SelectedItems.Remove(i);
                    }
                }

                SetIsSelectedItem(item, true);
                LastSelected = item;
            }
            else
            {
                foreach (var i in SelectedItems)
                {
                    if (i != item)
                    {
                        SetIsSelectedItem(i, false);
                    }
                }
                SelectedItems.Clear();

                SetIsSelectedItem(item, true);
                SelectedItems.Add(item);
                LastSelected = item;
            }
        }

        private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Template.FindName("HeaderScrollViewer", this) is ScrollViewer headerScrollViewer && Template.FindName("ContentScrollViewer", this) is ScrollViewer contentScrollViewewr)
            {
                headerScrollViewer.ScrollToHorizontalOffset(contentScrollViewewr.HorizontalOffset);
            }
        }

        private static List<TreeListViewItem> GetTreeListViewItems(ItemsControl parent, bool includeCollapsedItems, List<TreeListViewItem>? items = null)
        {
            items ??= new List<TreeListViewItem>();

            for (int i = 0, limit = parent.Items.Count; i < limit; i++)
            {
                var item = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeListViewItem;
                if (item == null)
                {
                    continue;
                }

                items.Add(item);
                if (includeCollapsedItems || item.IsExpanded)
                {
                    GetTreeListViewItems(item, includeCollapsedItems, items);
                }
            }

            return items;
        }

        static bool IsKeyDownControl()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        static bool IsKeyDownShift()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        static TreeListViewItem? GetContainedTreeListViewItem(DependencyObject? target)
        {
            while (target != null)
            {
                if (target is TreeListViewItem item)
                {
                    return item;
                }
                target = VisualTreeHelper.GetParent(target);
            }

            return null;
        }
    }
}
