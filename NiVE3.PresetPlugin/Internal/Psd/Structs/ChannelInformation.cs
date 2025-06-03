using NiVE3.PresetPlugin.Internal.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs
{
    class ChannelInformation
    {
        public readonly short Id;

        public readonly long Length;

        private ChannelInformation(short id, long length)
        {
            Id = id;
            Length = length;
        }

        public static ChannelInformation Parse(RandomAccessFileReader reader, bool isPsb)
        {
            var id = reader.ReadInt16();
            var length = isPsb ? reader.ReadInt64() : reader.ReadInt32();

            return new ChannelInformation(id, length);
        }
    }
}
