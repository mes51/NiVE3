using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Data.Config;
using NiVE3.Extension;
using NiVE3.Wpf.Input;
using SpanJson;
using SpanJson.Resolvers;

namespace NiVE3.Config
{
    class ShortcutKeySetting : DependencyObject
    {
        static readonly string FilePath = Path.Combine(SettingPath.ConfigDirectory, "shortcut_keys.json");

        public static ShortcutKeySetting Setting { get; }

        public static Dictionary<string, DependencyProperty> DependencyProperties { get; }

        public static readonly DependencyProperty OpenProjectGestureProperty = DependencyProperty.Register(
            nameof(OpenProjectGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.O, ModifierKeys.Control))
        );

        public static readonly DependencyProperty ExitGestureProperty = DependencyProperty.Register(
            nameof(ExitGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.F4, ModifierKeys.Alt))
        );

        public InputGesture OpenProjectGesture
        {
            get { return (InputGesture)GetValue(OpenProjectGestureProperty); }
            set { SetValue(OpenProjectGestureProperty, value); }
        }

        public InputGesture ExitGesture
        {
            get { return (InputGesture)GetValue(ExitGestureProperty); }
            set { SetValue(ExitGestureProperty, value); }
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
            var data = DependencyProperties.Values.Select(dp =>
            {
                switch (Setting.GetValue(dp))
                {
                    case KeyGesture keyGesture:
                        return new ShortcutKeyData
                        {
                            Name = dp.Name,
                            Type = nameof(KeyGesture),
                            GestureKey = keyGesture.Key,
                            Modifier = keyGesture.Modifiers
                        };
                    case SingleKeyGesture singleKeyGesture:
                        return new ShortcutKeyData
                        {
                            Name = dp.Name,
                            Type = nameof(SingleKeyGesture),
                            GestureKey = singleKeyGesture.Key,
                            Modifier = singleKeyGesture.IsUseShift ? ModifierKeys.Shift : ModifierKeys.None
                        };
                    default:
                        return new ShortcutKeyData
                        {
                            Name = dp.Name,
                            Type = nameof(KeyGesture),
                            GestureKey = Key.None,
                            Modifier = ModifierKeys.None
                        };
                }
            }).ToArray();

            var json = JsonSerializer.Generic.Utf8.Serialize<ShortcutKeyData[], IncludeNullsCamelCaseResolver<byte>>(data);
            using var fs = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.Write(json);
        }

        void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            try
            {
                var shortcutKeyData = JsonSerializer.Generic.Utf8.Deserialize<ShortcutKeyData[], IncludeNullsCamelCaseResolver<byte>>(File.ReadAllBytes(FilePath)) ?? Array.Empty<ShortcutKeyData>();
                foreach (var data in shortcutKeyData)
                {
                    data.CorrectData();
                    if (data.Name == null || !DependencyProperties.ContainsKey(data.Name))
                    {
                        continue;
                    }

                    switch (data.Type)
                    {
                        case nameof(KeyGesture):
                            SetValue(DependencyProperties[data.Name], new KeyGesture(data.GestureKey, data.Modifier));
                            break;
                        case nameof(SingleKeyGesture):
                            SetValue(DependencyProperties[data.Name], new SingleKeyGesture(data.GestureKey, data.Modifier == ModifierKeys.Shift));
                            break;
                    }
                }

                var keys = DependencyProperties.Keys.ToArray();
                var values = DependencyProperties.Values.Select(GetValue).OfType<InputGesture>().ToArray();
                for (var i = 0; i < keys.Length; i++)
                {
                    if (values[i] is KeyGesture ka && ka.Key == Key.None)
                    {
                        continue;
                    }

                    for (var n = i + 1; n < keys.Length; n++)
                    {
                        if (values[n] is KeyGesture kb && kb.Key == Key.None)
                        {
                            continue;
                        }

                        if (values[i].IsSameKeyGesture(values[n]))
                        {
                            values[n] = new KeyGesture(Key.None);
                            SetValue(DependencyProperties[keys[n]], values[n]);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
