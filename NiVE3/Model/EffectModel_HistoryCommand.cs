using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class EffectModel
    {
        private class ChangeNameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeEffectName);

            EffectModel Model { get; }

            string OldName { get; }

            string NewName { get; }

            public ChangeNameHistoryCommand(EffectModel model, string oldName, string newName)
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

        private class ChangeCommentHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeEffectComment);

            EffectModel Model { get; }

            string OldComment { get; }

            string NewComment { get; }

            public ChangeCommentHistoryCommand(EffectModel model, string oldComment, string newComment)
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

        private class OverwriteEffectHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_PasteEffects);

            EffectModel Model { get; }

            EffectData OldData { get; }

            EffectData NewData { get; }

            public OverwriteEffectHistoryCommand(EffectModel model, EffectData oldData, EffectData newData)
            {
                Model = model;
                OldData = oldData;
                NewData = newData;
            }

            public void Redo()
            {
                Model.LoadData(NewData);
            }

            public void Undo()
            {
                Model.LoadData(OldData);
            }

            public void Dispose() { }
        }
    }
}
