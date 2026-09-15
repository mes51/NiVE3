using System;
using System.Collections.Generic;
using System.Text;
using NiVE3.View.Primitive.PreviewText;

namespace NiVE3.ValueObject
{
    /// <param name="LayerId">テキストレイヤーの ID</param>
    /// <param name="CharacterGeometries">書記素ごとのジオメトリ (文字列順)</param>
    /// <param name="EmptyTextCaretGeometry">テキストが空のときのキャレット位置</param>
    /// <param name="IsVertical">縦書きか</param>
    record TextLayerPreviewText(Guid LayerId, CharacterGeometry[] CharacterGeometries, CharacterGeometry EmptyTextCaretGeometry, bool IsVertical)
    {
    }
}
