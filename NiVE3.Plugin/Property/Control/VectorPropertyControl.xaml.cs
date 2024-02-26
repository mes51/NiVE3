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
using NiVE3.Numerics;

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// VectorPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class VectorPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty Is3DProperty = DependencyProperty.Register(
            nameof(Is3D),
            typeof(bool),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ValueXProperty = DependencyProperty.Register(
            nameof(ValueX),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty ValueYProperty = DependencyProperty.Register(
            nameof(ValueY),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty ValueZProperty = DependencyProperty.Register(
            nameof(ValueZ),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty MinimumXProperty = DependencyProperty.Register(
            nameof(MinimumX),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(double.MinValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MinimumYProperty = DependencyProperty.Register(
            nameof(MinimumY),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(double.MinValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MinimumZProperty = DependencyProperty.Register(
            nameof(MinimumZ),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(double.MinValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MaximumXProperty = DependencyProperty.Register(
            nameof(MaximumX),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(double.MaxValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MaximumYProperty = DependencyProperty.Register(
            nameof(MaximumY),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(double.MaxValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MaximumZProperty = DependencyProperty.Register(
            nameof(MaximumZ),
            typeof(double),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata(double.MaxValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
            nameof(Unit),
            typeof(string),
            typeof(VectorPropertyControl),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public double MaximumZ
        {
            get { return (double)GetValue(MaximumZProperty); }
            set { SetValue(MaximumZProperty, value); }
        }

        public double MaximumY
        {
            get { return (double)GetValue(MaximumYProperty); }
            set { SetValue(MaximumYProperty, value); }
        }

        public double MaximumX
        {
            get { return (double)GetValue(MaximumXProperty); }
            set { SetValue(MaximumXProperty, value); }
        }

        public double MinimumZ
        {
            get { return (double)GetValue(MinimumZProperty); }
            set { SetValue(MinimumZProperty, value); }
        }

        public double MinimumY
        {
            get { return (double)GetValue(MinimumYProperty); }
            set { SetValue(MinimumYProperty, value); }
        }

        public double MinimumX
        {
            get { return (double)GetValue(MinimumXProperty); }
            set { SetValue(MinimumXProperty, value); }
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

        public VectorPropertyControl()
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

            if (ViewModel?.CurrentTimeValue is Vector3d vector)
            {
                IsValueChanging = true;

                ValueX = vector.X;
                ValueY = vector.Y;
                ValueZ = vector.Z;

                IsValueChanging = false;
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
            if (viewModel == null || viewModel.CurrentTimeValue is not Vector3d vector)
            {
                return;
            }

            IsValueChanging = true;

            ValueX = vector.X;
            ValueY = vector.Y;
            ValueZ = vector.Z;

            IsValueChanging = false;
        }

        static void VectorValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not VectorPropertyControl control || control.ViewModel is not IPropertyViewModel viewModel || control.IsValueChanging)
            {
                return;
            }

            viewModel.CurrentTimeValue = new Vector3d(control.ValueX, control.ValueY, control.ValueZ);
        }
    }
}
