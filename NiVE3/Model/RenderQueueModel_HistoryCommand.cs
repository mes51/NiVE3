using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class RenderQueueModel
    {
        private class EnqueueHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EnqueueRendering);

            RenderQueueModel Model { get; }

            RenderQueueItemModel Item { get; }

            public EnqueueHistoryCommand(RenderQueueModel model, RenderQueueItemModel item)
            {
                Model = model;
                Item = item;
            }

            public void Undo()
            {
                Model.Queue.Remove(Item);
            }

            public void Redo()
            {
                Model.Queue.Add(Item);
            }

            public void Dispose()
            {
                Item.Dispose();
            }
        }

        private class DeleteRenderQueueHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DeleteRenderQueue);

            RenderQueueModel Model { get; }

            RenderQueueItemModel[] Items { get; }

            int[] Indices { get; }

            public DeleteRenderQueueHistoryCommand(RenderQueueModel model, RenderQueueItemModel[] items, int[] indices)
            {
                Model = model;
                Items = items;
                Indices = indices;
            }

            public void Undo()
            {
                foreach (var (q, i) in Items.Zip(Indices))
                {
                    Model.Queue.Insert(i, q);
                }
            }

            public void Redo()
            {
                foreach (var item in Items)
                {
                    Model.Queue.Remove(item);
                }
            }

            public void Dispose() { }
        }
    }
}
