using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class LayerModel
    {
        private class EditDurationHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration);

            LayerModel Model { get; set; }

            double OldInPoint { get; set; }

            double OldOutPoint { get; set; }

            double OldSourceStartPoint { get; set; }

            double NewInPoint { get; set; }

            double NewOutPoint { get; set; }

            double NewSourceStartPoint { get; set; }

            public EditDurationHistoryCommand(LayerModel model, double oldInPoint, double oldOutPoint, double oldSourceStartPoint, double newInPoint, double newOutPoint, double newSourceStartPoint)
            {
                Model = model;
                OldInPoint = oldInPoint;
                OldOutPoint = oldOutPoint;
                OldSourceStartPoint = oldSourceStartPoint;
                NewInPoint = newInPoint;
                NewOutPoint = newOutPoint;
                NewSourceStartPoint = newSourceStartPoint;
            }

            public void Redo()
            {
                Model.InPoint = NewInPoint;
                Model.OutPoint = NewOutPoint;
                Model.SourceStartPoint = NewSourceStartPoint;
            }

            public void Undo()
            {
                Model.InPoint = OldInPoint;
                Model.OutPoint = OldOutPoint;
                Model.SourceStartPoint = OldSourceStartPoint;
            }

            public void Dispose() { }
        }

        private class ChangeNameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeLayerName);

            LayerModel Model { get; }

            string OldName { get; }

            string NewName { get; }

            public ChangeNameHistoryCommand(LayerModel model, string oldName, string newName)
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
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeLayerComment);

            LayerModel Model { get; }

            string OldComment { get; }

            string NewComment { get; }

            public ChangeCommentHistoryCommand(LayerModel model, string oldComment, string newComment)
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
}
