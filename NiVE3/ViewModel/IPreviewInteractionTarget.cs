using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.ViewModel
{
    interface IPreviewInteractionTarget
    {
        Guid ParentLayerId { get; }

        bool IsInteracting { get; }

        bool MouseLeftButtonDown(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer);

        void MouseLeftButtonDrag(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer);

        void MouseLeftButtonUp(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer);

        void AbortInteraction();

        void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Color tagColor, ICoordTransformerObject coordTransformer);

        bool IsAlive();
    }
}
