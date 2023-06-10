using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
            typeof(ObservableCollection<object>),
            typeof(TreeListView),
            new FrameworkPropertyMetadata(new ObservableCollection<object>(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
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

        public ObservableCollection<object> SelectedItems
        {
            get { return (ObservableCollection<object>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public GridViewColumnCollection Columns
        {
            get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        TreeListViewItem? LastSelected { get; set; }

        List<TreeListViewItem> SelectedTreeListViewItems { get; } = new List<TreeListViewItem>();

        static TreeListView()
        {
            ItemsSourceProperty.AddOwner(typeof(TreeListView), new FrameworkPropertyMetadata(null, ItemsSourcePropertyChanged));
        }

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
            var dataContext = item.DataContext;
            if (IsKeyDownControl())
            {
                if (SelectedItems.Contains(dataContext))
                {
                    SetIsSelectedItem(item, false);
                    SelectedItems.Remove(dataContext);
                    SelectedTreeListViewItems.Remove(item);
                }
                else
                {
                    SetIsSelectedItem(item, true);
                    SelectedItems.Add(dataContext);
                    SelectedTreeListViewItems.Add(item);
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

                var oldSelectedItems = SelectedTreeListViewItems.ToArray();
                var startIndex = items.IndexOf(LastSelected ?? items[0]);
                var endIndex = items.IndexOf(item);
                if (startIndex == endIndex)
                {
                    foreach (var i in oldSelectedItems)
                    {
                        if (i.DataContext != dataContext)
                        {
                            SetIsSelectedItem(i, false);
                            SelectedItems.Remove(i.DataContext);
                            SelectedTreeListViewItems.Remove(i);
                        }
                    }
                    if (!SelectedItems.Contains(dataContext))
                    {
                        SetIsSelectedItem(item, true);
                        SelectedItems.Add(dataContext);
                        SelectedTreeListViewItems.Add(item);
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
                    SelectedItems.Remove(i.DataContext);
                    SelectedTreeListViewItems.Remove(i);
                }

                foreach (var i in targets.Except(oldSelectedItems))
                {
                    SetIsSelectedItem(i, true);
                    SelectedItems.Add(i.DataContext);
                    SelectedTreeListViewItems.Add(i);
                }
            }
            else if (SelectedItems.Contains(dataContext))
            {
                foreach (var i in SelectedTreeListViewItems.ToArray())
                {
                    if (i.DataContext != dataContext)
                    {
                        SetIsSelectedItem(i, false);
                        SelectedItems.Remove(i.DataContext);
                        SelectedTreeListViewItems.Remove(i);
                    }
                }

                SetIsSelectedItem(item, true);
                LastSelected = item;
            }
            else
            {
                foreach (var i in SelectedTreeListViewItems)
                {
                    if (i.DataContext != dataContext)
                    {
                        SetIsSelectedItem(i, false);
                    }
                }
                SelectedItems.Clear();
                SelectedTreeListViewItems.Clear();

                SetIsSelectedItem(item, true);
                SelectedItems.Add(dataContext);
                SelectedTreeListViewItems.Add(item);
                LastSelected = item;
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

        void RefreshSelectedItems()
        {
            var removedSelectedItems = SelectedItems.Except(ItemsSource.OfType<object>());
            var newSelectedItems = SelectedItems.Except(removedSelectedItems);
            var oldSelectedTreeListViewItems = SelectedTreeListViewItems.ToArray();

            SelectedTreeListViewItems.Clear();

            var items = GetAllItems().ToArray();
            foreach (var i in newSelectedItems)
            {
                var treeListViewItem = items.FirstOrDefault(t => t.DataContext == i);
                if (treeListViewItem != null)
                {
                    SetIsSelectedItem(treeListViewItem, true);
                    SelectedTreeListViewItems.Add(treeListViewItem);
                }
            }

            foreach (var i in removedSelectedItems)
            {
                SelectedItems.Remove(i);
            }
            foreach (var t in oldSelectedTreeListViewItems.Except(SelectedTreeListViewItems))
            {
                SetIsSelectedItem(t, false);
            }

            if (LastSelected != null)
            {
                LastSelected = SelectedTreeListViewItems.FirstOrDefault(i => i.DataContext == LastSelected.DataContext);
            }
        }

        IEnumerable<TreeListViewItem> GetAllItems(Visual? parent = null)
        {
            if (parent == null)
            {
                parent = this;
            }

            var result = new List<TreeListViewItem>();

            for (int i = 0, count = VisualTreeHelper.GetChildrenCount(parent); i < count; i++)
            {
                if (VisualTreeHelper.GetChild(parent, i) is Visual child)
                {
                    if (child is TreeListViewItem item)
                    {
                        result.Add(item);
                    }
                    result.AddRange(GetAllItems(child));
                }
            }

            return result;
        }

        private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Template.FindName("HeaderScrollViewer", this) is ScrollViewer headerScrollViewer && Template.FindName("ContentScrollViewer", this) is ScrollViewer contentScrollViewewr)
            {
                headerScrollViewer.ScrollToHorizontalOffset(contentScrollViewewr.HorizontalOffset);
            }
        }

        private void ItemsCource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshSelectedItems();
        }

        static void ItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TreeListView treeListView)
            {
                if (e.OldValue is INotifyCollectionChanged oldItemsSource)
                {
                    oldItemsSource.CollectionChanged -= treeListView.ItemsCource_CollectionChanged;
                }
                if (e.NewValue is INotifyCollectionChanged newItemsSource)
                {
                    newItemsSource.CollectionChanged += treeListView.ItemsCource_CollectionChanged;
                }
            }
        }
    }
}
