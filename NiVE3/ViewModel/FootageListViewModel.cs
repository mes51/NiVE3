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
    [CommandHandling(nameof(AddSolidCommand), nameof(ShortcutKeySetting.AddSolidGesture), IsGlobal = true)]
    [CommandHandling(nameof(AddFootageFolderCommand), nameof(ShortcutKeySetting.AddFootageFolderGesture), IsGlobal = true)]
    [CommandHandling(nameof(DeleteFootageCommand), nameof(ShortcutKeySetting.DeleteItemGesture))]
    class FootageListViewModel : PaneViewModelBase, IFootageViewModelList, IDropTarget
    {
        private ObservableCollectionView<IFootageModel, IFootageViewModel> footages;
        public ObservableCollectionView<IFootageModel, IFootageViewModel> Footages
        {
            get { return footages; }
            set { SetProperty(ref footages, value); }
        }

        private IFootageViewModel? editingFootage;
        public IFootageViewModel? EditingFootage
        {
            get { return editingFootage; }
            private set { SetProperty(ref editingFootage, value); }
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

        public ICommand MoveFootageCommand { get; }

        public ICommand OpenFileCommand { get; }

        public ICommand DeleteFootageCommand { get; }

        public ICommand AddSolidCommand { get; }

        public ICommand AddFootageFolderCommand { get; }

        public ICommand BeginEditPropertyCommand { get; }

        public ICommand EndEditPropertyCommand { get; }

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

            OpenFileCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.OpenFileCommand is not implemented"));

            DeleteFootageCommand = new DelegateCommand(() => System.Diagnostics.Debug.WriteLine("FootageViewModel.DeleteFootageCommand is not implemented"));

            AddSolidCommand = new DelegateCommand(() => FootageListModel.AddSolid());

            AddFootageFolderCommand = new DelegateCommand(() => FootageListModel.AddFolder());

            BeginEditPropertyCommand = new DelegateCommand<Tuple<IFootageViewModel, string>>(t =>
            {
                if (EditingFootage != null)
                {
                    EditingFootage.EndEditProperty();
                }

                EditingFootage = t.Item1;
                EditingFootage.BeginEditProperty(t.Item2);
            });

            EndEditPropertyCommand = new DelegateCommand(() =>
            {
                if (EditingFootage != null)
                {
                    EditingFootage.EndEditProperty();
                    EditingFootage = null;
                }
            });

            FootageListModel.ShowLoadSetting += FootageListModel_ShowLoadSetting;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var source = dropInfo.Data as IFootageViewModel;
            var target = dropInfo.TargetItem as IFootageViewModelList ?? this;

            if (source == null)
            {
                return;
            }

            if (target is not IFootageViewModel targetFootage || (source.FootageId != targetFootage.FootageId && targetFootage.IsFolder))
            {
                if (source is not FootageFolderViewModel sourceFolder || !IsContainsTree(sourceFolder.Footages, target))
                {
                    dropInfo.Effects |= DragDropEffects.Move;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                }
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            var source = dropInfo.Data as IFootageViewModel;
            var target = dropInfo.TargetItem as IFootageViewModelList ?? this;

            if (source == null)
            {
                return;
            }

            if (target is not IFootageViewModel targetFootage || (source.FootageId != targetFootage.FootageId && targetFootage.IsFolder))
            {
                if (source is FootageFolderViewModel sourceFolder && IsContainsTree(sourceFolder.Footages, target))
                {
                    return;
                }

                MoveFootageCommand.Execute(Tuple.Create(source, target));
            }
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
}
