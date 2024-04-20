using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Native;
using NiVE3.PresetPlugin.Internal.Util;
using SharpAvi;
using SharpAvi.Codecs;
using static Vanara.PInvoke.AviFil32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Msvfw32;

namespace NiVE3.PresetPlugin.Internal.Encoder
{
    class CompressedVideoEncoder : IVideoEncoder, IVideoEncoderExtraData, IDisposable
    {
        public FourCC Codec { get; }

        public BitsPerPixel BitsPerPixel { get; }

        public int MaxEncodedSize { get; }

        public byte[] BitmapInfoHeader => OutputBitmapHeader.ToArray();

        public int Width { get; }

        public int Height { get; }

        public int FrameRate { get; }

        public int FrameCount { get; }

        public int Quality { get; set; }

        public int KeyFrameRate { get; set; }

        public bool CompressStarted { get; private set; }

        bool Disposed { get; set; }

        byte[] Buffer { get; }

        byte[] PrevBuffer { get; }

        BITMAPINFOHEADER InputBitmapHeader { get; }

        AllocatedNativeMemory OutputBitmapHeader { get; set; }

        SafeHIC CompressorHandle { get; }

        int FramesFromLastKeyFrame { get; set; } = 1000;

        int FrameIndex { get; set; }

        bool NeedCompressFrames { get; }

        public CompressedVideoEncoder(int width, int height, BitsPerPixel bitsPerPixel, FourCC codec, int frameCount, int frameRate)
        {
            Codec = codec;
            BitsPerPixel = bitsPerPixel;
            Width = width;
            Height = height;
            FrameRate = frameRate;
            FrameCount = frameCount;
            Buffer = new byte[Width * Height * (int)bitsPerPixel / 8];
            PrevBuffer = new byte[Width * Height * (int)bitsPerPixel / 8];
            InputBitmapHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = height,
                biPlanes = 1,
                biBitCount = (ushort)bitsPerPixel,
                biSizeImage = (uint)(width * height * ((int)bitsPerPixel / 8)),
                biCompression = BitmapCompressionMode.BI_RGB

            };
            CompressorHandle = CreateCompressor(codec, InputBitmapHeader, out var outputHeader, out var compressorInfo);
            NeedCompressFrames = (compressorInfo.dwFlags & VIDCF.VIDCF_COMPRESSFRAMES) == VIDCF.VIDCF_COMPRESSFRAMES;

            if (CompressorHandle.IsNull)
            {
                throw new InvalidOperationException();
            }

