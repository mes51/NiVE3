using System;
using System.Collections.Generic;
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
using NiVE3.Plugin.ValueObject;
using NiVE3.ViewModel;
using NiVE3.Wpf.Behavior;

namespace NiVE3.View.Pane
{
    /// <summary>
    /// TimelineView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineView : UserControl
    {
        // NOTE: なぜかTypeConverterをSourceTypeにつけてもNREが出てXAML上でリソースとして定義出来ないため、定数として定義する
        public static readonly SourceType CompositionDisplayableSourceType = SourceType.Image | SourceType.Video;

        public TimelineView()
        {
            InitializeComponent();
        }

        TimelineViewModel? ViewModel => DataContext as TimelineViewModel;

        private void TimeLocator_CurrentTimeChangeByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeCurrentTimeCommand?.Execute(null);
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is TimelineViewModel oldViewModel)
            {
                oldViewModel.FocusRequest -= ViewModel_FocusRequest;
            }
            if (e.NewValue is TimelineViewModel newViewModel)
            {
                newViewModel.FocusRequest += ViewModel_FocusRequest;
            }
        }

        private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            var dir = -Math.Sign(e.Delta);
            if (Keyboard.IsKeyDown(Key.LeftShift) ||  Keyboard.IsKeyDown(Key.RightShift))
            {
                viewModel.TimeBarRangeStart = Time.Clamp(viewModel.TimeBarRangeStart + viewModel.TimeBarRange * 0.05 * dir, Time.Zero, viewModel.Duration - viewModel.TimeBarRange);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) ||  Keyboard.IsKeyDown(Key.RightAlt))
            {
                if (e.Delta > 0)
                {
                    viewModel.TimeBarRange = Time.Max(viewModel.TimeBarRange * 0.5, TimeLocator.MinimumRange);
                }
                else
                {
                    viewModel.TimeBarRange = Time.Min(viewModel.TimeBarRange * 2.0, viewModel.Duration);
                }
                viewModel.TimeBarRangeStart = Time.Clamp(viewModel.CurrentTime - viewModel.TimeBarRange * 0.5, Time.Zero, viewModel.Duration - viewModel.TimeBarRange);

                e.Handled = true;
            }
        }

        private void LayerCollectionView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LayerCollectionView.Focus();
            e.Handled = true;
        }

        private void TiltWheelBehavior_MouseTiltWheel(object sender, MouseTiltWheelEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            var dir = Math.Sign(e.Delta);
            viewModel.TimeBarRangeStart = Time.Clamp(viewModel.TimeBarRangeStart + viewModel.TimeBarRange * 0.05 * dir, Time.Zero, viewModel.Duration - viewModel.TimeBarRange);
        }

        private void ViewModel_FocusRequest(object? sender, EventArgs e)
        {
            Focus();
        }
    }
}
