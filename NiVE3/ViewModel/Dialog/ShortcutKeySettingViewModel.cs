using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Effects;
using NiVE3.Config;
using NiVE3.Extension;
using NiVE3.UI.Command;
using NiVE3.Util;
using NiVE3.ValueObject;
using NiVE3.View.Resource;
using NiVE3.Wpf.Input;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace NiVE3.ViewModel.Dialog
{
    [UseReactiveProperty]
    partial class ShortcutKeySettingViewModel : BindableBase, IDialogAware
    {
        [GeneratedRegex("\\s+", RegexOptions.Compiled)]
        private static partial Regex GenerateFilterSeparatorRegex();

        static readonly ShortcutKeyName[] ShortcutKeyNames;

        static ShortcutKeySettingViewModel()
        {
            ShortcutKeyNames = [..ShortcutKeySetting.CategorizedShortcutKeys.SelectMany(kv => 
                kv.Value.Select(n => new ShortcutKeyName(
                    LanguageResourceDictionary.Dictionary.GetText($"{typeof(ShortcutKeyCategoryType).Name}_{kv.Key}"),
                    LanguageResourceDictionary.Dictionary.GetText($"ShortcutKeyName_{n}"),
                    ShortcutKeySetting.DependencyProperties[n]
                )
            ))];
        }

        public string Title => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.ShortcutKeySettingView_Title);

        [ReactiveProperty]
        public partial ObservableDictionary<ShortcutKeyName, InputGesture> ShortcutKeys { get; set; } = [];

        [ReactiveProperty]
        public partial ObservableDictionary<ShortcutKeyName, List<ShortcutKeyName>> DuplicatedKeys { get; set; } = [];

        [ReactiveProperty]
        public partial string FilterText { get; set; } = "";

        bool IsEdited { get; set; }

        public ICollectionView FilteredShortcutKeys { get; }

        public Dictionary<ShortcutKeyName, string> DisplayableDuplicateKeys => DuplicatedKeys.ToDictionary(kvp => kvp.Key, kvp => string.Join(", ", kvp.Value.Select(n => n.Name)));

        public ICommand OKCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand DeleteShortcutKeyCommand { get; }

        public ICommand ChangeShortcutKeyCommand { get; }

        public DialogCloseListener RequestClose { get; }

        public ShortcutKeySettingViewModel()
        {
            FilteredShortcutKeys = ShortcutKeyNames.CreateCollectionView(() => FilterText, FilterShortcutKeyName);
            FilteredShortcutKeys.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ShortcutKeyName.Category)));

            OKCommand = new RequerySuggestedCommand(() =>
            {
                foreach (var (name, key) in ShortcutKeys)
                {
                    ShortcutKeySetting.Setting.SetValue(name.Property, key);
                }
                ShortcutKeySetting.Setting.Save();

                RequestClose.Invoke(new DialogResult(ButtonResult.OK));
            }, () => IsEdited && DuplicatedKeys.Values.All(k => k.Count < 1));

            CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));

            DeleteShortcutKeyCommand = new DelegateCommand<ShortcutKeyName>(name =>
            {
                if (ShortcutKeys[name] is KeyGesture gesture && gesture.Key == Key.None)
                {
                    return;
                }

                ShortcutKeys[name] = new KeyGesture(Key.None);
                DuplicatedKeys[name].Clear();
                foreach (var duplicatedKeys in DuplicatedKeys.Values)
                {
                    if (duplicatedKeys.Contains(name))
                    {
                        duplicatedKeys.Remove(name);
                    }
                }

                IsEdited = true;
                RaisePropertyChanged(nameof(ShortcutKeys));
                RaisePropertyChanged(nameof(DisplayableDuplicateKeys));
            });

            ChangeShortcutKeyCommand = new DelegateCommand<Tuple<ShortcutKeyName, InputGesture>>(t =>
            {
                var (name, newGesture) = t;
                if ((ShortcutKeys[name] is KeyGesture oldKeyGesture && newGesture is KeyGesture newKeyGesture && oldKeyGesture.Key == newKeyGesture.Key && oldKeyGesture.Modifiers == newKeyGesture.Modifiers) ||
                    (ShortcutKeys[name] is SingleKeyGesture oldSingleKeyGesture && newGesture is SingleKeyGesture newSingleKeyGesture && oldSingleKeyGesture.Key == newSingleKeyGesture.Key && oldSingleKeyGesture.IsUseShift == newSingleKeyGesture.IsUseShift))
                {
                    return;
                }

                ShortcutKeys[name] = newGesture;
                foreach (var list in DuplicatedKeys.Values)
                {
                    list.Remove(name);
                }
                DuplicatedKeys[name].Clear();

                foreach (var (n, currentGesture) in ShortcutKeys)
                {
                    if (n == name || (currentGesture is KeyGesture k && k.Key == Key.None))
                    {
                        continue;
                    }

                    if (currentGesture.IsSameKeyGesture(newGesture))
                    {
                        DuplicatedKeys[name].Add(n);
                        DuplicatedKeys[n].Add(name);
                    }
                }

                IsEdited = true;
                RaisePropertyChanged(nameof(ShortcutKeys));
                RaisePropertyChanged(nameof(DisplayableDuplicateKeys));
            });

            PropertyChanged += ShortcutKeySettingViewModel_PropertyChanged;
        }

        static bool FilterShortcutKeyName(ShortcutKeyName shortcutKeyName, string filterKey)
        {
            if (string.IsNullOrEmpty(filterKey))
            {
                return true;
            }

            var keys = GenerateFilterSeparatorRegex().Split(filterKey);
            return keys.All(shortcutKeyName.Name.Contains) || keys.All(shortcutKeyName.Category.Contains);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            ShortcutKeys.Clear();
            DuplicatedKeys.Clear();

            foreach (var name in ShortcutKeyNames)
            {
                ShortcutKeys.Add(name, (InputGesture)ShortcutKeySetting.Setting.GetValue(name.Property));
                DuplicatedKeys.Add(name, []);
            }

            FilterText = "";
        }

        private void ShortcutKeySettingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterText))
            {
                FilteredShortcutKeys.Refresh();
            }
        }
    }
}
