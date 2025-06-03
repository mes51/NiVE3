using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Structs.Layer.Additional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class LayerAndMaskInformation
    {
        public LayerInfo LayerInfo { get; }

        public GlobalLayerMaskInfo? GlobalLayerMaskInfo { get; }

        public AdditionalLayerInfo[] AdditionalLayerInfos { get; }

        private LayerAndMaskInformation(LayerInfo layerInfo, GlobalLayerMaskInfo? globalLayerMaskInfo, AdditionalLayerInfo[] additionalLayerInfos)
        {
            LayerInfo = layerInfo;
            GlobalLayerMaskInfo = globalLayerMaskInfo;
            AdditionalLayerInfos = additionalLayerInfos;
        }

        public static LayerAndMaskInformation Parse(RandomAccessFileReader reader, in PsdFileHeader header)
        {
            var length = header.IsPsb ? reader.ReadInt64() : reader.ReadInt32();
            var start = reader.Position;
            var end = length + start;

            var layerInfo = LayerInfo.Parse(reader, header);
            var globalLayerMaskInfo = GlobalLayerMaskInfo.Parse(reader);

            var additionalLayerInfos = new List<AdditionalLayerInfo>();
            while (reader.Position < end)
            {
                var info = AdditionalLayerInfo.Parse(reader, header.IsPsb);
                if (info != null)
                {
                    additionalLayerInfos.Add(info);
                }
                else
                {
                    break;
                }
            }

            reader.Position = end;

            return new LayerAndMaskInformation(layerInfo, globalLayerMaskInfo, [..additionalLayerInfos]);
        }
    }
}
