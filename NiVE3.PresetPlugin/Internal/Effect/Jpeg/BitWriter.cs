using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Effect.Jpeg
{
    class BitWriter(int initialCapacity)
    {
        uint[] Buffer { get; set; } = new uint[initialCapacity];

        uint RemainData { get; set; }

        int RemainBit { get; set; } = 32;

        int Position { get; set; }

        public int WritedLength => Position + (RemainBit != 32 ? 1 : 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteHuffmanCode(int runLength, int value, HuffmanTable table)
        {
            if (runLength > 14)
            {
                var (zrlBits, zrlCode) = table.GetCode(15, 0);
                while (runLength >= 16)
                {
                    Write(zrlCode, zrlBits);
                    runLength -= 16;
                }
            }

            var (bits, code) = table.GetCode(runLength, value);
            Write(code, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value, int bits)
        {
            value = value & (0xFFFFFFFFU >> (32 - bits));
            if (RemainBit > bits)
            {
                RemainData |= value << (32 - RemainBit);
                RemainBit -= bits;
                return;
            }

            if (Position >= Buffer.Length)
            {
                var tmp = Buffer;
                Buffer = new uint[tmp.Length * 2];
                tmp.AsSpan().CopyTo(Buffer);
            }

            RemainData |= value << (32 - RemainBit);
            Buffer[Position] = RemainData;
            Position++;

            RemainBit = 32 - bits + RemainBit;
            var newUsedBits = bits - (32 - RemainBit);
            RemainData = (value & (0xFFFFFFFFU << newUsedBits)) >> newUsedBits;
        }

        public uint[] ToArray()
        {
            Flush();
            return Buffer;
        }

        void Flush()
        {
            if (RemainBit >= 32)
            {
                return;
            }

            if (Position >= Buffer.Length)
            {
                var tmp = Buffer;
                Buffer = new uint[tmp.Length + 1];
                tmp.AsSpan().CopyTo(Buffer);
            }

            Buffer[Position] = RemainData;
            Position++;
            RemainData = 0;
            RemainBit = 32;
        }
    }
}
