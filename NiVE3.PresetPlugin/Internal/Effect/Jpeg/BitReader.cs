using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Effect.Jpeg
{
    class BitReader
    {
        uint[] Buffer { get; set; }

        int Position { get; set; }

        int BitPosition { get; set; }

        public bool IsEnd => Position >= Buffer.Length;

        public BitReader(uint[] buffer)
        {
            Buffer = buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int runLength, int value) ReadHuffmanCode(HuffmanTable table)
        {
            const int InvalidCodeAdvanceBits = 8; // TODO: パフォーマンスと相談

            while (!IsEnd)
            {
                var (useBit, runLength, value) = table.GetValue(Peek());
                if (runLength > -1)
                {
                    AdvanceBits(useBit);
                    return (runLength, value);
                }

                AdvanceBits(InvalidCodeAdvanceBits);
            }

            throw new IndexOutOfRangeException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AdvanceBits(int bits)
        {
            BitPosition += bits;
            while (BitPosition >= 32)
            {
                Position++;
                BitPosition -= 32;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint Peek()
        {
            if (BitPosition == 0)
            {
                return Buffer[Position];
            }

            var current = Buffer[Position];
            var next = Position >= Buffer.Length - 1 ? 0U : Buffer[Position + 1];
            var nextBits = 32 - BitPosition;

            var currentMask = 0xFFFFFFFFU >> BitPosition;
            var nextMask = 0xFFFFFFFFU >> (nextBits);
            return ((current >> BitPosition) & currentMask) | ((next & nextMask) << nextBits);
        }
    }
}
