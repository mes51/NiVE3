using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Struct;

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// ScalePropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ScalePropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty Is3DProperty = DependencyProperty.Register(
            nameof(Is3D),
            typeof(bool),
            typeof(ScalePropertyControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ValueXProperty = DependencyProperty.Register(
            nameof(ValueX),
            typeof(double),
            typeof(ScalePropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty ValueYProperty = DependencyProperty.Register(
            nameof(ValueY),
            typeof(double),
            typeof(ScalePropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty ValueZProperty = DependencyProperty.Register(
            nameof(ValueZ),
            typeof(double),
            typeof(ScalePropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty IsLinkRatioProperty = DependencyProperty.Register(
            nameof(IsLinkRatio),
            typeof(bool),
            typeof(ScalePropertyControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IsLinkRatioChanged)
        );

        public bool IsLinkRatio
        {
            get { return (bool)GetValue(IsLinkRatioProperty); }
            set { SetValue(IsLinkRatioProperty, value); }
        }

        public double ValueZ
        {
            get { return (double)GetValue(ValueZProperty); }
            set { SetValue(ValueZProperty, value); }
        }

        public double ValueY
        {
            get { return (double)GetValue(ValueYProperty); }
            set { SetValue(ValueYProperty, value); }
        }

        public double ValueX
        {
            get { return (double)GetValue(ValueXProperty); }
            set { SetValue(ValueXProperty, value); }
        }

        public bool Is3D
        {
            get { return (bool)GetValue(Is3DProperty); }
            set { SetValue(Is3DProperty, value); }
        }

        bool IsValueChanging { get; set; }

        Vector3d ScaleRatio { get; set; } = new Vector3d(1.0, 1.0, 1.0);

        public ScalePropertyControl()
        {
            InitializeComponent();
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is INotifyPropertyChanged newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            if (ViewModel?.Value is Vector3d vector)
            {
                IsValueChanging = true;

                ValueX = vector.X;
                ValueY = vector.Y;
                ValueZ = vector.Z;

                IsValueChanging = false;

                if (IsLinkRatio)
                {
                    var baseValue = 1.0;
                    if (ValueX != 0.0)
                    {
                        baseValue = ValueX;
                    }
                    else if (ValueY != 0.0)
                    {
                        baseValue = ValueY;
                    }
                    else if (ValueZ != 0.0)
                    {
                        baseValue = ValueZ;
                    }
                    ScaleRatio = new Vector3d(ValueX, ValueY, ValueZ) / baseValue;
                }
            }
        }

        private void Root_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel is INotifyPropertyChanged viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null || viewModel.Value is not Vector3d vector)
            {
                return;
            }

            IsValueChanging = true;

            ValueX = vector.X;
            ValueY = vector.Y;
            ValueZ = vector.Z;

            IsValueChanging = false;

            if (IsLinkRatio)
            {
                var baseValue = 1.0;
                if (ValueX != 0.0)
                {
                    baseValue = ValueX;
                }
                else if (ValueY != 0.0)
                {
                    baseValue = ValueY;
                }
                else if (ValueZ != 0.0)
                {
                    baseValue = ValueZ;
                }
                ScaleRatio = new Vector3d(ValueX, ValueY, ValueZ) / baseValue;
            }
        }

        static void VectorValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScalePropertyControl control || control.ViewModel is not IPropertyViewModel viewModel || control.IsValueChanging)
            {
                return;
            }

            if (control.IsLinkRatio)
            {
                var scale = control.ScaleRatio;
                if ((e.Property == ValueXProperty && scale.X == 0.0) ||
                    (e.Property == ValueYProperty && scale.Y == 0.0) ||
                    (e.Property == ValueZProperty && scale.Z == 0.0))
                {
                    scale = new Vector3d(1.0, 1.0, 1.0);
                    control.ScaleRatio = scale;
                }
                else if (e.Property == ValueXProperty)
                {
                    scale /= scale.X;;
                }
                else if (e.Property == ValueYProperty)
                {
                    scale /= scale.Y;
                }
                else if (e.Property == ValueZProperty)
                {
                    scale /= scale.Z;
                }

                viewModel.Value = scale * (double)e.NewValue;
            }
            else
            {
                viewModel.Value = new Vector3d(control.ValueX, control.ValueY, control.ValueZ);
            }
        }

        static void IsLinkRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScalePropertyControl control)
            {
                if (control.IsLinkRatio)
                {
                    var baseValue = 1.0;
                    if (control.ValueX != 0.0)
                    {
                        baseValue = control.ValueX;
                    }
                    else if (control.ValueY != 0.0)
                    {
                        baseValue = control.ValueY;
                    }
                    else if (control.ValueZ != 0.0)
                    {
                        baseValue = control.ValueZ;
                    }
                    control.ScaleRatio = new Vector3d(control.ValueX, control.ValueY, control.ValueZ) / baseValue;
                }
            }
        }
    }
}
