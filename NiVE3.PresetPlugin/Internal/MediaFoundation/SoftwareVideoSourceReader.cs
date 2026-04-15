using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Reader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, mediaType);
            Reader.SetStreamSelection(SourceReaderIndex.FirstVideoStream, true);

            Format = FormatInfo.GetVideoFormat(Reader, SourceReaderIndex.FirstVideoStream);

            Succeeded = Format.Width != 0;
            if (Succeeded)
            {
                Duration = GetDuration();
                FrameRate = GetFrameRate();
            }
        }

        public override bool GetFrame(double time, Span<byte> dst)
        {
            var sample = ReadSample(time);
            if (sample == null)
            {
                return false;
            }

            ConvertSample(sample, dst);

            return true;
        }
    }
}
