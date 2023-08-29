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

namespace NiVE3.View.Primitive
{
    abstract class NestableItemsCollection : ItemsControl, IDragSource
    {
        public const double IndentWidth = 19.0;

        public static readonly GridLength IndentGridWidth = new GridLength(IndentWidth);

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.RegisterAttached(
            "IsSelected",
            typeof(bool),
            typeof(NestableItemsCollection),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsItemLockedProperty = DependencyProperty.RegisterAttached(
            "IsItemLocked",
            typeof(bool),
            typeof(NestableItemsCollection),
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
            if (d is FrameworkElement fe && ItemsControlFromItemContainer(fe) is NestableItemsCollection collection)
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
            return true;
        }

        public virtual void Dropped(IDropInfo dropInfo) { }

        public virtual void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) { }

        public virtual void DragCancelled() { }

        public virtual bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }
    }

    class NestableItemsCollection<T> : NestableItemsCollection where T : class
    {
        public static readonly Style DefaultStyle;

        public static readonly DependencyProperty ControlAreaWidthProperty = DependencyProperty.Register(
            nameof(ControlAreaWidth),
            typeof(double),
            typeof(NestableItemsCollection<T>),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        internal static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            nameof(SelectedItems),
            typeof(ObservableCollection<T>),
            typeof(NestableItemsCollection<T>),
            new FrameworkPropertyMetadata(new ObservableCollection<T>(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty CalculatedControlAreaWidthProperty = DependencyProperty.Register(
            nameof(CalculatedControlAreaWidth),
            typeof(double),
            typeof(NestableItemsCollection<T>),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IndentLevelProperty = DependencyProperty.Register(
            nameof(IndentLevel),
            typeof(int),
            typeof(NestableItemsCollection<T>),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ControlAreaWidthChanged)
        );

        public int IndentLevel
        {
            get { return (int)GetValue(IndentLevelProperty); }
            set { SetValue(IndentLevelProperty, value); }
        }

        public double CalculatedControlAreaWidth
        {
            get { return (double)GetValue(CalculatedControlAreaWidthProperty); }
            set { SetValue(CalculatedControlAreaWidthProperty, value); }
        }

        public double ControlAreaWidth
        {
            get { return (double)GetValue(ControlAreaWidthProperty); }
            set { SetValue(ControlAreaWidthProperty, value); }
        }

        internal ObservableCollection<T> SelectedItems
        {
            get { return (ObservableCollection<T>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        protected T? LastSelected { get; set; }

        static NestableItemsCollection()
        {
            var itemsPanelContainer = new FrameworkElementFactory(typeof(StackPanel));
            var itemsPanelTemplate = new ItemsPanelTemplate(itemsPanelContainer);

            DefaultStyle = new Style
            {
                TargetType = typeof(ItemsControl)
            };
            DefaultStyle.Setters.Add(new Setter(ItemsPanelProperty, itemsPanelTemplate));

            IsTabStopProperty.OverrideMetadata(typeof(NestableItemsCollection<T>), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        }

        public NestableItemsCollection()
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
                    foreach (var l in items)
                    {
                        SetSelected(l, false);
                    }
                    SelectedItems.Clear();
                }
                return;
            }

            if (selectMultiple)
            {
                if (SelectedItems.Contains(viewModel))
                {
                    SetSelected(viewModel, false);
                    SelectedItems.Remove(viewModel);
                }
                else
                {
                    SetSelected(viewModel, true);
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
                            SetSelected(l, false);
                            SelectedItems.Remove(l);
                        }
                    }
                    if (!SelectedItems.Contains(viewModel))
                    {
                        SetSelected(viewModel, true);
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
                    SetSelected(l, false);
                    SelectedItems.Remove(l);
                }
                foreach (var l in targets.Except(oldSelectedItems))
                {
                    if (GetIsItemLocked(ItemContainerGenerator.ContainerFromItem(l)))
                    {
                        continue;
                    }
                    SetSelected(l, true);
                    SelectedItems.Add(l);
                }
            }
            else if (!SelectedItems.Contains(viewModel))
            {
                foreach (var l in items)
                {
                    if (l != viewModel)
                    {
                        SetSelected(l, false);
                    }
                }
                SelectedItems.Clear();

                SetSelected(viewModel, true);
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
                SetSelected(viewModel, false);
                SelectedItems.Remove(viewModel);
                if (LastSelected == viewModel)
                {
                    LastSelected = SelectedItems.FirstOrDefault();
                }
            }
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            SelectedItems.Clear();
            LastSelected = null;
        }

        void SetSelected(object viewModel, bool selected)
        {
            var item = ItemContainerGenerator.ContainerFromItem(viewModel);
            SetIsSelected(item, selected);
        }

        static void ControlAreaWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NestableItemsCollection<T> collection)
            {
                collection.CalculatedControlAreaWidth = collection.ControlAreaWidth - collection.IndentLevel * IndentWidth;
            }
        }
    }

    class ItemDragData<T>
    {
        public T[] SelectedItems;

        public T DragItem;

        public ItemDragData(T[] selectedItems, T dragItem)
        {
            SelectedItems = selectedItems;
            DragItem = dragItem;
        }
    }
}
