using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.Extension;
using NiVE3.UI.Command;
using NiVE3.View.Command;
using NiVE3.View.Resource;
using NiVE3.ViewModel.CommandOnly;
using NiVE3.Wpf.Behavior;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    partial class CommandPaletteViewModel : BindableBase
    {
        [GeneratedRegex("\\s+", RegexOptions.Compiled)]
        private static partial Regex GenerateFilterSeparatorRegex();

        public static readonly string RegionName = "CommandPalette";

        static readonly Tuple<string, string[], string>[] AllShortcutCommands;

        private bool isOpen;
        public bool IsOpen
        {
            get { return isOpen; }
            set { SetProperty(ref isOpen, value); }
        }

        private ObservableCollection<Tuple<string, string[], ICommand, object?, bool>> commands = [];
        public ObservableCollection<Tuple<string, string[], ICommand, object?, bool>> Commands
        {
            get { return commands; }
            set { SetProperty(ref commands, value); }
        }

        private Tuple<string, string[], ICommand, object?, bool>? selectedCommand;
        public Tuple<string, string[], ICommand, object?, bool>? SelectedCommand
        {
            get { return selectedCommand; }
            set { SetProperty(ref selectedCommand, value); }
        }

        private string filterText = "";
        public string FilterText
        {
            get { return filterText; }
            set { SetProperty(ref filterText, value); }
        }

        public ICollectionView FilteredCommands { get; }

        public ICommand ExecuteCommand { get; }

        static CommandPaletteViewModel()
        {
            AllShortcutCommands = [..ShortcutKeySetting.CategorizedShortcutKeys.SelectMany(kv =>
            {
                var categoryKey = $"{typeof(ShortcutKeyCategoryType).Name}_{kv.Key}";
                var category = LanguageResourceDictionary.Dictionary.GetText(categoryKey);
                var jpCategory = LanguageResourceDictionary.JPDictionary.GetText(categoryKey);
                return kv.Value
                    .Where(n => n != nameof(ShortcutKeySetting.OpenCommandPaletteGesture)) // NOTE: コマンドパレットからコマンドパレットを開いても意味が無いので除去
                    .Select(n =>
                    {
                        var nameKey = $"ShortcutKeyName_{n}";
                        var name = LanguageResourceDictionary.Dictionary.GetText(nameKey);
                        var jpName = LanguageResourceDictionary.JPDictionary.GetText(nameKey);
                        return Tuple.Create($"{category} > {name}", new string[] { category, name, jpCategory, jpName }, n);
                    });
            })];
        }

        public CommandPaletteViewModel(IEventAggregator eventAggregator)
        {
            FilteredCommands = Commands.CreateCollectionView(() => FilterText, FilterCommand);

            ExecuteCommand = new RequerySuggestedCommand(() =>
            {
                if (SelectedCommand == null)
                {
                    return;
                }

                var inputElement = Keyboard.FocusedElement;
                if (SelectedCommand.Item3 is RoutedCommand routedCommand)
                {
                    if (routedCommand.CanExecute(SelectedCommand.Item4, inputElement))
                    {
                        routedCommand.Execute(SelectedCommand.Item4, inputElement);
                    }
                }
                else
                {
                    if (SelectedCommand.Item3.CanExecute(SelectedCommand.Item4))
                    {
                        SelectedCommand.Item3.Execute(SelectedCommand.Item4);
                    }
                }
            }, () => SelectedCommand != null);

            eventAggregator.GetEvent<OpenCommandPaletteEvent>().Subscribe(OpenPalette);

            PropertyChanged += CommandPaletteViewModel_PropertyChanged;
        }

        void OpenPalette()
        {
            FilterText = "";

            Commands.Clear();
            var inputElement = Keyboard.FocusedElement;
            var gestureCommand = WindowGestureBehavior.GestureCommand;
            foreach (var (displayName, searchTexts, gesture) in AllShortcutCommands)
            {
                Commands.Add(Tuple.Create<string, string[], ICommand, object?, bool>(displayName, searchTexts, gestureCommand, gesture, gestureCommand.CanExecute(gesture, inputElement)));
            }

            SelectedCommand = Commands.FirstOrDefault();
            IsOpen = true;
        }

        static bool FilterCommand(Tuple<string, string[], ICommand, object?, bool> command, string filterKey)
        {
            if (string.IsNullOrEmpty(filterKey))
            {
                return true;
            }

            var keys = GenerateFilterSeparatorRegex().Split(filterKey);
            return keys.All(k => command.Item2.Any(s => s.Contains(k)));
        }

        private void CommandPaletteViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterText))
            {
                FilteredCommands.Refresh();
                if (SelectedCommand == null || !FilterCommand(SelectedCommand, FilterText))
                {
                    SelectedCommand = FilteredCommands.Cast<Tuple<string, string[], ICommand, object?, bool>>().FirstOrDefault();
                }
            }
        }
    }
}
