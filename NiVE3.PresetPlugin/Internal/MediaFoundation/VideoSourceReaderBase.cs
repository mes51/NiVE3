using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        IMFSample? CurrentFrame { get; set; }

        long LastFrameTime { get; set; }

        public abstract bool GetFrame(double time, Span<byte> dest);

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

            var (numerator, denominator) = Util.GetDoubleUInt32(mediaType, MediaTypeAttributeKeys.FrameRate);
            return numerator / (double)denominator;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        /// <remarks>返値はDisposeしない</remarks>
        protected IMFSample? ReadSample(double time)
        {
            const int MaxSkipFrame = 500;

            if (Reader == null)
            {
                return null;
            }

            var seekTolerance = (long)(5000000.0 / FrameRate);
            var frameDuration = (long)(DurationRate / FrameRate);

            var longTime = (long)(time * DurationRate);
            if (CurrentFrame != null)
            {
                if (CurrentFrame.SampleTime >= longTime && (CurrentFrame.SampleTime + frameDuration) < longTime)
                {
                    return CurrentFrame;
                }
                else
                {
                    CurrentFrame.Dispose();
                    CurrentFrame = null;
                }
            }

            if (LastFrameTime > longTime)
            {
                Reader.SetCurrentPosition(longTime);
            }

            IMFSample? sample = null;
            var skipCount = 0;
            while (true)
            {
                var sampleTemp = Reader.ReadSample(SourceReaderIndex.FirstVideoStream, SourceReaderControlFlag.None, out var _, out var flags, out var _);

                if ((flags & SourceReaderFlag.EndOfStream) != 0)
                {
                    break;
                }

                if ((flags & SourceReaderFlag.CurrentMediaTypeChanged) != 0)
                {
                    OnCurrentMediaTypeChanged();
                }

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

                LastFrameTime = timestamp;

                break;
            }

            CurrentFrame = sample;

            return CurrentFrame;
        }

        protected unsafe void ConvertSample(IMFSample sample, Span<byte> dst)
        {
            var expectedLength = Format.Height * Format.Width * 4;
            using var buffer = sample.ConvertToContiguousBuffer();
            buffer.Lock(out var ptr, out var _, out var length);

            if (dst.Length > 0)
            {
                var bufferSpan = new Span<byte>(ptr.ToPointer(), length);
                bufferSpan[..Math.Min(dst.Length, length)].CopyTo(dst);
            }

            buffer.Unlock();
        }

        protected virtual void OnCurrentMediaTypeChanged() { }

        public virtual void Dispose()
        {
            CurrentFrame?.Dispose();
            Reader?.Dispose();
        }
    }
}
