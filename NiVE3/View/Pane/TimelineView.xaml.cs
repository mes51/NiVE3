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
using NiVE3.ViewModel;
using NiVE3.Wpf.Behavior;

namespace NiVE3.View.Pane
{
    /// <summary>
    /// TimelineView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineView : UserControl
    {
        public TimelineView()
        {
            InitializeComponent();

            FocusManager.SetIsFocusScope(LayerCollectionView, true);
        }

        TimelineViewModel? ViewModel => DataContext as TimelineViewModel;

        private void TimeLocator_CurrentTimeChangeByUser(object sender, RoutedEventArgs e)
        {
            ViewModel?.ChangeCurrentTimeCommand?.Execute(null);
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
                viewModel.TimeBarRangeStart = Math.Clamp(viewModel.TimeBarRangeStart + viewModel.TimeBarRange * 0.05 * dir, 0.0, viewModel.Duration - viewModel.TimeBarRange);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) ||  Keyboard.IsKeyDown(Key.RightAlt))
            {
                if (e.Delta > 0)
                {
                    viewModel.TimeBarRange = Math.Max(viewModel.TimeBarRange * 0.5, TimeLocator.MinimumRange);
                }
                else
                {
                    viewModel.TimeBarRange = Math.Min(viewModel.TimeBarRange * 2.0, viewModel.Duration);
                }
                viewModel.TimeBarRangeStart = Math.Clamp(viewModel.CurrentTime - viewModel.TimeBarRange * 0.5, 0.0, viewModel.Duration - viewModel.TimeBarRange);

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
            viewModel.TimeBarRangeStart = Math.Clamp(viewModel.TimeBarRangeStart + viewModel.TimeBarRange * 0.05 * dir, 0.0, viewModel.Duration - viewModel.TimeBarRange);
        }
    }
}
