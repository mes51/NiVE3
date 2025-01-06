using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace NiVE3.PresetPlugin.Internal.DirectShow
{
    abstract class DirectShowReaderBase<T> : IDisposable where T : SampleGrabberCallbackBase
    {
        protected const int TimeRoundDigit = 10;

        protected const double MediaTimeRate = 10000000.0;

        const int WaitingStateChangingCountLimit = 10;

        public double FrameRate { get; init; }

        public double Duration { get; init; }

        public bool IsLoaded { get; init; }

        protected IGraphBuilder GraphBuilder { get; }

        protected ISampleGrabber VideoSampleGrabber { get; }

        protected ISampleGrabber AudioSampleGrabber { get; }

        protected IBaseFilter NullRenderer { get; }

        protected abstract T? Sampler { get; }

        protected bool IsSupportedFrame { get; init; }

        protected bool IsLocked { get; private set; }

        protected bool Disposed { get; private set; }

        protected IMediaSeeking MediaSeeking => (IMediaSeeking)GraphBuilder;

        protected IMediaControl MediaControl => (IMediaControl)GraphBuilder;

        protected DirectShowReaderBase(bool withoutAudioMediaType, bool audioSubTypeIsFloat)
        {
            GraphBuilder = (IGraphBuilder)new FilterGraph();
            VideoSampleGrabber = (ISampleGrabber)new SampleGrabber();
            AudioSampleGrabber = (ISampleGrabber)new SampleGrabber();
            NullRenderer = (IBaseFilter)new NullRenderer();

            var mediaType = new PAMMediaType
            {
                majortype = MediaType.Video,
                subtype = MediaSubType.RGB32,
                formattype = Format.VideoInfo
            };

            var hr = VideoSampleGrabber.SetMediaType(mediaType);
            hr.ThrowIfFailed();

            if (!withoutAudioMediaType)
            {
                mediaType = new PAMMediaType
                {
                    majortype = MediaType.Audio,
                    subtype = audioSubTypeIsFloat ? MediaSubType.IEEEFloat : MediaSubType.PCM,
                    formattype = Format.WaveFormatEx
                };

                hr = AudioSampleGrabber.SetMediaType(mediaType);
                hr.ThrowIfFailed();
            }
        }

        protected bool SetTime(double time)
        {
            if (!IsLoaded || Sampler == null)
            {
                return false;
            }

            Sampler.SetTargetSamplingTime(time);
            while (IsLocked)
            {
                Thread.Sleep(1);
            }

            IsLocked = true;
            var position = 0L;
            if (IsSupportedFrame)
            {
                position = (long)Math.Round(time * FrameRate);
            }
            else
            {
                position = (long)Math.Round(time * MediaTimeRate);
            }
            var dummy = 0L;
            var hr = MediaSeeking.SetPositions(position, AMSeekingFlags.AbsolutePositioning, ref dummy, AMSeekingFlags.NoPositioning);
            IsLocked = false;

            return hr.Succeeded || hr == 1;
        }

        protected void WaitToReady()
        {
            const int VFW_S_STATE_INTERMEDIATE = 0x40237;
            const int VFW_S_CANT_CUE = 0x40268;

            var state = FilterState.Running;
            var oldTime = 0.0;
            var lastCheckTick = DateTime.Now.Ticks;
            while (state == FilterState.Running)
            {
                var count = 0;
                var hr = (HRESULT)HRESULT.S_OK;
                do
                {
                    hr = MediaControl.GetState(1, out state);
                    if (hr == VFW_S_STATE_INTERMEDIATE)
                    {
                        count++;
                    }
                    MediaSeeking.GetCurrentPosition(out var time);
                    if ((IsSupportedFrame && time / FrameRate >= Duration) || time / MediaTimeRate >= Duration || (oldTime == time && DateTime.Now.Ticks - lastCheckTick > TimeSpan.TicksPerSecond))
                    {
                        Pause();
                    }
                    if (oldTime != time)
                    {
                        oldTime = time;
                        lastCheckTick = DateTime.Now.Ticks;
                    }
                }
                while (hr.Failed && hr != VFW_S_CANT_CUE && count < WaitingStateChangingCountLimit);
            }
        }

        protected void Pause()
        {
            if (!IsLocked)
            {
                IsLocked = true;
                MediaControl.Pause();
                IsLocked = false;
            }
        }

        protected static HRESULT ReconnectFilter(IGraphBuilder graphBuilder, IBaseFilter outFilter, IBaseFilter? connected, IBaseFilter inFilter)
        {
            var hr = (HRESULT)HRESULT.E_ABORT;

            var inPin = GetPinFirst(inFilter, PinDirection.Input);
            if (inPin == null)
            {
                return hr;
            }
            var outPin = (IPin?)null;

            if (connected != null)
            {
                outPin = GetConnectedPin(outFilter, connected, PinDirection.Output);
                hr = graphBuilder.RemoveFilter(connected);
                Marshal.ReleaseComObject(connected);
            }
            else
            {
                outPin = GetPinFirst(outFilter, PinDirection.Output);
            }

            if (outPin != null)
            {
                hr = graphBuilder.Connect(outPin, inPin);
                Marshal.ReleaseComObject(outPin);
            }
            Marshal.ReleaseComObject(inPin);

            return hr;
        }

        protected static IPin? GetConnectedPin(IBaseFilter outFilter, IBaseFilter inFilter, PinDirection direction)
        {
            var pin = (IPin?)null;

            var targetFilter = direction == PinDirection.Output ? outFilter : inFilter;
            var fromFilter = direction == PinDirection.Output ? inFilter : outFilter;
            var fromDirection = direction == PinDirection.Output ? PinDirection.Input : PinDirection.Output;

            var pins = GetAllPins(fromFilter, fromDirection);
            if (pins.Length < 1)
            {
                return null;
            }

            foreach (var p in pins)
            {
                p.ConnectedTo(out var connectedPin);
                if (connectedPin == null)
                {
                    continue;
                }
                using var info = new PinInfo();
                connectedPin.QueryPinInfo(info);
                if (outFilter == info.pFilter)
                {
                    pin = connectedPin;
                    break;
                }
            }

            foreach (var p in pins)
            {
                Marshal.ReleaseComObject(p);
            }
            return pin;
        }

        protected static IBaseFilter? GetConnectedFilter(IBaseFilter filter, PinDirection direction)
        {
            var pin = GetPinFirst(filter, direction);
            if (pin == null)
            {
                return null;
            }

            pin.ConnectedTo(out var connected);
            Marshal.ReleaseComObject(pin);

            if (connected == null)
            {
                return null;
            }

            var info = new PinInfo();
            connected.QueryPinInfo(info);
            return info.pFilter;
        }

        protected static HRESULT RemoveConnectedFilter(IGraphBuilder graphBuilder, IBaseFilter filter, PinDirection direction)
        {
            var connected = GetConnectedFilter(filter, direction);

            if (connected == null)
            {
                return HRESULT.S_OK;
            }

            return graphBuilder.RemoveFilter(connected);
        }

        protected static IPin[] GetAllPins(IBaseFilter filter, PinDirection direction)
        {
            filter.EnumPins(out var pins);
            if (pins == null)
            {
                return [];
            }

            var result = new List<IPin>();
            while (pins.Next(1, out var pin, out var fetch) == 0)
            {
                if (pin == null)
                {
                    continue;
                }

                pin.QueryDirection(out var dir);
                if (dir == direction)
                {
                    result.Add(pin);
                }
                else
                {
                    Marshal.ReleaseComObject(pin);
                }
            }

            Marshal.ReleaseComObject(pins);

            return result.ToArray();
        }

        protected static IPin? GetPinFirst(IBaseFilter filter, PinDirection direction)
        {
            filter.EnumPins(out var pins);
            if (pins == null)
            {
                return null;
            }

            var pin = (IPin?)null;
            while (pins.Next(1, out pin, out var fetch) == 0)
            {
                if (pin == null)
                {
                    continue;
                }

                pin.QueryDirection(out var dir);
                if (dir == direction)
                {
                    break;
                }
                else
                {
                    Marshal.ReleaseComObject(pin);
                    pin = null;
                }
            }

            Marshal.ReleaseComObject(pins);
            return pin;
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                Disposed = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            Marshal.ReleaseComObject(GraphBuilder);
            Marshal.ReleaseComObject(VideoSampleGrabber);
            Marshal.ReleaseComObject(AudioSampleGrabber);
            Marshal.ReleaseComObject(NullRenderer);
        }

        ~DirectShowReaderBase()
        {
            Dispose(true);
        }
    }

    class DirectShowVideoReader : DirectShowReaderBase<VideoSampler>
    {
        const string AviDecoderName = "Avi Decoder";

        protected override VideoSampler? Sampler { get; }

        public bool PossibilityArgb { get; }

        public bool OutputIs32bpc { get; }

        public int Width { get; }

        public int Height { get; }

        IBaseFilter AviDecoder { get; }

        public DirectShowVideoReader(string filePath) : base(false, false)
        {
            AviDecoder = (IBaseFilter)new AviDecoder();

            var isAvi = Path.GetExtension(filePath) == ".avi";

            try
            {
                var hr = GraphBuilder.AddFilter((IBaseFilter)VideoSampleGrabber, "Video SampleGrabber");
                hr.ThrowIfFailed();
                hr = GraphBuilder.AddFilter((IBaseFilter)AudioSampleGrabber, "Audio SampleGrabber");
                hr.ThrowIfFailed();

                if (isAvi)
                {
                    hr = GraphBuilder.AddFilter(AviDecoder, AviDecoderName);
                    hr.ThrowIfFailed();
                }

                hr = GraphBuilder.RenderFile(filePath, null);
                hr.ThrowIfFailed();

                using var am = new PAMMediaType();
                hr = VideoSampleGrabber.GetConnectedMediaType(am);
                if (hr.Failed || am.formattype != Format.VideoInfo || am.pbFormat == nint.Zero)
                {
                    return;
                }

                var videoInfo = Marshal.PtrToStructure<VIDEOINFOHEADER>(am.pbFormat);
                GraphBuilder.AddFilter(NullRenderer, "Video NullRenderer");
                hr = ReconnectFilter(GraphBuilder, (IBaseFilter)VideoSampleGrabber, GetConnectedFilter((IBaseFilter)VideoSampleGrabber, PinDirection.Output), NullRenderer);
                hr.ThrowIfFailed();

                FrameRate = Math.Round(MediaTimeRate / videoInfo.AvgTimePerFrame, TimeRoundDigit);
                Width = videoInfo.bmiHeader.biWidth;
                Height = videoInfo.bmiHeader.biHeight;

                if (isAvi)
                {
                    var grabberBeforeIsDecoder = false;
                    var grabberInputFilter = GetConnectedFilter((IBaseFilter)VideoSampleGrabber, PinDirection.Input);
                    if (grabberInputFilter != null)
                    {
                        using var filterInfo = new FilterInfo();
                        grabberInputFilter.QueryFilterInfo(filterInfo);
                        grabberBeforeIsDecoder = filterInfo.achName == AviDecoderName;
                        Marshal.ReleaseComObject(grabberInputFilter);
                    }

                    if (grabberBeforeIsDecoder)
                    {
                        var pin = GetPinFirst(AviDecoder, PinDirection.Input);
                        if (pin != null)
                        {
                            using var decoderMediaType = new PAMMediaType();
                            if (pin.ConnectionMediaType(decoderMediaType).Succeeded)
                            {
                                if (decoderMediaType.formattype == Format.VideoInfo && decoderMediaType.pbFormat != nint.Zero)
                                {
                                    PossibilityArgb = Marshal.PtrToStructure<VIDEOINFOHEADER>(decoderMediaType.pbFormat).bmiHeader.biBitCount == 32;
                                }
                            }
                            Marshal.ReleaseComObject(pin);
                        }

                        pin = GetPinFirst((IBaseFilter)VideoSampleGrabber, PinDirection.Input);
                        if (pin != null)
                        {
                            using var grabberMediaType = new PAMMediaType();
                            if (pin.ConnectionMediaType(grabberMediaType).Succeeded)
                            {
                                if (grabberMediaType.formattype == Format.VideoInfo && grabberMediaType.pbFormat != nint.Zero)
                                {
                                    OutputIs32bpc = Marshal.PtrToStructure<VIDEOINFOHEADER>(grabberMediaType.pbFormat).bmiHeader.biBitCount == 32;
                                }
                            }
                            Marshal.ReleaseComObject(pin);
                        }
                    }
                    else
                    {
                        var decoderInputFilter = GetConnectedFilter(AviDecoder, PinDirection.Input);
                        if (decoderInputFilter == null)
                        {
                            var pin = GetPinFirst((IBaseFilter)VideoSampleGrabber, PinDirection.Input);
                            if (pin != null)
                            {
                                using var grabberMediaType = new PAMMediaType();
                                if (pin.ConnectionMediaType(grabberMediaType).Succeeded)
                                {
                                    if (grabberMediaType.formattype == Format.VideoInfo && grabberMediaType.pbFormat != nint.Zero)
                                    {
                                        PossibilityArgb = Marshal.PtrToStructure<VIDEOINFOHEADER>(grabberMediaType.pbFormat).bmiHeader.biBitCount == 32;
                                        OutputIs32bpc = PossibilityArgb;
                                    }
                                }
                                Marshal.ReleaseComObject(pin);
                            }
                        }
                        else
                        {
                            Marshal.ReleaseComObject(decoderInputFilter);
                        }
                    }
                }

                hr = RemoveConnectedFilter(GraphBuilder, (IBaseFilter)AudioSampleGrabber, PinDirection.Output);
                hr.ThrowIfFailed();
                hr = GraphBuilder.RemoveFilter((IBaseFilter)AudioSampleGrabber);
                hr.ThrowIfFailed();

                ((IMediaFilter)GraphBuilder).SetSyncSource(null);
                IsSupportedFrame = FrameRate > 0.0 && MediaSeeking.IsFormatSupported(TimeFormat.Frame).Succeeded;

                if (IsSupportedFrame)
                {
                    hr = MediaSeeking.SetTimeFormat(TimeFormat.Frame);
                    hr.ThrowIfFailed();
                    hr = MediaSeeking.GetDuration(out var durationFrames);
                    hr.ThrowIfFailed();
                    Duration = Math.Round(durationFrames / FrameRate, TimeRoundDigit);
                }
                else
                {
                    hr = MediaSeeking.SetTimeFormat(TimeFormat.MediaTime);
                    hr.ThrowIfFailed();
                    hr = MediaSeeking.GetDuration(out var durationMediaTime);
                    hr.ThrowIfFailed();
                    Duration = Math.Round(durationMediaTime / MediaTimeRate, TimeRoundDigit);
                }

                hr = MediaControl.Run();
                hr.ThrowIfFailed();
                hr = MediaControl.Pause();
                hr.ThrowIfFailed();

                Sampler = new VideoSampler(FrameRate);
                Sampler.SampleCompleted += Sampler_SampleCompleted;
                VideoSampleGrabber.SetCallback(Sampler, 1);

                IsLoaded = true;
            }
            catch { }
        }

        public byte[] GetImage(double time)
        {
            if (!IsLoaded || Sampler == null)
            {
                throw new InvalidOperationException();
            }

            var diffFrame = (int)Math.Round(time * FrameRate) - (int)Math.Round(Sampler.TargetSamplingTime * FrameRate);

            if (!SetTime(time))
            {
                return [];
            }
            if (diffFrame == 0 || diffFrame == 1)
            {
                Sampler.ForceGetNextFrame();
            }
            else
            {
                Sampler.SetTargetSamplingTime(time);
            }

            var backTime = time;
            MediaControl.Run();
            WaitToReady();
            while (Sampler.OverTime > 0.0 && backTime > 0.0)
            {
                backTime = Math.Max(backTime - 1.0, 0.0);
                SetTime(backTime);
                Sampler.SetTargetSamplingTime(time);
                MediaControl.Run();
                WaitToReady();
            }

            if (Sampler.IsCompleted)
            {
                return Sampler.SampledBuffer;
            }
            else
            {
                return [];
            }
        }

        private void Sampler_SampleCompleted(object? sender, EventArgs e)
        {
            Pause();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (AviDecoder != null)
            {
                Marshal.ReleaseComObject(AviDecoder);
            }
        }
    }

    class DirectShowAudioReader : DirectShowReaderBase<AudioSampler>
    {
        protected override AudioSampler? Sampler { get; }

        public int SamplingRate { get; }

        public int Channel { get; }

        public int BitPerSample { get; }

        public int BlockSize { get; }

        public DirectShowAudioReader(string filePath, bool withoutMediaType, bool audioSubTypeIsFloat) : base(withoutMediaType, audioSubTypeIsFloat)
        {
            try
            {
                var hr = GraphBuilder.AddFilter((IBaseFilter)AudioSampleGrabber, "Audio SampleGrabber");
                hr.ThrowIfFailed();
                hr = GraphBuilder.AddFilter((IBaseFilter)VideoSampleGrabber, "Video SampleGrabber");
                hr.ThrowIfFailed();

                hr = GraphBuilder.RenderFile(filePath, null);
                hr.ThrowIfFailed();

                using var am = new PAMMediaType();
                hr = AudioSampleGrabber.GetConnectedMediaType(am);
                if (hr.Failed || am.formattype != Format.WaveFormatEx || am.pbFormat == nint.Zero)
                {
                    return;
                }

                var waveFormat = Marshal.PtrToStructure<WinMm.WAVEFORMATEX>(am.pbFormat);
                GraphBuilder.AddFilter(NullRenderer, "Audio NullRenderer");
                hr = ReconnectFilter(GraphBuilder, (IBaseFilter)AudioSampleGrabber, GetConnectedFilter((IBaseFilter)AudioSampleGrabber, PinDirection.Output), NullRenderer);
                hr.ThrowIfFailed();

                SamplingRate = (int)waveFormat.nSamplesPerSec;
                Channel = waveFormat.nChannels;
                BitPerSample = waveFormat.wBitsPerSample;
                BlockSize = waveFormat.nBlockAlign;

                hr = RemoveConnectedFilter(GraphBuilder, (IBaseFilter)VideoSampleGrabber, PinDirection.Output);
                hr.ThrowIfFailed();

                ((IMediaFilter)GraphBuilder).SetSyncSource(null);

                hr = MediaSeeking.SetTimeFormat(TimeFormat.MediaTime);
                hr.ThrowIfFailed();
                hr = MediaSeeking.GetDuration(out var durationMediaTime);
                hr.ThrowIfFailed();
                Duration = Math.Round(durationMediaTime / MediaTimeRate, TimeRoundDigit);

                hr = MediaControl.Run();
                hr.ThrowIfFailed();
                hr = MediaControl.Pause();
                hr.ThrowIfFailed();

                Sampler = new AudioSampler(SamplingRate, waveFormat.nBlockAlign);
                Sampler.SampleCompleted += Sampler_SampleCompleted;
                AudioSampleGrabber.SetCallback(Sampler, 1);

                IsLoaded = true;
            }
            catch { }
        }

        public byte[] GetAudio(double time, double length)
        {
            if (!IsLoaded || Sampler == null)
            {
                throw new InvalidOperationException();
            }

            if (!SetTime(time))
            {
                return [];
            }
            Sampler.SetSampleLength(length);

            var backTime = time;
            MediaControl.Run();
            WaitToReady();
            while (Sampler.OverTime > 0.0 && backTime > 0.0)
            {
                backTime = Math.Max(backTime - 1.0, 0.0);
                SetTime(backTime);
                Sampler.SetTargetSamplingTime(time);
                MediaControl.Run();
                WaitToReady();
            }

            if (Sampler.IsCompleted)
            {
                return [..Sampler.AudioData];
            }
            else
            {
                return [];
            }
        }

        private void Sampler_SampleCompleted(object? sender, EventArgs e)
        {
            Pause();
        }
    }
}
