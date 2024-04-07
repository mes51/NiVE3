using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Interfaces;
using NiVE3.Shared.Extension;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class CompositionModel
    {
        private class AddLayersHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddLayers);

            CompositionModel CompositionModel { get; }

            LayerModel[] Layers { get; }

            int InsertStartIndex { get; }

            public AddLayersHistoryCommand(CompositionModel compositionModel, LayerModel[] layers, int insertStartIndex)
            {
                CompositionModel = compositionModel;
                Layers = layers;
                InsertStartIndex = insertStartIndex;
            }

            public void Redo()
            {
                var index = InsertStartIndex;
                foreach (var l in Layers)
                {
                    CompositionModel.Layers.Insert(index, l);
                    index++;
                }
            }

            public void Undo()
            {
                foreach (var l in Layers.Reverse())
                {
                    CompositionModel.Layers.Remove(l);
                }
            }

            public void Dispose()
            {
                foreach (var l in Layers)
                {
                    l.Dispose();
                }
            }
        }

        private class MoveLayersHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveLayers);

            CompositionModel CompositionModel { get; }

            LayerModel[] Layers { get; }

            int[] PrevIndices { get; }

            LayerModel[] NewOrderedLayers { get; }

            public MoveLayersHistoryCommand(CompositionModel compositionModel, LayerModel[] layers, int[] prevIndices, LayerModel[] newOrderedLayers)
            {
                CompositionModel = compositionModel;
                Layers = layers;
                PrevIndices = prevIndices;
                NewOrderedLayers = newOrderedLayers;
            }

            public void Redo()
            {
                CompositionModel.Layers.SortBy(l => Array.IndexOf(NewOrderedLayers, l));
            }

            public void Undo()
            {
                foreach (var l in Layers)
                {
                    CompositionModel.Layers.Remove(l);
                }

                foreach (var (l, i) in Layers.Zip(PrevIndices))
                {
                    CompositionModel.Layers.Insert(i, l);
                }
            }

            public void Dispose() { }
        }

        private class ChangeLayerSwitchHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeLayerSwitch);

            LayerModel[] Layers { get; }

            PropertyInfo SwitchInfo { get; }

            object?[] OldValues { get; }

            object NewValue { get; }

            public ChangeLayerSwitchHistoryCommand(LayerModel[] layers, PropertyInfo switchInfo, object?[] oldValues, object newValue)
            {
                Layers = layers;
                SwitchInfo = switchInfo;
                OldValues = oldValues;
                NewValue = newValue;
            }

            public void Redo()
            {
                foreach (var l in Layers)
                {
                    SwitchInfo.SetValue(l, NewValue);
                }
            }

            public void Undo()
            {
                foreach (var (l, o) in Layers.Zip(OldValues))
                {
                    SwitchInfo.SetValue(l, o);
                }
            }

            public void Dispose() { }
        }

        private class ChangeBlendModeHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeBlendMode);

            LayerModel[] Layers { get; }

            BlendMode[] OldValues { get; }

            BlendMode NewValue { get; }

            public ChangeBlendModeHistoryCommand(LayerModel[] layers, BlendMode[] oldValues, BlendMode newValue)
            {
                Layers = layers;
                OldValues = oldValues;
                NewValue = newValue;
            }

            public void Redo()
            {
                foreach (var l in Layers)
                {
                    l.BlendMode = NewValue;
                }
            }

            public void Undo()
            {
                foreach (var (l, b) in Layers.Zip(OldValues))
                {
                    l.BlendMode = b;
                }
            }

            public void Dispose() { }
        }

        private class ChangeTrackMatteLayerHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeTrackMatteLayer);

            LayerModel[] Layers { get; }

            Guid?[] OldValues { get; }

            Guid? NewValue { get; }

            public ChangeTrackMatteLayerHistoryCommand(LayerModel[] layers, Guid?[] oldValues, Guid? newValue)
            {
                Layers = layers;
                OldValues = oldValues;
                NewValue = newValue;
            }

            public void Redo()
            {
                foreach (var l in Layers)
                {
                    l.TrackMatteLayerId = NewValue;
                }
            }

            public void Undo()
            {
                foreach (var (l, t) in Layers.Zip(OldValues))
                {
                    l.TrackMatteLayerId = t;
                }
            }

            public void Dispose() { }
        }

        private class ChangeTrackMatteModeHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeTrackMatteMode);

            LayerModel[] Layers { get; }

            TrackMatteMode[] OldValues { get; }

            TrackMatteMode NewValue { get; }

            public ChangeTrackMatteModeHistoryCommand(LayerModel[] layers, TrackMatteMode[] oldValues, TrackMatteMode newValue)
            {
                Layers = layers;
                OldValues = oldValues;
                NewValue = newValue;
            }

            public void Redo()
            {
                foreach (var l in Layers)
                {
                    l.TrackMatteMode = NewValue;
                }
            }

            public void Undo()
            {
                foreach (var (l, m) in Layers.Zip(OldValues))
                {
                    l.TrackMatteMode = m;
                }
            }

            public void Dispose() { }
        }

        private class ChangeParentLayerHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeParentLayer);

            LayerModel[] Layers { get; }

            Guid?[] OldValues { get; }

            Guid? NewValue { get; }

            public ChangeParentLayerHistoryCommand(LayerModel[] layers, Guid?[] oldValues, Guid? newValue)
            {
                Layers = layers;
                OldValues = oldValues;
                NewValue = newValue;
            }

            public void Redo()
            {
                foreach (var l in Layers)
                {
                    l.ParentLayerId = null;
                }
                foreach (var l in Layers)
                {
                    l.ParentLayerId = NewValue;
                }
            }

            public void Undo()
            {
                foreach (var l in Layers)
                {
                    l.ParentLayerId = null;
                }
                foreach (var (l, p) in Layers.Zip(OldValues))
                {
                    l.ParentLayerId = p;
                }
            }

            public void Dispose() { }
        }

        private class DeleteLayersHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DeleteLayers);

            CompositionModel CompositionModel { get; }

            LayerModel[] Layers { get; }

            int[] Indices { get; }

            public DeleteLayersHistoryCommand(CompositionModel compositionModel, LayerModel[] layers, int[] indices)
            {
                CompositionModel = compositionModel;
                Layers = layers;
                Indices = indices;
            }

            public void Redo()
            {
                foreach (var l in Layers)
                {
                    CompositionModel.Layers.Remove(l);
                }
            }

            public void Undo()
            {
                foreach (var (l, i) in Layers.Zip(Indices))
                {
                    CompositionModel.Layers.Insert(i, l);
                }
            }

            public void Dispose() { }
        }
    }
}
