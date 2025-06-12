using NiVE3.Image;
using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Decoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs
{
    class ImageData
    {
        public int CompressionMethod { get; }

        public (long begin, long length) DataRange { get; }

        PsdFileHeader Header { get; }

        private ImageData(int compressionMethod, (long begin, long length) dataRange, PsdFileHeader header)
        {
            CompressionMethod = compressionMethod;
            DataRange = dataRange;
            Header = header;
        }

        public NManagedImage? ReadImage(RandomAccessFileReader reader, Vector4[] indexedColorTable, short transparencyIndex)
        {
            return ImageDecoder.DecodeImage(reader, Header, new RectTLBR(0, 0, Header.ImageHeight, Header.ImageWidth), indexedColorTable, transparencyIndex, Header.ColorChannels, [CompressionMethod], [DataRange.begin], [DataRange.length]);
        }

        public static ImageData Parse(RandomAccessFileReader reader, in PsdFileHeader header)
        {
            var compressionMethod = reader.ReadInt16();
            var begin = reader.Position;

            return new ImageData(compressionMethod, (begin, reader.Length - begin), header);
        }
    }
}
