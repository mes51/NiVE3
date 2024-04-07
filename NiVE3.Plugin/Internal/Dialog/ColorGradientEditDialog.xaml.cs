using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NiVE3.Image.Color;
using NiVE3.Plugin.Internal.View;
using NiVE3.Shared.Extension;
using NiVE3.UI.Command;

namespace NiVE3.Plugin.Internal.Dialog
{
    /// <summary>
    /// ColorGradientEditDialog.xaml の相互作用ロジック
    /// </summary>
    partial class ColorGradientEditDialog : Window
    {
        public static readonly DependencyProperty SelectedMarkerProperty = DependencyProperty.Register(
            nameof(SelectedMarker),
            typeof(GradientMarker),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(null, SelectedMarkerChanged)
        );

        public static readonly DependencyProperty ShowPreviewOKLabInterpolationProperty = DependencyProperty.Register(
            nameof(ShowPreviewOKLabInterpolation),
            typeof(bool),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(false)
        );

        private static readonly DependencyProperty ColorStopsProperty = DependencyProperty.Register(
            nameof(ColorStops),
            typeof(ObservableCollection<GradientMarker>),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata()
        );

        private static readonly DependencyProperty OpacityStopsProperty = DependencyProperty.Register(
            nameof(OpacityStops),
            typeof(ObservableCollection<GradientMarker>),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata()
        );

        private static readonly DependencyProperty IsSelectColorMarkerProperty = DependencyProperty.Register(
            nameof(IsSelectColorMarker),
            typeof(bool),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(false)
        );

        private static readonly DependencyProperty IsSelectOpacityMarkerProperty = DependencyProperty.Register(
            nameof(IsSelectOpacityMarker),
            typeof(bool),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(false)
        );

        private static readonly DependencyProperty ColorStopOldColorProperty = DependencyProperty.Register(
            nameof(ColorStopOldColor),
            typeof(Color),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(Colors.Transparent)
        );

        private static readonly DependencyProperty ColorStopVectorColorProperty = DependencyProperty.Register(
            nameof(ColorStopVectorColor),
            typeof(Vector4),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(Vector4.Zero, ColorStopValueChanged)
        );

        private static readonly DependencyProperty ColorStopPositionProperty = DependencyProperty.Register(
            nameof(ColorStopPosition),
            typeof(float),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(0.0F, StopPositionChanged)
        );

        private static readonly DependencyProperty OpacityStopOpacityProperty = DependencyProperty.Register(
            nameof(OpacityStopOpacity),
            typeof(float),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(0.0F, OpacityStopValueChanged)
        );

        private static readonly DependencyProperty OpacityStopPositionProperty = DependencyProperty.Register(
            nameof(OpacityStopPosition),
            typeof(float),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(0.0F, StopPositionChanged)
        );

        private static readonly DependencyProperty GradientBarImageProperty = DependencyProperty.Register(
            nameof(GradientBarImage),
            typeof(WriteableBitmap),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(null)
        );

        private static readonly DependencyProperty UseOkLabInterpolationProperty = DependencyProperty.Register(
            nameof(UseOkLabInterpolation),
            typeof(bool),
            typeof(ColorGradientEditDialog),
            new FrameworkPropertyMetadata(false, UseOkLabInterpolationChanged)
        );

        public bool ShowPreviewOKLabInterpolation
        {
            get { return (bool)GetValue(ShowPreviewOKLabInterpolationProperty); }
            set { SetValue(ShowPreviewOKLabInterpolationProperty, value); }
        }

        public GradientMarker? SelectedMarker
        {
            get { return (GradientMarker)GetValue(SelectedMarkerProperty); }
            set { SetValue(SelectedMarkerProperty, value); }
        }

        private bool UseOkLabInterpolation
        {
            get { return (bool)GetValue(UseOkLabInterpolationProperty); }
            set { SetValue(UseOkLabInterpolationProperty, value); }
        }

        private WriteableBitmap GradientBarImage
        {
            get { return (WriteableBitmap)GetValue(GradientBarImageProperty); }
            set { SetValue(GradientBarImageProperty, value); }
        }

        private float OpacityStopPosition
        {
            get { return (float)GetValue(OpacityStopPositionProperty); }
            set { SetValue(OpacityStopPositionProperty, value); }
        }

        private float OpacityStopOpacity
        {
            get { return (float)GetValue(OpacityStopOpacityProperty); }
            set { SetValue(OpacityStopOpacityProperty, value); }
        }

