using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using SharpGen.Runtime.Win32;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    class AudioSourceReader : IDisposable
    {
        const double DurationRate = 1E7;

        const long RewindRetrySpan = 1000;

        public double Duration { get; }

        public bool Success { get; }

        IMFSourceReader? Reader { get; }

        public AudioSourceReader(string filePath)
        {
            MFInitializer.Initialize();

            using var mediaType = MediaFactory.MFCreateMediaType();
            if (mediaType == null)
            {
                return;
            }
            if (mediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Audio).Failure)
            {
                return;
            }
            if (mediaType.Set(MediaTypeAttributeKeys.Subtype, AudioFormatGuids.Float).Failure)
            {
                return;
            }
            if (mediaType.Set(MediaTypeAttributeKeys.AudioSamplesPerSecond, IFootageSource.SupportAudioSamplingRate).Failure)
            {
                return;
            }
            if (mediaType.Set(MediaTypeAttributeKeys.AudioNumChannels, IFootageSource.SupportChannelCount).Failure)
            {
                return;
            }

            Reader = MediaFactory.MFCreateSourceReaderFromURL(filePath, null);
            Reader.SetCurrentMediaType(SourceReaderIndex.FirstAudioStream, mediaType);
            Reader.SetStreamSelection(SourceReaderIndex.FirstAudioStream, true);

            using var currentMediaType = Reader.GetCurrentMediaType(SourceReaderIndex.FirstAudioStream);
            Success = currentMediaType.GetGUID(MediaTypeAttributeKeys.Subtype) == AudioFormatGuids.Float &&
                currentMediaType.GetUInt32(MediaTypeAttributeKeys.AudioSamplesPerSecond) == IFootageSource.SupportAudioSamplingRate &&
                currentMediaType.GetUInt32(MediaTypeAttributeKeys.AudioNumChannels) == IFootageSource.SupportChannelCount &&
                currentMediaType.GetUInt32(MediaTypeAttributeKeys.AudioBitsPerSample) == sizeof(float) * 8;

            Duration = GetDuration();
        }

        double GetDuration()
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

        public float[] Read(double time, double length)
        {
            const int RewindLimit = 100;

            if (Reader == null)
            {
                return [];
            }

            var longTime = (long)(time * DurationRate);
            Reader.SetCurrentPosition(longTime);
            var result = new float[(int)(length * IFootageSource.SupportAudioSamplingRate) * 2];

            var writeCount = 0;
            var rewindCount = 0;
            var isFirst = true;
            while (writeCount < result.Length)
            {
                var sample = Reader.ReadSample(SourceReaderIndex.FirstAudioStream, 0, out var _, out var flags, out var _);

                if ((flags & SourceReaderFlag.EndOfStream) != 0)
                {
                    break;
                }

                if (sample == null)
                {
                    continue;
                }

                var timestamp = sample.SampleTime;
                if (isFirst && timestamp > longTime)
                {
                    sample.Dispose();
                    if (rewindCount > RewindLimit)
                    {
                        break;
                    }
                    Reader.SetCurrentPosition(longTime - RewindRetrySpan * (rewindCount + 1));
                    rewindCount++;
                    continue;
                }
                else if (timestamp + sample.SampleDuration < longTime)
                {
                    sample.Dispose();
                    continue;
                }

                using var buffer = sample.ConvertToContiguousBuffer();
                buffer.Lock(out nint ptr, out int _, out int bufferLength);

                if (isFirst && timestamp < longTime)
                {
                    var skipSamples = (int)((longTime - timestamp) / DurationRate * IFootageSource.SupportAudioSamplingRate) * 2;
                    ptr += skipSamples * sizeof(float);
                    bufferLength -= skipSamples * sizeof(float);
                }
                var needWrite = Math.Min(bufferLength / sizeof(float), result.Length - writeCount);
                Marshal.Copy(ptr, result, writeCount, needWrite);
                writeCount += needWrite;

                buffer.Unlock();
                isFirst = false;
            }

            return result;
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }
    }
}