            OutputBitmapHeader = outputHeader;
            MaxEncodedSize = IC.ICCompressGetSize(CompressorHandle, InputBitmapHeader, OutputBitmapHeader);
        }

        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            return EncodeFrame(source.AsSpan(srcOffset), destination.AsSpan(destOffset), out isKeyFrame);
        }

        public int EncodeFrame(ReadOnlySpan<byte> source, Span<byte> destination, out bool isKeyFrame)
        {
            if (!CompressStarted)
            {
                if (NeedCompressFrames)
                {
                    var compressFrames = new ICCOMPRESSFRAMES
                    {
                        lStartFrame = 0,
                        lFrameCount = FrameCount,
                        lQuality = Quality * 100,
                        lKeyRate = KeyFrameRate,
                        dwRate = (uint)FrameRate,
                        dwScale = 1
                    };
                    if (!IC.CompressFramesInfo(CompressorHandle, compressFrames))
                    {
                        throw new InvalidOperationException();
                    }
                }
                if (!IC.CompressBegin(CompressorHandle, InputBitmapHeader, OutputBitmapHeader))
                {
                    throw new InvalidOperationException();
                }
                CompressStarted = true;
            }

            if (BitsPerPixel == BitsPerPixel.Bpp32)
            {
                FlipVerticalBitmap(source, Buffer, Width, Height);
            }
            else
            {
                FlipAndConvertTo24(source, Buffer, Width, Height);
            }

            var inputHeader = InputBitmapHeader;

            var managedOutputHeader = OutputBitmapHeader.ToStruct<BITMAPINFOHEADER>();
            if (managedOutputHeader.biSizeImage != MaxEncodedSize)
            {
                managedOutputHeader.biSizeImage = (uint)MaxEncodedSize;
                Marshal.StructureToPtr(managedOutputHeader, OutputBitmapHeader, false);
            }

            var needKeyframe = FramesFromLastKeyFrame > KeyFrameRate;
            var result = IC.Compress(
                CompressorHandle,
                needKeyframe,
                managedOutputHeader,
                destination,
                inputHeader,
                Buffer,
                out var outputFlags,
                FrameIndex,
                0,
                Quality * 100,
                PrevBuffer
            );
            if (result)
            {
                throw new InvalidOperationException();
            }
            FrameIndex++;

            isKeyFrame = (outputFlags & AVIIF.AVIIF_KEYFRAME) == AVIIF.AVIIF_KEYFRAME;
            FramesFromLastKeyFrame = isKeyFrame ? 1 : (FramesFromLastKeyFrame + 1);

            Buffer.AsSpan().CopyTo(PrevBuffer);

            return (int)OutputBitmapHeader.ToStruct<BITMAPINFOHEADER>().biSizeImage; ;
        }

        public void SetState(byte[] state)
        {
            if (!CompressStarted)
            {
                using var nativeState = new AllocatedNativeMemory(state.Length);
                nativeState.CopyFrom(state);
                IC.SetState(CompressorHandle, nativeState);

                OutputBitmapHeader.Dispose();
                OutputBitmapHeader = IC.GetFormat(CompressorHandle, InputBitmapHeader);
            }
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                if (CompressStarted)
                {
                    IC.CompressEnd(CompressorHandle);
                }

                CompressorHandle.Dispose();
                OutputBitmapHeader.Dispose();
                Disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~CompressedVideoEncoder()
        {
            Dispose();
        }

        static SafeHIC CreateCompressor(FourCC codec, BITMAPINFOHEADER inputHeader, out AllocatedNativeMemory outputHeader, out ICINFO compressorInfo)
        {
            var hic = ICOpen(IC.TypeVideo, (uint)codec, ICMODE.ICMODE_COMPRESS);

            if (hic.IsNull)
            {
                compressorInfo = new ICINFO();
                outputHeader = AllocatedNativeMemory.Null;
                return hic;
            }

            outputHeader = IC.GetFormat(hic, inputHeader);

            if (!IC.CompressQuery(hic, inputHeader, outputHeader))
            {
                hic.Dispose();
                compressorInfo = new ICINFO();
                return new SafeHIC(nint.Zero);
            }

            compressorInfo = new ICINFO();
            var infoSize = ICGetInfo(hic, ref compressorInfo, (uint)Marshal.SizeOf<ICINFO>());
            if (infoSize > 0)
            {
                return hic;
            }
            else
            {
                hic.Dispose();
                return new SafeHIC(nint.Zero);
            }
        }

        static void FlipVerticalBitmap(ReadOnlySpan<byte> src, Span<byte> dst, int width, int height)
        {
            var srcInt = MemoryMarshal.Cast<byte, int>(src);
            var dstInt = MemoryMarshal.Cast<byte, int>(dst);

            for (var i = 0; i < height; i++)
            {
                srcInt.Slice(i * width, width).CopyTo(dstInt.Slice((height - i - 1) * width, width));
            }
        }

        static void FlipAndConvertTo24(ReadOnlySpan<byte> src, Span<byte> dst, int width, int height)
        {
            for (var h = 0; h < height; h++)
            {
                for (int w = 0, sp = h * width * 4, dp = (height - h - 1) * width * 3; w < width; w++, sp += 4, dp += 3)
                {
                    dst[dp] = src[sp];
                    dst[dp + 1] = src[sp + 1];
                    dst[dp + 2] = src[sp + 2];
                }
            }
        }
    }
}
