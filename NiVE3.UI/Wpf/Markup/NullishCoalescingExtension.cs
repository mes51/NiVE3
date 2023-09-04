using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace NiVE3.UI.Wpf.Markup
{
    class NullishCoalescingExtension : MarkupExtension
    {
        public BindingBase? Binding1 { get; set; }

        public BindingBase? Binding2 { get; set; }

        public object? Fallback { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new MultiBinding
            {
                Converter = new NullishCoalescingConverter(Fallback),
                Mode = BindingMode.OneWay
            };
            if (Binding1 != null)
            {
                binding.Bindings.Add(Binding1);
            }
            if (Binding2 != null)
            {
                binding.Bindings.Add(Binding2);
            }

            return binding.ProvideValue(serviceProvider);
        }
    }

    file class NullishCoalescingConverter : IMultiValueConverter
    {
        public object? Fallback { get; }

        public NullishCoalescingConverter(object? fallback)
        {
            Fallback = fallback;
        }

        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            return values.FirstOrDefault(v => v != null) ?? Fallback;
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
