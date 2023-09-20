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
    /// DirectionPropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class DirectionPropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty ValueXProperty = DependencyProperty.Register(
            nameof(ValueX),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty ValueYProperty = DependencyProperty.Register(
            nameof(ValueY),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
        );

        public static readonly DependencyProperty ValueZProperty = DependencyProperty.Register(
            nameof(ValueZ),
            typeof(double),
            typeof(DirectionPropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, VectorValueChanged)
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

            if (ViewModel?.CurrentTimeValue is Vector3d vector)
            {
                IsValueChanging = true;

                ValueX = vector.X % 360.0;
                ValueY = vector.Y % 360.0;
                ValueZ = vector.Z % 360.0;

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

            ValueX = vector.X % 360.0;
            ValueY = vector.Y % 360.0;
            ValueZ = vector.Z % 360.0;

            IsValueChanging = false;
        }

        static void VectorValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DirectionPropertyControl control || control.ViewModel is not IPropertyViewModel viewModel || control.IsValueChanging)
            {
                return;
            }

            viewModel.CurrentTimeValue = new Vector3d(control.ValueX % 360.0, control.ValueY % 360.0, control.ValueZ % 360.0);
        }
    }
}
