using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NiVE3.Extension
{
    static class EnumerableExtensions
    {
        public static ICollectionView CreateCollectionView<T, TKey>(this IEnumerable<T> collection, Func<TKey?> key, Func<T, TKey, bool> predicate)
        {
            var view = CollectionViewSource.GetDefaultView(collection);
            view.Filter = item =>
            {
                var keyValue = key();
                if (keyValue == null)
                {
                    return true;
                }

                if (item is not T v)
                {
                    return false;
                }

                return predicate(v, keyValue);
            };
            return view;
        }

        public static IEnumerable<IEnumerable<T>> GroupWhile<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return source.GroupWhile(predicate, true);
        }

        public static IEnumerable<IEnumerable<T>> GroupWhile<T>(this IEnumerable<T> source, Func<T, bool> predicate, bool excludeEmptyGroup)
        {
            var group = new List<T>();
            foreach (var e in source)
            {
                if (predicate(e))
                {
                    group.Add(e);
                }
                else if (group.Count > 0 || !excludeEmptyGroup)
                {
                    yield return group;
                    group = [];
                }
            }

            if (group.Count > 0 || !excludeEmptyGroup)
            {
                yield return group;
            }
        }
    }
}