        private float ColorStopPosition
        {
            get { return (float)GetValue(ColorStopPositionProperty); }
            set { SetValue(ColorStopPositionProperty, value); }
        }

        private Vector4 ColorStopVectorColor
        {
            get { return (Vector4)GetValue(ColorStopVectorColorProperty); }
            set { SetValue(ColorStopVectorColorProperty, value); }
        }

        private Color ColorStopOldColor
        {
            get { return (Color)GetValue(ColorStopOldColorProperty); }
            set { SetValue(ColorStopOldColorProperty, value); }
        }

        private bool IsSelectOpacityMarker
        {
            get { return (bool)GetValue(IsSelectOpacityMarkerProperty); }
            set { SetValue(IsSelectOpacityMarkerProperty, value); }
        }

        private bool IsSelectColorMarker
        {
            get { return (bool)GetValue(IsSelectColorMarkerProperty); }
            set { SetValue(IsSelectColorMarkerProperty, value); }
        }

        private ObservableCollection<GradientMarker> OpacityStops
        {
            get { return (ObservableCollection<GradientMarker>)GetValue(OpacityStopsProperty); }
            set { SetValue(OpacityStopsProperty, value); }
        }

        private ObservableCollection<GradientMarker> ColorStops
        {
            get { return (ObservableCollection<GradientMarker>)GetValue(ColorStopsProperty); }
            set { SetValue(ColorStopsProperty, value); }
        }

        public ICommand OKCommand { get; }

        public ColorGradientEditDialog(ColorGradient currentColorGradient)
        {
            OKCommand = new ActionCommand(() =>
            {
                DialogResult = true;
                Close();
            });

            // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/properties/collection-type-dependency-properties
            ColorStops = [];
            OpacityStops = [];

            foreach (var colorStop in currentColorGradient.ColorStops)
            {
                ColorStops.Add(new ColorGradientMarker { VectorColor = colorStop.Color, Position = colorStop.Position });
            }
            foreach (var opacityStop in currentColorGradient.OpacityStops)
            {
                OpacityStops.Add(new OpacityGradientMarker { Opacity = opacityStop.Opacity, Position = opacityStop.Position });
            }

            InitializeComponent();

            ColorStops.CollectionChanged += Stops_CollectionChanged;
            OpacityStops.CollectionChanged += Stops_CollectionChanged;
        }

        public ColorGradient GetColorGradient()
        {
            return new ColorGradient(
                [..ColorStops.OfType<ColorGradientMarker>().Select(m => new ColorStop(m.VectorColor, m.Position)).OrderBy(s => s.Position)],
                [..OpacityStops.OfType<OpacityGradientMarker>().Select(m => new OpacityStop(m.Opacity, m.Position)).OrderBy(s => s.Position)]
            );
        }

        void UpdateGradientBarImage()
        {
            var width = (int)GradientBarArea.ActualWidth;
            if (width < 1)
            {
                return;
            }

            var newImage = new WriteableBitmap(width, 1, 96.0, 96.0, PixelFormats.Bgra32, null);
            var colorGradient = GetColorGradient();
            var useOkLabInterpolation = UseOkLabInterpolation;

            var data = ArrayPool<int>.Shared.Rent(width);
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = colorGradient.GetrColor(i / (float)width, useOkLabInterpolation).ToIntColor();
            }
            newImage.WritePixels(new Int32Rect(0, 0, width, 1), data, width * 4, 0);
            ArrayPool<int>.Shared.Return(data);

            GradientBarImage = newImage;
        }

        private void RootWindow_ContentRendered(object sender, EventArgs e)
        {
            UpdateGradientBarImage();
        }

