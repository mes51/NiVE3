using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    // TODO: IHistoryCommand生成メソッドを外から見えないようにする
    partial interface IFootageModel
    {
        static IHistoryCommand CreateChangeNameHistory(IFootageModel footage, string oldName, string newName)
        {
            return new ChangeNameHistoryCommand(footage, oldName, newName);
        }

        static IHistoryCommand CreateChangeCommentHistoryCommand(IFootageModel footage, string oldComment, string newComment)
        {
            return new ChangeCommentHistoryCommand(footage, oldComment, newComment);
        }
    }

    file class ChangeNameHistoryCommand : IHistoryCommand
    {
        public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeFootageName);

        IFootageModel Model { get; }

        string OldName { get; }

        string NewName { get; }

        public ChangeNameHistoryCommand(IFootageModel model, string oldName, string newName)
        {
            Model = model;
            OldName = oldName;
            NewName = newName;
        }

        public void Redo()
        {
            Model.Name = NewName;
        }

        public void Undo()
        {
            Model.Name = OldName;
        }

        public void Dispose() { }
    }

    file class ChangeCommentHistoryCommand : IHistoryCommand
    {
        public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeFootageComment);

        IFootageModel Model { get; }

        string OldComment { get; }

        string NewComment { get; }

        public ChangeCommentHistoryCommand(IFootageModel model, string oldComment, string newComment)
        {
            Model = model;
            OldComment = oldComment;
            NewComment = newComment;
        }

        public void Redo()
        {
            Model.Comment = NewComment;
        }

        public void Undo()
        {
            Model.Comment = OldComment;
        }

        public void Dispose() { }
    }
}
