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
using NiVE3.Model.UI;
using NiVE3.View.Resource;
using NiVE3.ViewModel.CommandOnly;
using NiVE3.Wpf.Behavior;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace NiVE3.ViewModel
{
    [UseReactiveProperty]
    partial class CommandPaletteViewModel : BindableBase
    {
        [GeneratedRegex("\\s+", RegexOptions.Compiled)]
        private static partial Regex GenerateFilterSeparatorRegex();

        public static readonly string RegionName = "CommandPalette";

        static readonly Tuple<string, string[], string>[] AllShortcutCommands;

        [ReactiveProperty]
        public partial bool IsOpen { get; set; }

        [ReactiveProperty]
        public partial ObservableCollection<Tuple<string, string[], ICommand, object?, bool>> Commands { get; set; } = [];

        [ReactiveProperty]
        public partial Tuple<string, string[], ICommand, object?, bool>? SelectedCommand { get; set; }

        [ReactiveProperty]
        public partial string FilterText { get; set; } = "";

        public ICollectionView FilteredCommands { get; }

        public ICommand ExecuteCommand { get; }

        ViewStateModel ViewState { get; }

        EffectListStateModel EffectListStateModel { get; }

        EventHubModel EventHubModel { get; }

        Tuple<string, string[], ICommand, object?>[] EffectCommands { get; }

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

        public CommandPaletteViewModel(ViewStateModel viewState, EffectListStateModel effectListStateModel, EventHubModel eventHubModel, IEventAggregator eventAggregator)
        {
            ViewState = viewState;
            EffectListStateModel = effectListStateModel;
            EventHubModel = eventHubModel;

            var effectCategory = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.CommandPalettePopup_DisplayName_EffectCategory);
            var jpEffectCategory = LanguageResourceDictionary.JPDictionary.GetText(LanguageResourceDictionary.CommandPalettePopup_DisplayName_EffectCategory);
            var effectCommand = new DelegateCommand<EffectItem>(effectItem =>
            {
                EventHubModel.NotifyAddEffectToSelectedLayers(ViewState.CurrentEditingCompositionId ?? Guid.Empty, null, [effectItem.PluginId]);
            }, _ => ViewState.CurrentEditingCompositionId.HasValue && ViewState.LastSelectedLayerId.HasValue);
            EffectCommands = [..effectListStateModel.Effects.Select(e => Tuple.Create<string, string[], ICommand, object?>($"{effectCategory} > {e.Category} > {e.Name}", [effectCategory, e.Category, e.Name, jpEffectCategory], effectCommand, e))];

            FilteredCommands = Commands.CreateCollectionView(() => FilterText, FilterCommand);

            ExecuteCommand = new DelegateCommand(() =>
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
            }, () => SelectedCommand != null).ObservesProperty(() => SelectedCommand);

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
            foreach (var (displayName, searchTexts, command, effectItem) in EffectCommands)
            {
                Commands.Add(Tuple.Create(displayName, searchTexts, command, effectItem, command.CanExecute(effectItem)));
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
