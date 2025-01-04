using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Cache
{
    class PropertyValueCache : IDisposable
    {
        static PropertyValueCache? CurrentCache { get; set; }

        static int CacheStartCount { get; set; }

        Dictionary<(Int128, Time), object?> ValueCache { get; } = [];

        public static IDisposable Start()
        {
            CurrentCache ??= new PropertyValueCache();
            CacheStartCount++;

            return CurrentCache;
        }

        public static bool TryGet<T>(in Int128 objectId, in Time time, out T? value) where T : class
        {
            if (CurrentCache == null)
            {
                value = null;
                return false;
            }

            if (CurrentCache.ValueCache.TryGetValue((objectId, time), out var result) && result is T casted)
            {
                value = casted;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public static void Upsert(in Int128 objectId, in Time time, object? value)
        {
            if (CurrentCache != null)
            {
                var key = (objectId, time);
                if (!CurrentCache.ValueCache.TryAdd(key, value))
                {
                    CurrentCache.ValueCache[key] = value;
                }
            }
        }

        public void Dispose()
        {
            CacheStartCount--;
            if (CacheStartCount < 1)
            {
                CurrentCache = null;
            }
        }
    }
}
