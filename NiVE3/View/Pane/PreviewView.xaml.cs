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
using NiVE3.ViewModel;

namespace NiVE3.View.Pane
{
    /// <summary>
    /// PreviewView.xaml の相互作用ロジック
    /// </summary>
    public partial class PreviewView : UserControl
    {
        public PreviewView()
        {
            InitializeComponent();

            DataContextChanged += PreviewView_DataContextChanged;
        }

        bool IsMouseDown { get; set; }

        Point ClickPoint { get; set; }

        Point PrevPoint { get; set; }

        PreviewViewModel? ViewModel => DataContext as PreviewViewModel;

        bool NeedPositionReset { get; set; } = true;

        private void PreviewView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            NeedPositionReset = true;
        }

        private void PreviewCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null && !viewModel.IsFootage)
            {
                IsMouseDown = true;
                ClickPoint = e.GetPosition(PreviewCanvas);
                PrevPoint = new Point(Canvas.GetLeft(PreviewArea), Canvas.GetTop(PreviewArea));
                PreviewCanvas.CaptureMouse();
            }
        }

        private void PreviewCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseDown)
            {
                var newPoint = e.GetPosition(PreviewCanvas) - ClickPoint + PrevPoint;
                Canvas.SetLeft(PreviewArea, newPoint.X);
                Canvas.SetTop(PreviewArea, newPoint.Y);
            }
        }

        private void PreviewCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseDown)
            {
                var newPoint = e.GetPosition(PreviewCanvas) - ClickPoint + PrevPoint;
                Canvas.SetLeft(PreviewArea, newPoint.X);
                Canvas.SetTop(PreviewArea, newPoint.Y);
                PreviewCanvas.ReleaseMouseCapture();
                IsMouseDown = false;
            }
        }

        private void PreviewArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((ViewModel?.IsFootage ?? false) || NeedPositionReset)
            {
                Canvas.SetLeft(PreviewArea, (PreviewCanvas.ActualWidth - PreviewArea.ActualWidth) * 0.5);
                Canvas.SetTop(PreviewArea, (PreviewCanvas.ActualHeight - PreviewArea.ActualHeight) * 0.5);
                NeedPositionReset = false;
            }
        }

        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((ViewModel?.IsFootage ?? false) || NeedPositionReset)
            {
                Canvas.SetLeft(PreviewArea, (PreviewCanvas.ActualWidth - PreviewArea.ActualWidth) * 0.5);
                Canvas.SetTop(PreviewArea, (PreviewCanvas.ActualHeight - PreviewArea.ActualHeight) * 0.5);
                NeedPositionReset = false;
            }
            else
            {
                var move = ((Point)e.NewSize - (Point)e.PreviousSize) * 0.5;
                Canvas.SetLeft(PreviewArea, Canvas.GetLeft(PreviewArea) + move.X);
                Canvas.SetTop(PreviewArea, Canvas.GetTop(PreviewArea) + move.Y);
            }
        }
    }
}
