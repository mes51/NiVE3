using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime.Win32;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    class SoftwareVideoSourceReader : VideoSourceReaderBase
    {
        public SoftwareVideoSourceReader(string filePath) : base(filePath)
        {
            MFInitializer.Initialize();

            using var attributes = MediaFactory.MFCreateAttributes(1);
            if (attributes == null)
            {
                return;
            }

            attributes.Set(SourceReaderAttributeKeys.EnableAdvancedVideoProcessing, true);
            attributes.Set(SourceReaderAttributeKeys.EnableTranscodeOnlyTransforms, true);
            attributes.Set(SinkWriterAttributeKeys.ReadwriteEnableHardwareTransforms, true);

            using var mediaType = MediaFactory.MFCreateMediaType();
            if (mediaType == null)
            {
                return;
            }
            if (mediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video).Failure)
            {
                return;
            }
            if (mediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Argb32).Failure)
            {
                return;
            }

            Reader = MediaFactory.MFCreateSourceReaderFromURL(filePath, attributes);
            Reader.SetCurrentMediaType(FirstVideoStreamId, mediaType);
            Reader.SetStreamSelection(FirstVideoStreamId, true);

            Format = FormatInfo.GetVideoFormat(Reader, FirstVideoStreamId);

            Succeeded = Format.Width != 0;
            if (Succeeded)
            {
                Duration = GetDuration();
                FrameRate = GetFrameRate();
            }
        }

        /// <summary>
        /// フレームの読み出し
        /// </summary>
        /// <param name="time">読み込む時間</param>
        /// <returns>ArrayPool<byte>.Sharedから借りたbyte[]</byte></returns>
        public override byte[] GetFrame(double time)
        {
            using var sample = ReadSample(time);
            if (sample == null)
            {
                return Array.Empty<byte>();
            }

            return ConvertSampleToByteArray(sample);
        }
    }
}
