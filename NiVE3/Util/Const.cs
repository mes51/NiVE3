using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Util
{
    public static class Const
    {
        public const int AudioSamplingRate = 48000;

        public const int AudioChannelCount = 2;

        public const double AudioSampleTime = 1.0 / AudioSamplingRate;

        public const double DefaultCameraFov = 0.360000466176267;// Math.Tan(39.5978 * 0.5 * (Math.PI / 180.0))

        public const long KiB = 1024;

        public const long MiB = 1024 * 1024;

        public const int MinImageCacheSizeMiB = 32;

        public const int MinAutoSaveInterval = 1;

        public const int MaxAutoSaveInterval = 60;

        public const int MinAutoSaveCount = 1;

        public const int MaxAutoSaveCount = 100;
    }
}
