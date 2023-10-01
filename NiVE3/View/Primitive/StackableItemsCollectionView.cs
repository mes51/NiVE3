using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GongSolutions.Wpf.DragDrop;
using NiVE3.View.Part;
using NiVE3.ViewModel;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using NiVE3.View.Converter;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NiVE3.View.Primitive
{
    abstract class StackableItemsCollectionView : ItemsControl, IDragSource
    {
        public const double IndentWidth = 19.0;

        public static readonly GridLength IndentGridWidth = new GridLength(IndentWidth);

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.RegisterAttached(
            "IsSelected",
            typeof(bool),
            typeof(StackableItemsCollectionView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsItemLockedProperty = DependencyProperty.RegisterAttached(
            "IsItemLocked",
            typeof(bool),
            typeof(StackableItemsCollectionView),
            new PropertyMetadata(false, IsItemLockedChanged)
        );

        public static bool GetIsItemLocked(DependencyObject target)
        {
            return (bool)target.GetValue(IsItemLockedProperty);
        }

        public static void SetIsItemLocked(DependencyObject target, bool value)
        {
            target.SetValue(IsItemLockedProperty, value);
        }

        public static bool GetIsSelected(DependencyObject target)
        {
            return (bool)target.GetValue(IsSelectedProperty);
        }

        public static void SetIsSelected(DependencyObject target, bool value)
        {
            target.SetValue(IsSelectedProperty, value);
        }

        static void IsItemLockedChanged(DependencyObject d,  DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe && ItemsControlFromItemContainer(fe) is StackableItemsCollectionView collection)
            {
                if (GetIsItemLocked(fe))
                {
                    collection.DeselectItem(fe);
                }
            }
        }

        public abstract void SelectItem(FrameworkElement item, bool selectRange, bool selectMultiple);

        public abstract void DeselectItem(FrameworkElement item);

        public abstract void StartDrag(IDragInfo dragInfo);

        public virtual bool CanStartDrag(IDragInfo dragInfo)
        {
            return !GetIsItemLocked(dragInfo.VisualSourceItem);
        }

        public virtual void Dropped(IDropInfo dropInfo) { }

        public virtual void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) { }

        public virtual void DragCancelled() { }

        public virtual bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }
    }

    class StackableItemsCollectionView<T> : StackableItemsCollectionView where T : class
    {
        public static readonly Style DefaultStyle;

        protected static readonly IValueConverter IsSelectedConverter = new DelegateConverter<IEnumerable<T>, bool, T>((collection, target) => collection.Contains(target));

        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(double),
            typeof(StackableItemsCollectionView<T>),
            new FrameworkPropertyMetadata(0.0)
        );

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            nameof(SelectedItems),
            typeof(ObservableCollection<T>),
            typeof(StackableItemsCollectionView<T>),
            new FrameworkPropertyMetadata(new ObservableCollection<T>(), SelectedItemsChanged)
        );

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(StackableItemsCollectionView<T>),
            new FrameworkPropertyMetadata(0)
        );

        private static readonly DependencyProperty SelectedItemsViewProperty = DependencyProperty.Register(
            nameof(SelectedItemsView),
            typeof(ObservableCollectionView<T>),
            typeof(StackableItemsCollectionView<T>),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private ObservableCollectionView<T>? SelectedItemsView
        {
            get { return (ObservableCollectionView<T>)GetValue(SelectedItemsViewProperty); }
            set { SetValue(SelectedItemsViewProperty, value); }
        }

        public int IndentLevel
        {
            get { return (int)GetValue(IndentLevelProperty); }
            set { SetValue(IndentLevelProperty, value); }
        }

        public double ControlAreaWidth
        {
            get { return (double)GetValue(ControlAreaWidthProperty); }
            set { SetValue(ControlAreaWidthProperty, value); }
        }

        public ObservableCollection<T> SelectedItems
        {
            get { return (ObservableCollection<T>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        protected T? LastSelected { get; set; }

        static StackableItemsCollectionView()
        {
            var itemsPanelContainer = new FrameworkElementFactory(typeof(StackPanel));
            var itemsPanelTemplate = new ItemsPanelTemplate(itemsPanelContainer);

            DefaultStyle = new Style
            {
                TargetType = typeof(ItemsControl)
            };
            DefaultStyle.Setters.Add(new Setter(ItemsPanelProperty, itemsPanelTemplate));

            IsTabStopProperty.OverrideMetadata(typeof(StackableItemsCollectionView<T>), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
            ItemsSourceProperty.OverrideMetadata(typeof(StackableItemsCollectionView<T>), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, ItemsSourceChanged));
        }

        public StackableItemsCollectionView()
        {
            Style = DefaultStyle;
        }

        public override void StartDrag(IDragInfo dragInfo)
        {
            if (dragInfo.VisualSourceItem is FrameworkElement fe && fe.DataContext is T viewModel)
            {
                if (SelectedItems.Count > 1 && SelectedItems.Contains(viewModel))
                {
                    dragInfo.Data = new ItemDragData<T>(SelectedItems.ToArray(), viewModel);
                }
                else
                {
                    dragInfo.Data = viewModel;
                }
            }

            if (dragInfo.Data != null)
            {
                dragInfo.Effects = DragDropEffects.Move;
            }
            else
            {
                dragInfo.Effects = DragDropEffects.None;
            }
        }

        public override void SelectItem(FrameworkElement item, bool selectRange, bool selectMultiple)
        {
            var viewModel = item.DataContext as T;
            if (viewModel == null)
            {
                return;
            }
            var items = ItemsSource?.Cast<T>()?.ToArray();
            if (items == null || items.Length < 1 || !items.Contains(viewModel))
            {
                return;
            }

            if (GetIsItemLocked(item))
            {
                if (!selectRange && !selectMultiple)
                {
                    SelectedItems.Clear();
                }
                return;
            }

            if (selectMultiple)
            {
                if (SelectedItems.Contains(viewModel))
                {
                    SelectedItems.Remove(viewModel);
                }
                else
                {
                    SelectedItems.Add(viewModel);
                }
                LastSelected = viewModel;
            }
            else if (selectRange && SelectedItems.Count > 0)
            {
                var oldSelectedItems = SelectedItems.ToArray();
                if (LastSelected == null)
                {
                    LastSelected = items[0];
                }
                var startIndex = Array.IndexOf(items, LastSelected);
                var endIndex = Array.IndexOf(items, viewModel);
                if (startIndex == endIndex)
                {
                    foreach (var l in oldSelectedItems)
                    {
                        if (l != viewModel)
                        {
                            SelectedItems.Remove(l);
                        }
                    }
                    if (!SelectedItems.Contains(viewModel))
                    {
                        SelectedItems.Add(viewModel);
                    }
                    return;
                }
                else if (startIndex > endIndex)
                {
                    var temp = endIndex;
                    endIndex = startIndex;
                    startIndex = temp;
                }

                var targets = items.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
                foreach (var l in oldSelectedItems.Except(targets))
                {
                    SelectedItems.Remove(l);
                }
                foreach (var l in targets.Except(oldSelectedItems))
                {
                    if (GetIsItemLocked(ItemContainerGenerator.ContainerFromItem(l)))
                    {
                        continue;
                    }
                    SelectedItems.Add(l);
                }
            }
            else if (!SelectedItems.Contains(viewModel))
            {
                SelectedItems.Clear();

                SelectedItems.Add(viewModel);
                LastSelected = viewModel;
            }
        }

        public override void DeselectItem(FrameworkElement item)
        {
            var viewModel = item.DataContext as T;
            if (viewModel == null)
            {
                return;
            }
            if (SelectedItems.Contains(viewModel))
            {
                SelectedItems.Remove(viewModel);
                if (LastSelected == viewModel)
                {
                    LastSelected = SelectedItems.FirstOrDefault();
                }
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var isSelectedBinding = new Binding
            {
                Path = new PropertyPath($"{nameof(SelectedItemsView)}.{nameof(ObservableCollectionView<T>.Collection)}"),
                Source = this,
                Mode = BindingMode.OneWay,
                Converter = IsSelectedConverter,
                ConverterParameter = item
            };
            BindingOperations.SetBinding(element, IsSelectedProperty, isSelectedBinding);
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            SelectedItems.Clear();
            LastSelected = null;
        }

        static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StackableItemsCollectionView<T> collection)
            {
                collection.SelectedItems.Clear();
            }
        }

        static void SelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StackableItemsCollectionView<T> collection)
            {
                collection.SelectedItemsView = new ObservableCollectionView<T>(collection.SelectedItems);
            }
        }
    }

    record ItemDragData<T>(T[] SelectedItems, T DragItem) { }

    // CollectionViewSourceでどうにかならんか?
    class ObservableCollectionView<T> : INotifyPropertyChanged
    {
        public ObservableCollection<T> Collection { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollectionView(ObservableCollection<T> collection)
        {
            Collection = collection;
            collection.CollectionChanged += Collection_CollectionChanged;
        }

        private void Collection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Collection)));
        }
    }
}
