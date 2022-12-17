using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NiVE3.Config
{
    class ShortcutKeySetting : DependencyObject
    {
        public static ShortcutKeySetting Setting { get; }

        public static Dictionary<string, DependencyProperty> DependencyProperties { get; }

        public static readonly DependencyProperty OpenProjectGestureProperty = DependencyProperty.Register(
            nameof(OpenProjectGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.O, ModifierKeys.Control))
        );

        public static readonly DependencyProperty ExitProperty = DependencyProperty.Register(
            nameof(Exit),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.F4, ModifierKeys.Alt))
        );

        public InputGesture OpenProjectGesture
        {
            get { return (InputGesture)GetValue(OpenProjectGestureProperty); }
            set { SetValue(OpenProjectGestureProperty, value); }
        }

        public InputGesture Exit
        {
            get { return (InputGesture)GetValue(ExitProperty); }
            set { SetValue(ExitProperty, value); }
        }

        static ShortcutKeySetting()
        {
            Setting = new ShortcutKeySetting();

            DependencyProperties = typeof(ShortcutKeySetting).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => Tuple.Create(f.Name, (DependencyProperty)f.GetValue(null)!))
                .ToDictionary(t => t.Item1.Replace("Property", ""), t => t.Item2);

            Setting.Load();
        }

        private ShortcutKeySetting() { }

        public void Save()
        {
        }

        void Load()
        {

        }
    }
}
