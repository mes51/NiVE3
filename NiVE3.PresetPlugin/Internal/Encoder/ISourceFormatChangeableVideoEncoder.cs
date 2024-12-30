using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpAvi.Codecs;

namespace NiVE3.PresetPlugin.Internal.Encoder
{
    interface ISourceFormatChangeableVideoEncoder : IVideoEncoder
    {
        bool UseFormatConvertedSource { get; set; }
    }
}
