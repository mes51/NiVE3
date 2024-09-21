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

        static readonly Tuple<string, string, string>[] AllCommands;

        private bool isOpen;
        public bool IsOpen
        {
            get { return isOpen; }
            set { SetProperty(ref isOpen, value); }
        }

        private ObservableCollection<Tuple<string, string, string, bool>> commands = [];
        public ObservableCollection<Tuple<string, string, string, bool>> Commands
        {
            get { return commands; }
            set { SetProperty(ref commands, value); }
        }

        private Tuple<string, string, string, bool>? selectedCommand;
        public Tuple<string, string, string, bool>? SelectedCommand
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
            AllCommands = [..ShortcutKeySetting.CategorizedShortcutKeys.SelectMany(kv =>
            {
                var category = LanguageResourceDictionary.Dictionary.GetText($"{typeof(ShortcutKeyCategoryType).Name}_{kv.Key}");
                return kv.Value
                    .Where(n => n != nameof(ShortcutKeySetting.OpenCommandPaletteGesture)) // NOTE: コマンドパレットからコマンドパレットを開いても意味が無いので除去
                    .Select(n => Tuple.Create(category, LanguageResourceDictionary.Dictionary.GetText($"ShortcutKeyName_{n}"), n));
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
                if (WindowGestureBehavior.GestureCommand.CanExecute(SelectedCommand.Item3, inputElement))
                {
                    WindowGestureBehavior.GestureCommand.Execute(SelectedCommand.Item3, inputElement);
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
            foreach (var (category, name, gesture) in AllCommands)
            {
                Commands.Add(Tuple.Create(category, name, gesture, WindowGestureBehavior.GestureCommand.CanExecute(gesture, inputElement)));
            }

            SelectedCommand = Commands.FirstOrDefault();
            IsOpen = true;
        }

        static bool FilterCommand(Tuple<string, string, string, bool> command, string filterKey)
        {
            if (string.IsNullOrEmpty(filterKey))
            {
                return true;
            }

            var keys = GenerateFilterSeparatorRegex().Split(filterKey);
            return keys.All(command.Item1.Contains) || keys.All(command.Item2.Contains);
        }

        private void CommandPaletteViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterText))
            {
                FilteredCommands.Refresh();
                if (SelectedCommand == null || !FilterCommand(SelectedCommand, FilterText))
                {
                    SelectedCommand = FilteredCommands.Cast<Tuple<string, string, string, bool>>().FirstOrDefault();
                }
            }
        }
    }
}
