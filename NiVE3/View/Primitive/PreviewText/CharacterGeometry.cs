using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Windows;

namespace NiVE3.View.Primitive.PreviewText
{
    record CharacterGeometry(string Grapheme, Rect Bounds, Matrix3x2 GraphemeTransform, PreviewTextCoordTransformer Transformer);
}
