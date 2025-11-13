using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.View.Command;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.SourceGenerator.ViewModelWireGenerator;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using NiVE3.Mvvm;
using System.Collections.Specialized;
using NiVE3.Model.UI;

namespace NiVE3.ViewModel
{
    [CommandHandling(nameof(UndoCommand), nameof(ShortcutKeySetting.UndoGesture), IsGlobal = true)]
    [CommandHandling(nameof(RedoCommand), nameof(ShortcutKeySetting.RedoGesture), IsGlobal = true)]
    [PaneLocation(PaneLocation.Right1Bottom, Size = 200)]
    [UseReactiveProperty]
    [ViewModelWireable(nameof(WiringModel), WithInitializeProperty = true)]
    partial class HistoryViewModel : SingletonePaneViewModelBase
    {
        public IEnumerable<IHistoryCommand> FirstHistoryCommand { get; } = [NewProjectHistoryCommand.Instance];

        [NeedWire(nameof(HistoryModel), IsOneWay = true)]
        public ObservableStack<IHistoryCommand> UndoCommands
        {
            get;
            set
            {
                if (field != value)
                {
                    field.CollectionChanged -= UndoCommands_CollectionChanged;
                    value.CollectionChanged += UndoCommands_CollectionChanged;
                }
                SetProperty(ref field, value);
            }
        } = [];

        [NeedWire(nameof(HistoryModel), IsOneWay = true)]
        public ObservableStack<IHistoryCommand> RedoCommands
        {
            get;
            set
            {
                if (field != value)
                {
                    field.CollectionChanged -= RedoCommands_CollectionChanged;
                    value.CollectionChanged += RedoCommands_CollectionChanged;
                }
                SetProperty(ref field, value);
            }
        } = [];

        [ReactiveProperty]
        [NeedWire(nameof(ViewState))]
        public partial bool IsIgnoreUpdatePreview { get; set; }

        // NOTE: なぜかStackをそのままCollectionContainer等に渡すと順番がひっくり返るため、順序を固定する
        public IEnumerable<IHistoryCommand> ReversedRedoCommands => [..RedoCommands];

        public IHistoryCommand LatestUndoCommand => UndoCommands.Count > 0 ? UndoCommands.Peek() : FirstHistoryCommand.First();

        public ICommand UndoCommand { get; }

        public ICommand RedoCommand { get; }

        public ICommand ReproduceToTargetHistoryCommand { get; }

        HistoryModel HistoryModel { get; }

        ViewStateModel ViewState { get; }

        public HistoryViewModel(HistoryModel model, ViewStateModel viewState)
        {
            Title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.HistoryList_Title);
            HistoryModel = model;
            ViewState = viewState;

            UndoCommand = new DelegateCommand(() =>
            {
                IsIgnoreUpdatePreview = true;
                HistoryModel.Undo();
                IsIgnoreUpdatePreview = false;
            }, () => HistoryModel.CanUndo());

            RedoCommand = new DelegateCommand(() =>
            {
                IsIgnoreUpdatePreview = true;
                HistoryModel.Redo();
                IsIgnoreUpdatePreview = false;
            }, () => HistoryModel.CanRedo());

            ReproduceToTargetHistoryCommand = new DelegateCommand<IHistoryCommand>(targetHistory =>
            {
                IsIgnoreUpdatePreview = true;
                if (targetHistory is NewProjectHistoryCommand)
                {
                    while (HistoryModel.CanUndo())
                    {
                        HistoryModel.Undo();
                    }
                }
                else if (UndoCommands.Contains(targetHistory))
                {
                    while (HistoryModel.CanUndo() && UndoCommands.Peek() != targetHistory)
                    {
                        HistoryModel.Undo();
                    }
                }
                else
                {
                    while (HistoryModel.CanRedo() && (UndoCommands.Count < 1 || UndoCommands.Peek() != targetHistory))
                    {
                        HistoryModel.Redo();
                    }
                }
                IsIgnoreUpdatePreview = false;
            });

            WiringModel();
        }

        partial void WiringModel();

        private void UndoCommands_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(LatestUndoCommand));
        }

        private void RedoCommands_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(ReversedRedoCommands));
        }
    }

    class NewProjectHistoryCommand : IHistoryCommand
    {
        public static readonly IHistoryCommand Instance = new NewProjectHistoryCommand();

        public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_NewProject);

        private NewProjectHistoryCommand() { }

        public void Redo() { }

        public void Undo() { }

        public void Dispose() { }
    }
}
