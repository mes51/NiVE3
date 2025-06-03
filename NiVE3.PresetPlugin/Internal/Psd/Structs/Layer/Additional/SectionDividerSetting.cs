using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer.Additional
{
    class SectionDividerSetting
    {
        public SectionDividerType Type { get; }

        public BlendMode Key { get; }

        public int SubType { get; }

        private SectionDividerSetting(SectionDividerType type, BlendMode key, int subType)
        {
            Type = type;
            Key = key;
            SubType = subType;
        }

        public static SectionDividerSetting Parse(RandomAccessFileReader reader, long length)
        {
            var type = reader.ReadInt32();
            var key = length >= 12 ? reader.ReadStruct<BlendMode>() : new BlendMode();
            var subType = length >= 16 ? reader.ReadInt32() : 0;

            return new SectionDividerSetting((SectionDividerType)type, key, subType);
        }
    }

    enum SectionDividerType
    {
        Any = 0,
        OpenFolder = 1,
        ClosedFolder = 2,
        BoundingSectionDivider = 3
    }
}
