using NiVE3.PresetPlugin.Internal.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer
{
    class GlobalLayerMaskInfo
    {
        public short OverlayColorSpace { get; }

        public int ColorComponent1 { get; }

        public int ColorComponent2 { get; }

        public short Opacity { get; }

        public byte Kind { get; }

        private GlobalLayerMaskInfo(short overlayColorSpace, int colorComponent1, int colorComponent2, short opacity, byte kind)
        {
            OverlayColorSpace = overlayColorSpace;
            ColorComponent1 = colorComponent1;
            ColorComponent2 = colorComponent2;
            Opacity = opacity;
            Kind = kind;
        }

        public static GlobalLayerMaskInfo? Parse(RandomAccessFileReader reader)
        {
            var length = reader.ReadInt32();
            if (length < 1)
            {
                return null;
            }
            var start = reader.Position;
            var end = reader.Position + length;

            var overlayColorSpace = reader.ReadInt16();
            var colorComponent1 = reader.ReadInt32();
            var colorComponent2 = reader.ReadInt32();
            var opacity = reader.ReadInt16();
            var kind = reader.ReadByte();

            reader.Position = end;

            return new GlobalLayerMaskInfo(overlayColorSpace, colorComponent1, colorComponent2, opacity, kind);
        }
    }
}
