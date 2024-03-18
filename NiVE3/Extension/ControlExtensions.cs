using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using NiVE3.View.Primitive;

namespace NiVE3.Extension
{
    static class ControlExtensions
    {
        public static FormattedText CreateFormattedText(this Control control, string text, Brush fill)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                control.FlowDirection,
                control.GetTypeface(),
                control.FontSize,
                fill,
                VisualTreeHelper.GetDpi(control).PixelsPerDip
            );
        }

        public static FormattedText CreateFormattedText(this TextRenderableElement textRenderableElement, string text, Brush fill)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                textRenderableElement.FlowDirection,
                textRenderableElement.GetTypeface(),
                textRenderableElement.FontSize,
                fill,
                VisualTreeHelper.GetDpi(textRenderableElement).PixelsPerDip
            );
        }

        public static Typeface GetTypeface(this Control control)
        {
            return new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch);
        }

        public static Typeface GetTypeface(this TextRenderableElement textRenderableElement)
        {
            return new Typeface(textRenderableElement.FontFamily, textRenderableElement.FontStyle, textRenderableElement.FontWeight, textRenderableElement.FontStretch);
        }
    }
}
