using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.ValueObject
{
    record ColoredPreviewBoundingBox(IPreviewBoundingBox BoundingBox, Color Color)
    {
    }
}
