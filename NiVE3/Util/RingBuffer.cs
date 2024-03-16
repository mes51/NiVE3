using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    class RingBuffer<T> : IEnumerable<T>
    {
        public int Capacity { get; }

        public int Count { get; private set; }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return Buffer[(Head + index) % Capacity];
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                Buffer[(Head + index) % Capacity] = value;
            }
        }

        T[] Buffer { get; }

        int Head { get; set; }

        public RingBuffer(int capacity)
        {
            Capacity = capacity;
            Buffer = new T[Capacity];
        }

        public void Clear()
        {
            Buffer.AsSpan().Clear();
            Head = 0;
            Count = 0;
        }

        public void Append(T item)
        {
            Buffer[(Head + Count) % Capacity] = item;
            if (Count >= Capacity)
            {
                Head = (Head + 1) % Capacity;
            }
            else
            {
                Count++;
            }
        }

        public void Prepend(T item)
        {
            Head = Head > 0 ? Head - 1 : Capacity - 1;
            if (Count < Capacity)
            {
                Count++;
            }
            Buffer[Head] = item;
        }

        public bool RemoveTail()
        {
            if (Count < 1)
            {
                return false;
            }
            else
            {
                Count--;
                return true;
            }
        }

        public bool RemoveHead()
        {
            if (Count < 1)
            {
                return false;
            }
            else
            {
                Head = (Head + 1) % Capacity;
                Count--;
                return true;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
