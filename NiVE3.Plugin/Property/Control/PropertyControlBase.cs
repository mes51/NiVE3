using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NiVE3.Plugin.Interfaces;

namespace NiVE3.Plugin.Property.Control
{
    public class PropertyControlBase : UserControl
    {
        public static readonly DependencyProperty ViewStateProperty = DependencyProperty.Register(
            nameof(ViewState),
            typeof(PropertyViewState),
            typeof(PropertyControlBase),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public PropertyViewState? ViewState
        {
            get { return (PropertyViewState)GetValue(ViewStateProperty); }
            set { SetValue(ViewStateProperty, value); }
        }

        protected IPropertyViewModel? ViewModel => DataContext as IPropertyViewModel;
    }
}
