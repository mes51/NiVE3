using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class PropertyCollectionView : StackableItemsCollectionView<PropertyViewModel>
    {
        public static readonly DependencyProperty NameAreaWidthProperty = DependencyProperty.Register(
            nameof(NameAreaWidth),
            typeof(double),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public double NameAreaWidth
        {
            get { return (double)GetValue(NameAreaWidthProperty); }
            set { SetValue(NameAreaWidthProperty, value); }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new PropertyView();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PropertyView;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is not PropertyView propertyView || item is not PropertyViewModel viewModel)
            {
                return;
            }

            propertyView.PropertyControl = viewModel.CreateControl();

            var controlAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(ControlAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(propertyView, PropertyView.ControlAreaWidthProperty, controlAreaWidthBinding);

            propertyView.DataContext = viewModel;
            var nameAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(NameAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(propertyView, PropertyView.NameAreaWidthProperty, nameAreaWidthBinding);

            var indentLevelBinding = new Binding
            {
                Path = new PropertyPath(nameof(IndentLevel)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(propertyView, PropertyView.IndentLevelProperty, indentLevelBinding);
        }
    }
}
