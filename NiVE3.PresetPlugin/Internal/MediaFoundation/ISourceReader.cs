using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    interface ISourceReader : IDisposable
    {
        const double DurationRate = 1E7;

        const int FirstVideoStreamId = (int)MidlMidlItfMfreadwrite000000010001.MfSourceReaderFirstVideoStream;

        public bool Succeeded { get; }

        public double Duration { get; }

        public double FrameRate { get; }

        public string FilePath { get; }

        FormatInfo Format { get; }

        byte[] GetFrame(double time);
    }
}
