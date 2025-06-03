using NiVE3.PresetPlugin.Internal.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class ChannelImageData
    {
        public short[] CompressTypes { get; }

        public int DataCount => CompressTypes.Length;

        (long begin, long length)[] DataRanges { get; }

        PsdFileHeader Header { get; }

        private ChannelImageData(short[] compressTypes, (long begin, long length)[] dataRanges, PsdFileHeader header)
        {
            CompressTypes = compressTypes;
            DataRanges = dataRanges;
            Header = header;
        }

        public static ChannelImageData Parse(RandomAccessFileReader reader, ChannelInformation[] channelInformations, in PsdFileHeader header)
        {
            var dataRanges = new (long begin, long length)[channelInformations.Length];
            var compressTypes = new short[channelInformations.Length];
            for (var i = 0; i < channelInformations.Length; i++)
            {
                compressTypes[i] = reader.ReadInt16();
                var length = Math.Max(channelInformations[i].Length - sizeof(short), 0);
                dataRanges[i] = (reader.Position, length);
                reader.Position += length;
            }

            return new ChannelImageData(compressTypes, dataRanges, header);
        }
    }
}
