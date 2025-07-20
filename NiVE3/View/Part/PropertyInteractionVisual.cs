using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class PropertyInteractionVisual : FrameworkElement
    {
        public static readonly DependencyProperty PreviewImageLeftProperty = DependencyProperty.Register(
            nameof(PreviewImageLeft),
            typeof(double),
            typeof(PropertyInteractionVisual),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PreviewImageTopProperty = DependencyProperty.Register(
            nameof(PreviewImageTop),
            typeof(double),
            typeof(PropertyInteractionVisual),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PreviewImageScaleXProperty = DependencyProperty.Register(
            nameof(PreviewImageScaleX),
            typeof(double),
            typeof(PropertyInteractionVisual),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PreviewImageScaleYProperty = DependencyProperty.Register(
            nameof(PreviewImageScaleY),
            typeof(double),
            typeof(PropertyInteractionVisual),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PreviewViewModelProperty = DependencyProperty.Register(
            nameof(PreviewViewModel),
            typeof(PreviewViewModel),
            typeof(PropertyInteractionVisual),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, PreviewViewModelChanged)
        );

        public PreviewViewModel? PreviewViewModel
        {
            get { return (PreviewViewModel)GetValue(PreviewViewModelProperty); }
            set { SetValue(PreviewViewModelProperty, value); }
        }

        public double PreviewImageScaleY
        {
            get { return (double)GetValue(PreviewImageScaleYProperty); }
            set { SetValue(PreviewImageScaleYProperty, value); }
        }

        public double PreviewImageScaleX
        {
            get { return (double)GetValue(PreviewImageScaleXProperty); }
            set { SetValue(PreviewImageScaleXProperty, value); }
        }

        public double PreviewImageTop
        {
            get { return (double)GetValue(PreviewImageTopProperty); }
            set { SetValue(PreviewImageTopProperty, value); }
        }

        public double PreviewImageLeft
        {
            get { return (double)GetValue(PreviewImageLeftProperty); }
            set { SetValue(PreviewImageLeftProperty, value); }
        }

        static PropertyInteractionVisual()
        {
            IsHitTestVisibleProperty.OverrideMetadata(typeof(PropertyInteractionVisual), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            PreviewViewModel?.RenderPropertyInteractionCommand.Execute(Tuple.Create(drawingContext, new Vector2d(PreviewImageLeft, PreviewImageTop), new Vector2d(PreviewImageScaleX, PreviewImageScaleY)));
        }

        private void ViewModel_UpdatePropertyInteractionRequest(object? sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private static void PreviewViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PropertyInteractionVisual visual)
            {
                return;
            }

            if (e.OldValue is PreviewViewModel oldValue)
            {
                oldValue.UpdatePropertyInteractionRequest -= visual.ViewModel_UpdatePropertyInteractionRequest;
            }
            if (e.NewValue is PreviewViewModel newValue)
            {
                newValue.UpdatePropertyInteractionRequest += visual.ViewModel_UpdatePropertyInteractionRequest;
            }
        }
    }
}
