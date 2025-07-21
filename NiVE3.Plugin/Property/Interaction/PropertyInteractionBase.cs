using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Plugin.Property.Interaction
{
    public abstract class PropertyInteractionBase
    {
        protected IPropertyInteractionViewModel ViewModel { get; }

        protected PropertyInteractionBase(IPropertyInteractionViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public abstract bool MouseMove(Point mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);

        public abstract bool MouseDown(Point mousePositionInPreview, MouseButton mouseButton, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);

        public abstract bool MouseUp(Point mousePositionInPreview, MouseButton mouseButton, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);

        public abstract void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Color tagColor, ICoordTransformerObject coordTransformer);
    }
}
