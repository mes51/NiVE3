using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NiVE3.View.Resource;

namespace NiVE3.View.Validation
{
    class StringLengthValidationRule : ValidationRule
    {
        public int MinLength { get; set; } = 1;

        public int MaxLength { get; set; } = -1;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is not string str)
            {
                return new ValidationResult(false, LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ValidationRule_General_NotString));
            }

            switch ((MinLength, MaxLength))
            {
                case (int min, int max) when min < 0 && max < 0:
                    return ValidationResult.ValidResult;
                case (int min, int max) when min < 0 && max > -1:
                    if (str.Length <= max)
                    {
                        return ValidationResult.ValidResult;
                    }
                    else
                    {
                        return new ValidationResult(false, string.Format(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ValidationRule_StringLength_OverLength), max));
                    }
                case (int min, int max) when min > -1 && max < 0:
                    if (str.Length >= min)
                    {
                        return ValidationResult.ValidResult;
                    }
                    else
                    {
                        return new ValidationResult(false, string.Format(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ValidationRule_StringLength_LessLength), min));
                    }
                default:
                    if (str.Length >= MinLength && str.Length <= MaxLength)
                    {
                        return ValidationResult.ValidResult;
                    }
                    else
                    {
                        return new ValidationResult(false, string.Format(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ValidationRule_StringLength_OutOfRange), MinLength, MaxLength));
                    }
            }
        }
    }
}
