using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
