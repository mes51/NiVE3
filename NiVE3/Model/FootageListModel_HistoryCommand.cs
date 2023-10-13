using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class FootageListModel
    {
        private class AddFolderHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddFolder);

            FootageListModel Model { get; }

            string? FolderName { get; }

            Guid FolderId { get; }

            Guid? ParentFootageId { get; }

            public AddFolderHistoryCommand(FootageListModel model, string? folderName, Guid folderId, Guid? parentFootageId)
            {
                Model = model;
                FolderName = folderName;
                FolderId = folderId;
                ParentFootageId = parentFootageId;
            }

            public void Redo()
            {
                var folder = new FootageFolderModel(Model.HistoryModel, FolderId);
                if (FolderName != null)
                {
                    folder.Name = FolderName;
                }
                Model.AddFootage(folder, ParentFootageId);
            }

            public void Undo()
            {
                var folder = FindModel(FolderId, Model.Footages);
                if (folder == null)
                {
                    return;
                }

                var parent = Model.FindParent(FolderId);

                if (parent != null)
                {
                    parent.RemoveFootage(folder);
                }
                else
                {
                    Model.DeleteFootageFromRootInternal(folder);
                }
            }

            public void Dispose() { }
        }

        private class LoadFileHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_LoadFootageFile);

            FootageListModel Model { get; }

            InputModel Input { get; }

            IFootageModel LoadedFootage { get; }

            IFootageModel[] LoadedSourceModels { get; }

            Guid? TargetFootageId { get; }

            public LoadFileHistoryCommand(FootageListModel model, InputModel InputModel, IFootageModel loadedFootage, Guid? targetFootageId)
            {
                Model = model;
                Input = InputModel;
                LoadedFootage = loadedFootage;
                TargetFootageId = targetFootageId;

                var remain = new Queue<IFootageModel>();
                remain.Enqueue(LoadedFootage);
                var sources = new List<IFootageModel>();
                while (remain.Count > 0)
                {
                    var f = remain.Dequeue();
                    if (f is FootageModel)
                    {
                        sources.Add(f);
                    }
                    else if (f.Children != null)
                    {
                        foreach (var child in f.Children)
                        { 
                            remain.Enqueue(child);
                        }
                    }
                }

                LoadedSourceModels = sources.ToArray();
            }

            public void Redo()
            {
                Model.AddFootage(LoadedFootage, TargetFootageId);
                Model.AddInput(Input);
            }

            public void Undo()
            {
                Model.DeleteInput(Input);
                Model.DeleteFootageInternal(LoadedFootage);

                Model.OnDeleteFootageByUndo(LoadedSourceModels);
            }

            public void Dispose()
            {
                Input.Dispose();
            }
        }

        private class MoveHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveFootage);

            FootageListModel Model { get; }

            Guid TargetId { get; }

            Guid? OldParentId { get; }

            Guid? NewParentId { get; }

            public MoveHistoryCommand(FootageListModel model, Guid targetId, Guid? oldParentId, Guid? newParentId)
            {
                Model = model;
                TargetId = targetId;
                OldParentId = oldParentId;
                NewParentId = newParentId;
            }

            public void Redo()
            {
                var target = FindModel(TargetId, Model.Footages);
                if (target == null)
                {
                    return;
                }

                var oldParent = OldParentId.HasValue ? FindModel(OldParentId.Value, Model.Footages) : null;
                var newParent = NewParentId.HasValue ? FindModel(NewParentId.Value, Model.Footages) : null;

                if (oldParent != null)
                {
                    oldParent.RemoveFootage(target);
                }
                else
                {
                    Model.DeleteFootageFromRootInternal(target);
                }

                if (newParent != null)
                {
                    newParent.AddFootage(target);
                }
                else
                {
                    Model.AddFootageToRoot(target);
                }
            }

            public void Undo()
            {
                var target = FindModel(TargetId, Model.Footages);
                if (target == null)
                {
                    return;
                }

                var oldParent = OldParentId.HasValue ? FindModel(OldParentId.Value, Model.Footages) : null;
                var newParent = NewParentId.HasValue ? FindModel(NewParentId.Value, Model.Footages) : null;

                if (newParent != null)
                {
                    newParent.RemoveFootage(target);
                }
                else
                {
                    Model.DeleteFootageFromRootInternal(target);
                }

                if (oldParent != null)
                {
                    oldParent.AddFootage(target);
                }
                else
                {
                    Model.AddFootageToRoot(target);
                }
            }

            public void Dispose() { }
        }

        private class DeleteFootageHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DeleteFootages);

            FootageListModel Model { get; }

            IFootageModel[] Footages { get; }

            IFootageModel?[] Parents { get; }

            InputModel[] InputModels { get; }

            public DeleteFootageHistoryCommand(FootageListModel model, IFootageModel[] footages, IFootageModel?[] parents, InputModel[] inputModels)
            {
                Model = model;
                Footages = footages;
                Parents = parents;
                InputModels = inputModels;
            }

            public void Redo()
            {
                foreach (var f in Footages)
                {
                    Model.DeleteFootageInternal(f);
                }
                foreach (var i in InputModels)
                {
                    Model.DeleteInput(i);
                }
            }

            public void Undo()
            {
                foreach (var (f, p) in Footages.Zip(Parents))
                {
                    Model.AddFootage(f, p?.FootageId);
                }

                foreach (var i in InputModels)
                {
                    Model.AddInput(i);
                }
            }

            public void Dispose() { }
        }
    }
}
