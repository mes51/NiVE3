using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime.Win32;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    abstract class VideoSourceReaderBase : IDisposable
    {
        protected const double DurationRate = 1E7;

        protected const int FirstVideoStreamId = (int)MidlMidlItfMfreadwrite000000010001.MfSourceReaderFirstVideoStream;

        public bool Succeeded { get; init; }

        public double Duration { get; init; }

        public double FrameRate { get; init; }

        public int Width => Format.CorrectedWidth;

        public int Height => Format.CorrectedHeight;

        public string FilePath { get; }

        protected IMFSourceReader? Reader { get; init; }

        protected FormatInfo Format { get; set; }

        /// <summary>
        /// フレームの読み出し
        /// </summary>
        /// <param name="time">読み込む時間</param>
        /// <returns>ArrayPool&gt;byte&lt;.Sharedから借りたbyte[]</byte></returns>
        public abstract byte[] GetFrame(double time);

        protected VideoSourceReaderBase(string filePath)
        {
            FilePath = filePath;
        }

        protected double GetDuration()
        {
            if (Reader == null)
            {
                return -1.0;
            }

            var duration = Reader.GetPresentationAttribute((int)MidlMidlItfMfreadwrite000000010001.MfSourceReaderMediaSource, PresentationDescriptionAttributeKeys.Duration);

            if (duration.ElementType == SharpGen.Runtime.Win32.VariantElementType.ULong)
            {
                return unchecked((long)(ulong)duration.Value) / VideoSourceReaderBase.DurationRate;
            }
            else
            {
                return -1.0;
            }
        }

        protected double GetFrameRate()
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

        protected IMFSample? ReadSample(double time)
        {
            const int MaxSkipFrame = 10;
            const long SeekTolerance = 10000000;

            if (Reader == null)
            {
                return null;
            }

            var timeLong = (long)(time * VideoSourceReaderBase.DurationRate);
            var pos = new Variant { ElementType = VariantElementType.Long, Value = timeLong };
            Reader.SetCurrentPosition(Guid.Empty, pos);

            IMFSample? sample = null;
            int skipCount = 0;
            while (true)
            {
                Reader.ReadSample(VideoSourceReaderBase.FirstVideoStreamId, 0, out int _, out int flags, out long _, out IMFSample? sampleTemp);

                if ((flags & (int)SourceReaderFlag.FEndofstream) != 0)
                {
                    break;
                }

                if ((flags & (int)SourceReaderFlag.FCurrentmediatypechanged) != 0)
                {
                    Format = FormatInfo.GetVideoFormat(Reader, VideoSourceReaderBase.FirstVideoStreamId);
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

            return sample;
        }

        /// <summary>
        /// IMFSampleから画像を読み出す
        /// </summary>
        /// <param name="sample">読み出すフレームを保持するIMFSample</param>
        /// <returns>ArrayPool&gt;byte&lt;.Sharedから借りたbyte[]</byte></returns>
        protected byte[] ConvertSampleToByteArray(IMFSample sample)
        {
            var expectedLength = Format.Height * Format.Width * 4;
            using var buffer = sample.ConvertToContiguousBuffer();
            var result = Array.Empty<byte>();
            buffer.Lock(out nint ptr, out int _, out int length);

            if (length >= expectedLength)
            {
                var requestSize = Math.Min(length, expectedLength);
                result = ArrayPool<byte>.Shared.Rent(requestSize);
                Marshal.Copy(ptr, result, 0, requestSize);
            }

            buffer.Unlock();

            return result;
        }

        public virtual void Dispose()
        {
            Reader?.Dispose();
        }
    }
}
