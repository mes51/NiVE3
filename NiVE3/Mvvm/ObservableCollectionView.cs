using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Mvvm
{
    partial class ObservableCollectionView<T, TView> : IList, IList<TView>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        static readonly object SyncObj = new object();

        const string IndexerName = "Item[]";

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<NotifyCollectionViewChangedEventArgs<T, TView>>? ViewsCollectionChanged;

        Func<T, TView> Transform { get; }

        List<(T Model, TView View)> Views { get; }

        bool Disposed { get; set; }

        public int Count => Views.Count;

        public bool IsReadOnly => true;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => SyncObj;

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
            Transform = transform;
            Views = models.Select<T, (T Model, TView View)>(m => (m, transform(m))).ToList();
            BindCollectionChanged(models);
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
            throw new NotImplementedException();
        }

        public void Add(TView item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

        partial void BindCollectionChanged(ObservableCollection<T> models);

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
                throw new ArgumentException(null, nameof(action));
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
                throw new ArgumentException(null, nameof(action));
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
                throw new ArgumentException(null, nameof(action));
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
            ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
            Action = NotifyCollectionChangedAction.Move;
            Added = new List<(T Model, TView View)> { item }.AsReadOnly();
            Removed = new List<(T Model, TView View)> { item }.AsReadOnly();
            OldStartingIndex = oldIndex;
            NewStartingIndex = newIndex;
        }

        public NotifyCollectionViewChangedEventArgs(IList<(T Model, TView View)> items, int newIndex, int oldIndex = -1)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
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
