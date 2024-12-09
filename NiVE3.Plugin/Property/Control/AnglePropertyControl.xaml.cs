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
        public static readonly DependencyProperty RawRotateCountProperty = DependencyProperty.Register(
            nameof(RawRotateCount),
            typeof(int),
            typeof(AnglePropertyControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, AngleChanged)
        );

        public static readonly DependencyProperty RawAngleProperty = DependencyProperty.Register(
            nameof(RawAngle),
            typeof(double),
            typeof(AnglePropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, AngleChanged)
        );

        public static readonly DependencyProperty RotateCountProperty = DependencyProperty.Register(
            nameof(RotateCount),
            typeof(int),
            typeof(AnglePropertyControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            nameof(Angle),
            typeof(double),
            typeof(AnglePropertyControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsOnlyPositiveDirectionProperty = DependencyProperty.Register(
            nameof(IsOnlyPositiveDirection),
            typeof(bool),
            typeof(AnglePropertyControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public bool IsOnlyPositiveDirection
        {
            get { return (bool)GetValue(IsOnlyPositiveDirectionProperty); }
            set { SetValue(IsOnlyPositiveDirectionProperty, value); }
        }

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

        public double RawAngle
        {
            get { return (double)GetValue(RawAngleProperty); }
            set { SetValue(RawAngleProperty, value); }
        }

        public int RawRotateCount
        {
            get { return (int)GetValue(RawRotateCountProperty); }
            set { SetValue(RawRotateCountProperty, value); }
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

            if (ViewModel is not IPropertyViewModel viewModel)
            {
                return;
            }

            if (viewModel.CurrentTimeRawValue is double rawAngle)
            {
                var rawRotate = (int)(rawAngle / 360.0);
                rawAngle %= 360.0;

                IsValueChanging = true;

                RawRotateCount = rawRotate;
                RawAngle = rawAngle;

                IsValueChanging = false;
            }
            if (viewModel.IsEnableExpression && viewModel.CurrentTimeValue is double angle)
            {
                var rotate = (int)(angle / 360.0);
                angle %= 360.0;

                RotateCount = rotate;
                Angle = angle;
            }
            else
            {
                RotateCount = RawRotateCount;
                Angle = RawAngle;
            }
        }

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel is INotifyPropertyChanged viewModel)
            {
                // NOTE: 多重削除自体は問題ないので、多重登録を防ぐために一度削除する
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            if (viewModel == null || viewModel.CurrentTimeRawValue is not double rawAngle)
            {
                return;
            }

            var rawRotate = (int)(rawAngle / 360.0);
            rawAngle %= 360.0;

            IsValueChanging = true;

            RawRotateCount = rawRotate;
            RawAngle = rawAngle;

            IsValueChanging = false;

            if (viewModel.IsEnableExpression && viewModel.CurrentTimeValue is double angle)
            {
                var rotate = (int)(angle / 360.0);
                angle %= 360.0;

                RotateCount = rotate;
                Angle = angle;
            }
            else
            {
                RotateCount = RawRotateCount;
                Angle = RawAngle;
            }
        }

        private void SlidableNumberTextBox_BeginEditValue(object sender, RoutedEventArgs e)
        {
            ViewModel?.BeginEditCommand?.Execute(null);
        }

        private void SlidableNumberTextBox_EndEditValue(object sender, RoutedEventArgs e)
        {
            ViewModel?.EndEditCommand?.Execute(null);
        }

        private void SlidableNumberTextBox_AbortEditValue(object sender, RoutedEventArgs e)
        {
            ViewModel?.AbortEditCommand?.Execute(null);
        }

        static void AngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not AnglePropertyControl control || control.ViewModel is not IPropertyViewModel viewModel || control.IsValueChanging)
            {
                return;
            }

            if (control.IsOnlyPositiveDirection)
            {
                viewModel.CurrentTimeRawValue = Math.Max(control.RawRotateCount * 360.0 + control.RawAngle, 0.0);
            }
            else
            {
                viewModel.CurrentTimeRawValue = control.RawRotateCount * 360.0 + control.RawAngle;
            }
        }
    }
}
