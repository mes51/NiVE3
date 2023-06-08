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
    class SoftwareSourceReader : ISourceReader
    {
        public bool Succeeded { get; }

        public double Duration { get; }

        public double FrameRate { get; }

        public string FilePath { get; }

        public FormatInfo Format { set; get; }

        IMFAttributes? Attributes { get; }

        IMFSourceReader? Reader { get; }

        IMFMediaSource? Source { get; }

        public SoftwareSourceReader(string file)
        {
            MFInitializer.Initialize();

            FilePath = file;
            Attributes = MediaFactory.MFCreateAttributes(1);
            if (Attributes == null)
            {
                return;
            }

            Attributes.Set(SourceReaderAttributeKeys.EnableAdvancedVideoProcessing, true);
            Attributes.Set(SourceReaderAttributeKeys.EnableTranscodeOnlyTransforms, true);
            Attributes.Set(SinkWriterAttributeKeys.ReadwriteEnableHardwareTransforms, true);

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

            using var resolver = MediaFactory.MFCreateSourceResolver();
            if (resolver == null)
            {
                return;
            }

            Source = resolver.CreateObjectFromURL(file);
            Reader = MediaFactory.MFCreateSourceReaderFromMediaSource(Source, Attributes);
            Reader.SetCurrentMediaType(ISourceReader.FirstVideoStreamId, mediaType);
            Reader.SetStreamSelection(ISourceReader.FirstVideoStreamId, true);

            Format = FormatInfo.GetVideoFormat(Reader, ISourceReader.FirstVideoStreamId);

            Succeeded = Format.Width != 0;
            if (Succeeded)
            {
                Duration = GetDuration();
                FrameRate = GetFrameRate();
            }
        }

        public byte[] GetFrame(double time)
        {
            const int MaxSkipFrame = 10;
            const long SeekTolerance = 10000000;

            if (Reader == null)
            {
                return Array.Empty<byte>();
            }

            var timeLong = (long)(time * ISourceReader.DurationRate);
            var pos = new Variant { ElementType = VariantElementType.Long, Value = timeLong };
            Reader.SetCurrentPosition(Guid.Empty, pos);

            IMFSample? sample = null;
            int skipCount = 0;
            while (true)
            {
                Reader.ReadSample(ISourceReader.FirstVideoStreamId, 0, out int _, out int flags, out long _, out IMFSample? sampleTemp);

                if ((flags & (int)SourceReaderFlag.FEndofstream) != 0)
                {
                    break;
                }

                if ((flags & (int)SourceReaderFlag.FCurrentmediatypechanged) != 0)
                {
                    Format = FormatInfo.GetVideoFormat(Reader, ISourceReader.FirstVideoStreamId);
                }

                if (sampleTemp == null)
                {
                    continue;
                }

                sample?.Dispose();
                sample = sampleTemp;

                var timestamp = sampleTemp.SampleTime;
                if (skipCount < MaxSkipFrame && timestamp + SeekTolerance < timeLong)
                {
                    skipCount++;
                    continue;
                }

                break;
            }

            if (sample == null)
            {
                return Array.Empty<byte>();
            }

            var expectedLength = Format.Height * Format.Width * 4;
            using var buffer = sample.ConvertToContiguousBuffer();
            var result = Array.Empty<byte>();
            buffer.Lock(out nint ptr, out int _, out int length);

            if (length == expectedLength)
            {
                result = ArrayPool<byte>.Shared.Rent(length);
                Marshal.Copy(ptr, result, 0, length);
            }

            buffer.Unlock();

            sample.Dispose();

            return result;
        }

        double GetDuration()
        {
            if (Reader == null)
            {
                return -1.0;
            }

            var duration = Reader.GetPresentationAttribute((int)MidlMidlItfMfreadwrite000000010001.MfSourceReaderMediaSource, PresentationDescriptionAttributeKeys.Duration);

            if (duration.ElementType == SharpGen.Runtime.Win32.VariantElementType.ULong)
            {
                return unchecked((long)(ulong)duration.Value) / ISourceReader.DurationRate;
            }
            else
            {
                return -1.0;
            }
        }

        double GetFrameRate()
        {
            if (Reader == null)
            {
                return -1.0;
            }

            using var mediaType = Reader.GetCurrentMediaType((int)MidlMidlItfMfreadwrite000000010001.MfSourceReaderFirstVideoStream);
            if (mediaType == null)
            {
                return -1.0;
            }

            var (numerator, denominator) = Util.GetDoubleInt32(mediaType, MediaTypeAttributeKeys.FrameRate);
            return numerator / (double)denominator;
        }

        public void Dispose()
        {
            Reader?.Dispose();
            Source?.Dispose();
            Attributes?.Dispose();
        }
    }
}
