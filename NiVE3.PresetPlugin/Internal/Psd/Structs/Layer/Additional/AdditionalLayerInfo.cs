using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd.Structs.Layer.Additional
{
    class AdditionalLayerInfo
    {
        // NOTE: 8BIMの逆順
        const uint Signature8BIM = 943868237;

        // NOTE: 8B64の逆順
        const uint Signature8B64 = 943863348;

        public string Key { get; }

        public object? ParsedData { get; }

        public AdditionalLayerInfoType Type { get; }

        private AdditionalLayerInfo(string key, object? parsedData)
        {
            Key = key;
            ParsedData = parsedData;
            Type = Enum.TryParse<AdditionalLayerInfoType>(Key, out var type) ? type : AdditionalLayerInfoType.Unsupported;
        }

        public static AdditionalLayerInfo? Parse(RandomAccessFileReader reader, bool isPsb)
        {
            var signature = reader.ReadUInt32();
            if (signature != Signature8BIM && signature != Signature8B64)
            {
                reader.Position -= 4;
                return null;
            }

            var key = reader.ReadFixedSizeAsciiString(4);
            var length = DataLengthIsLarge(isPsb, key) ? reader.ReadInt64() : reader.ReadInt32();
            var data = ParseInfo(reader, isPsb, key, length);

            if (length % 4 != 0)
            {
                reader.Position += 4 - (length % 4);
            }

            return new AdditionalLayerInfo(key, data);
        }

        static object? ParseInfo(RandomAccessFileReader reader, bool isPsb, string key, long length)
        {
            var end = reader.Position + length;
            var result = key switch
            {
                "luni" => reader.ReadUnicodeString(),
                "lyid" => reader.ReadInt32(),
                "lsct" => SectionDividerSetting.Parse(reader, length),
                "lsdk" => SectionDividerSetting.Parse(reader, length),
                "iOpa" => reader.ReadByte() / 255.0F,
                _ => (object?)null,
            };

            reader.Position = end;
            return result;
        }

        static bool DataLengthIsLarge(bool isPsb, string key)
        {
            return isPsb && (key == "LMsk" || key == "Lr16" || key == "Lr32" || key == "Layr" || key == "Mt16" || key == "Mt32" || key == "Mtrn" || key == "Alph" || key == "FMsk" || key == "lnk2" || key == "FEid" || key == "FXid" || key == "PxSD");
        }
    }

    enum AdditionalLayerInfoType
    {
        /// <summary>
        /// Layer Name (Unicode)
        /// </summary>
        luni,
        /// <summary>
        /// Layer ID
        /// </summary>
        lyid,
        /// <summary>
        /// Section Divider Setting
        /// </summary>
        lsct,
        /// <summary>
        /// Section Divider Setting (undocumented)
        /// </summary>
        /// <see cref="https://github.com/psd-tools/psd-tools/issues/16"/>
        lsdk,
        LMsk,
        Lr16,
        Lr32,
        Layr,
        Mt16,
        Mt32,
        Mtrn,
        Alph,
        FMsk,
        lnk2,
        FEid,
        FXid,
        PxSD,
        /// <summary>
        /// Fill Opacity (undocumented)
        /// </summary>
        /// <see cref="https://qiita.com/yunyundetective/items/d29098ab3dbf8facee03"/>
        iOpa,

        Unsupported
    }
}
