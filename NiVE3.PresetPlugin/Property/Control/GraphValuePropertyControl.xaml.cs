using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using NiVE3.PresetPlugin.Property.Properties;

namespace NiVE3.PresetPlugin.Property.Control
{
    /// <summary>
    /// GraphValuePropertyControl.xaml の相互作用ロジック
    /// </summary>
    public partial class GraphValuePropertyControl : PropertyControlBase
    {

        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            nameof(Values),
            typeof(ObservableCollection<float>),
            typeof(GraphValuePropertyControl),
            new FrameworkPropertyMetadata(new ObservableCollection<float>())
        );

        public static readonly DependencyProperty IsSuppressNotifyUpdateValuesProperty = DependencyProperty.Register(
            nameof(IsSuppressNotifyUpdateValues),
            typeof(bool),
            typeof(GraphValuePropertyControl),
            new FrameworkPropertyMetadata(false)
        );

        public ObservableCollection<float> Values
        {
            get { return (ObservableCollection<float>)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        public bool IsSuppressNotifyUpdateValues
        {
            get { return (bool)GetValue(IsSuppressNotifyUpdateValuesProperty); }
            set { SetValue(IsSuppressNotifyUpdateValuesProperty, value); }
        }

        bool IsUpdatingPoints { get; set; }

        public GraphValuePropertyControl()
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

            if (ViewModel?.CurrentTimeRawValue is GraphValueParameter rawParameter)
            {
                Values = new ObservableCollection<float>(rawParameter.Values);
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
            if (IsUpdatingPoints || viewModel == null || viewModel.CurrentTimeRawValue is not GraphValueParameter rawParameter)
            {
                return;
            }

            Values = new ObservableCollection<float>(rawParameter.Values);
        }

        private void GraphValueEditView_BeginEdit(object sender, RoutedEventArgs e)
        {
            if (!IsSuppressNotifyUpdateValues)
            {
                ViewModel?.BeginEditCommand?.Execute(null);
            }
        }

        private void GraphValueEditView_EndEdit(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            if (IsSuppressNotifyUpdateValues)
            {
                var newValues = Values.ToArray();
                ViewModel?.BeginEditCommand?.Execute(null);

                IsUpdatingPoints = true;
                viewModel.CurrentTimeRawValue = new GraphValueParameter(newValues);
                IsUpdatingPoints = false;
            }
            else
            {
                IsUpdatingPoints = true;
                viewModel.CurrentTimeRawValue = new GraphValueParameter([.. Values]);
                IsUpdatingPoints = false;
            }

            ViewModel?.EndEditCommand?.Execute(null);
        }

        private void GraphValueEditView_AbortEdit(object sender, RoutedEventArgs e)
        {
            if (!IsSuppressNotifyUpdateValues)
            {
                ViewModel?.AbortEditCommand?.Execute(null);
            }
        }

        private void GraphValueEditView_UpdateValues(object sender, RoutedEventArgs e)
        {
            if (!IsSuppressNotifyUpdateValues)
            {
                var viewModel = ViewModel;
                if (viewModel != null)
                {
                    IsUpdatingPoints = true;
                    viewModel.CurrentTimeRawValue = new GraphValueParameter([..Values]);
                    IsUpdatingPoints = false;
                }
            }
        }

        private void ApplyPresetLinearUpButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            ViewModel?.BeginEditCommand?.Execute(null);

            IsUpdatingPoints = true;
            viewModel.CurrentTimeRawValue = GraphValueParameter.LinearUp;
            IsUpdatingPoints = false;

            ViewModel?.EndEditCommand?.Execute(null);
        }

        private void ApplyPresetLinearDownButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            ViewModel?.BeginEditCommand?.Execute(null);

            IsUpdatingPoints = true;
            viewModel.CurrentTimeRawValue = GraphValueParameter.LinearDown;
            IsUpdatingPoints = false;

            ViewModel?.EndEditCommand?.Execute(null);
        }

        private void ApplyPresetTriangleButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            ViewModel?.BeginEditCommand?.Execute(null);

            IsUpdatingPoints = true;
            viewModel.CurrentTimeRawValue = GraphValueParameter.Triangle;
            IsUpdatingPoints = false;

            ViewModel?.EndEditCommand?.Execute(null);
        }

        private void ApplyPresetCurveButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            ViewModel?.BeginEditCommand?.Execute(null);

            IsUpdatingPoints = true;
            viewModel.CurrentTimeRawValue = GraphValueParameter.Curve;
            IsUpdatingPoints = false;

            ViewModel?.EndEditCommand?.Execute(null);
        }
    }
}
