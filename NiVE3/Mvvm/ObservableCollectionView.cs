using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImTools;

namespace NiVE3.Mvvm
{
    class ObservableCollectionView<T, TView> : IList, IList<TView>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
    {
        const string IndexerName = "Item[]";

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<NotifyCollectionViewChangedEventArgs<T, TView>>? ViewsCollectionChanged;

        ObservableCollection<T> Models { get; }

        Func<T, TView> Transform { get; }

        List<(T Model, TView View)> Views { get; }

        bool Disposed { get; set; }

        public int Count => Views.Count;

        public bool IsReadOnly => false;

        public bool IsFixedSize => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        object? IList.this[int index]
        {
            get => Views[index].View;
            set => throw new NotImplementedException();
        }

        public TView this[int index]
        {
            get => Views[index].View;
            set => throw new NotImplementedException();
        }

        public ObservableCollectionView(ObservableCollection<T> models, Func<T, TView> transform)
        {
            Models = models;
            Transform = transform;
            Views = Models.Select<T, (T Model, TView View)>(m => (m, transform(m))).ToList();
            models.CollectionChanged += Models_CollectionChanged;
        }

        public int IndexOf(TView item)
        {
            return item != null ? Views.FindIndex(t => item.Equals(t.View)) : -1;
        }

        public void Insert(int index, TView item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            var removed = Views[index];
            Views.RemoveAt(index);
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            OnCollectionItemRemoved(removed, index);
        }

        public void Add(TView item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            Views.Clear();
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            OnCollectionCleared();
        }

        public bool Contains(TView item)
        {
            return item != null && Views.Any(t => item.Equals(t.View));
        }

        public void CopyTo(TView[] array, int arrayIndex)
        {
            var viewArray = Views.Select(t => t.View).ToArray();
            viewArray.CopyTo(array, arrayIndex);
        }

