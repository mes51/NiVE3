using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal
{
    static class Const
    {
        public const int AudioSamplingRate = 48000;

        public const int AudioChannelCount = 2;

        public const double AudioSampleTime = 1.0 / AudioSamplingRate;

        public static readonly Vector256<double> WithoutWMask256 = Vector256.Create(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0).AsDouble();

        public static readonly Vector128<float> WithoutWMask128 = Vector128.Create(0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU, 0).AsSingle();

        public static readonly Vector4 ConvertToGrayScale = new Vector4(0.114478F, 0.586611F, 0.298912F, 0.0F);

        public static readonly Float3 ConvertToGrayScaleFloat3 = new Float3(0.114478F, 0.586611F, 0.298912F);
    }
}
