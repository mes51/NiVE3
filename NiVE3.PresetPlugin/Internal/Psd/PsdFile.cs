using NiVE3.PresetPlugin.Internal.IO;
using NiVE3.PresetPlugin.Internal.Psd.Enums;
using NiVE3.PresetPlugin.Internal.Psd.Extensions;
using NiVE3.PresetPlugin.Internal.Psd.Structs;
using NiVE3.PresetPlugin.Internal.Psd.Structs.Layer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Internal.Psd
{
    class PsdFile
    {
        public string FilePath { get; }

        public PsdFileHeader Header { get; }

        public LayerAndMaskInformation LayerAndMaskInformation { get; }

        public ImageResource[] ImageResources { get; }

        public int Width => Header.ImageWidth;

        public int Height => Header.ImageHeight;

        ImageData ImageData { get; }

        Vector4[] IndexedColorTable { get; }

        private PsdFile(string filePath, PsdFileHeader header, byte[] colorModeData, ImageResource[] imageResources, LayerAndMaskInformation layerAndMaskInformation, ImageData imageData)
        {
            FilePath = filePath;
            Header = header;
            ImageResources = imageResources;
            LayerAndMaskInformation = layerAndMaskInformation;
            ImageData = imageData;

            if (Header.ColorMode == ColorMode.Indexed)
            {
                IndexedColorTable = new Vector4[256];
                for (var i = 0; i < IndexedColorTable.Length; i++)
                {
                    IndexedColorTable[i] = new Vector4(colorModeData[i + 512], colorModeData[i + 256], colorModeData[i], 255.0F) / 255.0F;
                }
            }
            else
            {
                IndexedColorTable = [];
            }
        }

        public Vector4[] ReadImageData()
        {
            using var reader = new RandomAccessFileReader(FilePath, true, true);

            var transparencyIndex = (short)(ImageResources.FirstOrDefault(i => i.ResourceId == ImageResourceType.TransparencyIndex)?.ParseData(reader) ?? (short)-1);
            return ImageData.ReadImage(reader, IndexedColorTable, transparencyIndex);
        }

        public Vector4[] DebugReadFirstLayer()
        {
            using var reader = new RandomAccessFileReader(FilePath, true, true);

            var transparencyIndex = (short)(ImageResources.FirstOrDefault(i => i.ResourceId == ImageResourceType.TransparencyIndex)?.ParseData(reader) ?? (short)-1);
            return LayerAndMaskInformation.LayerInfo.ChannelData.FirstOrDefault(l => !l.IsEmpty)?.ReadImage(reader, IndexedColorTable, transparencyIndex) ?? [];
        }

        public static PsdFile Parse(string filePath)
        {
            using var reader = new RandomAccessFileReader(filePath, true, true);

            var header = reader.ReadStruct<PsdFileHeader>();

            var colorModeDataLength = reader.ReadInt32();
            var colorModeData = new byte[colorModeDataLength];
            if (colorModeDataLength > 0)
            {
                reader.Read(colorModeData);
            }

            var imageResourceDataEndPosition = reader.ReadInt32() + reader.Position;
            var imageResources = new List<ImageResource>();
            while (reader.Position < imageResourceDataEndPosition)
            {
                imageResources.Add(ImageResource.Parse(reader));
            }

            var layerAndMaskInfo = LayerAndMaskInformation.Parse(reader, header);
            var imageData = ImageData.Parse(reader, header);

            return new PsdFile(filePath, header, colorModeData, [..imageResources], layerAndMaskInfo, imageData);
        }
    }
}
