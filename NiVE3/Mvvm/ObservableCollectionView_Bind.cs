using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Mvvm
{
    partial class ObservableCollectionView<T, TView>
    {
        partial void BindCollectionChanged(ObservableCollection<T> models)
        {
            _ = new ObservableCollectionBinder<T, TView>(models, this);
        }

        // ObservableCollectionBinderから参照したいが外には公開したくないのでfile partialで定義する
        internal void Models_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Clear();
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        var models = e.NewItems ?? e.OldItems;
                        if (models == null)
                        {
                            throw new InvalidOperationException(nameof(e));
                        }
                        var items = Views.Where(t => models.Contains(t.Model)).ToArray();
                        var oldStartIndex = Views.IndexOf(items[0]);
                        foreach (var i in items)
                        {
                            Views.Remove(i);
                        }

                        var newIndex = e.NewStartingIndex;
                        foreach (var i in items)
                        {
                            Views.Insert(newIndex, i);
                            newIndex++;
                        }

                        OnPropertyChanged(IndexerName);
                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(items.ToList(), e.NewStartingIndex, oldStartIndex));
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, items.Select(t => t.View).ToList(), e.NewStartingIndex, oldStartIndex));
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        var oldModels = e.OldItems;
                        var newModels = e.NewItems;
                        if (oldModels == null || newModels == null)
                        {
                            throw new InvalidOperationException(nameof(e));
                        }
                        var oldItems = Views.Where(t => oldModels.Contains(t.Model)).ToArray();
                        var newItems = newModels.OfType<T>().Select<T, (T Model, TView View)>(m => (m, Transform(m))).ToArray();
                        var oldStartingIndex = oldItems.Length > 0 ? Views.IndexOf(oldItems[0]) : -1;
                        var newIndex = e.NewStartingIndex;

                        foreach (var i in oldItems)
                        {
                            Views.Remove(i);
                        }
                        foreach (var i in newItems)
                        {
                            Views.Insert(newIndex, i);
                            newIndex++;
                        }

                        if (oldItems.Length != newItems.Length)
                        {
                            OnPropertyChanged(nameof(Count));
                        }
                        OnPropertyChanged(IndexerName);
                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(newItems, oldItems, oldStartingIndex));
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems.Select(t => t.View).ToList(), oldItems.Select(t => t.View).ToList(), oldStartingIndex));
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    {
                        var newItems = (e.NewItems?.OfType<T>() ?? Enumerable.Empty<T>()).Select<T, (T Model, TView View)>(m => (m, Transform(m))).ToList();
                        var newIndex = e.NewStartingIndex > -1 ? e.NewStartingIndex : Views.Count;
                        foreach (var i in newItems)
                        {
                            Views.Insert(newIndex, i);
                            newIndex++;
                        }

                        OnPropertyChanged(nameof(Count));
                        OnPropertyChanged(IndexerName);
                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(NotifyCollectionChangedAction.Add, newItems, e.NewStartingIndex));
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.Select(t => t.View).ToList(), e.NewStartingIndex));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        var oldItems = (e.OldItems?.OfType<T>() ?? Enumerable.Empty<T>()).Select(m => Views.Find(t => m!.Equals(t.Model))).ToList();
                        var oldIndex = oldItems.Count > 0 ? Views.IndexOf(oldItems[0]) : -1;
                        foreach (var i in oldItems)
                        {
                            Views.Remove(i);
                        }

                        OnPropertyChanged(nameof(Count));
                        OnPropertyChanged(IndexerName);
                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(NotifyCollectionChangedAction.Remove, oldItems, oldIndex));
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems.Select(t => t.View).ToList(), oldIndex));
                    }
                    break;
            }
        }
    }

    file class ObservableCollectionBinder<T, TView>
    {
        public WeakReference<ObservableCollectionView<T, TView>> ViewCollection { get; }

        public ObservableCollectionBinder(ObservableCollection<T> collection, ObservableCollectionView<T, TView> viewCollection)
        {
            ViewCollection = new WeakReference<ObservableCollectionView<T, TView>>(viewCollection);

            collection.CollectionChanged += Collection_CollectionChanged;
        }

        private void Collection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewCollection.TryGetTarget(out var viewCollection))
            {
                viewCollection.Models_CollectionChanged(sender, e);
            }
        }
    }
}
