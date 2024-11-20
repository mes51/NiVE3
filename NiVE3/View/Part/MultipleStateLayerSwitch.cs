using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using NiVE3.Extension;
using NiVE3.Shared.Extension;

namespace NiVE3.View.Part
{
    [ContentProperty(nameof(StateItems))]
    class MultipleStateLayerSwitch : FrameworkElement
    {
        const double InitialLightStrength = 0.5;

        public static readonly DependencyProperty IconBrushProperty = DependencyProperty.Register(
            nameof(IconBrush),
            typeof(Brush),
            typeof(MultipleStateLayerSwitch),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(MultipleStateLayerSwitch),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
            nameof(BorderBrush),
            typeof(Brush),
            typeof(MultipleStateLayerSwitch),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, BorderBrushChanged)
        );

        public static readonly DependencyProperty LightStrengthProperty = DependencyProperty.Register(
            nameof(LightStrength),
            typeof(double),
            typeof(MultipleStateLayerSwitch),
            new FrameworkPropertyMetadata(InitialLightStrength, FrameworkPropertyMetadataOptions.AffectsRender, LightStrengthChanged)
        );

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(MultipleStateLayerSwitch),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(MultipleStateLayerSwitch),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
            nameof(State),
            typeof(object),
            typeof(MultipleStateLayerSwitch),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public List<MultipleStateLayerSwitchItem> StateItems { get; set; } = [];

        public object? State
        {
            get { return (object)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public object? CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public double LightStrength
        {
            get { return (double)GetValue(LightStrengthProperty); }
            set { SetValue(LightStrengthProperty, value); }
        }

        public Brush? BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        public Brush? Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public Brush? IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }

        Pen? BorderPen { get; set; } = new Pen(Brushes.Black, 1.0).FreezeCurrentObject();

        Pen? LightBorderPen { get; set; } = new Pen(new SolidColorBrush(Colors.White) { Opacity = InitialLightStrength }, 1.0).FreezeCurrentObject();

        public MultipleStateLayerSwitch()
        {
            MouseDown += LayerSwitch_MouseDown;
        }

        private void LayerSwitch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Command != null && Command.CanExecute(CommandParameter))
                {
                    Command.Execute(CommandParameter);
                }
                else
                {
                    var items = StateItems;
                    if (items.Count < 1)
                    {
                        State = null;
                        return;
                    }

                    var state = State;
                    var index = Shared.Extension.EnumerableExtensions.FindIndex(items, i => (i.State == null && state == null) || (i.State?.Equals(state) ?? false));
                    if (index < 0)
                    {
                        State = items.First().State;
                    }
                    else
                    {
                        State = items[(index + 1) % items.Count].State;
                    }
                }
                e.Handled = true;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (ActualWidth <= 0.0 || ActualHeight <= 0.0)
            {
                return;
            }

            if (BorderPen != null && LightBorderPen != null)
            {
                drawingContext.DrawRectangle(Background, BorderPen, new Rect(0.5, 0.5, ActualWidth - 1.0, ActualHeight - 1.0));
                drawingContext.DrawLine(LightBorderPen, new Point(0.0, ActualHeight - 0.5), new Point(ActualWidth, ActualHeight - 0.5));
                drawingContext.DrawLine(LightBorderPen, new Point(ActualWidth - 0.5, 0.0), new Point(ActualWidth - 0.5, ActualHeight - 1.0));
            }
            else
            {
                drawingContext.DrawRectangle(Background, null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));
            }

            var state = State;
            var switchIcon = StateItems.FirstOrDefault(i => (i.State == null && state == null) || (i.State?.Equals(state) ?? false));
            var iconBrush = IconBrush;
            if (switchIcon != null && switchIcon.Icon != null && iconBrush != null)
            {
                var bounds = switchIcon.Icon.GetRenderBounds(null);
                drawingContext.PushTransform(new TranslateTransform((ActualWidth - bounds.Width) * 0.5, (ActualHeight - bounds.Height) * 0.5));
                drawingContext.DrawGeometry(iconBrush, null, switchIcon.Icon);
                drawingContext.Pop();
            }
        }

        static void BorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultipleStateLayerSwitch layerSwitch)
            {
                var borderBrush = layerSwitch.BorderBrush;
                if (borderBrush != null)
                {
                    layerSwitch.BorderPen = new Pen(layerSwitch.BorderBrush, 1.0).FreezeCurrentObject();
                    if (layerSwitch.LightBorderPen == null)
                    {
                        var lightBorderBrush = new SolidColorBrush(Colors.White) { Opacity = layerSwitch.LightStrength };
                        layerSwitch.LightBorderPen = new Pen(lightBorderBrush, 1.0).FreezeCurrentObject();
                    }
                }
                else
                {
                    layerSwitch.BorderPen = null;
                    layerSwitch.LightBorderPen = null;
                }
            }
        }

        static void LightStrengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultipleStateLayerSwitch layerSwitch && layerSwitch.BorderPen != null)
            {
                var lightBorderBrush = new SolidColorBrush(Colors.White) { Opacity = layerSwitch.LightStrength };
                layerSwitch.LightBorderPen = new Pen(lightBorderBrush, 1.0).FreezeCurrentObject();
            }
        }
    }

    class MultipleStateLayerSwitchItem
    {
        public object? State { get; set; }

        public Geometry? Icon { get; set; }
    }
}
