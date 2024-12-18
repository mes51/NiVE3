using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.DirectShow;
using Vanara.PInvoke;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(DirectShowInput), "DirectShowInput", "", "mes51", ID, "*.avi")]
    public sealed class DirectShowInput : IInput
    {
        const string ID = "BE18C71D-A752-4814-807A-2DD97D7656C3";

        public string FilePath { get; private set; } = "";

        DirectShowVideoReader? VideoReader { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public FootageSourceGroup GetGroup()
        {
            if (VideoReader?.IsLoaded ?? false)
            {
                return new FootageSourceGroup([new DirectShowVideoFootageSource(VideoReader, VideoAlphaType.PreMultiply)]);
            }
            else
            {
                return FootageSourceGroup.Empty;
            }
        }

        public bool Load(string filePath)
        {
            VideoReader = new DirectShowVideoReader(filePath);
            FilePath = filePath;
            return VideoReader.IsLoaded;
        }

        public void Dispose()
        {
            VideoReader?.Dispose();
        }
    }

    class HiddenWindow : IDisposable
    {
        public User32.SafeHWND Handle { get; }

        bool Disposed { get; set; }

        User32.WNDCLASSEX WindowClassInfo { get; }

        public HiddenWindow()
        {
            WindowClassInfo = GenerateWindowClass();

            var windowClass = WindowClassInfo;
            if (User32.RegisterClassEx(windowClass).IsInvalid)
            {
                throw new InvalidOperationException();
            }

            Handle = User32.CreateWindowEx(
                User32.WindowStylesEx.WS_EX_APPWINDOW | User32.WindowStylesEx.WS_EX_WINDOWEDGE,
                windowClass.lpszClassName,
                "DirectShowInput dummy window",
                0,
                0,
                0,
                8,
                8,
                HWND.NULL,
                HMENU.NULL,
                HINSTANCE.NULL,
                nint.Zero
            );
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                User32.DestroyWindow(Handle);
                User32.UnregisterClass(WindowClassInfo.lpszClassName, HINSTANCE.NULL);
                Disposed = true;
            }
        }

        static int ID { get; set; } = 0;

        static User32.WNDCLASSEX GenerateWindowClass()
        {
            ID++;
            return new User32.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<User32.WNDCLASSEX>(),
                lpszClassName = $"DirectShowInput_Dummy_{ID}",
                style = User32.WindowClassStyles.CS_HREDRAW | User32.WindowClassStyles.CS_VREDRAW,
                lpfnWndProc = User32.DefWindowProc,
                hCursor = WindowClass.StdArrowCursor,
                hInstance = Kernel32.GetModuleHandle()
            };
        }
    }

    class DirectShowVideoReader : IDisposable
    {
        const int TimeRoundDigit = 10;

        const double MediaTimeRate = 10000000.0;

        const int WaitingStateChangingCountLimit = 10;

        const string AviDecoderName = "Avi Decoder";

        [MemberNotNullWhen(true, nameof(VideoSampler))]
        public bool IsLoaded { get; }

        public bool PossibilityArgb { get; }

        public double FrameRate { get; }

        public double Duration { get; }

        public int Width { get; }

        public int Height { get; }

        bool IsLocked { get; set; }

        IGraphBuilder GraphBuilder { get; }

        ISampleGrabber? VideoGrabber { get; }

        IBaseFilter? VideoNullRenderer { get; }

        IBaseFilter? AviDecoder { get; }

        IMediaSeeking MediaSeeking => (IMediaSeeking)GraphBuilder;

        IMediaControl MediaControl => (IMediaControl)GraphBuilder;

        VideoSampler? VideoSampler { get; }

        bool IsSupportedFrame { get; set; }

        public DirectShowVideoReader(string filePath)
        {
            GraphBuilder = (IGraphBuilder)new FilterGraph();

            VideoGrabber = (ISampleGrabber)new SampleGrabber();
            VideoNullRenderer = (IBaseFilter)new NullRenderer();
            AviDecoder = (IBaseFilter)new AviDecoder();
            if (VideoGrabber == null || VideoNullRenderer == null || AviDecoder == null)
            {
                return;
            }

            var isAvi = Path.GetExtension(filePath) == ".avi";

            try
            {
                using var mediaType = new PAMMediaType
                {
                    majortype = MediaType.Video,
                    subtype = MediaSubType.RGB32,
                    formattype = Format.VideoInfo
                };

                var hr = VideoGrabber.SetMediaType(mediaType);
                hr.ThrowIfFailed();

                hr = GraphBuilder.AddFilter((IBaseFilter)VideoGrabber, "Video SampleGrabber");
                hr.ThrowIfFailed();

                if (isAvi)
                {
                    hr = GraphBuilder.AddFilter(AviDecoder, AviDecoderName);
                    hr.ThrowIfFailed();
                }

                hr = GraphBuilder.RenderFile(filePath, null);
                hr.ThrowIfFailed();

                using var am = new PAMMediaType();
                hr = VideoGrabber.GetConnectedMediaType(am);
                if (hr.Failed)
                {
                    return;
                }

                if (am.formattype != Format.VideoInfo || am.pbFormat == nint.Zero)
                {
                    return;
                }

                var videoInfo = Marshal.PtrToStructure<VIDEOINFOHEADER>(am.pbFormat);
                GraphBuilder.AddFilter(VideoNullRenderer, "Video NullRenderer");
                hr = ReconnectFilter(GraphBuilder, (IBaseFilter)VideoGrabber, GetConnectedFilter((IBaseFilter)VideoGrabber, PinDirection.Output), VideoNullRenderer);
                hr.ThrowIfFailed();

                FrameRate = Math.Round(MediaTimeRate / videoInfo.AvgTimePerFrame, TimeRoundDigit);
                Width = videoInfo.bmiHeader.biWidth;
                Height = videoInfo.bmiHeader.biHeight;

                if (isAvi)
                {
                    var grabberBeforeIsDecoder = false;
                    var grabberInputFilter = GetConnectedFilter((IBaseFilter)VideoGrabber, PinDirection.Input);
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
                    }
                    else
                    {
                        var decoderInputFilter = GetConnectedFilter(AviDecoder, PinDirection.Input);
                        if (decoderInputFilter == null)
                        {
                            var pin = GetPinFirst((IBaseFilter)VideoGrabber, PinDirection.Input);
                            if (pin != null)
                            {
                                using var grabberMediaType = new PAMMediaType();
                                if (pin.ConnectionMediaType(grabberMediaType).Succeeded)
                                {
                                    if (grabberMediaType.formattype == Format.VideoInfo && grabberMediaType.pbFormat != nint.Zero)
                                    {
                                        PossibilityArgb = Marshal.PtrToStructure<VIDEOINFOHEADER>(grabberMediaType.pbFormat).bmiHeader.biBitCount == 32;
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

                VideoSampler = new VideoSampler(FrameRate);
                VideoSampler.SampleCompleted += VideoSampler_SampleCompleted;
                VideoGrabber.SetCallback(VideoSampler, 1);

                IsLoaded = true;
            }
            catch { }
        }

        public byte[] GetImage(double time)
        {
            if (!IsLoaded)
            {
                throw new InvalidOperationException();
            }

            var diffFrame = (int)Math.Round(time * FrameRate) - (int)Math.Round(VideoSampler.TargetSamplingTime * FrameRate);

            if (!SetTime(time))
            {
                return [];
            }
            if (diffFrame == 0 || diffFrame == 1)
            {
                VideoSampler.ForceGetNextFrame();
            }

            var backTime = time;
            MediaControl.Run();
            WaitToReady();
            while (VideoSampler.OverTime > 0.0 && backTime > 0.0)
            {
                backTime = Math.Max(backTime - 1.0, 0.0);
                SetTime(backTime);
                VideoSampler.SetTargetSamplingTime(time);
                MediaControl.Run();
                WaitToReady();
            }

            if (VideoSampler.IsCompleted)
            {
                return VideoSampler.SampledBuffer;
            }
            else
            {
                return [];
            }
        }

        bool SetTime(double time)
        {
            if (!IsLoaded)
            {
                return false;
            }

            VideoSampler.SetTargetSamplingTime(time);
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

        void WaitToReady()
        {
            const int VFW_S_STATE_INTERMEDIATE = 0x40237;
            const int VFW_S_CANT_CUE = 0x40268;

            var state = FilterState.Running;
            var oldTime = 0.0;
            var lastCheckTick = 0L;
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

        void Pause()
        {
            if (!IsLocked)
            {
                IsLocked = true;
                MediaControl.Pause();
                IsLocked = false;
            }
        }

        private void VideoSampler_SampleCompleted(object? sender, EventArgs e)
        {
            Pause();
        }

        static HRESULT ReconnectFilter(IGraphBuilder graphBuilder, IBaseFilter outFilter, IBaseFilter? connected, IBaseFilter inFilter)
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

        static IPin? GetConnectedPin(IBaseFilter outFilter, IBaseFilter inFilter, PinDirection direction)
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

        static IBaseFilter? GetConnectedFilter(IBaseFilter filter, PinDirection direction)
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

        static IPin[] GetAllPins(IBaseFilter filter, PinDirection direction)
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

        static IPin? GetPinFirst(IBaseFilter filter, PinDirection direction)
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
            if (GraphBuilder != null)
            {
                Marshal.ReleaseComObject(GraphBuilder);
            }
            if (VideoGrabber != null)
            {
                Marshal.ReleaseComObject(VideoGrabber);
            }
            if (VideoNullRenderer != null)
            {
                Marshal.ReleaseComObject(VideoNullRenderer);
            }
            if (AviDecoder != null)
            {
                Marshal.ReleaseComObject(AviDecoder);
            }
        }
    }

    file class DirectShowVideoFootageSource : IFootageSource
    {
        const float ByteToFloat = 0.00392156862745098F;

        public string SourceId => "video";

        public string? Name => null;

        public double FrameRate => VideoReader.FrameRate;

        public int Width => VideoReader.Width;

        public int Height => VideoReader.Height;

        public double Duration => VideoReader.Duration;

        public SourceType SourceType => SourceType.Video;

        DirectShowVideoReader VideoReader { get; }

        VideoAlphaType VideoAlphaType { get; }

        int ChannelDataLength { get; }

        int BufferLineLength { get; }

        int VectorAlignedBufferLineLength { get; }

        public DirectShowVideoFootageSource(DirectShowVideoReader videoReader, VideoAlphaType videoAlphaType)
        {
            VideoReader = videoReader;
            ChannelDataLength = VideoReader.Width * VideoReader.Height;
            BufferLineLength = VideoReader.Width * (VideoReader.PossibilityArgb ? 4 : 3);
            VectorAlignedBufferLineLength = (BufferLineLength / Vector<byte>.Count) * Vector<byte>.Count;
        }

        public float[] ReadAudio(double time, double length)
        {
            throw new NotImplementedException();
        }

        public NImage ReadFrame(double time, double downSamplingRate, bool toGpu)
        {
            var result = new NManagedImage(Width, Height);

            var buffer = VideoReader.GetImage(time);
            var width = Width;
            var height = Height;
            var imageData = result.Data;
            if (buffer.Length / ChannelDataLength < (VideoReader.PossibilityArgb ? 4 : 3))
            {
                return result;
            }

            Parallel.For(0, height / 2, y =>
            {
                var topBufferSpan = buffer.AsSpan(y * BufferLineLength, BufferLineLength);
                var bottomBufferSpan = buffer.AsSpan((height - y) * BufferLineLength, BufferLineLength);

                var vectorTopBufferSpan = MemoryMarshal.Cast<byte, Vector<byte>>(topBufferSpan[..VectorAlignedBufferLineLength]);
                var vectorBottomBufferSpan = MemoryMarshal.Cast<byte, Vector<byte>>(bottomBufferSpan[..VectorAlignedBufferLineLength]);
                for (var i = 0; i < vectorTopBufferSpan.Length; i++)
                {
                    (vectorTopBufferSpan[i], vectorBottomBufferSpan[i]) = (vectorBottomBufferSpan[i], vectorTopBufferSpan[i]);
                }
                for (var i = VectorAlignedBufferLineLength; i < topBufferSpan.Length; i++)
                {
                    (topBufferSpan[i], bottomBufferSpan[i]) = (bottomBufferSpan[i], topBufferSpan[i]);
                }
            });

            if (VideoReader.PossibilityArgb)
            {
                switch (VideoAlphaType)
                {
                    case VideoAlphaType.PreMultiply:
                        Parallel.For(0, height, y =>
                        {
                            var bufferSpan = buffer.AsSpan(y * BufferLineLength, BufferLineLength);
                            var intBufferSpan = MemoryMarshal.Cast<byte, int>(bufferSpan);
                            var imageDataSpan = imageData.AsSpan(y * width, width);
                            for (int x = 0, bi = 3; x < imageDataSpan.Length; x++, bi += 4)
                            {
                                if (bufferSpan[bi] > 0)
                                {
                                    var ci = Sse2.ConvertScalarToVector128Int32(intBufferSpan[x]).AsByte();
                                    var cv = Sse2.UnpackLow(Sse2.UnpackLow(ci, Vector128<byte>.Zero), Vector128<byte>.Zero).AsInt32();
                                    var color = (Sse2.ConvertToVector128Single(cv) * ByteToFloat).AsVector4();
                                    var a = color.W;
                                    color /= a;
                                    color.W = a;
                                    imageDataSpan[x] = color;
                                }
                                else
                                {
                                    imageDataSpan[x] = Const.EmptyPixel;
                                }
                            }
                        });
                        break;
                    case VideoAlphaType.Ignore:
                        Parallel.For(0, height, y =>
                        {
                            var bufferSpan = buffer.AsSpan(y * BufferLineLength, BufferLineLength);
                            var imageDataSpan = imageData.AsSpan(y * width, width);
                            for (int x = 0, bi = 0; x < imageDataSpan.Length; x++, bi += 4)
                            {
                                imageDataSpan[x] = new Vector4(bufferSpan[bi], bufferSpan[bi + 1], bufferSpan[bi + 2], 255.0F) * ByteToFloat;
                            }
                        });
                        break;
                    default:
                        ImageConversion.ConvertToBGRA128(buffer, imageData, result.DataLength);
                        break;
                }
            }
            else
            {
                Parallel.For(0, height, y =>
                {
                    var bufferSpan = buffer.AsSpan(y * width * 3, width * 3);
                    var imageDataSpan = imageData.AsSpan(y * width, width);
                    for (int x = 0, bi = 0; x < imageDataSpan.Length; x++, bi += 3)
                    {
                        imageDataSpan[x] = new Vector4(bufferSpan[bi], bufferSpan[bi + 1], bufferSpan[bi + 2], 255.0F) * ByteToFloat;
                    }
                });
            }

            return result;
        }
    }

    enum VideoAlphaType
    {
        Straight,
        PreMultiply,
        Ignore
    }
}
