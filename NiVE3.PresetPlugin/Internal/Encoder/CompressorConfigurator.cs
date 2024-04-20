using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Native;
using NiVE3.PresetPlugin.Internal.Util;
using SharpAvi;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Msvfw32;

namespace NiVE3.PresetPlugin.Internal.Encoder
{
    class CompressorConfigurator : IDisposable
    {
        public bool SupportQuality { get; }

        public bool SupportKeyFrameRate { get; }

        public int DefaultKeyFrameRate { get; }

        public bool HasConfigure { get; }

        SafeHIC CompressorHandle { get; }

        public CompressorConfigurator(FourCC codec, int width, int height, BitsPerPixel bitsPerPixel)
        {
            var compressorInfo = new ICINFO();
            CompressorHandle = CreateCompressor(codec, width, height, bitsPerPixel, ref compressorInfo);
            SupportQuality = (compressorInfo.dwFlags & VIDCF.VIDCF_QUALITY) == VIDCF.VIDCF_QUALITY;
            HasConfigure = IC.QueryConfigure(CompressorHandle);
            SupportKeyFrameRate = IC.GetDefaultKeyFrameRate(CompressorHandle, out var keyFrameRate);
            DefaultKeyFrameRate = keyFrameRate;
        }

        public void OpenConfig(nint owner)
        {
            IC.Configure(CompressorHandle, owner);
        }

        public byte[] GetState()
        {
            using var state = IC.GetState(CompressorHandle);
            return state.ToArray();
        }

        public void SetState(byte[] state)
        {
            using var nativeState = new AllocatedNativeMemory(state.Length);
            nativeState.CopyFrom(state);
            IC.SetState(CompressorHandle, nativeState);
        }

        ~CompressorConfigurator()
        {
            Dispose();
        }

        public void Dispose()
        {
            CompressorHandle.Dispose();
            GC.SuppressFinalize(this);
        }

        public static Tuple<FourCC, string>[] GetSupportedCodec(int width, int height, BitsPerPixel bitsPerPixel)
        {
            var inputHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = height,
                biPlanes = 1,
                biBitCount = (ushort)bitsPerPixel,
                biSizeImage = (uint)(width * height * ((int)bitsPerPixel / 8)),
                biCompression = BitmapCompressionMode.BI_RGB
            };
            var result = new List<Tuple<FourCC, string>>
            {
                Tuple.Create(new FourCC(0), "None")
            };

            for (var i = 0; ICInfo(IC.TypeVideo, (uint)i, out var icInfo); i++)
            {
                using var hic = ICOpen(icInfo.fccType, icInfo.fccHandler, ICMODE.ICMODE_QUERY);
                if (hic.IsNull)
                {
                    continue;
                }

                if (!IC.CompressQuery(hic, inputHeader, 0))
                {
                    continue;
                }

                using var outputHeader = IC.GetFormat(hic, inputHeader);
                var managedOutputHeader = outputHeader.ToStruct<BITMAPINFOHEADER>();
                if (managedOutputHeader.biWidth != width || managedOutputHeader.biHeight != height)
                {
                    continue;
                }

                ICGetInfo(hic, ref icInfo, (uint)Marshal.SizeOf<ICINFO>());

                result.Add(Tuple.Create((FourCC)icInfo.fccHandler, icInfo.szDescription));
            }

            return [..result];
        }

        static SafeHIC CreateCompressor(FourCC codec, int width, int height, BitsPerPixel bitsPerPixel, ref ICINFO compressorInfo)
        {
            var inputHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = height,
                biPlanes = 1,
                biBitCount = (ushort)bitsPerPixel,
                biSizeImage = (uint)(width * height * ((int)bitsPerPixel / 8)),
                biCompression = BitmapCompressionMode.BI_RGB
            };
            var hic = ICOpen(IC.TypeVideo, (uint)codec, ICMODE.ICMODE_COMPRESS);

            if (hic.IsNull)
            {
                compressorInfo = new ICINFO();
                return hic;
            }

            using var outputHeader = IC.GetFormat(hic, inputHeader);

            if (!IC.CompressQuery(hic, inputHeader, outputHeader))
            {
                hic.Dispose();
                compressorInfo = new ICINFO();
                return new SafeHIC(0);
            }

            var infoSize = ICGetInfo(hic, ref compressorInfo, (uint)Marshal.SizeOf<ICINFO>());
            if (infoSize > 0)
            {
                return hic;
            }
            else
            {
                hic.Dispose();
                return new SafeHIC(0);
            }
        }
    }
}
