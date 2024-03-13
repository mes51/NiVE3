using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NiVE3.Util;

namespace NiVE3.Audio
{
    class SampleBufferedWaveProvider : IWaveProvider
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

        public int Read(byte[] buffer, int offset, int count)
        {
            if (count < 1)
            {
                return 0;
            }

            using var ms = new MemoryStream(buffer, offset, count);
            var lastSample = EmptyRemainSample;
            if (RemainSample.length - RemainSample.offset > 0)
            {
                var writeSampleCount = WriteSample(ms, RemainSample.sample.AsSpan(RemainSample.offset, RemainSample.length - RemainSample.offset));
                lastSample = (RemainSample.sample, RemainSample.offset + writeSampleCount, RemainSample.length);
            }
            else
            {
                while (ms.Position < ms.Length)
                {
                    lock(SampleQueue)
                    {
                        if (SampleQueue.Count < 1)
                        {
                            lastSample = EmptyRemainSample;
                            break;
                        }

                        var sample = SampleQueue.Dequeue();

                        var writeSampleCount = WriteSample(ms, sample.sample.AsSpan(0, sample.length));
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

            if (ms.Position < ms.Length)
            {
                buffer.AsSpan((int)ms.Position).Clear();
            }

            return count;
        }

        static int WriteSample(MemoryStream ms, Span<float> samples)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                var sampleData = BitConverter.GetBytes(samples[i]);
                ms.Write(sampleData, 0, sampleData.Length);

                if (ms.Position >= ms.Length)
                {
                    return i + 1;
                }
            }

            return samples.Length;
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
