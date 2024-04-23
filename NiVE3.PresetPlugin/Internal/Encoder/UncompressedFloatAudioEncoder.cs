using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpAvi.Codecs;

namespace NiVE3.PresetPlugin.Internal.Encoder
{
    class UncompressedFloatAudioEncoder : IAudioEncoder
    {
        public int ChannelCount => 2;

        public int SamplesPerSecond { get; }

        public int BitsPerSample => 32;

        public short Format => 3; // WAVE_FORMAT_IEEE_FLOAT

        public int BytesPerSecond => SamplesPerSecond * Granularity;

        public int Granularity => 8; // 4byte * 2ch

        public byte[] FormatSpecificData => [];

        public UncompressedFloatAudioEncoder(int samplingRate)
        {
            SamplesPerSecond = samplingRate;
        }

        public int EncodeBlock(byte[] source, int sourceOffset, int sourceCount, byte[] destination, int destinationOffset)
        {
            return EncodeBlock(source.AsSpan(sourceOffset, sourceCount), destination.AsSpan(destinationOffset));
        }

        public int EncodeBlock(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            source.CopyTo(destination);
            return destination.Length;
        }

        public int Flush(byte[] destination, int destinationOffset)
        {
            return 0;
        }

        public int Flush(Span<byte> destination)
        {
            return 0;
        }

        public int GetMaxEncodedLength(int sourceCount)
        {
            return sourceCount;
        }
    }
}
