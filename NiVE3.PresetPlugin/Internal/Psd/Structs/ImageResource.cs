using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs
{
    class ImageResource
    {
        public ImageResourceType ResourceId { get; }

        public string Name { get; }

        (long start, int length) ResourceDataRange { get; }

        private ImageResource(short resourceId, string name, (long start, int length) resourceDataRange)
        {
            ResourceId = (ImageResourceType)resourceId;
            Name = name;
            ResourceDataRange = resourceDataRange;
        }

        public object? ParseData(RandomAccessFileReader reader)
        {
            reader.Position = ResourceDataRange.start;
            switch (ResourceId)
            {
                case ImageResourceType.TransparencyIndex:
                    return reader.ReadInt16();
                default:
                    return null;
            }
        }

        public static ImageResource Parse(RandomAccessFileReader reader)
        {
            if (reader.ReadFixedSizeAsciiString(4) != "8BIM")
            {
                throw new Exception("invalid image resource");
            }

            var resourceId = reader.ReadInt16();
            var name = reader.ReadAsciiString(2);
            var resourceDataLength = reader.ReadInt32();
            var resourceDataStart = reader.Position;

            reader.Position += resourceDataLength;
            if (resourceDataLength % 2 != 0)
            {
                reader.Position++;
            }

            return new ImageResource(resourceId, name, (resourceDataStart, resourceDataLength));
        }
    }

    enum ImageResourceType : short
    {
        TransparencyIndex = 1047
    }
}
