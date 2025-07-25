using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Plugin.Property.Interaction
{
    public abstract class PropertyInteractionBase
    {
        public bool IsInteracting { get; set; }

        protected IPropertyInteractionViewModel ViewModel { get; }

        protected PropertyInteractionBase(IPropertyInteractionViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public abstract bool HitTestInteraction(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer);

        public abstract bool MouseLeftButtonDown(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer);

        public abstract void MouseLeftButtonDrag(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer);

        public abstract void MouseLeftButtonUp(Vector2d mousePositionInPreview, ICoordTransformerObject coordTransformer);

        public abstract void AbortInteraction();

        public abstract void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Color tagColor, ICoordTransformerObject coordTransformer);
    }
}
