using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NiVE3.PresetPlugin.Internal.View
{
    class GraphValueEditView : FrameworkElement
    {
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            nameof(Values),
            typeof(ObservableCollection<float>),
            typeof(GraphValueEditView),
            new FrameworkPropertyMetadata(new ObservableCollection<float>(), FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(GraphValueEditView),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            nameof(Foreground),
            typeof(Brush),
            typeof(GraphValueEditView),
            new FrameworkPropertyMetadata(Brushes.DarkRed, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public ObservableCollection<float> Values
        {
            get { return (ObservableCollection<float>)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        public static readonly RoutedEvent BeginEditEvent = EventManager.RegisterRoutedEvent(
            nameof(BeginEdit), RoutingStrategy.Direct, typeof(EventHandler<RoutedEventArgs>), typeof(GraphValueEditView)
        );

        public static readonly RoutedEvent EndEditEvent = EventManager.RegisterRoutedEvent(
            nameof(EndEdit), RoutingStrategy.Direct, typeof(EventHandler<RoutedEventArgs>), typeof(GraphValueEditView)
        );

        public static readonly RoutedEvent AbortEditEvent = EventManager.RegisterRoutedEvent(
            nameof(AbortEdit), RoutingStrategy.Direct, typeof(EventHandler<RoutedEventArgs>), typeof(GraphValueEditView)
        );

        public static readonly RoutedEvent UpdateValuesEvent = EventManager.RegisterRoutedEvent(
            nameof(UpdateValues), RoutingStrategy.Direct, typeof(EventHandler<RoutedEventArgs>), typeof(GraphValueEditView)
        );

        public event EventHandler<RoutedEventArgs> UpdateValues
        {
            add { AddHandler(UpdateValuesEvent, value); }
            remove { RemoveHandler(UpdateValuesEvent, value); }
        }

        public event EventHandler<RoutedEventArgs> AbortEdit
        {
            add { AddHandler(AbortEditEvent, value); }
            remove { RemoveHandler(AbortEditEvent, value); }
        }

        public event EventHandler<RoutedEventArgs> EndEdit
        {
            add { AddHandler(EndEditEvent, value); }
            remove { RemoveHandler(EndEditEvent, value); }
        }

        public event EventHandler<RoutedEventArgs> BeginEdit
        {
            add { AddHandler(BeginEditEvent, value); }
            remove { RemoveHandler(BeginEditEvent, value); }
        }

        bool IsClicked { get; set; }

        int LastEditedIndex { get; set; }

        float[] LastValues { get; set; } = [];

        public GraphValueEditView()
        {
            DataContextChanged += GraphValueEditView_DataContextChanged;
            MouseDown += GraphValueEditView_MouseDown;
            MouseMove += GraphValueEditView_MouseMove;
            MouseUp += GraphValueEditView_MouseUp;
        }

        private void GraphValueEditView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IsClicked = false;
        }

        private void GraphValueEditView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsClicked)
            {
                return;
            }

            EditValue(e.GetPosition(this));
            IsClicked = false;

            if (!LastValues.SequenceEqual(Values))
            {
                RaiseEvent(new RoutedEventArgs(EndEditEvent, this));
            }
            else
            {
                RaiseEvent(new RoutedEventArgs(AbortEditEvent, this));
            }

            ReleaseMouseCapture();
        }

        private void GraphValueEditView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsClicked)
            {
                return;
            }

            var mousePos = e.GetPosition(this);
            EditValue(mousePos);

            RaiseEvent(new RoutedEventArgs(UpdateValuesEvent, this));
        }

        private void GraphValueEditView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Values.Count < 1)
            {
                return;
            }

            RaiseEvent(new RoutedEventArgs(BeginEditEvent));

            IsClicked = true;

            var mousePos = e.GetPosition(this);
            var height = ActualHeight;
            var values = Values;
            var pixelsPerValue = ActualWidth / values.Count;

            var targetIndex = Math.Clamp((int)Math.Round(mousePos.X / pixelsPerValue), 0, values.Count - 1);
            values[targetIndex] = Math.Clamp((float)((ActualHeight - mousePos.Y) / ActualHeight), 0.0F, 1.0F);

            LastEditedIndex = targetIndex;
            LastValues = [..values];

            CaptureMouse();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var width = ActualWidth;
            var height = ActualHeight;
            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, width, height));

            var values = Values;
            if (values.Count < 1)
            {
                return;
            }

            var pixelsPerValue = width / values.Count;
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(0.0, height), true, true);

                context.LineTo(new Point(0.0, height - values[0] * height), false, false);
                for (var i = 0; i < values.Count; i++)
                {
                    context.LineTo(new Point((i + 1) * pixelsPerValue, height - values[i] * height), false, false);
                }

                context.LineTo(new Point(width, height), false, false);
            }

            drawingContext.DrawGeometry(Foreground, null, geometry);
        }

        void EditValue(Point mousePos)
        {
            var height = ActualHeight;
            var values = Values;
            var pixelsPerValue = ActualWidth / values.Count;

            var targetIndex = Math.Clamp((int)Math.Round(mousePos.X / pixelsPerValue), 0, values.Count - 1);
            var newValue = Math.Clamp((float)((ActualHeight - mousePos.Y) / ActualHeight), 0.0F, 1.0F);
            var lastTargetValue = values[LastEditedIndex];
            if (targetIndex < LastEditedIndex)
            {
                var diffIndex = LastEditedIndex - targetIndex;
                for (var i = targetIndex; i <= LastEditedIndex; i++)
                {
                    values[i] = float.Lerp(newValue, lastTargetValue, (i - targetIndex) / (float)diffIndex);
                }
            }
            else if (targetIndex > LastEditedIndex)
            {
                var diffIndex = targetIndex - LastEditedIndex;
                for (var i = LastEditedIndex; i <= targetIndex; i++)
                {
                    values[i] = float.Lerp(lastTargetValue, newValue, (i - LastEditedIndex) / (float)diffIndex);
                }
            }
            else
            {
                values[targetIndex] = newValue;
            }

            LastEditedIndex = targetIndex;
            InvalidateVisual();
        }
    }
}
