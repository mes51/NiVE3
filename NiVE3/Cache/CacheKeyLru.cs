using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Cache
{
    class CacheKeyLru
    {
        Dictionary<(Guid, Int128, double), LinkedListNode<(Guid, Int128, double)>> PrimaryKeys { get; } = [];

        Dictionary<(Guid, Int128), List<(Guid, Int128, double)>> SecondaryKeys { get; } = [];

        LinkedList<(Guid, Int128, double)> PrimaryKeyLru { get; } = [];

        public CacheKeyLru() { }

        public void Add(in Guid objectId, in Int128 key, double time)
        {
            var primaryKey = (objectId, key, time);
            if (!PrimaryKeys.TryGetValue(primaryKey, out var primaryKeyNode))
            {
                primaryKeyNode = new LinkedListNode<(Guid, Int128, double)>(primaryKey);
                PrimaryKeys.Add(primaryKey, primaryKeyNode);
                var secondaryKey = (objectId, key);
                if (SecondaryKeys.TryGetValue(secondaryKey, out var primaryKeys))
                {
                    primaryKeys.Add(primaryKey);
                }
                else
                {
                    SecondaryKeys.Add(secondaryKey, [primaryKey]);
                }
            }
            PrimaryKeyLru.Remove(primaryKey);
            PrimaryKeyLru.AddFirst(primaryKeyNode);
        }

        public void UpdateLastAccessBySecondaryKey(in Guid objectId, in Int128 key)
        {
            if (SecondaryKeys.TryGetValue((objectId, key), out var primaryKeys))
            {
                foreach (var p in primaryKeys)
                {
                    var primaryKeyNode = PrimaryKeys[p];
                    PrimaryKeyLru.Remove(primaryKeyNode);
                    PrimaryKeyLru.AddFirst(primaryKeyNode);
                }
            }
        }

        public (Guid, Int128, double) RemoveLast()
        {
            var node = PrimaryKeyLru.Last;
            if (node != null)
            {
                PrimaryKeyLru.RemoveLast();

                var result = node.Value;
                PrimaryKeys.Remove(result);
                if (SecondaryKeys.TryGetValue((result.Item1, result.Item2), out var primaryKeys))
                {
                    primaryKeys.RemoveAll(p => p.Item3 == result.Item3);
                }

                return result;
            }
            else
            {
                return (Guid.Empty, 0, 0.0);
            }
        }

        public void Remove(Guid objectId, Int128 key, double time)
        {
            var primaryKey = (objectId, key, time);
            if (PrimaryKeys.TryGetValue(primaryKey, out var primaryKeyNode))
            {
                PrimaryKeyLru.Remove(primaryKeyNode);
                var secondaryKey = (objectId, key);
                PrimaryKeys.Remove(primaryKey);
                if (SecondaryKeys.TryGetValue(secondaryKey, out var primaryKeys))
                {
                    primaryKeys.RemoveAll(p => p.Item3 == time);
                }
            }
        }

        public void Clear()
        {
            PrimaryKeys.Clear();
            SecondaryKeys.Clear();
            PrimaryKeyLru.Clear();
        }
    }
}
