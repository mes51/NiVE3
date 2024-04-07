using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shared.Extension
{
    public static class ObservableCollectionExtensions
    {
        public static void Sort<T>(this ObservableCollection<T> collection, IComparer<T> comparer) where T : class
        {
            ArrayList.Adapter(collection).Sort(new ComparerWrapper<T>(comparer));
        }

        public static void Sort<T>(this ObservableCollection<T> collection, IComparer<T?> comparer) where T : struct
        {
            ArrayList.Adapter(collection).Sort(new ComparerStructWrapper<T>(comparer));
        }

        public static void Sort<T>(this ObservableCollection<T> collection, IComparer comparer)
        {
            ArrayList.Adapter(collection).Sort(comparer);
        }

        public static void SortBy<T>(this ObservableCollection<T> collection, Func<T, int> sortKey)
        {
            var sorted = collection.OrderBy(e => sortKey(e)).ToArray();
            for (var i = 0; i < sorted.Length; i++)
            {
                collection.Move(collection.IndexOf(sorted[i]), i);
            }
        }

        public static void SortBy<T, TKey>(this ObservableCollection<T> collection, Func<T, TKey> sortKey)
        {
            var keys = collection.Select(v => (v,  sortKey(v))).OrderBy(t => t.Item2).ToArray();
            for (var i = 0; i < keys.Length; i++)
            {
                collection.Move(collection.IndexOf(keys[i].v), i);
            }
        }

        private class ComparerWrapper<T> : IComparer where T : class
        {
            IComparer<T> Comparer { get; }

            public ComparerWrapper(IComparer<T> comparer)
            {
                Comparer = comparer;
            }

            public int Compare(object? x, object? y)
            {
                return Comparer.Compare(x as T, y as T);
            }
        }

        private class ComparerStructWrapper<T> : IComparer where T : struct
        {
            IComparer<T?> Comparer { get; }

            public ComparerStructWrapper(IComparer<T?> comparer)
            {
                Comparer = comparer;
            }

            public int Compare(object? x, object? y)
            {
                var tx = x is T tcx ? tcx : (T?)null;
                var ty = y is T tcy ? tcy : (T?)null;
                return Comparer.Compare(tx, ty);
            }
        }

        private class SortByComparerWrapper<T> : IComparer where T : notnull
        {
            Dictionary<T, int> SortKeys { get; }

            public SortByComparerWrapper(Dictionary<T, int> sortKeys)
            {
                SortKeys = sortKeys;
            }

            public int Compare(object? x, object? y)
            {
                if (x == null)
                {
                    if (y != null)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (y is null)
                {
                    return 1;
                }

                return SortKeys[(T)x].CompareTo(SortKeys[(T)y]);
            }
        }
    }
}
