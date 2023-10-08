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
    }
}
