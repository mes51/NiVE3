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
    class DualKeyDictionary<TUpdateKey, TPrimarySub, TSecondary, TValue> : IDictionary<((TUpdateKey, TPrimarySub), TSecondary), TValue> where TUpdateKey : notnull where TPrimarySub : notnull where TSecondary : notnull
    {
        public TValue this[((TUpdateKey, TPrimarySub), TSecondary) key]
        {
            get => MainDictionary[key];
            set => throw new NotSupportedException($"use {nameof(Update)}");
        }

        public TValue this[TUpdateKey updateKey, TPrimarySub subKey]
        {
            get => PrimaryDictionary[(updateKey, subKey)];
        }

        public ReadOnlySpan<TValue> this[TSecondary key]
        {
            get => CollectionsMarshal.AsSpan(SecondaryDictionary[key]);
        }

        public ICollection<((TUpdateKey, TPrimarySub), TSecondary)> Keys => MainDictionary.Keys;

        public ICollection<TValue> Values => MainDictionary.Values;

        public int Count => MainDictionary.Count;

        public bool IsReadOnly => false;

        Dictionary<((TUpdateKey, TPrimarySub), TSecondary), TValue> MainDictionary { get; } = [];

        Dictionary<(TUpdateKey, TPrimarySub), TValue> PrimaryDictionary { get; } = [];

        Dictionary<TSecondary, List<TValue>> SecondaryDictionary { get; } = [];

        Dictionary<TUpdateKey, List<TPrimarySub>> UpdateTargetKeys { get; } = [];

        Dictionary<(TUpdateKey, TPrimarySub), TSecondary> SecondaryKeys { get; } = [];

        public void Add(((TUpdateKey, TPrimarySub), TSecondary) key, TValue value)
        {
            if (PrimaryDictionary.ContainsKey(key.Item1))
            {
                throw new ArgumentException($"duplicate primary key: {key.Item1}");
            }

            MainDictionary.Add(key, value);
            PrimaryDictionary.Add(key.Item1, value);
            SecondaryKeys.Add(key.Item1, key.Item2);

            if (!SecondaryDictionary.ContainsKey(key.Item2))
            {
                SecondaryDictionary.Add(key.Item2, []);
            }
            SecondaryDictionary[key.Item2].Add(value);

            if (!UpdateTargetKeys.ContainsKey(key.Item1.Item1))
            {
                UpdateTargetKeys.Add(key.Item1.Item1, []);
            }
            UpdateTargetKeys[key.Item1.Item1].Add(key.Item1.Item2);
        }

        public void Add(KeyValuePair<((TUpdateKey, TPrimarySub), TSecondary), TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(TUpdateKey updateKey, TPrimarySub subKey, TSecondary secondaryKey, TValue value)
        {
            Add(((updateKey, subKey), secondaryKey), value);
        }

        public void Clear()
        {
            MainDictionary.Clear();
            PrimaryDictionary.Clear();
            SecondaryDictionary.Clear();
            UpdateTargetKeys.Clear();
            SecondaryKeys.Clear();
        }

        public bool Contains(KeyValuePair<((TUpdateKey, TPrimarySub), TSecondary), TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public bool ContainsKey(((TUpdateKey, TPrimarySub), TSecondary) key)
        {
            return MainDictionary.ContainsKey(key);
        }

        public bool ContainsKey(TUpdateKey updateKey, TPrimarySub subKey)
        {
            return PrimaryDictionary.ContainsKey((updateKey, subKey));
        }

        public bool ContainsKey(TSecondary key)
        {
            return SecondaryDictionary.TryGetValue(key, out var secondaryValues) && secondaryValues.Count > 0;
        }

        public void CopyTo(KeyValuePair<((TUpdateKey, TPrimarySub), TSecondary), TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<((TUpdateKey, TPrimarySub), TSecondary), TValue>>)MainDictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<((TUpdateKey, TPrimarySub), TSecondary), TValue>> GetEnumerator()
        {
            return MainDictionary.GetEnumerator();
        }

        public bool Remove(((TUpdateKey, TPrimarySub), TSecondary) key)
        {
            if (MainDictionary.ContainsKey(key))
            {
                var value = MainDictionary[key];
                MainDictionary.Remove(key);
                PrimaryDictionary.Remove(key.Item1);
                SecondaryDictionary[key.Item2].Remove(value);
                UpdateTargetKeys[key.Item1.Item1].Remove(key.Item1.Item2);
                SecondaryKeys.Remove(key.Item1);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Remove(KeyValuePair<((TUpdateKey, TPrimarySub), TSecondary), TValue> item)
        {
            return Remove(item.Key);
        }

        public bool Remove(TUpdateKey updateKey, TPrimarySub subKey)
        {
            var primayKey = (updateKey, subKey);
            var secondaryKey = SecondaryKeys[primayKey];
            return Remove((primayKey, secondaryKey));
        }

        public bool Remove(TUpdateKey updateKey)
        {
            var result = false;
            foreach (var k in UpdateTargetKeys[updateKey].ToArray())
            {
                result |= Remove(updateKey, k);
            }

            return result;
        }

        public bool Remove(TSecondary secondaryKey)
        {
            var result = false;

            var comparer = EqualityComparer<TSecondary>.Default;
            foreach (var k in SecondaryKeys.Where(kv => comparer.Equals(kv.Value, secondaryKey)).ToArray())
            {
                result = Remove(k.Key.Item1, k.Key.Item2);
            }

            return result;
        }

        public bool TryGetValue(((TUpdateKey, TPrimarySub), TSecondary) key, [MaybeNullWhen(false)] out TValue value)
        {
            return MainDictionary.TryGetValue(key, out value);
        }

        public bool TryGetValue(TUpdateKey updateKey, TPrimarySub subKey, [MaybeNullWhen(false)] out TValue value)
        {
            return PrimaryDictionary.TryGetValue((updateKey, subKey), out value);
        }

        public bool TryGetValues(TSecondary key, out ReadOnlySpan<TValue> values)
        {
            if (SecondaryDictionary.TryGetValue(key, out var secondaryValues) && secondaryValues.Count > 0)
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

        public bool ContainsUpdateKey(TUpdateKey updateKey)
        {
            return UpdateTargetKeys.TryGetValue(updateKey, out var subKeys) && subKeys.Count > 0;
        }

        public TPrimarySub[] GetUpdateTargetKeys(TUpdateKey updateKey)
        {
            return UpdateTargetKeys[updateKey].ToArray();
        }

        public void Update(TUpdateKey updateKey, TPrimarySub subKey, TSecondary secondaryKey, TValue value)
        {
            Remove(updateKey, subKey);
            Add(updateKey, subKey, secondaryKey, value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
