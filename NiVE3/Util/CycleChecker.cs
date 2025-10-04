using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    class CycleChecker : IDisposable
    {
        static ConcurrentBag<Entry> Pool { get; } = [];

        static ThreadLocal<CycleChecker?> CurrentChecker { get; set; } = new ThreadLocal<CycleChecker?>();

        List<Entry> Entries { get; } = new List<Entry>();

        int StartCount { get; set; }

        private CycleChecker()
        {
            StartCount++;
        }

        Entry? TryEnterInternal(in Guid objectId)
        {
            return TryEnterInternal(Unsafe.BitCast<Guid, Int128>(objectId));
        }

        Entry? TryEnterInternal(Int128 objectId)
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
            Entries.Remove(entry);
            Pool.Add(entry);
        }

        public static IDisposable StartCheck()
        {
            if (CurrentChecker.Value != null)
            {
                CurrentChecker.Value.StartCount++;
                return CurrentChecker.Value;
            }

            var result = new CycleChecker();
            CurrentChecker.Value = result;
            return result;
        }

        public static IDisposable? TryEnter(in Guid objectId)
        {
            var checker = CurrentChecker.Value;
            if (checker == null)
            {
                throw new InvalidOperationException(); // bug
            }

            return checker.TryEnterInternal(objectId);
        }

        public static IDisposable? TryEnter(in Int128 objectId)
        {
            var checker = CurrentChecker.Value;
            if (checker == null)
            {
                throw new InvalidOperationException(); // bug
            }

            return checker.TryEnterInternal(objectId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Entry Get()
        {
            return Pool.TryTake(out var entry) ? entry : new Entry();
        }

        public void Dispose()
        {
            StartCount--;

            if (StartCount < 1)
            {
                if (Entries.Count > 0)
                {
                    // NOTE: 基本的には全部処理が終わってからDisposeされるはずなのでここには来ないはずだが、来たら修正 or 対応を考える
                    throw new InvalidOperationException("not ended entry found.");
                }

                CurrentChecker.Value = null;
            }
        }

        private class Entry : IDisposable
        {
            public Int128 ObjectId { get; private set; }

            CycleChecker? CurrentChecker { get; set; }

            public void Enter(CycleChecker checker, in Guid objectId)
            {
                if (CurrentChecker != null)
                {
                    throw new InvalidOperationException(); // bug;
                }

                CurrentChecker = checker;
                ObjectId = Unsafe.BitCast<Guid, Int128>(objectId);
            }

            public void Enter(CycleChecker checker, in Int128 objectId)
            {
                if (CurrentChecker != null)
                {
                    throw new InvalidOperationException(); // bug;
                }

                CurrentChecker = checker;
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
                ObjectId = 0;

                checker.Leave(this);
            }
        }
    }
}
