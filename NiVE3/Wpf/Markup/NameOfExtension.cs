using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace NiVE3.Wpf.Markup
{
    // see: https://stackoverflow.com/a/45760586
    [ContentProperty(nameof(Member))]
    class NameOfExtension : MarkupExtension
    {
        public Type? Type { get; set; }

        public string? Member { get; set; }

        public override object ProvideValue(IServiceProvider? serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            if (Type == null || string.IsNullOrEmpty(Member) || Member.Contains('.'))
            {
                throw new ArgumentException("Syntax for x:NameOf is Type={x:Type [className]} Member=[propertyName]");
            }

            var pinfo = Type.GetRuntimeProperties().FirstOrDefault(pi => pi.Name == Member);
            var finfo = Type.GetRuntimeFields().FirstOrDefault(fi => fi.Name == Member);
            if (pinfo == null && finfo == null)
            {
                throw new ArgumentException($"No property or field found for {Member} in {Type}");
            }

            return Member;
        }
    }
}
