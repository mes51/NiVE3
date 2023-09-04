using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.UI.Extension
{
    // TODO: InternalsVisibleToをつけて別DLLにする?
    static class EnumerableExtensions
    {
        public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }

        public static int IndexOfLast<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            var index = source.Count() - 1;
            foreach (var item in source.Reverse())
            {
                if (predicate(item))
                {
                    return index;
                }
                index--;
            }

            return -1;
        }
    }
}
