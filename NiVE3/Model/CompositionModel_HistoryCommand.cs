using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Extension;
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
    }
}
