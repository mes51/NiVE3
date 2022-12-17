using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using NiVE3.Extension;
using NiVE3.Mvvm;
using NiVE3.Config;

namespace NiVE3.View.Resource
{
    class AppearanceResourceDictionary : ResourceDictionary
    {
        public PropertyPublisher<double> Appearance { get; private set; } = new PropertyPublisher<double>(0.0);

        [BrushColorRange("#313131", "#FFFFFF")]
        public object? BackgroundFill { get; private set; }

        public AppearanceResourceDictionary()
        {
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<AppearanceChangeableAttribute>();
                if (attribute != null)
                {
                    var subscriber = Appearance.Subscribe(value => {
                        var color = attribute.GetValue(value);
                        this[property.Name] = color;
                    });
                    property.SetValue(this, subscriber);
                }
            }

            Appearance.ForceUpdateValue(ApplicationSetting.Setting.Appearance);
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        abstract class AppearanceChangeableAttribute : Attribute
        {
            public abstract object GetValue(double appearance);
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        sealed class BrushColorRangeAttribute : AppearanceChangeableAttribute
        {
            public Color DarkColor { get; set; }

            public Color LightColor { get; set; }

            public BrushColorRangeAttribute(string darkColorHex, string lightColorHex)
            {
                DarkColor = ColorExtensions.FromHex(darkColorHex);
                LightColor = ColorExtensions.FromHex(lightColorHex);
            }

            public override object GetValue(double appearance)
            {
                var brush = new SolidColorBrush(DarkColor.Interpolate(LightColor, appearance));
                brush.Freeze();
                return brush;
            }
        }
    }
}
