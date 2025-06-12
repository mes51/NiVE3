using NiVE3.Image;
using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Decoder;
using NiVE3.PresetPlugin.Internal.Psd.Enums;
using NiVE3.Shared.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class ChannelImageData
    {
        static readonly int[] GrayScaleChannelIds = [0, -1];

        static readonly int[] RgbChannelIds = [0, 1, 2, -1];

        public int DataCount => CompressionMethods.Length;

        public bool IsEmpty => DataRanges.All(t => t.length < 1);

        RectTLBR Bounds { get; }

        short[] CompressionMethods { get; }

        (long begin, long length)[] DataRanges { get; }

        ChannelInformation[] ChannelInformations { get; }

        PsdFileHeader Header { get; }

        private ChannelImageData(RectTLBR bounds, short[] compressionMethods, (long begin, long length)[] dataRanges, PsdFileHeader header, ChannelInformation[] channelInformations)
        {
            Bounds = bounds;
            CompressionMethods = compressionMethods;
            DataRanges = dataRanges;
            Header = header;
            ChannelInformations = channelInformations;
        }

        public NManagedImage? ReadImage(RandomAccessFileReader reader, Vector4[] indexedColorTable, short transparencyIndex)
        {
            var targetChannelIds = Header.ColorMode switch
            {
                ColorMode.GrayScale => GrayScaleChannelIds,
                ColorMode.RGB => RgbChannelIds,
                _ => null
            };

            if (targetChannelIds == null)
            {
                return null;
            }

            var channelIndices = targetChannelIds.Take(Header.ColorChannels).Select(id => ChannelInformations.FindIndex(ci => ci.Id == id)).ToArray();

            var compressionMethods = channelIndices.Select(i => i > -1 && DataRanges[i].length > 0 ? CompressionMethods[i] : int.MinValue).ToArray();
            var imageDataBegins = channelIndices.Select(i => i > -1 ? DataRanges[i].begin : 0L).ToArray();
            var imageDataLengths = channelIndices.Select(i => i > -1 ? DataRanges[i].length : 0L).ToArray();

            return ImageDecoder.DecodeImage(reader, Header, Bounds, indexedColorTable, transparencyIndex, 1, compressionMethods, imageDataBegins, imageDataLengths);
        }

        public static ChannelImageData Parse(RandomAccessFileReader reader, LayerRecord layerRecord, in PsdFileHeader header)
        {
            var channelInformations = layerRecord.Channels;
            var dataRanges = new (long begin, long length)[channelInformations.Length];
            var compressTypes = new short[channelInformations.Length];
            for (var i = 0; i < channelInformations.Length; i++)
            {
                compressTypes[i] = reader.ReadInt16();
                var length = Math.Max(channelInformations[i].Length - sizeof(short), 0);
                dataRanges[i] = (reader.Position, length);
                reader.Position += length;
            }

            return new ChannelImageData(layerRecord.Bounds, compressTypes, dataRanges, header, channelInformations);
        }
    }
}
