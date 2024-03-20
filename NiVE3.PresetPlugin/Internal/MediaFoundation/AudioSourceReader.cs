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
        const int FirstAudioStreamId = (int)MidlMidlItfMfreadwrite000000010001.MfSourceReaderFirstAudioStream;

        const double DurationRate = 1E7;

        const long RewindRetrySpan = 1000;

        public double Duration { get; }

        public bool Success { get; }

        string FilePath { get; }

        IMFSourceReader? Reader { get; }

        public AudioSourceReader(string filePath)
        {
            FilePath = filePath;

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
            if (mediaType.SetUInt32(MediaTypeAttributeKeys.AudioSamplesPerSecond, IFootageSource.SupportAudioSamplingRate).Failure)
            {
                return;
            }
            if (mediaType.SetUInt32(MediaTypeAttributeKeys.AudioNumChannels, IFootageSource.SupportChannelCount).Failure)
            {
                return;
            }

            Reader = MediaFactory.MFCreateSourceReaderFromURL(filePath, null);
            Reader.SetCurrentMediaType(FirstAudioStreamId, mediaType);
            Reader.SetStreamSelection(FirstAudioStreamId, true);

            using var currentMediaType = Reader.GetCurrentMediaType(FirstAudioStreamId);
            Success = currentMediaType.Get<Guid>(MediaTypeAttributeKeys.Subtype) == AudioFormatGuids.Float &&
                currentMediaType.Get<int>(MediaTypeAttributeKeys.AudioSamplesPerSecond) == IFootageSource.SupportAudioSamplingRate &&
                currentMediaType.Get<int>(MediaTypeAttributeKeys.AudioNumChannels) == IFootageSource.SupportChannelCount &&
                currentMediaType.Get<int>(MediaTypeAttributeKeys.AudioBitsPerSample) == sizeof(float) * 8;

            Duration = GetDuration();
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
                return Array.Empty<float>();
            }

            var longTime = (long)(time * DurationRate);
            var pos = new Variant { ElementType = VariantElementType.Long, Value = longTime };
            Reader.SetCurrentPosition(Guid.Empty, pos);
            var result = new float[(int)(length * IFootageSource.SupportAudioSamplingRate) * 2];

            var writeCount = 0;
            var rewindCount = 0;
            var isFirst = true;
            while (writeCount < result.Length)
            {
                Reader.ReadSample(FirstAudioStreamId, 0, out int _, out int flags, out long _, out IMFSample? sample);

                if ((flags & (int)SourceReaderFlag.FEndofstream) != 0)
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
                    pos = new Variant { ElementType = VariantElementType.Long, Value = longTime - RewindRetrySpan * (rewindCount + 1) };
                    Reader.SetCurrentPosition(Guid.Empty, pos);
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
