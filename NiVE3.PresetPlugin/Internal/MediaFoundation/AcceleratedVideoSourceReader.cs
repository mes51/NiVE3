using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    class AcceleratedVideoSourceReader : VideoSourceReaderBase
    {
        static readonly Guid CLSID_VideoProcessorMFT = new Guid("88753b26-5b24-49bd-b2e7-0c445c78c982");

        ID3D11Device? Device { get; }

        IMFDXGIDeviceManager? DxgiManager { get; }

        IMFTransform? Transform { get; }

        IMFSample? Sample { get; }

        public AcceleratedVideoSourceReader(string filePath) : base(filePath)
        {
            MFInitializer.Initialize();

            using var attributes = MediaFactory.MFCreateAttributes(3);
            if (attributes == null)
            {
                return;
            }
            attributes.Set(SourceReaderAttributeKeys.DisableDxva, false);
            attributes.Set(SourceReaderAttributeKeys.EnableAdvancedVideoProcessing, true);

            if (D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.VideoSupport, [FeatureLevel.Level_11_1], out var device).Failure || device == null)
            {
                return;
            }
            Device = device;
            device.QueryInterface<ID3D11Multithread>().SetMultithreadProtected(true);

            DxgiManager = MediaFactory.MFCreateDXGIDeviceManager();
            DxgiManager.ResetDevice(device).CheckError();
            attributes.DxgiManager = DxgiManager;

            Reader = MediaFactory.MFCreateSourceReaderFromURL(filePath, attributes);
            using var nativeMediaType = Reader.GetNativeMediaType(SourceReaderIndex.FirstVideoStream, 0);
            var majorType = nativeMediaType.GetGUID(MediaTypeAttributeKeys.MajorType);
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
            Reader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, mediaType);
            Reader.SetStreamSelection(SourceReaderIndex.FirstVideoStream, true);

            var decoder = (IMFTransform)Reader.GetServiceForStream(SourceReaderIndex.FirstVideoStream, Guid.Empty, typeof(IMFTransform).GUID);
            if (decoder == null)
            {
                return;
            }
            decoder.ProcessMessage(TMessageType.MessageSetD3DManager, unchecked((nuint)DxgiManager.NativePointer));

            Format = FormatInfo.GetVideoFormat(Reader, SourceReaderIndex.FirstVideoStream);

            var transformPtr = Com.CoCreateInstance(CLSID_VideoProcessorMFT, null, CLSCTX.CLSCTX_INPROC_SERVER, typeof(IMFTransform).GUID);
            if (transformPtr == nint.Zero)
            {
                return;
            }

            Transform = new IMFTransform(transformPtr);
            Sample = MediaFactory.MFCreateSample();

            Succeeded = Format.Width != 0;
            if (Succeeded)
            {
                Duration = GetDuration();
                FrameRate = GetFrameRate();
                ConfigureTransform();
            }
        }

        /// <summary>
        /// フレームの読み出し
        /// </summary>
        /// <param name="time">読み込む時間</param>
        /// <returns>ArrayPool<byte>.Sharedから借りたbyte[]</byte></returns>
        public override byte[] GetFrame(double time)
        {
            if (Transform == null || Sample == null)
            {
                return [];
            }

            using var sample = ReadSample(time);
            if (sample == null)
            {
                return [];
            }

            Transform.ProcessInput(0, sample, 0);
            var dataBuffer = new OutputDataBuffer { Sample = Sample };
            Transform.ProcessOutput(ProcessOutputFlags.None, 1, ref dataBuffer, out _);
            Transform.ProcessMessage(TMessageType.MessageCommandFlush, 0);

            return ConvertSampleToByteArray(Sample);
        }

        void ConfigureTransform()
        {
            if (Reader == null || Transform == null || Sample == null)
            {
                return;
            }

            using var mediaType = Reader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);

            //const int WidthAlign = 2;
            //const int HeightAlign = 16;
            //var height = Format.Height % HeightAlign != 0 ? (Format.Height / HeightAlign + 1) * HeightAlign : Format.Height;
            //var alignedCorrectedWidth = Format.CorrectedWidth % WidthAlign != 0 ? (Format.CorrectedWidth / WidthAlign + 1) * WidthAlign : Format.CorrectedWidth;
            //var alignedCorrectedHeight = Format.CorrectedHeight % HeightAlign != 0 ? (Format.CorrectedHeight / HeightAlign + 1) * HeightAlign : Format.CorrectedHeight;

            using var inputMediaType = Reader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);

            inputMediaType.Set(MediaTypeAttributeKeys.AllSamplesIndependent, true);
            inputMediaType.Set(MediaTypeAttributeKeys.FixedSizeSamples, true);
            Transform.SetInputType(0, inputMediaType, 0);

            using var outputMediaType = MediaFactory.MFCreateMediaType();
            outputMediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
            outputMediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Argb32);
            outputMediaType.Set(MediaTypeAttributeKeys.AllSamplesIndependent, true);
            outputMediaType.Set(MediaTypeAttributeKeys.FixedSizeSamples, true);
            outputMediaType.Set(MediaTypeAttributeKeys.DefaultStride, (uint)(Format.CorrectedWidth * 4)); // NOTE: int だと float 扱いにされるため uint にする
            Util.SetDoubleInt32(outputMediaType, MediaTypeAttributeKeys.PixelAspectRatio, 1, 1);
            Util.SetDoubleInt32(outputMediaType, MediaTypeAttributeKeys.FrameSize, Format.CorrectedWidth, Format.CorrectedHeight);
            Transform.SetOutputType(0, outputMediaType, 0);

            Transform.ProcessMessage(TMessageType.MessageNotifyEndOfStream, 0);
            Transform.ProcessMessage(TMessageType.MessageNotifyBeginStreaming, 0);

            var streamInfo = Transform.GetOutputStreamInfo(0);

            using var buffer = MediaFactory.MFCreateMemoryBuffer(streamInfo.Size);
            Sample.AddBuffer(buffer);
        }

        public override void Dispose()
        {
            base.Dispose();

            Sample?.Dispose();
            Transform?.Dispose();
            DxgiManager?.Dispose();
            Device?.Dispose();
        }
    }
}
