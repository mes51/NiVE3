using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Extension;
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

        private class InsertEffectsHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddEffects);

            LayerModel Model { get; }

            EffectModel[] Effects { get; }

            int StartIndex { get; }

            public InsertEffectsHistoryCommand(LayerModel model, EffectModel[] effects, int startIndex)
            {
                Model = model;
                Effects = effects;
                StartIndex = startIndex;
            }

            public void Redo()
            {
                var i = StartIndex;
                foreach (var e in Effects)
                {
                    Model.Effects.Insert(i, e);
                    i++;
                }
            }

            public void Undo()
            {
                foreach (var e in Effects)
                {
                    Model.Effects.Remove(e);
                }
            }

            public void Dispose()
            {
                foreach (var e in Effects)
                {
                    e.Dispose();
                }
            }
        }

        private class MoveEffectsHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveEffects);

            LayerModel LayerModel { get; }

            EffectModel[] Effects { get; }

            int[] PrevIndices { get; }

            EffectModel[] NewOrderedEffects { get; }

            public MoveEffectsHistoryCommand(LayerModel compositionModel, EffectModel[] effects, int[] prevIndices, EffectModel[] newOrderedEffects)
            {
                LayerModel = compositionModel;
                Effects = effects;
                PrevIndices = prevIndices;
                NewOrderedEffects = newOrderedEffects;
            }

            public void Redo()
            {
                LayerModel.Effects.SortBy(l => Array.IndexOf(NewOrderedEffects, l));
            }

            public void Undo()
            {
                foreach (var l in Effects)
                {
                    LayerModel.Effects.Remove(l);
                }

                foreach (var (l, i) in Effects.Zip(PrevIndices))
                {
                    LayerModel.Effects.Insert(i, l);
                }
            }

            public void Dispose() { }
        }

        private class ChangeEffectEnableHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeEffectsEnable);

            EffectModel[] Effects { get; }

            bool[] OldValues { get; }

            bool NewValue { get; }

            public ChangeEffectEnableHistoryCommand(EffectModel[] effects, bool[] oldValues, bool newValue)
            {
                Effects = effects;
                OldValues = oldValues;
                NewValue = newValue;
            }

            public void Redo()
            {
                foreach (var e in Effects)
                {
                    e.IsEnable = NewValue;
                }
            }

            public void Undo()
            {
                foreach (var (e, enable) in Effects.Zip(OldValues))
                {
                    e.IsEnable = enable;
                }
            }

            public void Dispose() { }
        }

        private class DeleteEffectEnableHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DeleteEffects);

            LayerModel Model { get; }

            EffectModel[] Effects { get; }

            int[] Indices { get; }

            public DeleteEffectEnableHistoryCommand(LayerModel model, EffectModel[] effects, int[] indices)
            {
                Model = model;
                Effects = effects;
                Indices = indices;
            }

            public void Redo()
            {
                foreach (var e in Effects)
                {
                    Model.Effects.Remove(e);
                }
            }

            public void Undo()
            {
                foreach (var (e, i) in Effects.Zip(Indices))
                {
                    Model.Effects.Insert(i, e);
                }
            }

            public void Dispose() { }
        }
    }
}
