using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Wave;
using NiVE3.Util;

namespace NiVE3.Audio
{
    class SpeedChangeableWaveProvider : IWaveProvider
    {
        const int BytePerSample = sizeof(float) * Const.AudioChannelCount;

        const int PositionHistoryEntry = 100;

        public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(Const.AudioSamplingRate, Const.AudioChannelCount);

        public int Position { get; set; }

        public int LoopStart { get; set; }

        public int LoopEnd { get; set; }

        RingBuffer<(long startBytes, long endBytes, int positionStart, int sampleCount)> PositionHistory { get; } = new RingBuffer<(long, long, int, int)>(PositionHistoryEntry);

        long TotalReadBytes { get; set; }

        double speed;
        public double Speed
        {
            get { return speed; }
            set
            {
                if (speed != value)
                {
                    speed = value;
                    UpdateSpeed();
                }
            }
        }

        public float[] Audio { get; private set; } = [];

        WdlResampler Resampler { get; }

        public SpeedChangeableWaveProvider()
        {
            Resampler = new WdlResampler();
            UpdateSpeed();
        }

        public void SetAudio(float[] audio)
        {
            Audio = audio;
            LoopStart = 0;
            LoopEnd = audio.Length;
            Position = 0;
            ClearHistory();
        }

        public void ClearHistory()
        {
            TotalReadBytes = 0;
            PositionHistory.Clear();
        }

        public int GetActualPosition(long bytes)
        {
            var (startBytes, endBytes, positionStart, sampleCount) = PositionHistory.FirstOrDefault(t => (t.startBytes <= bytes && t.endBytes > bytes));
            if (sampleCount < 1)
            {
                return PositionHistory.FirstOrDefault().positionStart;
            }

            var positionInEntry = (int)((bytes - startBytes) / sizeof(float) / Const.AudioChannelCount / (double)((endBytes - startBytes) / sizeof(float) / Const.AudioChannelCount) * sampleCount) * Const.AudioChannelCount;
            var actualPosition = positionStart + positionInEntry;
            while (actualPosition >= LoopEnd)
            {
                actualPosition = LoopStart + (actualPosition - LoopEnd);
            }

            return actualPosition;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (Audio.Length < Const.AudioChannelCount || LoopStart >= LoopEnd)
            {
                return 0;
            }

            if (Position < 0 && -Position < count)
            {
                Position += count;
                return count;
            }

            buffer.AsSpan(offset, count).Clear();

            var requestFrameCount = count / sizeof(float) / Const.AudioChannelCount;
            var resamplerNeeded = Resampler.ResamplePrepare(requestFrameCount, Const.AudioChannelCount, out var inBuffer, out var inBufferOffset) * Const.AudioChannelCount;

            var positionStart = Position;
            if (resamplerNeeded > 0)
            {
                var bufferFilledCount = -Math.Min(Position, 0);
                Position = Math.Max(Position, 0);
                while (bufferFilledCount < resamplerNeeded)
                {
                    var copyCount = Math.Min(resamplerNeeded - bufferFilledCount, LoopEnd - Position);
                    Audio.AsSpan(Position, copyCount).CopyTo(inBuffer.AsSpan(inBufferOffset + bufferFilledCount));
                    bufferFilledCount += copyCount;

                    Position += copyCount;
                    if (Position >= LoopEnd)
                    {
                        Position = LoopStart;
                    }
                }

                var floatOutputBuffer = ArrayPool<float>.Shared.Rent(count / sizeof(float));
                floatOutputBuffer.AsSpan().Clear();
                var outCount = Resampler.ResampleOut(floatOutputBuffer, 0, resamplerNeeded, requestFrameCount, Const.AudioChannelCount) * Const.AudioChannelCount;

                floatOutputBuffer.AsSpan(0, outCount).CopyTo(MemoryMarshal.Cast<byte, float>(buffer.AsSpan(offset)));
                ArrayPool<float>.Shared.Return(floatOutputBuffer);

                PositionHistory.Append((TotalReadBytes, TotalReadBytes + count, positionStart, resamplerNeeded / Const.AudioChannelCount));
            }
            else
            {
                var floatBuffer = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(offset, count));
                var filledCount = -Math.Min(Position, 0);
                Position = Math.Max(Position, 0);
                while (filledCount < floatBuffer.Length)
                {
                    var copyCount = Math.Min(floatBuffer.Length - filledCount, LoopEnd - Position);
                    if (copyCount < 1)
                    {
                        Position = LoopStart;
                        continue;
                    }

                    Audio.AsSpan(Position, copyCount).CopyTo(floatBuffer);
                    filledCount += copyCount;

                    Position += copyCount;
                    if (Position >= LoopEnd)
                    {
                        Position = LoopStart;
                    }
                }

                PositionHistory.Append((TotalReadBytes, TotalReadBytes + count, positionStart, filledCount / Const.AudioChannelCount));
            }

            TotalReadBytes += count;

            return count;
        }

        void UpdateSpeed()
        {
            var virtualSamplingRate = Const.AudioSamplingRate * Speed;
            Resampler.Reset();
            Resampler.SetMode(true, 2, true);
            Resampler.SetFilterParms();
            Resampler.SetFeedMode(false);
            Resampler.SetRates(virtualSamplingRate, Const.AudioSamplingRate);
        }
    }
}
