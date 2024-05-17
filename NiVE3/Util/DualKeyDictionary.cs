using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    class DualKeyDictionary<TPrimary, TSecondary, TValue> : IDictionary<Tuple<TPrimary, TSecondary>, TValue> where TPrimary : notnull where TSecondary : notnull
    {
        public TValue this[Tuple<TPrimary, TSecondary> key]
        {
            get => MainDictionary[key];
            set
            {
                MainDictionary[key] = value;

                var old = PrimaryDictionary[key.Item1];
                PrimaryDictionary[key.Item1] = value;
                SecondaryDictionary[key.Item2].Remove(old);
                SecondaryDictionary[key.Item2].Add(value);
            }
        }

        public TValue this[TPrimary key]
        {
            get => PrimaryDictionary[key];
        }

        public ReadOnlySpan<TValue> this[TSecondary key]
        {
            get => CollectionsMarshal.AsSpan(SecondaryDictionary[key]);
        }

        public ICollection<Tuple<TPrimary, TSecondary>> Keys => MainDictionary.Keys;

        public ICollection<TValue> Values => MainDictionary.Values;

        public int Count => MainDictionary.Count;

        public bool IsReadOnly => false;

        Dictionary<Tuple<TPrimary, TSecondary>, TValue> MainDictionary { get; } = [];

        Dictionary<TPrimary, TValue> PrimaryDictionary { get; } = [];

        Dictionary<TSecondary, List<TValue>> SecondaryDictionary { get; } = [];

        Dictionary<TSecondary, List<TPrimary>> KeyLookUpDictionary { get; } = [];

        public void Add(Tuple<TPrimary, TSecondary> key, TValue value)
        {
            if (PrimaryDictionary.ContainsKey(key.Item1))
            {
                throw new ArgumentException($"duplicate primary key: {key.Item1}");
            }

            MainDictionary.Add(key, value);
            PrimaryDictionary.Add(key.Item1, value);

            if (!SecondaryDictionary.ContainsKey(key.Item2))
            {
                SecondaryDictionary.Add(key.Item2, []);
                KeyLookUpDictionary.Add(key.Item2, []);
            }
            SecondaryDictionary[key.Item2].Add(value);
            KeyLookUpDictionary[key.Item2].Add(key.Item1);
        }

        public void Add(KeyValuePair<Tuple<TPrimary, TSecondary>, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(TPrimary primaryKey, TSecondary secondaryKey, TValue value)
        {
            Add(Tuple.Create(primaryKey, secondaryKey), value);
        }

        public void Clear()
        {
            MainDictionary.Clear();
            PrimaryDictionary.Clear();
            SecondaryDictionary.Clear();
        }

        public bool Contains(KeyValuePair<Tuple<TPrimary, TSecondary>, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public bool ContainsKey(Tuple<TPrimary, TSecondary> key)
        {
            return MainDictionary.ContainsKey(key);
        }

        public bool ContainsKey(TPrimary key)
        {
            return PrimaryDictionary.ContainsKey(key);
        }

        public bool ContainsKey(TSecondary key)
        {
            return SecondaryDictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<Tuple<TPrimary, TSecondary>, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<Tuple<TPrimary, TSecondary>, TValue>>)MainDictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<Tuple<TPrimary, TSecondary>, TValue>> GetEnumerator()
        {
            return MainDictionary.GetEnumerator();
        }

        public bool Remove(Tuple<TPrimary, TSecondary> key)
        {
            if (MainDictionary.Remove(key))
            {
                PrimaryDictionary.Remove(key.Item1);
                SecondaryDictionary.Remove(key.Item2);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Remove(KeyValuePair<Tuple<TPrimary, TSecondary>, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(Tuple<TPrimary, TSecondary> key, [MaybeNullWhen(false)] out TValue value)
        {
            return MainDictionary.TryGetValue(key, out value);
        }

        public bool TryGetValue(TPrimary key, [MaybeNullWhen(false)] out TValue value)
        {
            return PrimaryDictionary.TryGetValue(key, out value);
        }

        public bool TryGetValues(TSecondary key, out ReadOnlySpan<TValue> values)
        {
            if (SecondaryDictionary.TryGetValue(key, out var secondaryValues))
            {
                values = CollectionsMarshal.AsSpan(secondaryValues);
                return true;
            }
            else
            {
                values = [];
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
