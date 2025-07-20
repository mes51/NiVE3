using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.ViewModel
{
    interface IPreviewInteractionTarget
    {
        Guid ParentLayerId { get; }

        void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);
    }
}
