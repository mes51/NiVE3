using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NiVE3.UI.Resources;
using NiVE3.View.Primitive;
using NiVE3.ViewModel;

namespace NiVE3.View.Part
{
    class PropertyCollectionView : StackableItemsCollectionView<PropertyViewModel>
    {
        public static readonly DependencyProperty IsAVSwitchColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsAVSwitchColumnVisible),
            typeof(bool),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty IsTagColumnVisibleProperty = DependencyProperty.Register(
            nameof(IsTagColumnVisible),
            typeof(bool),
            typeof(PropertyCollectionView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

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

        public bool IsTagColumnVisible
        {
            get { return (bool)GetValue(IsTagColumnVisibleProperty); }
            set { SetValue(IsTagColumnVisibleProperty, value); }
        }

        public bool IsAVSwitchColumnVisible
        {
            get { return (bool)GetValue(IsAVSwitchColumnVisibleProperty); }
            set { SetValue(IsAVSwitchColumnVisibleProperty, value); }
        }

        public PropertyCollectionView()
        {
            var templateSelector = new DataTemplateCollection();
            templateSelector.Templates.Add(new DataTemplate
            {
                DataType = typeof(PropertyViewModel),
                VisualTree = CreateControlFactory(typeof(PropertyView), PropertyView.ControlAreaWidthProperty, PropertyView.NameAreaWidthProperty, PropertyView.IndentLevelProperty, PropertyView.IsAVSwitchColumnVisibleProperty, PropertyView.IsTagColumnVisibleProperty)
            });
            templateSelector.Templates.Add(new DataTemplate
            {
                DataType = typeof(PropertyGroupViewModel),
                VisualTree = CreateControlFactory(typeof(PropertyGroupView), PropertyGroupView.ControlAreaWidthProperty, PropertyGroupView.NameAreaWidthProperty, PropertyGroupView.IndentLevelProperty, PropertyGroupView.IsAVSwitchColumnVisibleProperty, PropertyGroupView.IsTagColumnVisibleProperty)
            });

            ItemTemplateSelector = templateSelector;
        }

        /*
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

            if (element is PropertyView propertyView && item is PropertyViewModel viewModel)
            {
                propertyView.DataContext = viewModel;
                propertyView.PropertyControl = viewModel.CreateControl();

                BindingProperty(propertyView, PropertyView.ControlAreaWidthProperty, PropertyView.NameAreaWidthProperty, PropertyView.IndentLevelProperty, PropertyView.IsAVSwitchColumnVisibleProperty, PropertyView.IsTagColumnVisibleProperty);
            }
            else if (element is PropertyGroupView propertyGroupView && item is PropertyGroupViewModel groupViewModel)
            {
                propertyGroupView.DataContext = groupViewModel;

                BindingProperty(propertyGroupView, PropertyGroupView.ControlAreaWidthProperty, PropertyGroupView.NameAreaWidthProperty, PropertyGroupView.IndentLevelProperty, PropertyGroupView.IsAVSwitchColumnVisibleProperty, PropertyGroupView.IsTagColumnVisibleProperty);
            }
        }

        void BindingProperty(DependencyObject target, DependencyProperty controlAreaWidthProperty, DependencyProperty nameAreaWidthProperty, DependencyProperty indentLevelProperty, DependencyProperty isAVSwitchColumnVisibleProperty, DependencyProperty isTagColumnVisibleProperty)
        {
            var controlAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(ControlAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(target, controlAreaWidthProperty, controlAreaWidthBinding);

            var nameAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(NameAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(target, nameAreaWidthProperty, nameAreaWidthBinding);

            var indentLevelBinding = new Binding
            {
                Path = new PropertyPath(nameof(IndentLevel)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(target, indentLevelProperty, indentLevelBinding);

            var isAVSwitchColumnVisibleBinding = new Binding
            {
                Path = new PropertyPath(IsAVSwitchColumnVisibleProperty),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(target, isAVSwitchColumnVisibleProperty, isAVSwitchColumnVisibleBinding);

            var isTagColumnVisibleBinding = new Binding
            {
                Path = new PropertyPath(IsTagColumnVisibleProperty),
                Source = this,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(target, isTagColumnVisibleProperty, isTagColumnVisibleBinding);
        }
        */

        FrameworkElementFactory CreateControlFactory(Type uiType, DependencyProperty controlAreaWidthProperty, DependencyProperty nameAreaWidthProperty, DependencyProperty indentLevelProperty, DependencyProperty isAVSwitchColumnVisibleProperty, DependencyProperty isTagColumnVisibleProperty)
        {
            var factory = new FrameworkElementFactory(uiType);
            var controlAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(ControlAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(controlAreaWidthProperty, controlAreaWidthBinding);

            var nameAreaWidthBinding = new Binding
            {
                Path = new PropertyPath(nameof(NameAreaWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(nameAreaWidthProperty, nameAreaWidthBinding);

            var indentLevelBinding = new Binding
            {
                Path = new PropertyPath(nameof(IndentLevel)),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(indentLevelProperty, indentLevelBinding);

            var isAVSwitchColumnVisibleBinding = new Binding
            {
                Path = new PropertyPath(IsAVSwitchColumnVisibleProperty),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(isAVSwitchColumnVisibleProperty, isAVSwitchColumnVisibleBinding);

            var isTagColumnVisibleBinding = new Binding
            {
                Path = new PropertyPath(IsTagColumnVisibleProperty),
                Source = this,
                Mode = BindingMode.OneWay
            };
            factory.SetBinding(isTagColumnVisibleProperty, isTagColumnVisibleBinding);

            return factory;
        }
    }
}
