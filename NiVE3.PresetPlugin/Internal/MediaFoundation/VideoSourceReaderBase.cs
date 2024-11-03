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

            var duration = Reader.GetPresentationAttribute(SourceReaderIndex.MediaSource, PresentationDescriptionAttributeKeys.Duration);

            if (duration.ElementType == SharpGen.Runtime.Win32.VariantElementType.ULong)
            {
                return unchecked((long)(ulong)duration.Value) / DurationRate;
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

            using var mediaType = Reader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);
            if (mediaType == null)
            {
                return -1.0;
            }

            var (numerator, denominator) = Util.GetDoubleInt32(mediaType, MediaTypeAttributeKeys.FrameRate);
            return numerator / (double)denominator;
        }

        protected IMFSample? ReadSample(double time)
        {
            const int MaxSkipFrame = 100;

            if (Reader == null)
            {
                return null;
            }

            var seekTolerance = (long)(5000000.0 / FrameRate);

            var longTime = (long)(time * DurationRate);
            Reader.SetCurrentPosition(longTime);

            IMFSample? sample = null;
            var skipCount = 0;
            while (true)
            {
                var sampleTemp = Reader.ReadSample(SourceReaderIndex.FirstVideoStream, SourceReaderControlFlag.None, out var _, out var flags, out var _);

                if ((flags & SourceReaderFlag.EndOfStream) != 0)
                {
                    break;
                }

                // TODO: 必要になるタイミングの調査。少なくともGPUアクセラレーションが有効の時はあると×
                /*
                if ((flags & (int)SourceReaderFlag.FCurrentmediatypechanged) != 0)
                {
                    Format = FormatInfo.GetVideoFormat(Reader, VideoSourceReaderBase.FirstVideoStreamId);
                }
                */

                if (sampleTemp == null)
                {
                    continue;
                }

                sample?.Dispose();
                sample = sampleTemp;

                var timestamp = sampleTemp.SampleTime;
                if (skipCount < MaxSkipFrame && Math.Abs(longTime - timestamp) > seekTolerance)
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
            buffer.Lock(out var ptr, out var _, out var length);

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
