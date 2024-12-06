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

        private CycleChecker() { }

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
            // TODO: 今後呼び出し階層が深くなると地獄が見えるので、何度も呼び出し可能にした上でDispose時に抜けていないEntryがないかどうか、checkerに名前を付けてデバッグしやすくする程度にする
            if (CurrentChecker.Value != null)
            {
                throw new InvalidOperationException(); // bug
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
            foreach (var e in Entries.ToArray())
            {
                e.Dispose();
            }

            CurrentChecker.Value = null;
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
