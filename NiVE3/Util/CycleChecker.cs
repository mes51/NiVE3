using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    class CycleChecker : IDisposable
    {
        static readonly object SyncObject = new object();

        static ConcurrentBag<Entry> Pool { get; } = [];

        static CycleChecker? CurrentChecker { get; set; }

        List<Entry> Entries { get; } = new List<Entry>();

        private CycleChecker() { }

        Entry? TryEnter(Guid objectId)
        {
            if (Entries.Any(e => e.ObjectId == objectId))
            {
                return null;
            }
            else
            {
                var entry = Get();
                entry.Enter(this, objectId);
                Entries.Add(entry);

                return entry;
            }
        }

        void Leave(Entry entry)
        {
            lock (SyncObject)
            {
                Entries.Remove(entry);
                Pool.Add(entry);
            }
        }

        public static IDisposable StartCheck()
        {
            if (CurrentChecker != null)
            {
                throw new InvalidOperationException(); // bug
            }

            CurrentChecker = new CycleChecker();
            return CurrentChecker;
        }

        public static IDisposable? TryEnter(in Guid objectId)
        {
            if (CurrentChecker == null)
            {
                throw new InvalidOperationException(); // bug
            }

            return CurrentChecker.TryEnter(objectId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Entry Get()
        {
            return Pool.TryTake(out var entry) ? entry : new Entry();
        }

        public void Dispose()
        {
            foreach (var e in Entries.ToArray())
            {
                e.Dispose();
            }

            CurrentChecker = null;
        }

        private class Entry : IDisposable
        {
            public Guid ObjectId { get; private set; }

            CycleChecker? CurrentChecker { get; set; }

            public void Enter(CycleChecker chekcer, Guid objectId)
            {
                if (CurrentChecker != null)
                {
                    throw new InvalidOperationException(); // bug;
                }

                CurrentChecker = chekcer;
                ObjectId = objectId;
            }

            public void Dispose()
            {
                if (CurrentChecker == null)
                {
                    throw new InvalidOperationException(); // bug;
                }

                var checker = CurrentChecker;
                CurrentChecker = null;
                ObjectId = Guid.Empty;

                checker.Leave(this);
            }
        }
    }
}
