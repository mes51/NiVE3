using NiVE3.PresetPlugin.Internal.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class LayerInfo
    {
        public short RawLayerCount { get; }

        public LayerRecord[] LayerRecords { get; }

        public ChannelImageData[] ChannelData { get; }

        private LayerInfo(short rawLayerCount, LayerRecord[] layerRecords, ChannelImageData[] channelData)
        {
            RawLayerCount = rawLayerCount;
            LayerRecords = layerRecords;
            ChannelData = channelData;
        }

        public static LayerInfo Parse(RandomAccessFileReader reader, in PsdFileHeader header)
        {
            var length = header.IsPsb ? reader.ReadInt64() : reader.ReadInt32();
            var start = reader.Position;
            var end = start + length;

            if (length < 1)
            {
                return new LayerInfo(0, [], []);
            }

            var rawLayerRecordCount = reader.ReadInt16();
            var layerRecordCount = Math.Abs(rawLayerRecordCount);
            var layerRecords = new LayerRecord[layerRecordCount];
            for (var i = 0; i < layerRecords.Length; i++)
            {
                layerRecords[i] = LayerRecord.Parse(reader, header.IsPsb);
            }

            var channelImageData = new ChannelImageData[layerRecordCount];
            for (var i = 0; i < channelImageData.Length; i++)
            {
                channelImageData[i] = ChannelImageData.Parse(reader, layerRecords[i].Channels, header);
            }

            reader.Position = end;

            return new LayerInfo(rawLayerRecordCount, layerRecords, channelImageData);
        }
    }
}
