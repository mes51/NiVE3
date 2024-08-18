using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shared.Extension
{
    public static class EnumerableExtensions
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

        public static IEnumerable<(T, int)> ZipWithIndex<T>(this IEnumerable<T> source)
        {
            var index = 0;
            foreach (var e in source)
            {
                yield return (e, index);
                index++;
            }
        }

        public static IEnumerable<T> NonNull<T>(this IEnumerable<T?> source)
        {
            foreach (var v in source)
            {
                if (v != null)
                {
                    yield return v;
                }
            }
        }

        public static IEnumerable<T> NonNull<T>(this IEnumerable<T?> source) where T : struct
        {
            foreach (var v in source)
            {
                if (v != null)
                {
                    yield return v.Value;
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> GroupByPrev<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector) where T : notnull where TKey : IEquatable<TKey>
        {
            var key = default(TKey);
            var group = new List<T>();
            foreach (var e in source)
            {
                var currentKey = keySelector(e);
                if (group.Count > 0 && !currentKey.Equals(key))
                {
                    yield return group;
                    group = [];
                }

                group.Add(e);
                key = currentKey;
            }

            if (group.Count > 0)
            {
                yield return group;
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            switch (source)
            {
                case T[] array:
                    return Array.IndexOf(array, value);
                case List<T> list:
                    return list.IndexOf(value);
                default:
                    {
                        var index = 0;
                        var eq = EqualityComparer<T>.Default;
                        foreach (var e in source)
                        {
                            if (eq.Equals(e, value))
                            {
                                return index;
                            }
                            index++;
                        }

                        return -1;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> RepeatInfinity<T>(this IEnumerable<T> source)
        {
            switch (source)
            {
                case T[] array:
                    for (var i = 0; ; i = (i + 1) % array.Length)
                    {
                        yield return array[i];
                    }
                case IList<T> list:
                    for (var i = 0; ; i = (i + 1) % list.Count)
                    {
                        yield return list[i];
                    }
                case IReadOnlyList<T> list:
                    for (var i = 0; ; i = (i + 1) % list.Count)
                    {
                        yield return list[i];
                    }
                default:
                    while (true)
                    {
                        foreach (var e in source)
                        {
                            yield return e;
                        }
                    }
            }
        }
    }
}
