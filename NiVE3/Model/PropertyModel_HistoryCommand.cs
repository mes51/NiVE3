using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Extension;
using NiVE3.Plugin.Property;
using NiVE3.Shared.Extension;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class PropertyModel
    {
        private class ValueChangeHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue);

            PropertyModel Model { get; }

            object? OldValue { get; }

            object? NewValue { get; }

            public ValueChangeHistoryCommand(PropertyModel model, object? oldValue, object? newValue)
            {
                Model = model;
                OldValue = oldValue;
                NewValue = newValue;
            }

            public void Redo()
            {
                Model.Value = NewValue;
            }

            public void Undo()
            {
                Model.Value = OldValue;
            }

            public void Dispose() { }
        }

        private class AddFirstKeyFrameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddKeyFrame);

            PropertyModel Model { get; }

            KeyFrame KeyFrame { get; }

            object? OldValue { get; }

            public AddFirstKeyFrameHistoryCommand(PropertyModel model, KeyFrame keyFrame, object? oldValue)
            {
                Model = model;
                KeyFrame = keyFrame;
                OldValue = oldValue;
            }

            public void Redo()
            {
                Model.KeyFrames.Add(KeyFrame);
            }

            public void Undo()
            {
                Model.KeyFrames.Remove(KeyFrame);
                Model.Value = OldValue;
            }

            public void Dispose() { }
        }

        private class AddKeyFrameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddKeyFrame);

            PropertyModel Model { get; }

            KeyFrame KeyFrame { get; }

            int InsertIndex { get; }

            public AddKeyFrameHistoryCommand(PropertyModel model, KeyFrame keyFrame, int insertIndex)
            {
                Model = model;
                KeyFrame = keyFrame;
                InsertIndex = insertIndex;
            }

            public void Redo()
            {
                Model.KeyFrames.Insert(InsertIndex, KeyFrame);
            }

            public void Undo()
            {
                Model.KeyFrames.Remove(KeyFrame);
            }

            public void Dispose() { }
        }

        private class ReplaceSingleKeyFrameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue);

            PropertyModel Model { get; }

            KeyFrame OldKeyFrame { get; }

            KeyFrame NewKeyFrame { get; }

            int Index { get; }

            public ReplaceSingleKeyFrameHistoryCommand(PropertyModel model, KeyFrame oldKeyFrame, KeyFrame newKeyFrame, int index)
            {
                Model = model;
                OldKeyFrame = oldKeyFrame;
                NewKeyFrame = newKeyFrame;
                Index = index;
            }

            public void Redo()
            {
                Model.KeyFrames[Index] = NewKeyFrame;
            }

            public void Undo()
            {
                Model.KeyFrames[Index] = OldKeyFrame;
            }

            public void Dispose() { }
        }

        private class ClearKeyFramesHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveKeyFrame);

            PropertyModel Model { get; }

            KeyFrame[] KeyFrames { get; }

            public ClearKeyFramesHistoryCommand(PropertyModel model, KeyFrame[] keyFrames)
            {
                Model = model;
                KeyFrames = keyFrames;
            }

            public void Redo()
            {
                Model.KeyFrames.Clear();
            }

            public void Undo()
            {
                foreach (var k in KeyFrames)
                {
                    Model.KeyFrames.Add(k);
                }
            }

            public void Dispose() { }
        }

        private class ReplaceKeyFramesHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(NameKey);

            PropertyModel Model { get; }

            KeyFrame[] OldKeyFrames { get; }

            KeyFrame[] NewKeyFrames { get; }

            string NameKey { get; }

            public ReplaceKeyFramesHistoryCommand(PropertyModel model, KeyFrame[] oldKeyFrames, KeyFrame[] newKeyFrames, string nameKey)
            {
                Model = model;
                OldKeyFrames = oldKeyFrames;
                NewKeyFrames = newKeyFrames;
                NameKey = nameKey;
            }

            public void Redo()
            {
                ReplaceKeyFrames(OldKeyFrames, NewKeyFrames);
            }

            public void Undo()
            {
                ReplaceKeyFrames(NewKeyFrames, OldKeyFrames);
            }

            public void Dispose() { }

            void ReplaceKeyFrames(KeyFrame[] oldKeyFrames, KeyFrame[] newKeyFrames)
            {
                foreach (var k in oldKeyFrames)
                {
                    Model.KeyFrames.Remove(k);
                }

                foreach (var k in newKeyFrames)
                {
                    var index = Model.KeyFrames.IndexOfLast(k => k.Time <= k.Time) + 1;
                    if (index > 0 && Model.KeyFrames[index - 1].Time == k.Time)
                    {
                        Model.KeyFrames[index - 1] = k;
                    }
                    else
                    {
                        Model.KeyFrames.Insert(index, k);
                    }
                }
            }
        }

        private class DeleteKeyFramesHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveKeyFrame);

            PropertyModel Model { get; }

            KeyFrame[] KeyFrames { get; }

            int[] Indices { get; }

            public DeleteKeyFramesHistoryCommand(PropertyModel model, KeyFrame[] keyFrames, int[] indices)
            {
                Model = model;
                KeyFrames = keyFrames;
                Indices = indices;
            }

            public void Redo()
            {
                foreach (var k in KeyFrames)
                {
                    Model.KeyFrames.Remove(k);
                }
            }

            public void Undo()
            {
                foreach (var (k, i) in KeyFrames.Zip(Indices))
                {
                    Model.KeyFrames.Insert(i, k);
                }
            }

            public void Dispose() { }
        }
    }

    partial class PropertyGroupModel
    {
        private class ChangeNameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue);

            PropertyGroupModel Model { get; }

            string OldName { get; }

            string NewName { get; }

            public ChangeNameHistoryCommand(PropertyGroupModel model, string oldName, string newName)
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
    }

    partial class AppendablePropertyModel
    {
        private class AddAppendablePropertyChildHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue);

            AppendablePropertyModel Model { get; }

            PropertyGroupModel Child { get; }

            int Index { get; }

            public AddAppendablePropertyChildHistoryCommand(AppendablePropertyModel model, PropertyGroupModel child, int index)
            {
                Model = model;
                Child = child;
                Index = index;
            }

            public void Redo()
            {
                Model.InsertInternal(Index, Child);
            }

            public void Undo()
            {
                Model.RemoveInternal(Child);
            }

            public void Dispose() { }
        }

        private class DeleteAppendablePropertyChildHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue);

            AppendablePropertyModel Model { get; }

            PropertyGroupModel[] Children { get; }

            int[] Indices { get; }

            public DeleteAppendablePropertyChildHistoryCommand(AppendablePropertyModel model, PropertyGroupModel[] children, int[] indices)
            {
                Model = model;
                Children = children;
                Indices = indices;
            }

            public void Redo()
            {
                foreach (var c in Children)
                {
                    Model.RemoveInternal(c);
                }
            }

            public void Undo()
            {
                foreach (var (c, i) in Children.Zip(Indices))
                {
                    Model.InsertInternal(i, c);
                }
            }

            public void Dispose() { }
        }

        private class MoveAppendablePropertyChildrenHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue);

            AppendablePropertyModel Model { get; }

            IPropertyModel[] PrevOrderedChildren { get; }

            IPropertyModel[] NewOrderedChildren { get; }

            public MoveAppendablePropertyChildrenHistoryCommand(AppendablePropertyModel model, IPropertyModel[] prevOrderedChildren, IPropertyModel[] newOrderedChildren)
            {
                Model = model;
                PrevOrderedChildren = prevOrderedChildren;
                NewOrderedChildren = newOrderedChildren;
            }

            public void Redo()
            {
                Model.Children.SortBy(c => Array.IndexOf(NewOrderedChildren, c));
            }

            public void Undo()
            {
                Model.Children.SortBy(c => Array.IndexOf(PrevOrderedChildren, c));
            }

            public void Dispose() { }
        }
    }
}
