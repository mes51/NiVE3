using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shared.Extension
{
    public static class DictionaryExtensions
    {
        public static bool TryGetValue<TKey, TValue, TResult>(this IDictionary<TKey, TValue> dictionary, TKey key, [NotNullWhen(true)] out TResult? value)
        {
            if (!dictionary.TryGetValue(key, out var result))
            {
                value = default;
                return false;
            }
            if (result is TResult tr)
            {
                value = tr;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
