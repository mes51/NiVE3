using System;
using System.Collections.Generic;
using System.Text;
using NiVE3.View.Primitive.PreviewText;

namespace NiVE3.ValueObject
{
    record TextLayerPreviewText(Guid LayerId, CharacterGeometry[] CharacterGeometries, CharacterGeometry EmptyTextCaretGeometry)
    {
    }
}
