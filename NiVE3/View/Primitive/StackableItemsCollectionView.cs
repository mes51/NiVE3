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
using System.Windows.Media.Effects;

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

        static StackableItemsCollectionView()
        {
            IsTabStopProperty.OverrideMetadata(typeof(StackableItemsCollectionView), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
            FocusableProperty.OverrideMetadata(typeof(StackableItemsCollectionView), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        }

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

        public static readonly DependencyProperty ItemContextMenuProperty = DependencyProperty.Register(
            nameof(ItemContextMenu),
            typeof(ContextMenu),
            typeof(StackableItemsCollectionView<T>),
            new FrameworkPropertyMetadata(null)
        );

        private static readonly DependencyProperty SelectedItemsViewProperty = DependencyProperty.Register(
            nameof(SelectedItemsView),
            typeof(ObservableCollectionView<T>),
            typeof(StackableItemsCollectionView<T>),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        private static readonly DependencyPropertyKey LastSelectedPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(LastSelected),
            typeof(T),
            typeof(StackableItemsCollectionView<T>),
            new FrameworkPropertyMetadata(null)
        );

        public static readonly DependencyProperty LastSelectedProperty = LastSelectedPropertyKey.DependencyProperty;

        public T? LastSelected
        {
            get { return (T?)GetValue(LastSelectedProperty); }
            private set { SetValue(LastSelectedPropertyKey, value); }
        }

        public ContextMenu? ItemContextMenu
        {
            get { return (ContextMenu?)GetValue(ItemContextMenuProperty); }
            set { SetValue(ItemContextMenuProperty, value); }
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

        public ObservableCollection<T>? SelectedItems
        {
            get { return (ObservableCollection<T>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        private ObservableCollectionView<T>? SelectedItemsView
        {
            get { return (ObservableCollectionView<T>)GetValue(SelectedItemsViewProperty); }
            set { SetValue(SelectedItemsViewProperty, value); }
        }

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
            StyleProperty.OverrideMetadata(typeof(StackableItemsCollectionView<T>), new FrameworkPropertyMetadata(DefaultStyle, FrameworkPropertyMetadataOptions.Inherits));
        }

        public override void StartDrag(IDragInfo dragInfo)
        {
            if (dragInfo.VisualSourceItem is FrameworkElement fe && fe.DataContext is T viewModel)
            {
                if (SelectedItems != null && SelectedItems.Count > 1 && SelectedItems.Contains(viewModel))
                {
                    dragInfo.Data = new ItemDragData<T>([..SelectedItems], viewModel);
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
            if (item.DataContext is not T viewModel)
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
                    SelectedItems?.Clear();
                }
                return;
            }

            if (selectMultiple)
            {
                if (SelectedItems != null)
                {
                    if (!SelectedItems.Remove(viewModel))
                    {
                        SelectedItems.Add(viewModel);
                    }
                    LastSelected = viewModel;
                }
            }
            else if (selectRange && SelectedItems != null && SelectedItems.Count > 0)
            {
                var oldSelectedItems = SelectedItems.ToArray();
                LastSelected ??= items[0];
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
                    (startIndex, endIndex) = (endIndex, startIndex);
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
            else if (!(SelectedItems?.Contains(viewModel) ?? true))
            {
                SelectedItems.Clear();

                SelectedItems.Add(viewModel);
                LastSelected = viewModel;
            }
        }

        public override void DeselectItem(FrameworkElement item)
        {
            if (item.DataContext is not T viewModel)
            {
                return;
            }
            if (SelectedItems?.Contains(viewModel) ?? false)
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

            var contextMenuBinding = new Binding
            {
                Path = new PropertyPath(ItemContextMenuProperty),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(element, ContextMenuProperty, contextMenuBinding);
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            SelectedItems?.Clear();
            LastSelected = null;
        }

        static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StackableItemsCollectionView<T> collection)
            {
                collection.SelectedItems?.Clear();
            }
        }

        static void SelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StackableItemsCollectionView<T> collection)
            {
                if (collection.SelectedItems != null)
                {
                    collection.SelectedItemsView = new ObservableCollectionView<T>(collection.SelectedItems);
                }
                else
                {
                    collection.SelectedItemsView = null;
                }
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
