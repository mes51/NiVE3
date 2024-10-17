using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace NiVE3.Wpf.Input
{
    class SingleKeyGesture : InputGesture
    {
        public Key Key { get; }

        public bool IsUseShift { get; }

        static KeyConverter Converter { get; } = new KeyConverter();

        public SingleKeyGesture(Key key) : this(key, false) { }

        public SingleKeyGesture(Key key, bool isUseShift)
        {
            Key = key;
            IsUseShift = isUseShift;
        }

        public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
        {
            if (inputEventArgs is KeyEventArgs e)
            {
                if (e.OriginalSource is not TextBoxBase && !(e.OriginalSource.GetType().Namespace?.StartsWith(typeof(TextEditor).Namespace ?? "") ?? false))
                {
                    return (int)Key == (int)e.Key &&
                        !Keyboard.IsKeyDown(Key.LeftCtrl) &&
                        !Keyboard.IsKeyDown(Key.RightCtrl) &&
                        (IsUseShift == (Keyboard.IsKeyDown(Key.LeftShift) | Keyboard.IsKeyDown(Key.RightShift)));
                }
            }

            return false;
        }

        public string GetDisplayStringForCulture(CultureInfo cultureInfo)
        {
            return (IsUseShift ? "Shift + " : "") + Converter.ConvertTo(null, cultureInfo, Key, typeof(string)) as string ?? "";
        }
    }
}
