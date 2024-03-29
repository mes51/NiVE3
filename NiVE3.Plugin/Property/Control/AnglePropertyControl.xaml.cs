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

namespace NiVE3.Plugin.Property.Control
{
    /// <summary>
    /// AnglePropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class AnglePropertyControl : PropertyControlBase
    {
        public static readonly DependencyProperty RotateCountProperty = DependencyProperty.Register(
            nameof(RotateCount),
            typeof(int),
            typeof(AnglePropertyControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, AngleChanged)
        );

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            nameof(Angle),
            typeof(double),
            typeof(AnglePropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, AngleChanged)
        );

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public int RotateCount
        {
            get { return (int)GetValue(RotateCountProperty); }
            set { SetValue(RotateCountProperty, value); }
        }

        bool IsValueChanging { get; set; }

        public AnglePropertyControl()
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

            if (ViewModel?.CurrentTimeValue is double angle)
            {
                var rotate = (int)(angle / 360.0);
                angle %= 360.0;

                IsValueChanging = true;

                RotateCount = rotate;
                Angle = angle;

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
            if (viewModel == null || viewModel.CurrentTimeValue is not double angle)
            {
                return;
            }

            var rotate = (int)(angle / 360.0);
            angle %= 360.0;

            IsValueChanging = true;

            RotateCount = rotate;
            Angle = angle;

            IsValueChanging = false;
        }

        static void AngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not AnglePropertyControl control || control.ViewModel is not IPropertyViewModel viewModel || control.IsValueChanging)
            {
                return;
            }

            viewModel.CurrentTimeValue = control.RotateCount * 360.0 + control.Angle;
        }
    }
}
