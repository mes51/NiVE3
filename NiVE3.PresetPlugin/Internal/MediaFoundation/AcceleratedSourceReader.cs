using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime.Win32;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.MediaFoundation;
using System.Buffers;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    class AcceleratedSourceReader : ISourceReader
    {
        static readonly Guid CLSID_VideoProcessorMFT = new Guid("88753b26-5b24-49bd-b2e7-0c445c78c982");

        public bool Succeeded { get; }

        public double Duration { get; }

        public double FrameRate { get; }

        public string FilePath { get; }

        public FormatInfo Format { get; private set; }

        IMFAttributes? Attributes { get; }

        ID3D11Device? Device { get; }

        IMFDXGIDeviceManager? DxgiManager { get; }

        IMFSourceReader? Reader { get; }

        IMFTransform? Transform { get; }

        IMFSample? Sample { get; }

        public AcceleratedSourceReader(string filePath)
        {
            MFInitializer.Initialize();

            FilePath = filePath;
            Attributes = MediaFactory.MFCreateAttributes(3);
            if (Attributes == null)
            {
                return;
            }
            Attributes.Set(SourceReaderAttributeKeys.DisableDxva, false);
            Attributes.Set(SourceReaderAttributeKeys.EnableAdvancedVideoProcessing, true);

            if (D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.VideoSupport, new FeatureLevel[] { FeatureLevel.Level_11_1 }, out var device).Failure || device == null)
            {
                return;
            }
            Device = device;
            device.QueryInterface<ID3D11Multithread>().SetMultithreadProtected(true);

            DxgiManager = MediaFactory.MFCreateDXGIDeviceManager();
            DxgiManager.ResetDevice(device).CheckError();
            Attributes.DxgiManager = DxgiManager;

            Reader = MediaFactory.MFCreateSourceReaderFromURL(filePath, Attributes);
            using var nativeMediaType = Reader.GetNativeMediaType(ISourceReader.FirstVideoStreamId, 0);
            var majorType = nativeMediaType.Get<Guid>(MediaTypeAttributeKeys.MajorType);
            if (majorType != MediaTypeGuids.Video)
            {
                return;
            }

            using var mediaType = MediaFactory.MFCreateMediaType();
            if (mediaType.Set(MediaTypeAttributeKeys.MajorType, majorType).Failure)
            {
                return;
            }
            if (mediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.NV12).Failure)
            {
                return;
            }
            Reader.SetCurrentMediaType(ISourceReader.FirstVideoStreamId, mediaType);
            Reader.SetStreamSelection(ISourceReader.FirstVideoStreamId, true);

            var decoder = (IMFTransform)Reader.GetServiceForStream(ISourceReader.FirstVideoStreamId, Guid.Empty, typeof(IMFTransform).GUID);
            if (decoder == null)
            {
                return;
            }
            decoder.ProcessMessage(TMessageType.MessageSetD3DManager, unchecked((nuint)DxgiManager.NativePointer));

            Format = FormatInfo.GetVideoFormat(Reader, ISourceReader.FirstVideoStreamId);

            Succeeded = Format.Width != 0;
            if (Succeeded)
            {
                Duration = GetDuration();
                FrameRate = GetFrameRate();
            }

            var transformPtr = Com.CoCreateInstance(CLSID_VideoProcessorMFT, null, CLSCTX.CLSCTX_INPROC_SERVER, typeof(IMFTransform).GUID);
            if (transformPtr == nint.Zero)
            {
                return;
            }

            Transform = new IMFTransform(transformPtr);
            Sample = MediaFactory.MFCreateSample();

            ConfigureTransform();
        }

        public byte[] GetFrame(double time)
        {
            const int MaxSkipFrame = 10;
            const long SeekTolerance = 10000000;

            if (Reader == null || Transform == null || Sample == null)
            {
                return Array.Empty<byte>();
            }

            var timeLong = (long)(time * ISourceReader.DurationRate);
            var pos = new Variant { ElementType = VariantElementType.Long, Value = timeLong };
            Reader.SetCurrentPosition(Guid.Empty, pos);

            IMFSample? sample = null;
            int skipCount = 0;
            while (true)
            {
                Reader.ReadSample(ISourceReader.FirstVideoStreamId, 0, out int _, out int flags, out long _, out IMFSample? sampleTemp);

                if ((flags & (int)SourceReaderFlag.FEndofstream) != 0)
                {
                    break;
                }

                if (sampleTemp == null)
                {
                    continue;
                }

                sample?.Dispose();
                sample = sampleTemp;

                var timestamp = sampleTemp.SampleTime;
                if (skipCount < MaxSkipFrame && timestamp + SeekTolerance < timeLong)
                {
                    skipCount++;
                    continue;
                }

                break;
            }

            if (sample == null)
            {
                return Array.Empty<byte>();
            }

            try
            {
                Transform.ProcessInput(0, sample, 0);
            }
            catch (SharpGenException)
            {
                Transform.ProcessMessage(TMessageType.MessageCommandFlush, 0);
                Transform.ProcessInput(0, sample, 0);
            }
            var dataBuffer = new OutputDataBuffer { Sample = Sample };
            Transform.ProcessOutput(ProcessOutputFlags.None, 1, ref dataBuffer, out _);

            var expectedLength = Format.Height * Format.Width * 4;
            using var buffer = Sample.ConvertToContiguousBuffer();
            var result = Array.Empty<byte>();
            buffer.Lock(out nint ptr, out int _, out int length);

            if (length >= expectedLength)
            {
                var requestSize = Math.Min(length, expectedLength);
                result = ArrayPool<byte>.Shared.Rent(requestSize);
                Marshal.Copy(ptr, result, 0, requestSize);
            }

            buffer.Unlock();

            sample.Dispose();

            return result;
        }

        void ConfigureTransform()
        {
            if (Reader == null || Transform == null || Sample == null)
            {
                return;
            }

            using var mediaType = Reader.GetCurrentMediaType(ISourceReader.FirstVideoStreamId);

            const int WidthAlign = 2;
            const int HeightAlign = 16;
            var height = Format.Height % HeightAlign != 0 ? (Format.Height / HeightAlign + 1) * HeightAlign : Format.Height;
            var alignedCorrectedWidth = Format.CorrectedWidth % WidthAlign != 0 ? (Format.CorrectedWidth / WidthAlign + 1) * WidthAlign : Format.CorrectedWidth;
            var alignedCorrectedHeight = Format.CorrectedHeight % HeightAlign != 0 ? (Format.CorrectedHeight / HeightAlign + 1) * HeightAlign : Format.CorrectedHeight;

            using var inputMediaType = Reader.GetCurrentMediaType(ISourceReader.FirstVideoStreamId);
            for (var i = 0; i < inputMediaType.Count; i++)
            {
                inputMediaType.GetByIndex((uint)i, out var key);
                System.Diagnostics.Debug.WriteLine("{0}: {1}", key, inputMediaType.Get(key));
            }

            inputMediaType.Set(MediaTypeAttributeKeys.AllSamplesIndependent, true);
            inputMediaType.Set(MediaTypeAttributeKeys.FixedSizeSamples, true);
            Transform.SetInputType(0, inputMediaType, 0);

            using var outputMediaType = MediaFactory.MFCreateMediaType();
            outputMediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
            outputMediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Argb32);
            outputMediaType.Set(MediaTypeAttributeKeys.AllSamplesIndependent, true);
            outputMediaType.Set(MediaTypeAttributeKeys.FixedSizeSamples, true);
            outputMediaType.Set(MediaTypeAttributeKeys.DefaultStride, Format.CorrectedWidth);
            Util.SetDoubleInt32(outputMediaType, MediaTypeAttributeKeys.PixelAspectRatio, 1, 1);
            Util.SetDoubleInt32(outputMediaType, MediaTypeAttributeKeys.FrameSize, Format.CorrectedWidth, Format.CorrectedHeight);
            Transform.SetOutputType(0, outputMediaType, 0);

            Transform.ProcessMessage(TMessageType.MessageNotifyEndOfStream, 0);
            Transform.ProcessMessage(TMessageType.MessageNotifyBeginStreaming, 0);

            var streamInfo = Transform.GetOutputStreamInfo(0);

            using var buffer = MediaFactory.MFCreateMemoryBuffer(streamInfo.Size);
            Sample.AddBuffer(buffer);
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
                return unchecked((long)(ulong)duration.Value) / ISourceReader.DurationRate;
            }
            else
            {
                return -1.0;
            }
        }

        double GetFrameRate()
        {
            if (Reader == null)
            {
                return -1.0;
            }

            using var mediaType = Reader.GetCurrentMediaType((int)MidlMidlItfMfreadwrite000000010001.MfSourceReaderFirstVideoStream);
            if (mediaType == null)
            {
                return -1.0;
            }

            var (numerator, denominator) = Util.GetDoubleInt32(mediaType, MediaTypeAttributeKeys.FrameRate);
            return numerator / (double)denominator;
        }

        public void Dispose()
        {
            Sample?.Dispose();
            Transform?.Dispose();
            Reader?.Dispose();
            Attributes?.Dispose();
            DxgiManager?.Dispose();
            Device?.Dispose();
        }
    }
}
