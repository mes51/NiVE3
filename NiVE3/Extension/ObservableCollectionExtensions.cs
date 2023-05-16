using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class ObservableCollectionExtensions
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
                var tx = x is T ? (T)x : (T?)null;
                var ty = y is T ? (T)y : (T?)null;
                return Comparer.Compare(tx, ty);
            }
        }
    }
}
