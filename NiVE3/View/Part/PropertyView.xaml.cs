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

        public static readonly DependencyProperty HasExpressionProperty = DependencyProperty.Register(
            nameof(HasExpression),
            typeof(bool),
            typeof(PropertyView),
            new FrameworkPropertyMetadata(false)
        );

        public bool HasExpression
        {
            get { return (bool)GetValue(HasExpressionProperty); }
            set { SetValue(HasExpressionProperty, value); }
        }

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
            if (e.OldValue is PropertyViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= PropertyViewModel_PropertyChanged;
            }
            if (e.NewValue is PropertyViewModel newViewModel)
            {
                PropertyControlGrid.Children.Clear();
                PropertyControlGrid.Children.Add(newViewModel.CreateControl());

                HasExpression = !string.IsNullOrEmpty(newViewModel.ExpressionCode);
                BeforeNameSpaceWidth = KeyFrameSwitchWidth + (HasExpression ? UIParameters.ArrowWidth : 0.0);

                newViewModel.PropertyChanged += PropertyViewModel_PropertyChanged;
            }
        }

        private void PropertyNameTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
            {
                return;
            }

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

        private void PropertyViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PropertyViewModel.ExpressionCode))
            {
                HasExpression = !string.IsNullOrEmpty(ViewModel?.ExpressionCode);
                BeforeNameSpaceWidth = KeyFrameSwitchWidth + (HasExpression ? UIParameters.ArrowWidth : 0.0);
            }
        }
    }
}
