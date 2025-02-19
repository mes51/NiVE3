using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class LayerModel
    {
        private class EditDurationHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration);

            LayerModel Model { get; set; }

            Time OldInPoint { get; set; }

            Time OldOutPoint { get; set; }

            Time OldSourceStartPoint { get; set; }

            Time NewInPoint { get; set; }

            Time NewOutPoint { get; set; }

            Time NewSourceStartPoint { get; set; }

            public EditDurationHistoryCommand(LayerModel model, Time oldInPoint, Time oldOutPoint, Time oldSourceStartPoint, Time newInPoint, Time newOutPoint, Time newSourceStartPoint)
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

            EffectModel[] OldOrderedEffects { get; }

            EffectModel[] NewOrderedEffects { get; }

            public MoveEffectsHistoryCommand(LayerModel compositionModel, EffectModel[] oldOrderedEffects, EffectModel[] newOrderedEffects)
            {
                LayerModel = compositionModel;
                OldOrderedEffects = oldOrderedEffects;
                NewOrderedEffects = newOrderedEffects;
            }

            public void Redo()
            {
                LayerModel.Effects.SortBy(l => Array.IndexOf(NewOrderedEffects, l));
            }

            public void Undo()
            {
                LayerModel.Effects.SortBy(l => Array.IndexOf(OldOrderedEffects, l));
            }

            public void Dispose() { }
        }

        private class ChangeEffectsEnableHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeEffectsEnable);

            EffectModel[] Effects { get; }

            bool[] OldValues { get; }

            bool NewValue { get; }

            public ChangeEffectsEnableHistoryCommand(EffectModel[] effects, bool[] oldValues, bool newValue)
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

        private class DeleteEffectHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(IsCut ? LanguageResourceDictionary.History_CutEffects : LanguageResourceDictionary.History_RemoveEffects);

            LayerModel Model { get; }

            EffectModel[] Effects { get; }

            int[] Indices { get; }

            bool IsCut { get; }

            public DeleteEffectHistoryCommand(LayerModel model, EffectModel[] effects, int[] indices, bool isCut)
            {
                Model = model;
                Effects = effects;
                Indices = indices;
                IsCut = isCut;
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

        private class PasteNewEffectsHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(IsDuplicate ? LanguageResourceDictionary.History_DuplicateEffects : LanguageResourceDictionary.History_PasteEffects);

            LayerModel Model { get; }

            EffectModel[] NewEffects { get; }

            int InsertStartIndex { get; }

            bool IsDuplicate { get; }

            public PasteNewEffectsHistoryCommand(LayerModel model, EffectModel[] newEffects, int insertStartIndex, bool isDuplicate)
            {
                Model = model;
                NewEffects = newEffects;
                InsertStartIndex = insertStartIndex;
                IsDuplicate = isDuplicate;
            }

            public void Redo()
            {
                var index = InsertStartIndex;
                foreach (var e in NewEffects)
                {
                    Model.Effects.Insert(index, e);
                    index++;
                }
            }

            public void Undo()
            {
                foreach (var e in NewEffects)
                {
                    Model.Effects.Remove(e);
                }
            }

            public void Dispose()
            {
                foreach (var e in NewEffects)
                {
                    e.Dispose();
                }
            }
        }

        private class MoveMasksHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveMasks);

            LayerModel LayerModel { get; }

            MaskModel[] OldOrderedMasks { get; }

            MaskModel[] NewOrderedMasks { get; }

            public MoveMasksHistoryCommand(LayerModel layerModel, MaskModel[] oldOrderedMasks, MaskModel[] newOrderedMasks)
            {
                LayerModel = layerModel;
                OldOrderedMasks = oldOrderedMasks;
                NewOrderedMasks = newOrderedMasks;
            }

            public void Redo()
            {
                LayerModel.Masks.SortBy(m => Array.IndexOf(NewOrderedMasks, m));
            }

            public void Undo()
            {
                LayerModel.Masks.SortBy(m => Array.IndexOf(OldOrderedMasks, m));
            }

            public void Dispose() { }
        }

        private class ChangeMasksEnableHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeMasksEnable);

            MaskModel[] Masks { get; }

            bool[] OldValues { get; }

            bool NewValue { get; }

            public ChangeMasksEnableHistoryCommand(MaskModel[] masks, bool[] oldValues, bool newValue)
            {
                Masks = masks;
                OldValues = oldValues;
                NewValue = newValue;
            }

            public void Redo()
            {
                foreach (var m in Masks)
                {
                    m.IsEnable = NewValue;
                }
            }

            public void Undo()
            {
                foreach (var (m, enable) in Masks.Zip(OldValues))
                {
                    m.IsEnable = enable;
                }
            }

            public void Dispose() { }
        }

        private class DeleteMaskHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(IsCut ? LanguageResourceDictionary.History_CutMasks : LanguageResourceDictionary.History_RemoveMasks);

            LayerModel Model { get; }

            MaskModel[] Masks { get; }

            int[] Indices { get; }

            bool IsCut { get; }

            public DeleteMaskHistoryCommand(LayerModel model, MaskModel[] masks, int[] indices, bool isCut)
            {
                Model = model;
                Masks = masks;
                Indices = indices;
                IsCut = isCut;
            }

            public void Redo()
            {
                foreach (var m in Masks)
                {
                    Model.Masks.Remove(m);
                }
            }

            public void Undo()
            {
                foreach (var (m, i) in Masks.Zip(Indices))
                {
                    Model.Masks.Insert(i, m);
                }
            }

            public void Dispose() { }
        }

        private class PasteNewMasksHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(IsDuplicate ? LanguageResourceDictionary.History_DuplicateMasks : LanguageResourceDictionary.History_PasteMasks);

            LayerModel Model { get; }

            MaskModel[] NewMasks { get; }

            int InsertStartIndex { get; }

            bool IsDuplicate { get; }

            public PasteNewMasksHistoryCommand(LayerModel model, MaskModel[] newMasks, int insertStartIndex, bool isDuplicate)
            {
                Model = model;
                NewMasks = newMasks;
                InsertStartIndex = insertStartIndex;
                IsDuplicate = isDuplicate;
            }

            public void Redo()
            {
                var index = InsertStartIndex;
                foreach (var m in NewMasks)
                {
                    Model.Masks.Insert(index, m);
                    index++;
                }
            }

            public void Undo()
            {
                foreach (var m in NewMasks)
                {
                    Model.Masks.Remove(m);
                }
            }

            public void Dispose() { }
        }

        private class ChangeTagColorHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeTagColor);

            LayerModel Model { get; }

            Color OldColor { get; }

            Color NewColor { get; }

            public ChangeTagColorHistoryCommand(LayerModel model, Color oldColor, Color newColor)
            {
                Model = model;
                OldColor = oldColor;
                NewColor = newColor;
            }

            public void Redo()
            {
                Model.TagColor = NewColor;
            }

            public void Undo()
            {
                Model.TagColor = OldColor;
            }

            public void Dispose() { }
        }

        private class ChangePlayRateHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeLayerPlayRate);

            LayerModel Model { get; }

            double OldPlayRate { get; }

            Time OldInPoint { get; }

            Time OldOutPoint { get; }

            double NewPlayRate { get; }

            Time NewInPoint { get; }

            Time NewOutPoint { get; }

            public ChangePlayRateHistoryCommand(LayerModel model, double oldPlayRate, Time oldInPoint, Time oldOutPoint, double newPlayRate, Time newInPoint, Time newOutPoint)
            {
                Model = model;
                OldPlayRate = oldPlayRate;
                OldInPoint = oldInPoint;
                OldOutPoint = oldOutPoint;
                NewPlayRate = newPlayRate;
                NewInPoint = newInPoint;
                NewOutPoint = newOutPoint;
            }

            public void Redo()
            {
                Model.PlayRate = NewPlayRate;
                Model.InPoint = NewInPoint;
                Model.OutPoint = NewOutPoint;
            }

            public void Undo()
            {
                Model.PlayRate = OldPlayRate;
                Model.InPoint = OldInPoint;
                Model.OutPoint = OldOutPoint;
            }

            public void Dispose() { }
        }

        private class ChangeFreezeFrameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeFreezeFrame);

            LayerModel Model { get; }

            bool OldIsFreezeFrame { get; }

            Time OldFreezeFrameTime { get; }

            Time OldInPoint { get; }

            Time OldOutPoint { get; }

            bool NewIsFreezeFrame { get; }

            Time NewFreezeFrameTime { get; }

            Time NewInPoint { get; }

            Time NewOutPoint { get; }

            public ChangeFreezeFrameHistoryCommand(LayerModel model, bool oldIsFreezeFrame, Time oldFreezeFrameTime, Time oldInPoint, Time oldOutPoint, bool newIsFreezeFrame, Time newFreezeFrameTime, Time newInPoint, Time newOutPoint)
            {
                Model = model;
                OldIsFreezeFrame = oldIsFreezeFrame;
                OldFreezeFrameTime = oldFreezeFrameTime;
                OldInPoint = oldInPoint;
                OldOutPoint = oldOutPoint;
                NewIsFreezeFrame = newIsFreezeFrame;
                NewFreezeFrameTime = newFreezeFrameTime;
                NewInPoint = newInPoint;
                NewOutPoint = newOutPoint;
            }

            public void Redo()
            {
                Model.IsFreezeFrame = NewIsFreezeFrame;
                Model.FreezeFrameTime = NewFreezeFrameTime;
                Model.InPoint = NewInPoint;
                Model.OutPoint = NewOutPoint;
            }

            public void Undo()
            {
                Model.IsFreezeFrame = OldIsFreezeFrame;
                Model.FreezeFrameTime = OldFreezeFrameTime;
                Model.InPoint = OldInPoint;
                Model.OutPoint = OldOutPoint;
            }

            public void Dispose() { }
        }

        private class InsertMaskHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddMask);

            LayerModel Model { get; }

            MaskModel NewMask { get; }

            int Index { get; }

            public InsertMaskHistoryCommand(LayerModel model, MaskModel newMask, int index)
            {
                Model = model;
                NewMask = newMask;
                Index = index;
            }

            public void Redo()
            {
                Model.Masks.Insert(Index, NewMask);
            }

            public void Undo()
            {
                Model.Masks.Remove(NewMask);
            }

            public void Dispose() { }
        }
    }
}
