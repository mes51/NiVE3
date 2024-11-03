using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    readonly struct FormatInfo
    {
        public readonly int Width;

        public readonly int Height;

        public readonly int AspectRatioNumerator;

        public readonly int AspectRatioDenominator;

        public readonly int CorrectedWidth;

        public readonly int CorrectedHeight;

        public readonly bool IsTopDown;

        public FormatInfo(int width, int height, int aspectRatioNumerator, int aspectRatioDenominator, int correctedWidth, int correctedHeight, bool isTopDown)
        {
            Width = width;
            Height = height;
            AspectRatioNumerator = aspectRatioNumerator;
            AspectRatioDenominator = aspectRatioDenominator;
            CorrectedWidth = correctedWidth;
            CorrectedHeight = correctedHeight;
            IsTopDown = isTopDown;
        }

        public static FormatInfo GetVideoFormat(IMFSourceReader reader, SourceReaderIndex videoStreamId)
        {
            using var mediaType = reader.GetCurrentMediaType(videoStreamId);
            if (mediaType == null)
            {
                return new FormatInfo();
            }

            if (mediaType.GetGUID(MediaTypeAttributeKeys.Subtype) != VideoFormatGuids.Argb32 && mediaType.GetGUID(MediaTypeAttributeKeys.Subtype) != VideoFormatGuids.NV12)
            {
                return new FormatInfo();
            }

            var (width, height) = Util.GetDoubleInt32(mediaType, MediaTypeAttributeKeys.FrameSize);
            var stride = 1;
            try
            {
                stride = unchecked((int)mediaType.GetUInt32(MediaTypeAttributeKeys.DefaultStride));
            }
            catch { }
            var (aspectRatioNumerator, aspectRatioDenominator) = (1, 1);
            try
            {
                (aspectRatioNumerator, aspectRatioDenominator) = Util.GetDoubleInt32(mediaType, MediaTypeAttributeKeys.PixelAspectRatio);
            }
            catch { }

            var (correctedWidth, correctedHeight) = CorrectAspectRatio(width, height, aspectRatioNumerator, aspectRatioDenominator);

            return new FormatInfo(width, height, aspectRatioNumerator, aspectRatioDenominator, correctedWidth, correctedHeight, stride > 0);
        }

        static (int, int) CorrectAspectRatio(int width, int height, int numerator, int denominator)
        {
            if (numerator != 1 || denominator != 1)
            {
                if (numerator > denominator)
                {
                    return (Util.MulDiv(width, numerator, denominator), height);
                }
                else
                {
                    return (width, Util.MulDiv(height, denominator, numerator));
                }
            }
            else
            {
                return (width, height);
            }
        }
    }
}
