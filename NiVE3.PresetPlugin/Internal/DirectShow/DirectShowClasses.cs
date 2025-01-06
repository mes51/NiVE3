using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace NiVE3.PresetPlugin.Internal.DirectShow
{
    #region interfaces
#pragma warning disable SYSLIB1096 // NOTE: https://tan.hatenadiary.jp/entry/2024/01/11/004953
    // TODO: .NET 9以降に移行する際にGeneratedComInterfaceに変更する

    [ComImport, Guid("56a868a9-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphBuilder
    {
        #region IFilterGraph

        [PreserveSig]
        HRESULT AddFilter(IBaseFilter pFilter, [In, MarshalAs(UnmanagedType.LPWStr)] string pName);

        [PreserveSig]
        HRESULT RemoveFilter(IBaseFilter pFilter);

        [PreserveSig]
        HRESULT EnumFilters(out IEnumFilters ppEnum);

        [PreserveSig]
        HRESULT FindFilterByName([In, MarshalAs(UnmanagedType.LPWStr)] string pName, out IBaseFilter ppFilter);

        [PreserveSig]
        HRESULT ConnectDirect(IPin ppinOut, IPin ppinIn, [In, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pmt);

        [PreserveSig]
        HRESULT Reconnect(IPin ppin);

        [PreserveSig]
        HRESULT Disconnect(IPin ppin);

        [PreserveSig]
        HRESULT SetDefaultSyncSource();

        #endregion IFilterGraph

        [PreserveSig]
        HRESULT Connect(IPin ppinOut, IPin ppinIn);

        [PreserveSig]
        HRESULT Render(IPin ppinOut);

        [PreserveSig]
        HRESULT RenderFile([In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrFile, [In, MarshalAs(UnmanagedType.LPWStr)] string? lpcwstrPlayList);

        [PreserveSig]
        HRESULT AddSourceFilter([In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrFileName, [In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrFilterName, out IBaseFilter ppFilter);

        [PreserveSig]
        HRESULT SetLogFile(nint hFile);

        [PreserveSig]
        HRESULT Abort();

        [PreserveSig]
        HRESULT ShouldOperationContinue();
    }

    [ComImport, Guid("56a8689f-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFilterGraph
    {
        [PreserveSig]
        HRESULT AddFilter(IBaseFilter pFilter, [In, MarshalAs(UnmanagedType.LPWStr)] string pName);

        [PreserveSig]
        HRESULT RemoveFilter(IBaseFilter pFilter);

        [PreserveSig]
        HRESULT EnumFilters(out IEnumFilters ppEnum);

        [PreserveSig]
        HRESULT FindFilterByName([In, MarshalAs(UnmanagedType.LPWStr)] string pName, out IBaseFilter ppFilter);

        [PreserveSig]
        HRESULT ConnectDirect(IPin ppinOut, IPin ppinIn, [In, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pmt);

        [PreserveSig]
        HRESULT Reconnect(IPin ppin);

        [PreserveSig]
        HRESULT Disconnect(IPin ppin);

        [PreserveSig]
        HRESULT SetDefaultSyncSource();
    }

    [ComImport, Guid("56a86895-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IBaseFilter
    {
        #region IPersist

        [PreserveSig]
        HRESULT GetClassID(out Guid pClassID);

        #endregion IPersist

        #region IMediaFilter

        [PreserveSig]
        HRESULT Stop();

        [PreserveSig]
        HRESULT Pause();

        [PreserveSig]
        HRESULT Run(long tStart);

        [PreserveSig]
        HRESULT GetState(ushort dwMilliSecsTimeout, out FilterState State);

        [PreserveSig]
        HRESULT SetSyncSource(IReferenceClock? pClock);

        [PreserveSig]
        HRESULT GetSyncSource(out IReferenceClock? pClock);

        #endregion IMediaFilter

        [PreserveSig]
        HRESULT EnumPins(out IEnumPins ppEnum);

        [PreserveSig]
        HRESULT FindPin([In, MarshalAs(UnmanagedType.LPWStr)] string Id, out IPin ppPin);

        [PreserveSig]
        HRESULT QueryFilterInfo([In, Out, MarshalAs(UnmanagedType.LPStruct)] FilterInfo pInfo);

        [PreserveSig]
        HRESULT JoinFilterGraph(IFilterGraph pGraph, [In, MarshalAs(UnmanagedType.LPWStr)] string pName);

        [PreserveSig]
        HRESULT QueryVendorInfo([MarshalAs(UnmanagedType.LPWStr)] out string pVendorInfo);
    }

    [ComImport, Guid("56a86899-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMediaFilter
    {
        #region IPersist

        [PreserveSig]
        HRESULT GetClassID(out Guid pClassID);

        #endregion IPersist

        [PreserveSig]
        HRESULT Stop();

        [PreserveSig]
        HRESULT Pause();

        [PreserveSig]
        HRESULT Run(long tStart);

        [PreserveSig]
        HRESULT GetState(ushort dwMilliSecsTimeout, out FilterState State);

        [PreserveSig]
        HRESULT SetSyncSource(IReferenceClock? pClock);

        [PreserveSig]
        HRESULT GetSyncSource(out IReferenceClock? pClock);
    }

    [ComImport, Guid("56a86891-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPin
    {
        [PreserveSig]
        HRESULT Connect(IPin pReceivePin, [In, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pmt);

        [PreserveSig]
        HRESULT ReceiveConnection(IPin pConnector, [In, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pmt);

        [PreserveSig]
        HRESULT Disconnect();

        [PreserveSig]
        HRESULT ConnectedTo(out IPin pPin);

        [PreserveSig]
        HRESULT ConnectionMediaType([In, Out, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pmt);

        [PreserveSig]
        HRESULT QueryPinInfo([In, Out, MarshalAs(UnmanagedType.LPStruct)] PinInfo pInfo);

        [PreserveSig]
        HRESULT QueryDirection(out PinDirection pPinDir);

        [PreserveSig]
        HRESULT QueryId([MarshalAs(UnmanagedType.LPWStr)] out string Id);

        [PreserveSig]
        HRESULT QueryAccept([In, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pmt);

        [PreserveSig]
        HRESULT EnumMediaTypes(out IEnumMediaTypes ppEnum);

        [PreserveSig]
        HRESULT QueryInternalConnections(nint apPin, ref int nPin);

        [PreserveSig]
        HRESULT EndOfStream();

        [PreserveSig]
        HRESULT BeginFlush();

        [PreserveSig]
        HRESULT EndFlush();

        [PreserveSig]
        HRESULT NewSegment(long tStart, long tStop, double dRate);
    }

    [ComImport, Guid("56a86892-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumPins
    {
        [PreserveSig]
        HRESULT Next(int cPins, out IPin? ppPins, out int pcFetched);

        [PreserveSig]
        HRESULT Skip(int cPins);

        void Reset();

        void Clone(out IEnumPins ppEnum);
    }

    [ComImport, Guid("6B652FFF-11FE-4fce-92AD-0266B5D7C78F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISampleGrabber
    {
        [PreserveSig]
        HRESULT SetOneShot(bool OneShot);

        [PreserveSig]
        HRESULT SetMediaType([In, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType? pType);

        [PreserveSig]
        HRESULT GetConnectedMediaType([Out, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pType);

        [PreserveSig]
        HRESULT SetBufferSamples(bool BufferThem);

        [PreserveSig]
        HRESULT GetCurrentBuffer(ref int pBufferSize, nint pBuffer);

        [PreserveSig]
        HRESULT GetCurrentSample(nint ppSample);

        [PreserveSig]
        HRESULT SetCallback(ISampleGrabberCB pCallback, int WhichMethodToCallback);
    }

    [ComImport, Guid("0579154A-2B53-4994-B0D0-E773148EFF85"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISampleGrabberCB
    {
        [PreserveSig]
        HRESULT SampleCB(double SampleTime, IMediaSample pSample);

        [PreserveSig]
        HRESULT BufferCB(double SampleTime, nint pBuffer, int BufferLen);
    }

    [ComImport, Guid("56a8689a-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMediaSample
    {
        [PreserveSig]
        HRESULT GetPointer(ref nint ppBuffer);

        [PreserveSig]
        HRESULT GetSize();

        [PreserveSig]
        HRESULT GetTime(out long pTimeStart, out long pTimeEnd);

        [PreserveSig]
        HRESULT SetTime(long pTimeStart, long pTimeEnd);

        [PreserveSig]
        HRESULT IsSyncPoint();

        [PreserveSig]
        HRESULT SetSyncPoint(bool bIsSyncPoint);

        [PreserveSig]
        HRESULT IsPreroll();

        [PreserveSig]
        HRESULT SetPreroll(bool bIsPreroll);

        [PreserveSig]
        HRESULT GetActualDataLength();

        [PreserveSig]
        HRESULT SetActualDataLength(int lLen);

        [PreserveSig]
        HRESULT GetMediaType([MarshalAs(UnmanagedType.LPStruct)] out PAMMediaType ppMediaType);

        [PreserveSig]
        HRESULT SetMediaType([In, MarshalAs(UnmanagedType.LPStruct)] PAMMediaType pMediaType);

        [PreserveSig]
        HRESULT IsDiscontinuity();

        [PreserveSig]
        HRESULT SetDiscontinuity(bool bDiscontinuity);

        [PreserveSig]
        HRESULT GetMediaTime(ref long pTimeStart, ref long pTimeEnd);

        [PreserveSig]
        HRESULT SetMediaTime(long pTimeStart, long pTimeEnd);
    }

    [ComImport, Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    interface IMediaControl
    {
        [PreserveSig]
        HRESULT Run();

        [PreserveSig]
        HRESULT Pause();

        [PreserveSig]
        HRESULT Stop();

        [PreserveSig]
        HRESULT GetState(int msTimeout, out FilterState pfs);

        [PreserveSig]
        HRESULT RenderFile(string strFilename);

        [PreserveSig]
        HRESULT AddSourceFilter(string strFilename, [MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);

        [PreserveSig]
        HRESULT get_FilterCollection([MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);

        [PreserveSig]
        HRESULT get_RegFilterCollection([MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);

        [PreserveSig]
        HRESULT StopWhenReady();
    }

    [ComImport, Guid("36b73880-c2c8-11cf-8b46-00805f6cef60"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMediaSeeking
    {
        [PreserveSig]
        HRESULT GetCapabilities(out AMSeekingCapabilities pCapabilities);

        [PreserveSig]
        HRESULT CheckCapabilities(ref AMSeekingCapabilities pCapabilities);

        [PreserveSig]
        HRESULT IsFormatSupported(in Guid pFormat);

        [PreserveSig]
        HRESULT QueryPreferredFormat(out Guid pFormat);

        [PreserveSig]
        HRESULT GetTimeFormat(out Guid pFormat);

        [PreserveSig]
        HRESULT IsUsingTimeFormat(Guid pFormat);

        [PreserveSig]
        HRESULT SetTimeFormat(in Guid pFormat);

        [PreserveSig]
        HRESULT GetDuration(out long pDuration);

        [PreserveSig]
        HRESULT GetStopPosition(out long pStop);

        [PreserveSig]
        HRESULT GetCurrentPosition(out long pCurrent);

        [PreserveSig]
        HRESULT ConvertTimeFormat(out long pTarget, [In] ref Guid pTargetFormat, long Source, [In] ref Guid pSourceFormat);

        [PreserveSig]
        HRESULT SetPositions(ref long pCurrent, AMSeekingFlags dwCurrentFlags, ref long pStop, AMSeekingFlags dwStopFlags);

        [PreserveSig]
        HRESULT GetPositions(out long pCurrent, out long pStop);

        [PreserveSig]
        HRESULT GetAvailable(out long pEarliest, out long pLatest);

        [PreserveSig]
        HRESULT SetRate(double dRate);

        [PreserveSig]
        HRESULT GetRate(out double pdRate);

        [PreserveSig]
        HRESULT GetPreroll(out long pllPreroll);
    }

    [ComImport, Guid("89c31040-846b-11ce-97d3-00aa0055595a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumMediaTypes { }

    [ComImport, Guid("56a86893-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumFilters { }

    [ComImport, Guid("56a86897-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IReferenceClock { }

    [ComImport, Guid("0000010c-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPersist { }

#pragma warning restore SYSLIB1096 // 'GeneratedComInterface' に変換する
    #endregion interfaces

    #region concrete classes

    [ComImport, Guid("E436EBB3-524F-11CE-9f53-0020AF0BA770")]
    class FilterGraph { }

    [ComImport, Guid("C1F400A0-3F08-11d3-9F0B-006008039E37")]
    class SampleGrabber { }

    [ComImport, Guid("C1F400A4-3F08-11d3-9F0B-006008039E37")]
    class NullRenderer { }

    [ComImport, Guid("CF49D4E0-1115-11ce-B03A-0020AF0BA770")]
    class AviDecoder { }

    #endregion concrete classes

    #region guids

    static class MediaType
    {
        public static readonly Guid File = new Guid(0x656c6966, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);

        public static readonly Guid Video = new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);

        public static readonly Guid Audio = new Guid(0x73647561, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);

        public static readonly Guid Stream = new Guid(0xe436eb83, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);
    }

    static class MediaSubType
    {
        public static readonly Guid RGB565 = new Guid(0xe436eb7b, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);

        public static readonly Guid RGB555 = new Guid(0xe436eb7c, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);

        public static readonly Guid RGB24 = new Guid(0xe436eb7d, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);

        public static readonly Guid RGB32 = new Guid(0xe436eb7e, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);

        public static readonly Guid ARGB32 = new Guid(0x773c9ac0, 0x3274, 0x11d0, 0xb7, 0x24, 0x0, 0xaa, 0x0, 0x6c, 0x1a, 0x1);

        public static readonly Guid PCM = new Guid(0x00000001, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

        public static readonly Guid IEEEFloat = new Guid(0x00000003, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);

        public static readonly Guid WAVE = new Guid(0xe436eb8b, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);
    }

    static class IID
    {
        public static readonly Guid IMediaControl = new Guid(0x56a868b1, 0x0ad4, 0x11ce, 0xb0, 0x3a, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);
    }

    static class Format
    {
        public static readonly Guid VideoInfo = new Guid(0x05589f80, 0xc356, 0x11ce, 0xbf, 0x01, 0x00, 0xaa, 0x00, 0x55, 0x59, 0x5a);

        public static readonly Guid WaveFormatEx = new Guid(0x05589f81, 0xc356, 0x11ce, 0xbf, 0x01, 0x00, 0xaa, 0x00, 0x55, 0x59, 0x5a);
    }

    static class TimeFormat
    {
        public static readonly Guid Frame = new Guid(0x7b785570, 0x8c82, 0x11cf, 0xbc, 0xc, 0x0, 0xaa, 0x0, 0xac, 0x74, 0xf6);

        public static readonly Guid Sample = new Guid(0x7b785572, 0x8c82, 0x11cf, 0xbc, 0xc, 0x0, 0xaa, 0x0, 0xac, 0x74, 0xf6);

        public static readonly Guid MediaTime = new Guid(0x7b785574, 0x8c82, 0x11cf, 0xbc, 0xc, 0x0, 0xaa, 0x0, 0xac, 0x74, 0xf6);
    }

    #endregion guids

    #region enums

    enum PinDirection
    {
        Input = 0,
        Output = Input + 1
    }

    enum FilterState : int
    {
        Stopped = 0,
        Paused = Stopped + 1,
        Running = Paused + 1
    }

    enum AMSeekingCapabilities
    {
        CanSeekAbsolute = 0x1,
        CanSeekForwards = 0x2,
        CanSeekBackwards = 0x4,
        CanGetCurrentPos = 0x8,
        CanGetStopPos = 0x10,
        CanGetDuration = 0x20,
        CanPlayBackwards = 0x40,
        CanDoSegments = 0x80,
        Source = 0x100
    }

    enum AMSeekingFlags
    {
        NoPositioning = 0,
        AbsolutePositioning = 0x1,
        RelativePositioning = 0x2,
        IncrementalPositioning = 0x3,
        PositioningBitsMask = 0x3,
        SeekToKeyFrame = 0x4,
        ReturnTime = 0x8,
        Segment = 0x10,
        NoFlush = 0x20
    }

    #endregion enums

    #region structs

    [StructLayout(LayoutKind.Sequential)]
    class PAMMediaType : IDisposable
    {
        public Guid majortype;

        public Guid subtype;

        [MarshalAs(UnmanagedType.Bool)]
        public bool bFixedSizeSamples;

        [MarshalAs(UnmanagedType.Bool)]
        public bool bTemporalCompression;

        public int lSampleSize;

        public Guid formattype;

        public nint pUnk;

        public int cbFormat;

        public nint pbFormat;

        public void Dispose()
        {
            if (cbFormat > 0)
            {
                Marshal.FreeCoTaskMem(pbFormat);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    class PinInfo : IDisposable
    {
        public IBaseFilter? pFilter;

        public PinDirection dir;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string achName = "";

        public void Dispose()
        {
            if (pFilter != null)
            {
                Marshal.ReleaseComObject(pFilter);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    class FilterInfo : IDisposable
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string achName = "";

        public IFilterGraph? pGraph;

        public void Dispose()
        {
            if (pGraph != null)
            {
                Marshal.ReleaseComObject(pGraph);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct VIDEOINFOHEADER
    {
        public RECT rcSource;

        public RECT rcTarget;

        public int dwBitRate;

        public int dwBitErrorRate;

        public long AvgTimePerFrame;

        public Gdi32.BITMAPINFOHEADER bmiHeader;
    }

    #endregion structs

    #region SampleGrabber Callbacks

    class VideoSampler : SampleGrabberCallbackBase
    {
        public byte[] SampledBuffer { get; private set; } = [];

        public bool IsForceGet { get; private set; }

        double ToleranceTime { get; }

        public VideoSampler(double frameRate)
        {
            ToleranceTime = 1.0 / frameRate * 0.5;
        }

        public void ForceGetNextFrame()
        {
            IsForceGet = true;
        }

        public override HRESULT BufferCB(double SampleTime, nint pBuffer, int BufferLen)
        {
            if (TargetSamplingTime <= SampleTime)
            {
                if (IsForceGet || Math.Abs(TargetSamplingTime - SampleTime) <= ToleranceTime)
                {
                    if (SampledBuffer.Length > 1)
                    {
                        ArrayPool<byte>.Shared.Return(SampledBuffer);
                        SampledBuffer = [];
                    }
                    SampledBuffer = ArrayPool<byte>.Shared.Rent(BufferLen);
                    Marshal.Copy(pBuffer, SampledBuffer, 0, BufferLen);
                    IsForceGet = false;
                    IsCompleted = true;
                }
                else
                {
                    OverTime = SampleTime;
                }

                OnSampleCompleted();
            }
            return HRESULT.S_OK;
        }

        public override HRESULT SampleCB(double SampleTime, IMediaSample pSample)
        {
            return HRESULT.S_OK;
        }
    }

    class AudioSampler : SampleGrabberCallbackBase
    {
        public IReadOnlyList<byte> AudioData => AudioDataList;

        List<byte> AudioDataList { get; } = [];

        int SamplingRate { get; }

        int BlockSize { get; }

        double NeedLength { get; set; }

        double CurrentTime { get; set; }

        public AudioSampler(int samplingRate, int blockSize)
        {
            SamplingRate = samplingRate;
            BlockSize = blockSize;
        }

        public void SetSampleLength(double needLength)
        {
            NeedLength = needLength;
            CurrentTime = 0.0;
            AudioDataList.Clear();
        }

        public override HRESULT BufferCB(double SampleTime, nint pBuffer, int BufferLen)
        {
            if (NeedLength <= 0.0)
            {
                return HRESULT.S_OK;
            }

            if (SampleTime <= TargetSamplingTime + NeedLength)
            {
                if (SampleTime <= CurrentTime)
                {
                    AudioDataList.Clear();
                }

                var pool = ArrayPool<byte>.Shared;
                if (SampleTime < TargetSamplingTime)
                {
                    var offset = (int)((TargetSamplingTime - SampleTime) * SamplingRate) * BlockSize;
                    if (offset < BufferLen)
                    {
                        var data = pool.Rent(BufferLen - offset);
                        Marshal.Copy(pBuffer + offset, data, 0, BufferLen - offset);
                        AudioDataList.AddRange(data);
                        pool.Return(data);
                    }
                }
                else
                {
                    if (AudioDataList.Count < 1)
                    {
                        var offset = (int)((SampleTime - TargetSamplingTime) * SamplingRate) * BlockSize;
                        AudioDataList.AddRange(Enumerable.Repeat((byte)0, offset));
                    }

                    var trim = (int)(Math.Max((SampleTime + BufferLen / BlockSize / (double)SamplingRate) - TargetSamplingTime - NeedLength, 0.0) * SamplingRate) * BlockSize;
                    if (trim < BufferLen)
                    {
                        var data = pool.Rent(BufferLen - trim);
                        Marshal.Copy(pBuffer, data, 0, BufferLen - trim);
                        AudioDataList.AddRange(data.AsSpan(0, BufferLen - trim));
                        pool.Return(data);
                    }

                    var sampledTime = AudioDataList.Count / BlockSize / (double)SamplingRate;
                    if (sampledTime >= NeedLength)
                    {
                        IsCompleted = true;
                        OnSampleCompleted();
                    }
                }

                CurrentTime = SampleTime;
            }
            else
            {
                OverTime = SampleTime;
            }
            return HRESULT.S_OK;
        }

        public override HRESULT SampleCB(double SampleTime, IMediaSample pSample)
        {
            return HRESULT.S_OK;
        }
    }

    abstract class SampleGrabberCallbackBase : ISampleGrabberCB
    {
        public bool IsCompleted { get; protected set; }

        public double OverTime { get; protected set; }

        public double TargetSamplingTime { get; protected set; }

        public event EventHandler<EventArgs>? SampleCompleted;

        public void SetTargetSamplingTime(double time)
        {
            TargetSamplingTime = time;
            OverTime = -1.0;
            IsCompleted = false;
        }

        public abstract HRESULT SampleCB(double SampleTime, IMediaSample pSample);

        public abstract HRESULT BufferCB(double SampleTime, nint pBuffer, int BufferLen);

        protected void OnSampleCompleted()
        {
            SampleCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion SampleGrabber Callbacks
}