        private void SelectedMaker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (SelectedMarker is ColorGradientMarker colorGradientMarker)
            {
                ColorStopVectorColor = colorGradientMarker.VectorColor;
                ColorStopPosition = colorGradientMarker.Position * 100.0F;
            }
            else if (SelectedMarker is OpacityGradientMarker opacityGradientMarker)
            {
                OpacityStopOpacity = opacityGradientMarker.Opacity * 100.0F;
                OpacityStopPosition = opacityGradientMarker.Position * 100.0F;
            }
            UpdateGradientBarImage();
        }

        private void Stops_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateGradientBarImage();
        }

        static void SelectedMarkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorGradientEditDialog dialog)
            {
                return;
            }

            if (e.OldValue is GradientMarker oldMaker)
            {
                oldMaker.PropertyChanged -= dialog.SelectedMaker_PropertyChanged;
            }
            if (e.NewValue is GradientMarker newMaker)
            {
                newMaker.PropertyChanged += dialog.SelectedMaker_PropertyChanged;
            }

            if (dialog.SelectedMarker is ColorGradientMarker colorGradientMarker)
            {
                dialog.IsSelectColorMarker = true;
                dialog.IsSelectOpacityMarker = false;
                dialog.ColorStopOldColor = colorGradientMarker.Color;
                dialog.ColorStopVectorColor = colorGradientMarker.VectorColor;
                dialog.ColorStopPosition = colorGradientMarker.Position * 100.0F;
            }
            else if (dialog.SelectedMarker is OpacityGradientMarker opacityGradientMarker)
            {
                dialog.IsSelectColorMarker = false;
                dialog.IsSelectOpacityMarker = true;
                dialog.OpacityStopOpacity = opacityGradientMarker.Opacity * 100.0F;
                dialog.OpacityStopPosition = opacityGradientMarker.Position * 100.0F;
            }
            else
            {
                dialog.IsSelectColorMarker = false;
                dialog.IsSelectOpacityMarker = false;
            }
        }

        static void ColorStopValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorGradientEditDialog dialog || dialog.SelectedMarker is not ColorGradientMarker colorGradientMarker)
            {
                return;
            }

            colorGradientMarker.VectorColor = dialog.ColorStopVectorColor;
        }

        static void OpacityStopValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorGradientEditDialog dialog || dialog.SelectedMarker is not OpacityGradientMarker opacityGradientMarker)
            {
                return;
            }

            opacityGradientMarker.Opacity = dialog.OpacityStopOpacity * 0.01F;
        }

        static void StopPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorGradientEditDialog dialog || dialog.SelectedMarker == null)
            {
                return;
            }

            dialog.SelectedMarker.Position = ((float)e.NewValue) * 0.01F;
        }

        static void UseOkLabInterpolationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorGradientEditDialog dialog)
            {
                dialog.UpdateGradientBarImage();
            }
        }
    }

    class ColorGradientMarker : GradientMarker
    {
        private Vector4 vectorColor;
        public Vector4 VectorColor
        {
            get { return vectorColor; }
            set { SetProperty(ref vectorColor, value); }
        }

        public override Color Color
        {
            get
            {
                var v = Vector4.Clamp(VectorColor, Vector4.Zero, Vector4.One) * 255.0F;
                return Color.FromArgb(255, (byte)v.Z, (byte)v.Y, (byte)v.X);
            }
        }

        public override GradientMarker Copy()
        {
            return new ColorGradientMarker
            {
                VectorColor = VectorColor,
                Position = Position,
            };
        }

        public override GradientMarker Interpolation(GradientMarker nextMarker, float position)
        {
            if (Position > position || nextMarker is not ColorGradientMarker nextColorMaker)
            {
                return this;
            }
            else if (nextMarker.Position < position)
            {
                return nextMarker;
            }
            else
            {
                return new ColorGradientMarker
                {
                    VectorColor = Vector4.Lerp(VectorColor, nextColorMaker.VectorColor, (position - Position) / (nextMarker.Position - Position)),
                    Position = position
                };
            }
        }
    }

    class OpacityGradientMarker : GradientMarker
    {
        private float opacity;
        public float Opacity
        {
            get { return opacity; }
            set { SetProperty(ref opacity, value); }
        }

        public override Color Color
        {
            get
            {
                var color = (byte)((1.0F - Opacity) * 255.0F);
                return Color.FromRgb(color, color, color);
            }
        }

        public override GradientMarker Copy()
        {
            return new OpacityGradientMarker
            {
                Opacity = Opacity,
                Position = Position
            };
        }

        public override GradientMarker Interpolation(GradientMarker nextMarker, float position)
        {
            if (Position > position || nextMarker is not OpacityGradientMarker nextOpacityMaker)
            {
                return this;
            }
            else if (nextMarker.Position < position)
            {
                return nextMarker;
            }
            else
            {
                return new OpacityGradientMarker
                {
                    Opacity = float.Lerp(Opacity, nextOpacityMaker.Opacity, (position - Position) / (nextMarker.Position - Position)),
                    Position = position
                };
            }
        }
    }
}
