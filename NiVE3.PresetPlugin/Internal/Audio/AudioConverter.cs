using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NAudio.Dsp;

namespace NiVE3.PresetPlugin.Internal.Audio
{
    static class AudioConverter
    {
        public static float[] ConvertToSpecificFormat(ReadOnlySpan<byte> data, int samplingRate, int channel, int bitPerSample)
        {
            var floatData = Array.Empty<float>();
            switch (channel)
            {
                case 0:
                    return [];
                case 1:
                    {
                        var temp = ArrayPool<byte>.Shared.Rent(data.Length * 2);
                        var sampleSize = bitPerSample / 8;
                        for (int i = 0, ti = 0; i < data.Length; ti += sampleSize)
                        {
                            for (var c = 0; c < sampleSize; c++, i++, ti++)
                            {
                                temp[ti] = temp[ti + sampleSize] = data[i];
                            }
                        }
                        floatData = ConvertQuantizedToFloat(temp, bitPerSample);
                        ArrayPool<byte>.Shared.Return(temp);
                    }
                    break;
                case 2:
                    floatData = ConvertQuantizedToFloat(data, bitPerSample);
                    break;
                default:
                    {
                        var temp = ArrayPool<byte>.Shared.Rent(data.Length / channel * 2);
                        var stereoBlockSize = (bitPerSample / 8) * 2;
                        var blockSize = (bitPerSample / 8) * channel;
                        for (int i = 0, ti = 0; i < data.Length; i += blockSize)
                        {
                            for (var c = 0; c < stereoBlockSize; c++, ti++)
                            {
                                temp[ti] = data[i + c];
                            }
                        }
                        floatData = ConvertQuantizedToFloat(temp, bitPerSample);
                        ArrayPool<byte>.Shared.Return(temp);
                    }
                    break;
            }

            if (samplingRate != Const.AudioSamplingRate)
            {
                return ConvertSamplingRate(floatData, samplingRate, Const.AudioSamplingRate, Const.AudioChannelCount);
            }
            else
            {
                return floatData;
            }
        }

        public static float[] ConvertSamplingRate(float[] audio, int baseSamplingRate, int targetSamplingRate, int channel)
        {
            var resampler = new WdlResampler();
            resampler.SetMode(true, 2, true);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false);
            resampler.SetRates(baseSamplingRate, targetSamplingRate);

            var resampledAudio = new float[(int)(audio.Length / channel / (double)baseSamplingRate * targetSamplingRate) * channel];
            var needed = resampler.ResamplePrepare(resampledAudio.Length / channel, channel, out var buffer, out var offset);
            audio.AsSpan(0, Math.Min(audio.Length, needed * channel)).CopyTo(buffer.AsSpan(offset));
            resampler.ResampleOut(resampledAudio, 0, needed, resampledAudio.Length / channel, channel);
            return resampledAudio;
        }

        public static float[] ConvertQuantizedToFloat(ReadOnlySpan<byte> data, int bitPerSample)
        {
            switch (bitPerSample)
            {
                case 32:
                    {
                        var dataSpan = MemoryMarshal.Cast<byte, float>(data);
                        var result = new float[dataSpan.Length];
                        dataSpan.CopyTo(result);
                        return result;
                    }
                case 24:
                    {
                        var result = new float[data.Length / 3];
                        for (int i = 0, di = 0; i < result.Length; i++, di += 3)
                        {
                            result[i] = (((sbyte)data[i + 2] << 16) | (data[i + 1] << 8) | data[i]) / (float)0x7FFFFF;
                        }
                        return result;
                    };
                case 16:
                    {
                        var dataSpan = MemoryMarshal.Cast<byte, short>(data);
                        var result = new float[dataSpan.Length];
                        for (var i = 0; i < result.Length; i++)
                        {
                            result[i] = dataSpan[i] / (float)short.MaxValue;
                        }
                        return result;
                    }
                case 8:
                    {
                        var result = new float[data.Length];
                        for (var i = 0; i < data.Length; i++)
                        {
                            result[i] = (data[i] - 128) / 128.0F;
                        }
                        return result;
                    }
            }

            return [];
        }
    }
}
