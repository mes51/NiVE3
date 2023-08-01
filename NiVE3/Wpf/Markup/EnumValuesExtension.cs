using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace NiVE3.Wpf.Markup
{
    class EnumValuesExtension : MarkupExtension
    {
        public Type? EnumType { get; set; }

        public EnumValuesExtension() { }

        public EnumValuesExtension(Type? enumType)
        {
            EnumType = enumType;
        }

        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (EnumType == null || !EnumType.IsEnum)
            {
                return null;
            }

            return Enum.GetValues(EnumType);
        }
    }
}
