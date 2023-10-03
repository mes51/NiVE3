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
using NiVE3.Config;
using NiVE3.Model;
using NiVE3.Mvvm;
using NiVE3.View.Command;
using NiVE3.View.Dialog;
using NiVE3.View.Dock;
using NiVE3.View.Resource;
using NiVE3.ViewModel.Dialog;
using Prism.Commands;
using Prism.Services.Dialogs;

namespace NiVE3.ViewModel
{
    interface IFootageViewModelList
    {
        ObservableCollectionView<IFootageModel, IFootageViewModel>? Footages { get; }
    }

    [PaneLocation(PaneLocation.Left)]
    [CommandHandling(nameof(OpenFileCommand), nameof(ShortcutKeySetting.OpenFileGesture), IsGlobal = true)]
    [CommandHandling(nameof(AddFootageFolderCommand), nameof(ShortcutKeySetting.AddFootageFolderGesture), IsGlobal = true)]
    [CommandHandling(nameof(DeleteFootageCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    [CommandHandling(nameof(BeginEditNameCommand), nameof(ShortcutKeySetting.BeginEditNameGesture))]
    class FootageListViewModel : SingletonePaneViewModelBase, IFootageViewModelList, IDropTarget, IDragSource
    {
        private ObservableCollectionView<IFootageModel, IFootageViewModel> footages;
        public ObservableCollectionView<IFootageModel, IFootageViewModel> Footages
        {
            get { return footages; }
            set { SetProperty(ref footages, value); }
        }

        private ObservableCollection<IFootageViewModel> selectedFootages = new ObservableCollection<IFootageViewModel>();
        public ObservableCollection<IFootageViewModel> SelectedFootages
        {
            get { return selectedFootages; }
            set { SetProperty(ref selectedFootages, value); }
        }

        private bool showFileExtension = true;
        public bool ShowFileExtension
        {
            get { return showFileExtension; }
            set { SetProperty(ref showFileExtension, value); }
        }

        private bool showSize = true;
        public bool ShowSize
        {
            get { return showSize; }
            set { SetProperty(ref showSize, value); }
        }

        private bool showFrameRate = true;
        public bool ShowFrameRate
        {
            get { return showFrameRate; }
            set { SetProperty(ref showFrameRate, value); }
        }

        private bool showDuration = true;
        public bool ShowDuration
        {
            get { return showDuration; }
            set { SetProperty(ref showDuration, value); }
        }

        private bool showComment = true;
        public bool ShowComment
        {
            get { return showComment; }
            set { SetProperty(ref showComment, value); }
        }

        private bool showFilePath = true;
        public bool ShowFilePath
        {
            get { return showFilePath; }
            set { SetProperty(ref showFilePath, value); }
        }

        private EditingFootageParameter editingProperty;
        public EditingFootageParameter EditingParameter
        {
            get { return editingProperty; }
            set { SetProperty(ref editingProperty, value); }
        }

        private IFootageViewModel? editingFootage;
        public IFootageViewModel? EditingFootage
        {
            get { return editingFootage; }
            set { SetProperty(ref editingFootage, value); }
        }

        public ICommand MoveFootageCommand { get; }

        public ICommand MoveFootageListCommand { get; }

        public ICommand OpenFileCommand { get; }

        public ICommand DeleteFootageCommand { get; }

        public ICommand AddSolidCommand { get; }

        public ICommand AddFootageFolderCommand { get; }

        public ICommand LoadFileCommand { get; }

        public ICommand ShowPreviewCommand { get; }

        public ICommand BeginEditNameCommand { get; }

        public ICommand EndEditNameCommand { get; }

        public ICommand BeginEditCommentCommand { get; }

        public ICommand EndEditCommentCommand { get; }

        FootageListModel FootageListModel { get; }

        IDialogService DialogService { get; }

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public FootageListViewModel(FootageListModel footageListModel, IDialogService dialogService)
#pragma warning restore CS8618
        {
            FootageListModel = footageListModel;
            DialogService = dialogService;
            Footages = footageListModel.Footages.CreateViewCollection<IFootageModel, IFootageViewModel>(m => m is FootageModel ? new FootageViewModel((FootageModel)m) : new FootageFolderViewModel((FootageFolderModel)m));

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

            OpenFileCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.OpenFileCommand is not implemented"));

            DeleteFootageCommand = new RequerySuggestedCommand(() =>
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
            }, () => SelectedFootages.Count > 0);

            AddSolidCommand = new DelegateCommand(() => FootageListModel.AddSolid());

            AddFootageFolderCommand = new DelegateCommand(() => FootageListModel.AddFolder());

            LoadFileCommand = new DelegateCommand<Tuple<string, Guid?>>(t => FootageListModel.LoadFile(t.Item1, t.Item2));

            ShowPreviewCommand = new DelegateCommand<FootageViewModel>(f => FootageListModel.ShowPreview(f.FootageId));

            BeginEditNameCommand = new RequerySuggestedCommand(() =>
            {
                EditingParameter = EditingFootageParameter.Name;
                EditingFootage = SelectedFootages.First();
                EditingFootage.BeginEditNameCommand.Execute(null);
            }, () => SelectedFootages.Count > 0 && EditingParameter == EditingFootageParameter.None);

            EndEditNameCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (EditingFootage == null)
                {
                    return;
                }

                EditingFootage.EndEditNameCommand.Execute(commit);
                EditingParameter = EditingFootageParameter.None;
            }, _ => EditingParameter == EditingFootageParameter.Name);

            BeginEditCommentCommand = new RequerySuggestedCommand<IFootageViewModel>(viewModel =>
            {
                EditingParameter = EditingFootageParameter.Comment;
                EditingFootage = viewModel;
                EditingFootage.BeginEditCommentCommand.Execute(null);
            }, _ => EditingParameter == EditingFootageParameter.None);

            EndEditCommentCommand = new RequerySuggestedCommand<bool>(commit =>
            {
                if (EditingFootage == null)
                {
                    return;
                }

                EditingFootage.EndtEditCommentCommand.Execute(commit); ;
                EditingParameter = EditingFootageParameter.None;
            }, _ => EditingParameter == EditingFootageParameter.Comment);

            FootageListModel.ShowLoadSetting += FootageListModel_ShowLoadSetting;
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
                        foreach (var f in files)
                        {
                            LoadFileCommand.Execute(Tuple.Create(f, targetFolderId));
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
                { nameof(InputSettingViewModel.SettingView), e.View }
            };
            IDialogResult? result = null;
            DialogService.ShowDialog(nameof(InputSettingView), param, r => result = r);
            e.IsOK = result?.Result == ButtonResult.OK;
        }
    }

    enum EditingFootageParameter
    {
        None,
        Name,
        Comment
    }
}
