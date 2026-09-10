using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NiVE3.Util;

namespace NiVE3.Audio
{
    class ScrubWaveProvider : IWaveProvider
    {
        const int BytePerSample = sizeof(float) * Const.AudioChannelCount;

        static readonly (float[] sample, int offset, int length) EmptyRemainSample = (Array.Empty<float>(), 0, 0);

        public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(Const.AudioSamplingRate, Const.AudioChannelCount);

        Queue<(float[] sample, int length)> SampleQueue { get; } = new Queue<(float[], int)>();

        (float[] sample, int offset, int length) RemainSample { get; set; } = EmptyRemainSample;

        public void AddSample(float[] samples)
        {
            var windowedSample = ArrayPool<float>.Shared.Rent(samples.Length);
            windowedSample.AsSpan(0, samples.Length).Clear();
            samples.CopyTo(windowedSample, 0);
            for (var i = 0; i < samples.Length; i++)
            {
                windowedSample[i] = samples[i] * GetSampleWindow(samples.Length, i);
            }
            lock (SampleQueue)
            {
                SampleQueue.Enqueue((windowedSample, samples.Length));
            }
        }

        public void ClearBuffer()
        {
            lock (SampleQueue)
            {
                SampleQueue.Clear();
            }
        }

        public int Read(Span<byte> buffer)
        {
            var count = buffer.Length;

            var bufferCursor = 0;
            var lastSample = EmptyRemainSample;
            if (RemainSample.length - RemainSample.offset > 0)
            {
                var writeSampleCount = WriteSample(buffer, RemainSample.sample.AsSpan(RemainSample.offset, RemainSample.length - RemainSample.offset));
                bufferCursor += writeSampleCount * sizeof(float);
                lastSample = (RemainSample.sample, RemainSample.offset + writeSampleCount, RemainSample.length);
            }
            else
            {
                while (bufferCursor < buffer.Length)
                {
                    lock (SampleQueue)
                    {
                        if (SampleQueue.Count < 1)
                        {
                            lastSample = EmptyRemainSample;
                            break;
                        }

                        var sample = SampleQueue.Dequeue();

                        var writeSampleCount = WriteSample(buffer[bufferCursor..], sample.sample.AsSpan(0, sample.length));
                        bufferCursor += writeSampleCount * sizeof(float);
                        if (lastSample.length > 0)
                        {
                            ArrayPool<float>.Shared.Return(lastSample.sample);
                        }
                        lastSample = (sample.sample, writeSampleCount, sample.length);
                    }
                }
            }

            if (lastSample.offset >= lastSample.length)
            {
                if (lastSample.length > 0)
                {
                    ArrayPool<float>.Shared.Return(lastSample.sample);
                }
                RemainSample = EmptyRemainSample;
            }
            else
            {
                RemainSample = lastSample;
            }

            if (bufferCursor < buffer.Length)
            {
                buffer[bufferCursor..].Clear();
            }

            return count;
        }

        static int WriteSample(Span<byte> buffer, Span<float> samples)
        {
            var count = Math.Min(buffer.Length / sizeof(float), samples.Length);

            MemoryMarshal.Cast<float, byte>(samples[..count]).CopyTo(buffer);

            return count;
        }

        static float GetSampleWindow(int sampleCount, int index)
        {
            var edgeArea = (int)(sampleCount * 0.05);
            var p = Math.PI / edgeArea;
            if (index < edgeArea)
            {
                return (float)(Math.Cos(p * index - Math.PI) * 0.5 + 0.5);
            }
            else if (index + edgeArea >= sampleCount)
            {
                return (float)(Math.Cos(Math.PI - p * (sampleCount - index)) * 0.5 + 0.5);
            }
            else
            {
                return 1.0F;
            }
        }
    }
}
