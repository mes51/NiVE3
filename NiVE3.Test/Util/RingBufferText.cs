using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Util;

namespace NiVE3.Test.Util
{
    public class RingBufferText
    {
        [Test]
        public void TestAppendItemUnderCapacity()
        {
            const int Capacity = 10;
            const int AddCount = Capacity - 4;

            var ringBuffer = new RingBuffer<int>(Capacity);

            for (var i = 0; i < AddCount; i++)
            {
                ringBuffer.Append(i);
            }

            Assert.That(ringBuffer, Has.Count.EqualTo(AddCount));
            Assert.That(ringBuffer.ToArray().SequenceEqual(Enumerable.Range(0, AddCount)), Is.True);
        }

        [Test]
        public void TestAppendItemOverCapacity()
        {
            const int Capacity = 10;
            const int AddCount = Capacity + 5;

            var ringBuffer = new RingBuffer<int>(Capacity);

            for (var i = 0; i < AddCount; i++)
            {
                ringBuffer.Append(i);
            }

            Assert.That(ringBuffer, Has.Count.EqualTo(Capacity));
            Assert.That(ringBuffer.ToArray().SequenceEqual(Enumerable.Range(AddCount - Capacity, Capacity)), Is.True);
        }

        [Test]
        public void TestPrependItemUnderCapacity()
        {
            const int Capacity = 10;
            const int AddCount = Capacity - 4;

            var ringBuffer = new RingBuffer<int>(Capacity);

            for (var i = 0; i < AddCount; i++)
            {
                ringBuffer.Prepend(i);
            }

            Assert.That(ringBuffer, Has.Count.EqualTo(AddCount));
            Assert.That(ringBuffer.ToArray().SequenceEqual(Enumerable.Range(0, AddCount).Reverse()), Is.True);
        }

        [Test]
        public void TestPrependItemOverCapacity()
        {
            const int Capacity = 10;
            const int AddCount = Capacity + 5;

            var ringBuffer = new RingBuffer<int>(Capacity);

            for (var i = 0; i < AddCount; i++)
            {
                ringBuffer.Prepend(i);
            }

            Assert.That(ringBuffer, Has.Count.EqualTo(Capacity));
            Assert.That(ringBuffer.ToArray().SequenceEqual(Enumerable.Range(AddCount - Capacity, Capacity).Reverse()), Is.True);
        }

        [Test]
        public void TestRemoveTail()
        {
            const int Capacity = 10;
            const int AddCount = 20;
            const int RemoveCount = 6;

            var ringBuffer = new RingBuffer<int>(Capacity);
            for (var i = 0; i < AddCount; i++)
            {
                ringBuffer.Append(i);
            }

            for (var i = 0; i < RemoveCount; i++)
            {
                ringBuffer.RemoveTail();
            }

            Assert.That(ringBuffer, Has.Count.EqualTo(Capacity - RemoveCount));
            Assert.That(ringBuffer.ToArray().SequenceEqual(Enumerable.Range(AddCount - Capacity, Capacity - RemoveCount)), Is.True);
        }

        [Test]
        public void TestRemoveHead()
        {
            const int Capacity = 10;
            const int AddCount = 20;
            const int RemoveCount = 6;

            var ringBuffer = new RingBuffer<int>(Capacity);
            for (var i = 0; i < AddCount; i++)
            {
                ringBuffer.Append(i);
            }

            for (var i = 0; i < RemoveCount; i++)
            {
                ringBuffer.RemoveHead();
            }

            Assert.That(ringBuffer, Has.Count.EqualTo(Capacity - RemoveCount));
            Assert.That(ringBuffer.ToArray().SequenceEqual(Enumerable.Range(AddCount - Capacity + RemoveCount, Capacity - RemoveCount)), Is.True);
        }
    }
}
