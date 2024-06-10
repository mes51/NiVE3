using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Util;

namespace NiVE3.Mvvm
{
    partial class ObservableCollectionView<T, TView>
    {
        partial void BindCollectionChanged(ObservableCollection<T> models)
        {
            _ = new ObservableCollectionBinder<T, TView>(models, this);
        }

        void ClearInternal()
        {
            Views.Clear();
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            OnCollectionCleared();
        }

        // ObservableCollectionBinderから参照したいが外には公開したくないのでfile partialで定義する
        internal void Models_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    ClearInternal();
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        var models = e.NewItems ?? e.OldItems;
                        OperationGuard.ThrowIfNull(models, nameof(e));

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
                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>([..items], e.NewStartingIndex, oldStartIndex));
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
                        else if (oldModels.Count > 1 || newModels.Count > 1)
                        {
                            // NOTE: ObservableCollectionは範囲アクションに非対応なのでここは来ないはず
                            throw new NotSupportedException("Not supported range action.");
                        }
                        else if (e.OldStartingIndex != e.NewStartingIndex)
                        {
                            // NOTE: 変更前後でインデックスが異なる場合は非対応(ObservableCollectionについては発生しないはず)
                            throw new NotSupportedException("Not supported differ in the index before and after the change.");
                        }

                        var oldItem = Views.First(t => oldModels.Contains(t.Model));
                        // ?? が使用できないため3項演算子を使用する
                        var newItem = Views.Any(t => newModels.Contains(t.Model))
                            ? Views.First(t => newModels.Contains(t.Model))
                            : newModels.OfType<T>().Select<T, (T Model, TView View)>(m => (m, Transform(m))).First();

                        var index = e.OldStartingIndex;
                        if (index == -1)
                        {
                            index = Views.IndexOf(oldItem);
                        }

                        Views[index] = newItem;

                        OnPropertyChanged(IndexerName);
                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(newItem, oldItem, index));
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem.View, oldItem.View, index));
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    {
                        var newItems = (e.NewItems?.OfType<T>() ?? []).Select<T, (T Model, TView View)>(m => (m, Transform(m))).ToList();
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
                        var oldItems = (e.OldItems?.OfType<T>() ?? []).Select(m => Views.Find(t => m!.Equals(t.Model))).ToList();
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
