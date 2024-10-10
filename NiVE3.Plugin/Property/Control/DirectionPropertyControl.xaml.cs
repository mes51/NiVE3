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
    /// DirectionPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class DirectionPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty RawValueXProperty = DependencyProperty.Register(
            nameof(RawValueX),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty RawValueYProperty = DependencyProperty.Register(
            nameof(RawValueY),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty RawValueZProperty = DependencyProperty.Register(
            nameof(RawValueZ),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty ValueXProperty = DependencyProperty.Register(
            nameof(ValueX),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ValueYProperty = DependencyProperty.Register(
            nameof(ValueY),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty ValueZProperty = DependencyProperty.Register(
            nameof(ValueZ),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

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

        public double RawValueZ
        {
            get { return (double)GetValue(RawValueZProperty); }
            set { SetValue(RawValueZProperty, value); }
        }

        public double RawValueY
        {
            get { return (double)GetValue(RawValueYProperty); }
            set { SetValue(RawValueYProperty, value); }
        }

        public double RawValueX
        {
            get { return (double)GetValue(RawValueXProperty); }
            set { SetValue(RawValueXProperty, value); }
        }

        bool IsValueChanging { get; set; }

        public DirectionPropertyControl()
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

            if (ViewModel is not IPropertyViewModel viewModel)
            {
                return;
            }

            if (viewModel.CurrentTimeRawValue is Vector3d rawVector)
            {
                IsValueChanging = true;

                RawValueX = rawVector.X % 360.0;
                RawValueY = rawVector.Y % 360.0;
                RawValueZ = rawVector.Z % 360.0;

                IsValueChanging = false;
            }
            if (viewModel.IsEnableExpression && viewModel.CurrentTimeValue is Vector3d vector)
            {
                ValueX = vector.X % 360.0;
                ValueY = vector.Y % 360.0;
                ValueZ = vector.Z % 360.0;
            }
            else
            {
                ValueX = RawValueX;
                ValueY = RawValueY;
                ValueZ = RawValueZ;
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
            if (viewModel == null || viewModel.CurrentTimeRawValue is not Vector3d rawVector)
            {
                return;
            }

            IsValueChanging = true;

            RawValueX = rawVector.X % 360.0;
            RawValueY = rawVector.Y % 360.0;
            RawValueZ = rawVector.Z % 360.0;

            IsValueChanging = false;

            if (viewModel.IsEnableExpression && viewModel.CurrentTimeValue is Vector3d vector)
            {
                ValueX = vector.X % 360.0;
                ValueY = vector.Y % 360.0;
                ValueZ = vector.Z % 360.0;
            }
            else
            {
                ValueX = RawValueX;
                ValueY = RawValueY;
                ValueZ = RawValueZ;
            }
        }

        static void VectorValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DirectionPropertyControl control || control.ViewModel is not IPropertyViewModel viewModel || control.IsValueChanging)
            {
                return;
            }

            viewModel.CurrentTimeRawValue = new Vector3d(control.RawValueX % 360.0, control.RawValueY % 360.0, control.RawValueZ % 360.0);
        }
    }
}
