using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Plugin.Property.Interaction
{
    class Vector3dPropertyInteraction : PropertyInteractionBase
    {
        public Vector3dPropertyInteraction(IPropertyInteractionViewModel viewModel) : base(viewModel) { }

        public override bool MouseDown(Point mousePositionInPreview, MouseButton mouseButton, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            throw new NotImplementedException();
        }

        public override bool MouseMove(Point mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            throw new NotImplementedException();
        }

        public override bool MouseUp(Point mousePositionInPreview, MouseButton mouseButton, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {
            throw new NotImplementedException();
        }

        public override void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, ICoordTransformerObject coordTransformer)
        {

        }
    }
}
