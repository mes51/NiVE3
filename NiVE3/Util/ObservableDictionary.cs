using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyPropertyChanged where TKey : notnull
    {
        const string IndexerName = "Item[]";

        public TValue this[TKey key]
        {
            get
            {
                return Dictionary[key];
            }
            set
            {
                Dictionary[key] = value;
                OnPropertyChanged(IndexerName);
            }
        }

        public ICollection<TKey> Keys => Dictionary.Keys;

        public ICollection<TValue> Values => Dictionary.Values;

        public int Count => Dictionary.Count;

        public bool IsReadOnly => false;

        public event PropertyChangedEventHandler? PropertyChanged;

        Dictionary<TKey, TValue> Dictionary { get; }

        public ObservableDictionary() : this(new Dictionary<TKey, TValue>()) { }

        public ObservableDictionary(IDictionary<TKey, TValue> dic)
        {
            Dictionary = new Dictionary<TKey, TValue>(dic);
        }

        public void Add(TKey key, TValue value)
        {
            Dictionary.Add(key, value);
            OnPropertyChanged(IndexerName);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Dictionary.Add(item.Key, item.Value);
            OnPropertyChanged(IndexerName);
        }

        public void Clear()
        {
            Dictionary.Clear();
            OnPropertyChanged(IndexerName);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            var index = arrayIndex;
            foreach (var item in Dictionary)
            {
                array[index] = item;
                index++;
                if (index >= array.Length)
                {
                    break;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            if (Dictionary.Remove(key))
            {
                OnPropertyChanged(IndexerName);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
