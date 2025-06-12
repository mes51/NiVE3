using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Extensions;
using NiVE3.PresetPlugin.Internal.Psd.Structs.Layer.Additional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class LayerRecord
    {
        public RectTLBR Bounds { get; }

        public ChannelInformation[] Channels { get; }

        public BlendMode BlendMode { get; }

        public float Opacity {  get; }

        public byte Clipping { get; }

        public byte Flags { get; }

        public LayerMaskData? LayerMaskData { get; }

        public string LayerName { get; }

        public AdditionalLayerInfo[] AdditionalLayerInfos { get; }

        public bool IsVisible => (Flags & 0b0010) == 0;

        public bool IsDisplayable => IsVisible && !IsClippingLayer && TotalOpacity > 0.0F;

        public bool IsClippingLayer => Clipping != 0;

        public float TotalOpacity { get; }

        private LayerRecord(RectTLBR bounds, ChannelInformation[] channels, BlendMode blendMode, byte opacity, byte clipping, byte flags, LayerMaskData? layerMaskData, string layerName, AdditionalLayerInfo[] additionalLayerInfos)
        {
            Bounds = bounds;
            Channels = channels;
            BlendMode = blendMode;
            Opacity = opacity / 255.0F;
            Clipping = clipping;
            Flags = flags;
            LayerMaskData = layerMaskData;
            LayerName = layerName;
            AdditionalLayerInfos = additionalLayerInfos;

            TotalOpacity = opacity / 255.0F * (float)(additionalLayerInfos.FirstOrDefault(ai => ai.Type == AdditionalLayerInfoType.iOpa)?.ParsedData ?? 1.0F);
        }

        public SectionDividerSetting? GetSectionDividerSetting()
        {
            return AdditionalLayerInfos.FirstOrDefault(ai => ai.Type == AdditionalLayerInfoType.lsct || ai.Type == AdditionalLayerInfoType.lsdk)?.ParsedData as SectionDividerSetting;
        }

        public static LayerRecord Parse(RandomAccessFileReader reader, in PsdFileHeader header)
        {
            var bounds = reader.ReadStruct<RectTLBR>();

            var channelCount = reader.ReadInt16();
            var channels = new ChannelInformation[channelCount];
            for (var i = 0; i < channelCount; i++)
            {
                channels[i] = ChannelInformation.Parse(reader, header.IsPsb);
            }

            var blendMode = reader.ReadStruct<BlendMode>();
            var opacity = reader.ReadByte();
            var clipping = reader.ReadByte();
            var flags = reader.ReadByte();

            reader.Position++; // skip filler

            var extraDataLength = reader.ReadInt32();
            var extraStart = reader.Position;
            var extraEnd = extraStart + extraDataLength;

            var layerMaskData = LayerMaskData.Parse(reader);

            // NOTE: skip Layer blending ranges data
            var layerBlendingDataLength = reader.ReadInt32();
            reader.Position += layerBlendingDataLength;

            var layerName = reader.ReadAsciiString(4);

            var additionalLayerInfos = new List<AdditionalLayerInfo>();
            while (reader.Position < extraEnd)
            {
                var info = AdditionalLayerInfo.Parse(reader, header);
                if (info != null)
                {
                    additionalLayerInfos.Add(info);
                }
                else
                {
                    break;
                }
            }

            reader.Position = extraEnd;

            return new LayerRecord(bounds, channels, blendMode, opacity, clipping, flags, layerMaskData, layerName, [..additionalLayerInfos]);
        }
    }
}
