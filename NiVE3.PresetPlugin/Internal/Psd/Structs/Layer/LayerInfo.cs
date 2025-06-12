using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Structs.Layer.Additional;
using NiVE3.PresetPlugin.Internal.Psd.Util;
using SharpGen.Runtime;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class LayerInfo
    {
        public short RawLayerCount { get; }

        public LayerRecord[] LayerRecords { get; }

        public ChannelImageData[] ChannelData { get; }

        IStructuredLayer[] StructuredLayers { get; }

        private LayerInfo(short rawLayerCount, LayerRecord[] layerRecords, ChannelImageData[] channelData)
        {
            RawLayerCount = rawLayerCount;
            LayerRecords = layerRecords;
            ChannelData = channelData;

            var reversedLayerRecords = LayerRecords.Reverse().ToArray();
            var reversedChannelData = ChannelData.Reverse().ToArray();

            var index = 0;
            StructuredLayers = ParseGroup(reversedLayerRecords, reversedChannelData, ref index);
        }

        public NManagedImage ReadCompositedImage(RandomAccessFileReader reader, in PsdFileHeader header)
        {
            var result = new NManagedImage(header.ImageWidth, header.ImageHeight);

            foreach (var l in StructuredLayers.Reverse().Where(l => l.IsDisplayable))
            {
                l.DrawImage(reader, header, 1.0F, result);
            }

            return result;
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
                layerRecords[i] = LayerRecord.Parse(reader, header);
            }

            var channelImageData = new ChannelImageData[layerRecordCount];
            for (var i = 0; i < channelImageData.Length; i++)
            {
                channelImageData[i] = ChannelImageData.Parse(reader, layerRecords[i], header);
            }

            reader.Position = end;

            return new LayerInfo(rawLayerRecordCount, layerRecords, channelImageData);
        }

        static IStructuredLayer[] ParseGroup(LayerRecord[] reversedLayerRecords, ChannelImageData[] reversedChannelImageData, ref int index)
        {
            var currentGroup = new List<IStructuredLayer>();

            for (; index < reversedLayerRecords.Length; index++)
            {
                var record = reversedLayerRecords[index];
                var divider = record.GetSectionDividerSetting();
                if (divider != null)
                {
                    if (divider.Type == SectionDividerType.BoundingSectionDivider)
                    {
                        break;
                    }
                    else
                    {
                        var currentIndex = index;
                        index++;
                        var group = new LayerGroup(ParseGroup(reversedLayerRecords, reversedChannelImageData, ref index), record);
                        currentGroup.Add(group);
                    }
                }
                else
                {
                    currentGroup.Add(new LayerItem(reversedChannelImageData[index], record));
                }
            }

            return [..currentGroup];
        }


        private interface IStructuredLayer
        {
            LayerRecord LayerRecord { get; }

            bool IsDisplayable { get; }

            void DrawImage(RandomAccessFileReader reader, in PsdFileHeader header, float opacity, NManagedImage back);

            RectTLBR GetBounds();
        }

        private class LayerGroup : IStructuredLayer
        {
            public LayerRecord LayerRecord { get; }

            public bool IsDisplayable => Layers.Length > 0 && LayerRecord.IsDisplayable && Layers.Any(l => l.IsDisplayable);

            IStructuredLayer[] Layers { get; }

            SectionDividerSetting? SectionDividerSetting { get; }

            public LayerGroup(IStructuredLayer[] layers, LayerRecord groupRecord)
            {
                Layers = layers;
                LayerRecord = groupRecord;
                SectionDividerSetting = groupRecord.GetSectionDividerSetting();
            }

            public void DrawImage(RandomAccessFileReader reader, in PsdFileHeader header, float opacity, NManagedImage back)
            {
                var bounds = GetBounds();
                var blendMode = (SectionDividerSetting?.Key ?? LayerRecord.BlendMode).BlendModeType;

                if (blendMode != BlendModeType.PassThrough)
                {
                    using var temp = new NManagedImage(back.Width, back.Height);

                    foreach (var l in Layers.Reverse().Where(l => l.IsDisplayable))
                    {
                        l.DrawImage(reader, header, 1.0F, temp);
                    }

                    PsdImageCompositor.Blend(back, temp, bounds, LayerRecord.TotalOpacity * opacity, LayerRecord.BlendMode.BlendModeType);
                }
                else
                {
                    // NOTE: どうもPSの通過モードでの合成結果は単純にグループの下までの合成結果に対して1レイヤーずつ合成するだけではないらしい
                    //       一度グループ内で合成したものを下の画像と合成してそうだけども合成方法は不明(Compositeとも違う模様)
                    // TODO: PSの通過モードの時の合成結果をちゃんと再現する
                    var totalOpacity = LayerRecord.TotalOpacity * opacity;
                    foreach (var l in Layers.Reverse().Where(l => l.IsDisplayable))
                    {
                        l.DrawImage(reader, header, totalOpacity, back);
                    }
                }
            }

            public RectTLBR GetBounds()
            {
                var top = int.MaxValue;
                var left = int.MaxValue;
                var right = int.MinValue;
                var bottom = int.MinValue;
                foreach (var l in Layers.Where(l => l.IsDisplayable))
                {
                    var bounds = l.GetBounds();
                    top = Math.Min(top, bounds.Top);
                    left = Math.Min(left, bounds.Left);
                    right = Math.Max(right, bounds.Right);
                    bottom = Math.Max(bottom, bounds.Bottom);
                }

                return new RectTLBR(top, left, bottom, right);
            }
        }

        private class LayerItem : IStructuredLayer
        {
            public LayerRecord LayerRecord { get; }

            public bool IsDisplayable => LayerRecord.IsDisplayable;

            ChannelImageData ChannelImageData { get; }

            public LayerItem(ChannelImageData channelImageData, LayerRecord layerRecord)
            {
                ChannelImageData = channelImageData;
                LayerRecord = layerRecord;
            }

            public void DrawImage(RandomAccessFileReader reader, in PsdFileHeader header, float opacity, NManagedImage back)
            {
                using var image = ChannelImageData.ReadImage(reader, [], -1);
                if (image != null)
                {
                    PsdImageCompositor.Blend(back, image, LayerRecord.Bounds, LayerRecord.TotalOpacity * opacity, LayerRecord.BlendMode.BlendModeType);
                }
            }

            public RectTLBR GetBounds()
            {
                return LayerRecord.Bounds;
            }
        }
    }
}
