using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Effect.Util.Jpeg
{
    class HuffmanTable
    {
        // from RFC2435 Appendix D
        // SEE: https://datatracker.ietf.org/doc/html/rfc2435#appendix-D
        private static readonly int[] LuminanceDCCodeLengths = [0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0];

        private static readonly int[] LuminanceDCSymbols = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

        private static readonly int[] LuminanceACCodeLengths = [0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7D];

        private static readonly int[] LuminanceACSymbols =
        [
            0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
            0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
            0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
            0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
            0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16,
            0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
            0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
            0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
            0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
            0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
            0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
            0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
            0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
            0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
            0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
            0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4,
            0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
            0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
            0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
            0xF9, 0xFA
        ];

        private static readonly int[] ChrominanceDCCodeLengths = [0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0];

        private static readonly int[] ChrominanceDCSymbols = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

        private static readonly int[] ChrominanceACCodeLengths = [0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77];

        private static readonly int[] ChrominanceACSymbols =
        [
            0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
            0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
            0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
            0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
            0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34,
            0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
            0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38,
            0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
            0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
            0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
            0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96,
            0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
            0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4,
            0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
            0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2,
            0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
            0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9,
            0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
            0xF9, 0xFA
        ];

        public static readonly HuffmanTable LuminanceDCTable = new HuffmanTable(LuminanceDCCodeLengths, LuminanceDCSymbols);

        public static readonly HuffmanTable LuminanceACTable = new HuffmanTable(LuminanceACCodeLengths, LuminanceACSymbols);

        public static readonly HuffmanTable ChrominanceDCTable = new HuffmanTable(ChrominanceDCCodeLengths, ChrominanceDCSymbols);

        public static readonly HuffmanTable ChrominanceACTable = new HuffmanTable(ChrominanceACCodeLengths, ChrominanceACSymbols);

        static readonly uint[] EncodeValues;

        static readonly int[] EncodedValueBits;

        static readonly int[] DecodedValues;

        HuffmanCode[] HuffmanCodes { get; }

        HuffmanCode[] CodeToValues { get; }

        int[] MaxCodes { get; }

        static HuffmanTable()
        {
            ReadOnlySpan<uint> valueMask =
            [
                0b0000000000000000,
                0b0000000000000001,
                0b0000000000000011,
                0b0000000000000111,
                0b0000000000001111,
                0b0000000000011111,
                0b0000000000111111,
                0b0000000001111111,
                0b0000000011111111,
                0b0000000111111111,
                0b0000001111111111,
                0b0000011111111111,
                0b0000111111111111,
                0b0001111111111111,
                0b0011111111111111,
                0b0111111111111111,
                0b1111111111111111,
            ];

            EncodeValues = new uint[ushort.MaxValue];
            EncodedValueBits = new int[ushort.MaxValue];
            DecodedValues = new int[(uint)ushort.MaxValue << 4];
            for (var i = 0; i < EncodeValues.Length; i++)
            {
                var value = i - 32767;
                var bitLength = 32 - BitOperations.LeadingZeroCount((uint)Math.Abs(value));

                if (value < 0)
                {
                    value--;
                }
                EncodeValues[i] = ((uint)(value & valueMask[bitLength])).BitReverse(bitLength);
                EncodedValueBits[i] = bitLength;
                DecodedValues[bitLength << 16 | (ushort)EncodeValues[i]] = i - 32767; // -((coeff - 1) & [bitLength]);
            }
        }

        private HuffmanTable(int[] lengths, int[] values)
        {

            var count = lengths.Sum();
            var codes = new HuffmanCode[count];

            var maxValue = values.Max();
            HuffmanCodes = new HuffmanCode[maxValue + 1];
            MaxCodes = [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1];

            var pos = 0;
            var code = 0U;
            for (var i = 0; i < lengths.Length; i++)
            {
                var size = lengths[i];
                for (var n = 0; n < size; n++, pos++, code++)
                {
                    codes[pos] = new HuffmanCode(i + 1, code, values[pos]);
                    HuffmanCodes[values[pos]] = codes[pos];
                    MaxCodes[i] = Math.Max(MaxCodes[i], (int)code);
                }
                code <<= 1;
            }

            CodeToValues = new HuffmanCode[code >> 1];
            for (var i = 0; i < count; i++)
            {
                CodeToValues[codes[i].OriginalCode] = codes[i];
            }
        }

        public (int length, uint encodedValue) GetCode(int runLength, int coeff)
        {
            var encodedValue = EncodeValues[32767 + coeff];
            var valueBits = EncodedValueBits[32767 + coeff];

            ref var code = ref HuffmanCodes[runLength << 4 | valueBits];
            return (code.Size + valueBits, encodedValue << code.Size | code.Code);
        }

        public (int useBits, int runLength, int coeff) GetValue(uint encodedValue)
        {
            ref var code = ref CodeToValues[0];
            var codeLength = 0;
            for (var i = 1; i <= 16; i++)
            {
                var mask = 0xFFFFFFFFU >> 32 - i;
                var extractedCode = (encodedValue & mask).BitReverse(i);
                if (extractedCode <= MaxCodes[i - 1])
                {
                    code = ref CodeToValues[extractedCode];
                    codeLength = i;
                    break;
                }
            }

            if (codeLength < 1)
            {
                return (0, -1, 0);
            }

            var value = code.Value;
            var runLength = value >> 4 & 0xF;
            var valueBits = value & 0xF;
            var valueMask = 0x7FFFFFFF >> 31 - valueBits;
            var coeff = DecodedValues[valueBits << 16 | (ushort)(encodedValue >> codeLength & valueMask)];
            return (codeLength + valueBits, runLength, coeff);
        }

        readonly record struct HuffmanCode(int Size, uint OriginalCode, int Value)
        {
            public readonly uint Code = OriginalCode.BitReverse(Size);
        }
    }

    file static class Uint32Extension
    {
        static readonly uint[][] BitReversedTable;

        static Uint32Extension()
        {
            BitReversedTable = new uint[16][];
            for (var i = 0; i < 16; i++)
            {
                BitReversedTable[i] = new uint[(1 << i + 1)];
                for (var v = 0U; v < BitReversedTable[i].Length; v++)
                {
                    var result = 0U;
                    for (var b = 0; b <= i; b++)
                    {
                        var bit = (v & 1U << b) >> b;
                        result |= bit << i - b;
                    }
                    BitReversedTable[i][v] = result;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BitReverse(this uint v, int bitCount)
        {
            if (bitCount < 1)
            {
                return 0;
            }
            else if (bitCount <= BitReversedTable.Length)
            {
                return BitReversedTable[bitCount - 1][v & 0xFFFFFFFFU >> 32 - bitCount];
            }
            else
            {
                return v.BitReverseSlow(bitCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BitReverseSlow(this uint v, int bitCount)
        {
            var result = 0U;
            for (var i = 0; i < bitCount; i++)
            {
                var bit = (v & (uint)(1 << i)) >> i;
                result |= bit << bitCount - 1 - i;
            }
            return result;
        }
    }
}
