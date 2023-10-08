using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NiVE3.View.Converter
{
    class ContainsCollectionConverter : Freezable, IValueConverter
    {
        public static readonly DependencyProperty CollectionProperty = DependencyProperty.Register(
            nameof(Collection),
            typeof(IEnumerable),
            typeof(ContainsCollectionConverter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public IEnumerable? Collection
        {
            get { return (IEnumerable)GetValue(CollectionProperty); }
            set { SetValue(CollectionProperty, value); }
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Collection?.Cast<object>()?.Any(v => v.Equals(value)) ?? false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new ContainsCollectionConverter();
        }
    }
}
