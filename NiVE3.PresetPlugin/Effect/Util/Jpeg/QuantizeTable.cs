using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.PresetPlugin.Effect.Util.Jpeg
{
    // from RFC2435 Appendix A
    // SEE: https://datatracker.ietf.org/doc/html/rfc2435#appendix-A
    public class QuantizeTable
    {
        internal static readonly float[] LuminanceBaseQuantizeTable =
        [
            16.0F, 11.0F, 10.0F, 16.0F,  24.0F,  40.0F,  51.0F,  61.0F,
            12.0F, 12.0F, 14.0F, 19.0F,  26.0F,  58.0F,  60.0F,  55.0F,
            14.0F, 13.0F, 16.0F, 24.0F,  40.0F,  57.0F,  69.0F,  56.0F,
            14.0F, 17.0F, 22.0F, 29.0F,  51.0F,  87.0F,  80.0F,  62.0F,
            18.0F, 22.0F, 37.0F, 56.0F,  68.0F, 109.0F, 103.0F,  77.0F,
            24.0F, 35.0F, 55.0F, 64.0F,  81.0F, 104.0F, 113.0F,  92.0F,
            49.0F, 64.0F, 78.0F, 87.0F, 103.0F, 121.0F, 120.0F, 101.0F,
            72.0F, 92.0F, 95.0F, 98.0F, 112.0F, 100.0F, 103.0F,  99.0F
        ];

        internal static readonly float[] ChrominanceBaseQuantizeTable =
        [
            17.0F, 18.0F, 24.0F, 47.0F, 99.0F, 99.0F, 99.0F, 99.0F,
            18.0F, 21.0F, 26.0F, 66.0F, 99.0F, 99.0F, 99.0F, 99.0F,
            24.0F, 26.0F, 56.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F,
            47.0F, 66.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F,
            99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F,
            99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F,
            99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F,
            99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F, 99.0F
        ];

        public float[] Table { get; }

        public QuantizeTable(float[] baseTable, float quality)
        {
            Table = new float[64];

            quality = Math.Clamp(quality, 1.0F, 100.0F);
            if (quality < 50.0F)
            {
                quality = 5000.0F / quality;
            }
            else
            {
                quality = 200.0F - quality * 2.0F;
            }
            for (var i = 0; i < baseTable.Length; i++)
            {
                var q = (baseTable[i] * quality + 50.0F) / 100.0F;
                Table[i] = Math.Clamp(q, 1.0F, 255.0F);
            }
        }

        public ReadOnlySpan<Vector256<float>> GetVectorTable()
        {
            return MemoryMarshal.Cast<float, Vector256<float>>(Table);
        }
    }
}
