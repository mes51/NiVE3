using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.Image.Color
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct OkLab(float L, float a, float b)
    {
        public float L = L;

        public float a = a;

        public float b = b;

#pragma warning disable IDE0040 // for cast to Vector4
        readonly float Spacer;
#pragma warning restore IDE0040

        // https://bottosson.github.io/posts/oklab/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 ToRgb()
        {
            ref var lab = ref Unsafe.As<OkLab, Vector128<float>>(ref this);
            var lmsRow1 = Vector128.Create(1.0F, 0.3963377774F, 0.2158037573F, 0.0F);
            var lmsRow2 = Vector128.Create(1.0F, -0.1055613458F, -0.0638541728F, 0.0F);
            var lmsRow3 = Vector128.Create(1.0F, -0.0894841775F, -1.2914855480F, 0.0F);
            var lms = Sse.Shuffle(
                Sse41.Blend(
                    (lab * lmsRow1).HorizontalAdd(),
                    (lab * lmsRow2).HorizontalAdd(),
                    0b0010
                ),
                Sse.And((lab * lmsRow3).HorizontalAdd(), Vector128.Create(-1, 0, 0, 0).AsSingle()),
                0b01000100
            );
            lms = lms * lms * lms;

            var rgbRow1 = Vector128.Create(4.0767416621F, -3.3077115913F, 0.2309699292F, 0.0F);
            var rgbRow2 = Vector128.Create(-1.2684380046F, 2.6097574011F, -0.3413193965F, 0.0F);
            var rgbRow3 = Vector128.Create(-0.0041960863F, -0.7034186147F, 1.7076147010F, 0.0F);
            var linear = Sse.Shuffle(
                Sse41.Blend(
                    (lms * rgbRow3).HorizontalAdd(),
                    (lms * rgbRow2).HorizontalAdd(),
                    0b0010
                ),
                (lms * rgbRow1).HorizontalAdd(),
                0b11100100
            );

            var mask = Sse.CompareGreaterThanOrEqual(linear, Vector128.Create(0.0031308F));
            return Sse.Or(
                Sse.And(linear.SignWithoutZero() * (linear.Pow(Vector128.Create(1.0F / 2.4F)) * 1.055F - Vector128.Create(0.055F)), mask),
                //Sse.And((linear * Vector128.Create(1.055F)).Pow(Vector128.Create(1.0F / 2.4F - 0.055F)), mask),
                Sse.And(linear * 12.92F, mask.Not())
            ).AsVector4() + new Vector4(0.0F, 0.0F, 0.0F, 1.0F);
        }

        // https://bottosson.github.io/posts/oklab/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OkLab FromRgb(in Vector4 color)
        {
            var vColor = color.AsVector128();
            var mask = Sse.CompareGreaterThanOrEqual(vColor, Vector128.Create(0.04045F));
            var linear = Sse.Or(
                Sse.And(((vColor + Vector128.Create(0.055F)) / 1.055F).Pow(Vector128.Create(2.4F)), mask),
                Sse.And(vColor / 12.92F, mask.Not())
            );

            var lmsRow1 = Vector128.Create(0.0514459929F, 0.5363325363F, 0.4122214708F, 0.0F);
            var lmsRow2 = Vector128.Create(0.1073969566F, 0.6806995451F, 0.2119034982F, 0.0F);
            var lmsRow3 = Vector128.Create(0.6299787005F, 0.2817188376F, 0.0883024619F, 0.0F);
            var lms = Sse.Shuffle(
                Sse41.Blend(
                    (linear * lmsRow1).HorizontalAdd(),
                    (linear * lmsRow2).HorizontalAdd(),
                    0b0010
                ),
                Sse.And((linear * lmsRow3).HorizontalAdd(), Vector128.Create(-1, 0, 0, 0).AsSingle()),
                0b01000100
            ).Cbrt();

            var labRow1 = Vector128.Create(0.2104542553F, 0.7936177850F, -0.0040720468F, 0.0F);
            var labRow2 = Vector128.Create(1.9779984951F, -2.4285922050F, 0.4505937099F, 0.0F);
            var labRow3 = Vector128.Create(0.0259040371F, 0.7827717662F, -0.8086757660F, 0.0F);

            return new OkLab(
                (lms * labRow1).HorizontalAdd()[0],
                (lms * labRow2).HorizontalAdd()[0],
                (lms * labRow3).HorizontalAdd()[0]
            );
        }
    }
}
