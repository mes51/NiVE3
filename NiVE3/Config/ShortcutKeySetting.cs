using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Data.Config;
using NiVE3.Extension;
using NiVE3.Util;
using NiVE3.Wpf.Input;

namespace NiVE3.Config
{
    class ShortcutKeySetting : DependencyObject
    {
        static readonly string FilePath = Path.Combine(Paths.ConfigDirectory, "shortcut_keys.json");

        public static ShortcutKeySetting Setting { get; }

        public static Dictionary<string, DependencyProperty> DependencyProperties { get; }

        public static readonly DependencyProperty OpenProjectGestureProperty = DependencyProperty.Register(
            nameof(OpenProjectGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.O, ModifierKeys.Control))
        );

        public static readonly DependencyProperty SaveProjectGestureProperty = DependencyProperty.Register(
            nameof(SaveProjectGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.S, ModifierKeys.Control))
        );

        public static readonly DependencyProperty ExitGestureProperty = DependencyProperty.Register(
            nameof(ExitGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.F4, ModifierKeys.Alt))
        );

        public static readonly DependencyProperty OpenFileGestureProperty = DependencyProperty.Register(
            nameof(OpenFileGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift))
        );

        public static readonly DependencyProperty DeleteItemGestureProperty = DependencyProperty.Register(
            nameof(DeleteItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.Delete))
        );

        public static readonly DependencyProperty CutItemGestureProperty = DependencyProperty.Register(
            nameof(CutItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.X, ModifierKeys.Control))
        );

        public static readonly DependencyProperty CopyItemGestureProperty = DependencyProperty.Register(
            nameof(CopyItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.C, ModifierKeys.Control))
        );

        public static readonly DependencyProperty PasteItemGestureProperty = DependencyProperty.Register(
            nameof(PasteItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.V, ModifierKeys.Control))
        );

        public static readonly DependencyProperty DuplicateItemGestureProperty = DependencyProperty.Register(
            nameof(DuplicateItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.D, ModifierKeys.Control))
        );

        public static readonly DependencyProperty SplitLayerGestureProperty = DependencyProperty.Register(
            nameof(SplitLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.D, ModifierKeys.Shift | ModifierKeys.Control))
        );

        public static readonly DependencyProperty NewCompositionGestureProperty = DependencyProperty.Register(
            nameof(NewCompositionGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.N, ModifierKeys.Control))
        );

        public static readonly DependencyProperty AddSolidGestureProperty = DependencyProperty.Register(
            nameof(AddSolidGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Y, ModifierKeys.Control))
        );

        public static readonly DependencyProperty AddFootageFolderGestureProperty = DependencyProperty.Register(
            nameof(AddFootageFolderGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.N, ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt))
        );

        public static readonly DependencyProperty UndoGestureProperty = DependencyProperty.Register(
            nameof(UndoGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Z, ModifierKeys.Control))
        );

        public static readonly DependencyProperty RedoGestureProperty = DependencyProperty.Register(
            nameof(RedoGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Z, ModifierKeys.Shift | ModifierKeys.Control))
        );

        public static readonly DependencyProperty BeginEditNameGestureProperty = DependencyProperty.Register(
            nameof(BeginEditNameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.Enter))
        );

        public static readonly DependencyProperty OpenRenderSettingGestureProperty = DependencyProperty.Register(
            nameof(OpenRenderSettingGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.M, ModifierKeys.Control))
        );

        public InputGesture OpenRenderSettingGesture
        {
            get { return (InputGesture)GetValue(OpenRenderSettingGestureProperty); }
            set { SetValue(OpenRenderSettingGestureProperty, value); }
        }

        public InputGesture BeginEditNameGesture
        {
            get { return (InputGesture)GetValue(BeginEditNameGestureProperty); }
            set { SetValue(BeginEditNameGestureProperty, value); }
        }

        public InputGesture RedoGesture
        {
            get { return (InputGesture)GetValue(RedoGestureProperty); }
            set { SetValue(RedoGestureProperty, value); }
        }

        public InputGesture UndoGesture
        {
            get { return (InputGesture)GetValue(UndoGestureProperty); }
            set { SetValue(UndoGestureProperty, value); }
        }

        public InputGesture AddFootageFolderGesture
        {
            get { return (InputGesture)GetValue(AddFootageFolderGestureProperty); }
            set { SetValue(AddFootageFolderGestureProperty, value); }
        }

        public InputGesture AddSolidGesture
        {
            get { return (InputGesture)GetValue(AddSolidGestureProperty); }
            set { SetValue(AddSolidGestureProperty, value); }
        }

        public InputGesture SaveProjectGesture
        {
            get { return (InputGesture)GetValue(SaveProjectGestureProperty); }
            set { SetValue(SaveProjectGestureProperty, value); }
        }

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

        public InputGesture OpenFileGesture
        {
            get { return (InputGesture)GetValue(OpenFileGestureProperty); }
            set { SetValue(OpenFileGestureProperty, value); }
        }

        public InputGesture SplitLayerGesture
        {
            get { return (InputGesture)GetValue(SplitLayerGestureProperty); }
            set { SetValue(SplitLayerGestureProperty, value); }
        }

        public InputGesture DuplicateItemGesture
        {
            get { return (InputGesture)GetValue(DuplicateItemGestureProperty); }
            set { SetValue(DuplicateItemGestureProperty, value); }
        }

        public InputGesture PasteItemGesture
        {
            get { return (InputGesture)GetValue(PasteItemGestureProperty); }
            set { SetValue(PasteItemGestureProperty, value); }
        }

        public InputGesture CopyItemGesture
        {
            get { return (InputGesture)GetValue(CopyItemGestureProperty); }
            set { SetValue(CopyItemGestureProperty, value); }
        }

        public InputGesture CutItemGesture
        {
            get { return (InputGesture)GetValue(CutItemGestureProperty); }
            set { SetValue(CutItemGestureProperty, value); }
        }

        public InputGesture DeleteItemGesture
        {
            get { return (InputGesture)GetValue(DeleteItemGestureProperty); }
            set { SetValue(DeleteItemGestureProperty, value); }
        }

        public InputGesture NewCompositionGesture
        {
            get { return (InputGesture)GetValue(NewCompositionGestureProperty); }
            set { SetValue(NewCompositionGestureProperty, value); }
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
                return Setting.GetValue(dp) switch
                {
                    KeyGesture keyGesture => new ShortcutKeyData
                    {
                        Name = dp.Name,
                        Type = nameof(KeyGesture),
                        GestureKey = keyGesture.Key,
                        Modifier = keyGesture.Modifiers
                    },
                    SingleKeyGesture singleKeyGesture => new ShortcutKeyData
                    {
                        Name = dp.Name,
                        Type = nameof(SingleKeyGesture),
                        GestureKey = singleKeyGesture.Key,
                        Modifier = singleKeyGesture.IsUseShift ? ModifierKeys.Shift : ModifierKeys.None
                    },
                    _ => new ShortcutKeyData
                    {
                        Name = dp.Name,
                        Type = nameof(KeyGesture),
                        GestureKey = Key.None,
                        Modifier = ModifierKeys.None
                    },
                };
            }).ToArray();

            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(FilePath, json);
        }

        void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            try
            {
                var shortcutKeyData = JsonSerializer.Deserialize<ShortcutKeyData[]>(File.ReadAllBytes(FilePath)) ?? [];
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
