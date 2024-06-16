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
using ILGPU.IR;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Control;
using NiVE3.View.Resource;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    /// <summary>
    /// PropertyView.xaml の相互作用ロジック
    /// </summary>
    public partial class PropertyView : PropertyViewBase
    {
        const double KeyFrameSwitchWidth = 22.0;

        public static readonly GridLength KeyFrameSwitchGridLength = new GridLength(KeyFrameSwitchWidth);

        PropertyViewModel? ViewModel => DataContext as PropertyViewModel;

        static PropertyView()
        {
            BeforeNameSpaceWidthProperty.OverrideMetadata(typeof(PropertyView), new FrameworkPropertyMetadata(KeyFrameSwitchWidth, FrameworkPropertyMetadataOptions.Inherits));
        }

        public PropertyView()
        {
            InitializeComponent();
        }

        private void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                PropertyControlGrid.Children.Clear();
                PropertyControlGrid.Children.Add(viewModel.CreateControl());
            }
        }

        private void PropertyNameTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ParentCollection?.SelectItem(ParentContainer, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            (DataContext as IInternalPropertyViewModel)?.SelectItemCommand?.Execute(null);
            e.Handled = true;
        }

        private void KeyFrameCollectionView_KeyFrameMoveRequest(object sender, KeyFrameMoveEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.MoveTimeKeyFramesCommand.Execute(Tuple.Create(e.KeyFrames, e.NewTimes));
        }

        private void KeyFrameCollectionView_KeyFrameInterpolationTypeChangeRequest(object sender, ChangeKeyFrameInterpolationTypeEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.ChangeKeyFramesInterpolationTypeCommand.Execute(Tuple.Create(e.KeyFrames, e.InterpolationType));
        }
    }
}
