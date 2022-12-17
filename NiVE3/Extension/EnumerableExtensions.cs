using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Extension
{
    static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Grouped<T>(this IEnumerable<T> source, int count)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (true)
                {
                    var result = new T[count];
                    var size = 0;
                    for (; size < count && enumerator.MoveNext(); size++)
                    {
                        result[size] = enumerator.Current;
                    }
                    if (size > 0)
                    {
                        if (size < count)
                        {
                            Array.Resize(ref result, size);
                        }
                        yield return result;
                    }
                    if (size < count)
                    {
                        break;
                    }
                }
            }
        }
    }
}