        public bool Remove(TView item)
        {
            var index = IndexOf(item);
            if (index > -1)
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public int Add(object? value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object? value)
        {
            if (value is TView view)
            {
                return Contains(view);
            }
            else
            {
                return false;
            }
        }

        public int IndexOf(object? value)
        {
            if (value is TView view)
            {
                return IndexOf(view);
            }
            else
            {
                return -1;
            }
        }

        public void Insert(int index, object? value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object? value)
        {
            if (value is TView view)
            {
                Remove(view);
            }
        }

        public void CopyTo(Array array, int index)
        {
            if (array is TView[] viewArray)
            {
                CopyTo(viewArray, index);
            }
        }

        public IEnumerator<TView> GetEnumerator()
        {
            return Views.Select(t => t.View).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Views.Select(t => t.View).GetEnumerator();
        }

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void OnCollectionItemRemoved((T Model, TView View) removed, int index)
        {
            ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(NotifyCollectionChangedAction.Remove, removed, index));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed.View, index));
        }

        void OnCollectionCleared()
        {
            ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(NotifyCollectionChangedAction.Reset));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void Models_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                        var oldStartIndex = items.IndexOf(items[0]);
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
                        var newItems = (e.NewItems as IEnumerable<T> ?? Enumerable.Empty<T>()).Select<T, (T Model, TView View)>(m => (m, Transform(m))).ToList();
                        var newIndex = e.NewStartingIndex > -1 ? e.NewStartingIndex : Views.Count;
                        foreach (var i in newItems)
                        {
                            Views.Insert(newIndex, i);
                            newIndex++;
                        }

                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(NotifyCollectionChangedAction.Add, newItems, e.NewStartingIndex));
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems.Select(t => t.View).ToList(), e.NewStartingIndex));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        var oldItems = (e.OldItems as IEnumerable<T> ?? Enumerable.Empty<T>()).Select(m => Views.Find(t => m.Equals(t.Model))).ToList();
                        var oldIndex = oldItems.Count > 0 ? Views.IndexOf(oldItems[0]) : -1;
                        foreach (var i in oldItems)
                        {
                            Views.Remove(i);
                        }

                        ViewsCollectionChanged?.Invoke(this, new NotifyCollectionViewChangedEventArgs<T, TView>(NotifyCollectionChangedAction.Remove, oldItems, oldIndex));
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems.Select(t => t.View).ToList(), oldIndex));
                    }
                    break;
            }
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Models.CollectionChanged -= Models_CollectionChanged;
                Disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~ObservableCollectionView()
        {
            Dispose();
        }
    }

    class NotifyCollectionViewChangedEventArgs<T, TView> : EventArgs
    {
        public NotifyCollectionChangedAction Action { get; }

        public IReadOnlyList<(T Model, TView View)>? Added { get; }

        public IReadOnlyList<(T Model, TView View)>? Removed { get; }

        public int OldStartingIndex { get; } = -1;

        public int NewStartingIndex { get; } = -1;

        public NotifyCollectionViewChangedEventArgs(NotifyCollectionChangedAction action)
        {
            if (action != NotifyCollectionChangedAction.Reset)
            {
                throw new ArgumentException(nameof(action));
            }
            Action = action;
        }

        public NotifyCollectionViewChangedEventArgs(NotifyCollectionChangedAction action, (T Model, TView View) item, int index = -1)
        {
            if (action == NotifyCollectionChangedAction.Add)
            {
                Added = new List<(T Model, TView View)> { item }.AsReadOnly();
                NewStartingIndex = index;
            }
            else if (action == NotifyCollectionChangedAction.Remove)
            {
                Removed = new List<(T Model, TView View)> { item }.AsReadOnly();
                OldStartingIndex = index;
            }
            else
            {
                throw new ArgumentException(nameof(action));
            }
            Action = action;
        }

        public NotifyCollectionViewChangedEventArgs(NotifyCollectionChangedAction action, IList<(T Model, TView View)> items, int index = -1)
        {
            if (action == NotifyCollectionChangedAction.Add)
            {
                Added = items.AsReadOnly();
                NewStartingIndex = index;
            }
            else if (action == NotifyCollectionChangedAction.Remove)
            {
                Removed = items.AsReadOnly();
                OldStartingIndex = index;
            }
            else
            {
                throw new ArgumentException(nameof(action));
            }
            Action = action;
        }

        public NotifyCollectionViewChangedEventArgs((T Model, TView View) newItem, (T Model, TView View) oldItem, int index = -1)
        {
            Action = NotifyCollectionChangedAction.Replace;
            Added = new List<(T Model, TView View)> { newItem }.AsReadOnly();
            Removed = new List<(T Model, TView View)> { oldItem }.AsReadOnly();
            OldStartingIndex = index;
            NewStartingIndex = index;
        }

        public NotifyCollectionViewChangedEventArgs(IList<(T Model, TView View)> newItems, IList<(T Model, TView View)> oldItems, int index = -1)
        {
            Action = NotifyCollectionChangedAction.Replace;
            Added = newItems.AsReadOnly();
            Removed = oldItems.AsReadOnly();
            OldStartingIndex = index;
            NewStartingIndex = index;
        }

        public NotifyCollectionViewChangedEventArgs((T Model, TView View) item, int newIndex, int oldIndex = -1)
        {
            if (newIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newIndex));
            }
            Action = NotifyCollectionChangedAction.Move;
            Added = new List<(T Model, TView View)> { item }.AsReadOnly();
            Removed = new List<(T Model, TView View)> { item }.AsReadOnly();
            OldStartingIndex = oldIndex;
            NewStartingIndex = newIndex;
        }

        public NotifyCollectionViewChangedEventArgs(IList<(T Model, TView View)> items, int newIndex, int oldIndex = -1)
        {
            if (newIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newIndex));
            }
            Action = NotifyCollectionChangedAction.Move;
            Added = items.AsReadOnly();
            Removed = items.AsReadOnly();
            OldStartingIndex = oldIndex;
            NewStartingIndex = newIndex;
        }
    }

    static class ObservableCollectionViewExtensions
    {
        public static ObservableCollectionView<T, TView> CreateViewCollection<T, TView>(this ObservableCollection<T> models, Func<T, TView> transform)
        {
            return new ObservableCollectionView<T, TView>(models, transform);
        }
    }
}
