using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using NiVE3.View.Resource;

namespace NiVE3.View.Validation
{
    [ContentProperty(nameof(Names))]
    class RegisteredNamesValidationRule : ValidationRule
    {
        public RegisteredNames Names { get; set; } = new RegisteredNames();

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is not string str)
            {
                return new ValidationResult(false, LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ValidationRule_General_NotString));
            }

            if (Names.Contains(str))
            {
                return new ValidationResult(false, string.Format(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ValidationRule_RegisteredNames_AlreadyUsed), str));
            }

            return ValidationResult.ValidResult;
        }
    }

    class RegisteredNames : Freezable
    {
        public static readonly DependencyProperty NamesProperty = DependencyProperty.Register(
            nameof(Names),
            typeof(string[]),
            typeof(RegisteredNames),
            new PropertyMetadata(new string[0])
        );

        public string[] Names
        {
            get { return (string[])GetValue(NamesProperty); }
            set { SetValue(NamesProperty, value); }
        }

        public bool Contains(string str)
        {
            return Names.Contains(str);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new RegisteredNames();
        }
    }
}
