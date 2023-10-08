using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Mvvm
{
    class ObservableStack<T> : ICollection, IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        Stack<T> Stack { get; } = new Stack<T>();

        public int Count => Stack.Count;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Clear()
        {
            Stack.Clear();
            OnCollectionCleared();
        }

        public bool Contains(T item)
        {
            return Stack.Contains(item);
        }

        public T Peek()
        {
            return Stack.Peek();
        }

        public bool TryPeek([MaybeNullWhen(false)] out T value)
        {
            return Stack.TryPeek(out value);
        }

        public T Pop()
        {
            var result = Stack.Pop();
            OnCollectionPoped(result);
            return result;
        }

        public bool TryPop([MaybeNullWhen(false)] out T value)
        {
            if (Stack.TryPop(out value))
            {
                OnCollectionPoped(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Push(T item)
        {
            Stack.Push(item);
            OnCollectionPushed(item);
        }

        public void CopyTo(T[] array, int index)
        {
            Stack.CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Stack.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Stack.GetEnumerator();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)Stack).CopyTo(array, index);
        }

        void OnCollectionCleared()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnCountChanged();
        }

        void OnCollectionPushed(T newItem)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, Count - 1));
            OnCountChanged();
        }

        void OnCollectionPoped(T oldItem)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, Count));
            OnCountChanged();
        }

        void OnCountChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }
    }
}
