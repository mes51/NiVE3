using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Util;
using static Vanara.PInvoke.AviFil32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Msvfw32;

namespace NiVE3.PresetPlugin.Internal.Native
{
    static class IC
    {
        public const uint TypeVideo = 'v' | ((uint)'i' << 8) | ((uint)'d' << 16) | ((uint)'c' << 24);

        public const uint TypeAudio = 'a' | ((uint)'u' << 8) | ((uint)'d' << 16) | ((uint)'c' << 24);

        public static bool CompressFramesInfo(HIC hic, ICCOMPRESSFRAMES compressFrames)
        {
            return ICSendMessage(hic, ICM_Message.ICM_COMPRESS_FRAMES_INFO, compressFrames) == (nint)ICERR.ICERR_OK;
        }

        public static bool CompressBegin(HIC hic, in BITMAPINFOHEADER inputBitmapInfoHeader, nint outputBitmapInfoHeader)
        {
            using var ibihPtr = AllocatedNativeMemory.FromStruct(inputBitmapInfoHeader);
            return ICSendMessage(hic, ICM_Message.ICM_COMPRESS_BEGIN, ibihPtr, outputBitmapInfoHeader) == (nint)ICERR.ICERR_OK;
        }

        public static void CompressEnd(HIC hic)
        {
            ICSendMessage(hic, ICM_Message.ICM_COMPRESS_END);
        }

        public static AllocatedNativeMemory GetFormat(HIC hic, BITMAPINFOHEADER inputBitmapInfoHeader)
        {
            using var ibihPtr = AllocatedNativeMemory.FromStruct(inputBitmapInfoHeader);
            var size = ICSendMessage(hic, ICM_Message.ICM_COMPRESS_GET_FORMAT, ibihPtr, 0).ToInt32();
            if (size < 1)
            {
                return AllocatedNativeMemory.Null;
            }

            var globalMem = new AllocatedNativeMemory(size);
            ICSendMessage(hic, ICM_Message.ICM_COMPRESS_GET_FORMAT, ibihPtr, globalMem);

            return globalMem;
        }

        public static bool CompressQuery(HIC hic, in BITMAPINFOHEADER inputBitmapInfoHeader, nint outputBitmapInfoHeader)
        {
            using var ibihPtr = AllocatedNativeMemory.FromStruct(inputBitmapInfoHeader);
            return ICSendMessage(hic, (uint)ICM_Message.ICM_COMPRESS_QUERY, ibihPtr, outputBitmapInfoHeader) == (nint)ICERR.ICERR_OK;
        }

        public static int ICCompressGetSize(HIC hic, in BITMAPINFOHEADER inputBitmapInfoHeader, nint outputBitmapInfoHeader)
        {
            using var ibihPtr = AllocatedNativeMemory.FromStruct(inputBitmapInfoHeader);
            return ICSendMessage(hic, ICM_Message.ICM_COMPRESS_GET_SIZE, ibihPtr, outputBitmapInfoHeader).ToInt32();
        }

        public static AllocatedNativeMemory GetState(HIC hic)
        {
            var size = ICSendMessage(hic, ICM_Message.ICM_GETSTATE).ToInt32();
            var result = new AllocatedNativeMemory(size);
            ICSendMessage(hic, ICM_Message.ICM_GETSTATE, result, result.Size);
            return result;
        }

        public static void SetState(HIC hic, AllocatedNativeMemory state)
        {
            ICSendMessage(hic, ICM_Message.ICM_SETSTATE, state, state.Size);
        }

        public static bool QueryConfigure(HIC hic)
        {
            return ICSendMessage(hic, ICM_Message.ICM_CONFIGURE, -1) == (nint)ICERR.ICERR_OK;
        }

        public static void Configure(HIC hic, nint hwnd)
        {
            ICSendMessage(hic, ICM_Message.ICM_CONFIGURE, hwnd, 0);
        }

        public static bool GetDefaultKeyFrameRate(HIC hic, out int keyFrameRate)
        {
            using var store = new AllocatedNativeMemory(sizeof(int));
            if (ICSendMessage(hic, ICM_Message.ICM_GETDEFAULTKEYFRAMERATE, store, store.Size) == (nint)ICERR.ICERR_OK)
            {
                keyFrameRate = store.ToStruct<int>();
                return true;
            }
            else
            {
                keyFrameRate = -1;
                return false;
            }
        }

        public unsafe static bool Compress(HIC hic, bool isKeyframe, in BITMAPINFOHEADER lpbiOutput, Span<byte> lpData, in BITMAPINFOHEADER lpbiInput, ReadOnlySpan<byte> lpBits, out AVIIF lpdwFlags, int lFrameNum, int dwFrameSize, int dwQuality, ReadOnlySpan<byte> lpPrev)
        {
            fixed (byte* srcPtr = &MemoryMarshal.GetReference(lpBits))
            fixed (byte* dstPtr = &MemoryMarshal.GetReference(lpData))
            fixed (byte* prevSrcPtr = &MemoryMarshal.GetReference(lpPrev))
            {
                if (isKeyframe)
                {
                    return ICCompress(hic, ICCOMPRESSF.ICCOMPRESS_KEYFRAME, lpbiOutput, (nint)dstPtr, lpbiInput, (nint)srcPtr, 0, out lpdwFlags, lFrameNum, (uint)dwFrameSize, (uint)dwQuality, 0, 0) == ICERR.ICERR_OK;
                }
                else
                {
                    return ICCompress(hic, 0, lpbiOutput, (nint)dstPtr, lpbiInput, (nint)srcPtr, 0, out lpdwFlags, lFrameNum, (uint)dwFrameSize, (uint)dwQuality, lpbiInput, (nint)prevSrcPtr) == ICERR.ICERR_OK;
                }
            }
        }
    }
}
