using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NiVE3.Util
{
    class LruCache<TKey, TValue> where TKey : IEquatable<TKey>
    {
        int MaxCount { get; }

        LinkedList<TKey> Keys { get; } = [];

        Dictionary<TKey, TValue> Values { get; } = [];

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                if (Values.ContainsKey(key))
                {
                    Keys.Remove(key);
                    Keys.AddFirst(key);
                    Values[key] = value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
        }

        public LruCache(int maxCount)
        {
            MaxCount = maxCount;
        }

        public void Add(TKey key, TValue value)
        {
            if (!Values.TryAdd(key, value))
            {
                Values[key] = value;
            }

            UpdateKeyOrder(key);
            if (Keys.Count > MaxCount && Keys.Last is LinkedListNode<TKey> last)
            {
                Values.Remove(last.Value);
                Keys.RemoveLast();
            }
        }

        public bool Remove(TKey key)
        {
            if (Values.ContainsKey(key))
            {
                Keys.Remove(key);
                Values.Remove(key);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (Values.TryGetValue(key, out value))
            {
                UpdateKeyOrder(key);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            Keys.Clear();
            Values.Clear();
        }

        void UpdateKeyOrder(TKey key)
        {
            Keys.Remove(key);
            Keys.AddFirst(key);
        }
    }
}
