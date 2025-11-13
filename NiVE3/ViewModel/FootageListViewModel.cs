using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.Model.UI;
using NiVE3.Mvvm;
using NiVE3.View.Command;
using NiVE3.View.Dialog;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.ViewModel.Dialog;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Commands;
using Prism.Dialogs;

namespace NiVE3.ViewModel
{
    interface IFootageViewModelList
    {
        ObservableCollectionView<IFootageModel, IFootageViewModel>? Footages { get; }
    }

    [PaneLocation(PaneLocation.Left1Center, Size = 578)]
    [CommandHandling(nameof(AddFootageFolderCommand), nameof(ShortcutKeySetting.NewFootageFolderGesture), IsGlobal = true)]
    [CommandHandling(nameof(DeleteFootageCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    [CommandHandling(nameof(BeginEditNameCommand), nameof(ShortcutKeySetting.BeginEditNameGesture))]
    [CommandHandling(nameof(LoadSolidCommand), nameof(ShortcutKeySetting.LoadSolidGesture), IsGlobal = true)]
    [CommandHandling(nameof(LoadFileCommand), nameof(ShortcutKeySetting.LoadFileGesture), IsGlobal = true)]
    [UseReactiveProperty]
    partial class FootageListViewModel : SingletonePaneViewModelBase, IFootageViewModelList, IDropTarget, IDragSource
    {
        [ReactiveProperty]
        public partial ObservableCollectionView<IFootageModel, IFootageViewModel> Footages { get; set; }

        [ReactiveProperty]
        public partial ObservableCollection<IFootageViewModel> SelectedFootages { get; set; } = [];

        [ReactiveProperty]
        public partial bool ShowFileExtension { get; set; } = true;

        [ReactiveProperty]
        public partial bool ShowSize { get; set; } = true;

        [ReactiveProperty]
        public partial bool ShowFrameRate { get; set; } = true;

        [ReactiveProperty]
        public partial bool ShowDuration { get; set; } = true;

        [ReactiveProperty]
        public partial bool ShowComment { get; set; } = true;

        [ReactiveProperty]
        public partial bool ShowFilePath { get; set; } = true;

        [ReactiveProperty]
        public partial EditingFootageParameter EditingParameter { get; set; }

        [ReactiveProperty]
        public partial IFootageViewModel? EditingFootage { get; set; }

        public ICommand MoveFootageCommand { get; }

        public ICommand MoveFootageListCommand { get; }

        public ICommand LoadSolidCommand { get; }

        public ICommand LoadFileCommand { get; }

        public ICommand DeleteFootageCommand { get; }

        public ICommand AddFootageFolderCommand { get; }

        public ICommand ShowPreviewCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        FootageListModel FootageListModel { get; }

        EventHubModel EventHubModel { get; }

        IDialogService DialogService { get; }

        public FootageListViewModel(FootageListModel footageListModel, ApplicationModel applicationModel, EventHubModel eventHubModel, IDialogService dialogService)
        {
            FootageListModel = footageListModel;
            EventHubModel = eventHubModel;
            DialogService = dialogService;
            Footages = footageListModel.Footages.CreateViewCollection<IFootageModel, IFootageViewModel>(m => m is FootageModel footage ? new FootageViewModel(footage, applicationModel) : new FootageFolderViewModel((FootageFolderModel)m, applicationModel));

            Title = "フッテージ";

            MoveFootageCommand = new DelegateCommand<Tuple<IFootageViewModel, IFootageViewModelList>>(t =>
            {
                var (source, newParent) = t;
                if (newParent is IFootageViewModel targetFolder)
                {
                    FootageListModel.MoveFootage(source.FootageId, targetFolder.FootageId);
                }
                else
                {
                    FootageListModel.MoveFootageToRoot(source.FootageId);
                }
            });

            MoveFootageListCommand = new DelegateCommand<Tuple<IEnumerable<IFootageViewModel>, IFootageViewModelList>>(t =>
            {
                var (sources, newParent) = t;
                var ids = sources.Select(f => f.FootageId).ToArray();
                if (newParent is IFootageViewModel targetFolder)
                {
                    FootageListModel.MoveFootages(ids, targetFolder.FootageId);
                }
                else
                {
                    FootageListModel.MoveFootagesToRoot(ids);
                }
            });

            LoadSolidCommand = new DelegateCommand(() => FootageListModel.AddSolid());

            LoadFileCommand = new DelegateCommand(() =>
            {
                var open = new OpenFileDialog
                {
                    Filter = $"{LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFile_Filter_SupportedAllTypes)}({string.Join(",", FootageListModel.SupportedAllExtensions)})|{string.Join(";", FootageListModel.SupportedAllExtensions)}"
                };
                if (open.ShowDialog() ?? false)
                {
                    var result = FootageListModel.LoadFile(open.FileName, null);
                    if (result != FootageLoadResultType.Success)
                    {
                        ShowFootageLoadErrorDialog([result], false);
                    }
                }
            });

            DeleteFootageCommand = new DelegateCommand(() =>
            {
                if (SelectedFootages.Count < 1)
                {
                    return;
                }

                var rd = LanguageResourceDictionary.Dictionary;
                var title = rd.GetText(LanguageResourceDictionary.Dialog_ConfirmDeleteFootage_Title);
                var text = "";

                if (SelectedFootages.Any(f => f.IsFolder))
                {
                    text = rd.GetText(LanguageResourceDictionary.Dialog_ConfirmDeleteFootageFolder_Text);
                }
                else
                {
                    text = rd.GetText(LanguageResourceDictionary.Dialog_ConfirmDeleteFootage_Text);
                }

                if (MessageBox.Show(text, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                {
                    return;
                }

                FootageListModel.DeleteFootages(SelectedFootages.Select(f => f.FootageId).ToArray());
            }, () => SelectedFootages.Count > 0).ObservesProperty(() => SelectedFootages.Count);

            AddFootageFolderCommand = new DelegateCommand(() => FootageListModel.AddFolder());

            ShowPreviewCommand = new DelegateCommand<FootageViewModel>(f => FootageListModel.ShowPreview(f.FootageId));

            BeginEditNameCommand = new DelegateCommand(() =>
            {
                EditingParameter = EditingFootageParameter.Name;
                EditingFootage = SelectedFootages.First();
                EditingFootage.BeginEditNameCommand.Execute(null);
            }, () => SelectedFootages.Count > 0 && EditingParameter == EditingFootageParameter.None)
                .ObservesProperty(() => SelectedFootages.Count)
                .ObservesProperty(() => EditingParameter);

            EndEditNameCommand = new DelegateCommand<bool?>(commit =>
            {
                if (EditingFootage == null)
                {
                    return;
                }

                EditingFootage.EndEditNameCommand.Execute(commit ?? false);
                EditingParameter = EditingFootageParameter.None;
            }, _ => EditingParameter == EditingFootageParameter.Name).ObservesProperty(() => EditingParameter);

            BeginEditCommentCommand = new DelegateCommand<IFootageViewModel>(viewModel =>
            {
                EditingParameter = EditingFootageParameter.Comment;
                EditingFootage = viewModel;
                EditingFootage.BeginEditCommentCommand.Execute(null);
            }, _ => SelectedFootages.Count > 0 && EditingParameter == EditingFootageParameter.None)
                .ObservesProperty(() => SelectedFootages.Count)
                .ObservesProperty(() => EditingParameter);

            EndEditCommentCommand = new DelegateCommand<bool?>(commit =>
            {
                if (EditingFootage == null)
                {
                    return;
                }

                EditingFootage.EndEditCommentCommand.Execute(commit ?? false);
                EditingParameter = EditingFootageParameter.None;
            }, _ => EditingParameter == EditingFootageParameter.Comment).ObservesProperty(() => EditingParameter);

            FootageListModel.ShowLoadSetting += FootageListModel_ShowLoadSetting;

            // TODO: TimelineViewModelからCompositionModel経由でFootageListModelにアクセスする方がいいかどうか検討する
            EventHubModel.ShowFootagePreviewRequest += EventHubModel_ShowFootagePreviewRequest;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var target = dropInfo.TargetItem as IFootageViewModelList ?? this;

            switch (dropInfo.Data)
            {
                case IFootageViewModel footageViewModel:
                    {
                        if (target is not IFootageViewModel targetFootage || (targetFootage.IsFolder && footageViewModel.FootageId != targetFootage.FootageId))
                        {
                            if (footageViewModel is not FootageFolderViewModel sourceFolder || !IsContainsTree(sourceFolder.Footages, target))
                            {
                                dropInfo.Effects |= DragDropEffects.Move;
                                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                            }
                        }
                    }
                    break;
                case IFootageViewModel[] footageViewModels:
                    {
                        if (target is not IFootageViewModel targetFootage || (targetFootage.IsFolder && footageViewModels.All(f => f.FootageId != targetFootage.FootageId)))
                        {
                            if (footageViewModels.All(f => f is not FootageFolderViewModel sourceFolder || !IsContainsTree(sourceFolder.Footages, target)))
                            {
                                dropInfo.Effects |= DragDropEffects.Move;
                                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                            }
                        }
                    }
                    break;
                case IDataObject dataObject:
                    if (dataObject.GetData(DataFormats.FileDrop) is string[] files)
                    {
                        if (files.Any(f => !FootageListModel.CheckSupportFile(f)))
                        {
                            return;
                        }

                        dropInfo.Effects |= DragDropEffects.Copy;
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    }
                    break;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            var target = dropInfo.TargetItem as IFootageViewModelList ?? this;

            switch (dropInfo.Data)
            {
                case IFootageViewModel footageViewModel:
                    {
                        if (target is not IFootageViewModel targetFootage || (targetFootage.IsFolder && footageViewModel.FootageId != targetFootage.FootageId))
                        {
                            if (footageViewModel is FootageFolderViewModel sourceFolder && IsContainsTree(sourceFolder.Footages, target))
                            {
                                return;
                            }

                            MoveFootageCommand.Execute(Tuple.Create(footageViewModel, target));
                        }
                    }
                    break;
                case IFootageViewModel[] footageViewModels:
                    {
                        if (target is not IFootageViewModel targetFootage || (targetFootage.IsFolder && footageViewModels.All(f => f.FootageId != targetFootage.FootageId)))
                        {
                            if (footageViewModels.Any(f => f is FootageFolderViewModel sourceFolder && IsContainsTree(sourceFolder.Footages, target)))
                            {
                                return;
                            }

                            MoveFootageListCommand.Execute(Tuple.Create(footageViewModels.AsEnumerable(), target));
                        }
                    }
                    break;
                case IDataObject dataObject:
                    if (dataObject.GetData(DataFormats.FileDrop) is string[] files)
                    {
                        var targetFolderId = (target as IFootageViewModel)?.FootageId;
                        var results = new List<FootageLoadResultType>();
                        foreach (var f in files)
                        {
                            results.Add(FootageListModel.LoadFile(f, targetFolderId));
                        }

                        if (results.Any(r => r != FootageLoadResultType.Success))
                        {
                            ShowFootageLoadErrorDialog([..results.Where(r => r != FootageLoadResultType.Success)], true);
                        }
                    }
                    break;
            }
        }

        public void StartDrag(IDragInfo dragInfo)
        {
            if (SelectedFootages.Count > 1)
            {
                dragInfo.Data = SelectedFootages.ToArray();
            }
            else
            {
                dragInfo.Data = SelectedFootages.FirstOrDefault();
            }

            if (dragInfo.Data != null)
            {
                dragInfo.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            }
            else
            {
                dragInfo.Effects = DragDropEffects.None;
            }
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            return true;
        }

        public void Dropped(IDropInfo dropInfo) { }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) { }

        public void DragCancelled() { }

        public bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }

        static void ShowFootageLoadErrorDialog(FootageLoadResultType[] footageLoadResultType, bool ignoreCancel)
        {
            var targetTypes = footageLoadResultType.Except(ignoreCancel ? [FootageLoadResultType.Success, FootageLoadResultType.Cancel] : [FootageLoadResultType.Success]).ToArray();
            if (targetTypes.Length < 1)
            {
                return;
            }


            var title = "";
            var text = "";
            if (targetTypes.Length > 1)
            {
                title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageCannotLoadMultiple_Title);
                text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageCannotLoadMultiple_Text);
            }
            else
            {
                switch (footageLoadResultType[0])
                {
                    case FootageLoadResultType.Cancel:
                        if (ignoreCancel)
                        {
                            return;
                        }
                        else
                        {
                            title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageCancel_Title);
                            text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageCancel_Text);
                            break;
                        }
                    case FootageLoadResultType.CannotLoad:
                        title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageCannotLoad_Title);
                        text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageCannotLoad_Text);
                        break;
                    case FootageLoadResultType.NotSupported:
                        title = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageNotSupported_Title);
                        text = LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.Dialog_LoadFootageNotSupported_Text);
                        break;
                }
            }

            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        static bool IsContainsTree(IEnumerable<IFootageViewModel> items, IFootageViewModelList target)
        {
            foreach (var item in items)
            {
                if ((target is IFootageViewModel targetFootage && item.FootageId == targetFootage.FootageId) || (item.Footages != null && IsContainsTree(item.Footages, target)))
                {
                    return true;
                }
            }

            return false;
        }

        private void FootageListModel_ShowLoadSetting(object? sender, ShowLoadSettingEventArgs e)
        {
            var param = new DialogParameters
            {
                { PluginSettingViewModel.TitleLanguageResourceName, LanguageResourceDictionary.InputSettingView_Title },
                { nameof(PluginSettingViewModel.SettingView), e.View }
            };
            IDialogResult? result = null;
            DialogService.ShowDialog(nameof(PluginSettingView), param, r => result = r);
            e.IsOK = result?.Result == ButtonResult.OK;
        }

        private void EventHubModel_ShowFootagePreviewRequest(object? sender, ShowFootagePreviewEventArgs e)
        {
            FootageListModel.ShowPreview(e.FootageId);
        }
    }

    enum EditingFootageParameter
    {
        None,
        Name,
        Comment
    }
}
