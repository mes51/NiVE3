using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using NiVE3.Plugin.Property.Control;

namespace NiVE3.PresetPlugin.Property.Control
{
    /// <summary>
    /// ToneCurvePropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ToneCurvePropertyControl : PropertyControlBase
    {
        internal static readonly DependencyProperty RgbPointsProperty = DependencyProperty.Register(
            nameof(RgbPoints),
            typeof(ObservableCollection<ToneCurvePoint>),
            typeof(ToneCurvePropertyControl),
            new FrameworkPropertyMetadata(new ObservableCollection<ToneCurvePoint>())
        );

        internal static readonly DependencyProperty RPointsProperty = DependencyProperty.Register(
            nameof(RPoints),
            typeof(ObservableCollection<ToneCurvePoint>),
            typeof(ToneCurvePropertyControl),
            new FrameworkPropertyMetadata(new ObservableCollection<ToneCurvePoint>())
        );

        internal static readonly DependencyProperty GPointsProperty = DependencyProperty.Register(
            nameof(GPoints),
            typeof(ObservableCollection<ToneCurvePoint>),
            typeof(ToneCurvePropertyControl),
            new FrameworkPropertyMetadata(new ObservableCollection<ToneCurvePoint>())
        );

        internal static readonly DependencyProperty BPointsProperty = DependencyProperty.Register(
            nameof(BPoints),
            typeof(ObservableCollection<ToneCurvePoint>),
            typeof(ToneCurvePropertyControl),
            new FrameworkPropertyMetadata(new ObservableCollection<ToneCurvePoint>())
        );

        internal static readonly DependencyProperty APointsProperty = DependencyProperty.Register(
            nameof(APoints),
            typeof(ObservableCollection<ToneCurvePoint>),
            typeof(ToneCurvePropertyControl),
            new FrameworkPropertyMetadata(new ObservableCollection<ToneCurvePoint>())
        );

        internal ObservableCollection<ToneCurvePoint> APoints
        {
            get { return (ObservableCollection<ToneCurvePoint>)GetValue(APointsProperty); }
            set { SetValue(APointsProperty, value); }
        }

        internal ObservableCollection<ToneCurvePoint> BPoints
        {
            get { return (ObservableCollection<ToneCurvePoint>)GetValue(BPointsProperty); }
            set { SetValue(BPointsProperty, value); }
        }

        internal ObservableCollection<ToneCurvePoint> GPoints
        {
            get { return (ObservableCollection<ToneCurvePoint>)GetValue(GPointsProperty); }
            set { SetValue(GPointsProperty, value); }
        }

        internal ObservableCollection<ToneCurvePoint> RPoints
        {
            get { return (ObservableCollection<ToneCurvePoint>)GetValue(RPointsProperty); }
            set { SetValue(RPointsProperty, value); }
        }

        internal ObservableCollection<ToneCurvePoint> RgbPoints
        {
            get { return (ObservableCollection<ToneCurvePoint>)GetValue(RgbPointsProperty); }
            set { SetValue(RgbPointsProperty, value); }
        }

        bool IsChangingPoints { get; set; }

        bool IsUpdatingPoints { get; set; }

        public ToneCurvePropertyControl()
        {
            InitializeComponent();
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is IPropertyViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is IPropertyViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            IsChangingPoints = true;
            if (ViewModel?.CurrentTimeValue is ToneCurveParameters parameters)
            {
                RgbPoints = [..parameters.Rgb];
                RPoints = [..parameters.R];
                GPoints = [..parameters.G];
                BPoints = [..parameters.B];
                APoints = [..parameters.A];
            }
            IsChangingPoints = false;
        }

        private void Root_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel is INotifyPropertyChanged viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private void ToneCurvePointEditView_BeginEdit(object sender, RoutedEventArgs e)
        {
            ViewModel?.BeginEditCommand.Execute(null);
        }

        private void ToneCurvePointEditView_EndEdit(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            IsUpdatingPoints = true;
            viewModel.CurrentTimeRawValue = new ToneCurveParameters(RgbPoints, RPoints, GPoints, BPoints, APoints);
            IsUpdatingPoints = false;

            viewModel.EndEditCommand.Execute(null);
        }

        private void ToneCurvePointEditView_AbortEdit(object sender, RoutedEventArgs e)
        {
            ViewModel?.AbortEditCommand.Execute(null);
        }

        private void ToneCurvePointEditView_UpdatePoints(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                IsUpdatingPoints = true;
                viewModel.CurrentTimeRawValue = new ToneCurveParameters(RgbPoints, RPoints, GPoints, BPoints, APoints);
                IsUpdatingPoints = false;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var viewModel = ViewModel;
            if (IsUpdatingPoints || viewModel == null || viewModel.CurrentTimeRawValue is not ToneCurveParameters rawParameters)
            {
                return;
            }

            IsChangingPoints = true;

            RgbPoints = [..rawParameters.Rgb];
            RPoints = [..rawParameters.R];
            GPoints = [..rawParameters.G];
            BPoints = [..rawParameters.B];
            APoints = [..rawParameters.A];

            IsChangingPoints = false;
        }
    }
}
