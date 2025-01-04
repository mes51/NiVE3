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
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EnqueueRender);

            RenderQueueModel Model { get; }

            RenderQueueItemModel Queue { get; }

            public EnqueueHistoryCommand(RenderQueueModel model, RenderQueueItemModel queue)
            {
                Model = model;
                Queue = queue;
            }

            public void Redo()
            {
                Model.Items.Add(Queue);
            }

            public void Undo()
            {
                Model.Items.Remove(Queue);
            }

            public void Dispose()
            {
                Queue.Dispose();
            }
        }

        private class RemoveQueuesHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveRenderQueues);

            RenderQueueModel Model { get; }

            RenderQueueItemModel[] Items { get; }

            int[] Indices { get; }

            public RemoveQueuesHistoryCommand(RenderQueueModel model, RenderQueueItemModel[] items, int[] indices)
            {
                Model = model;
                Items = items;
                Indices = indices;
            }

            public void Redo()
            {
                foreach (var item in Items)
                {
                    Model.Items.Remove(item);
                }
            }

            public void Undo()
            {
                foreach (var (item, i) in Items.Zip(Indices))
                {
                    Model.Items.Insert(i, item);
                }
            }

            public void Dispose() { }
        }

        private class DuplicateQueuesHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DuplicateRenderQueues);

            RenderQueueModel Model { get; }

            RenderQueueItemModel[] Items { get; }

            public DuplicateQueuesHistoryCommand(RenderQueueModel model, RenderQueueItemModel[] items)
            {
                Model = model;
                Items = items;
            }

            public void Redo()
            {
                foreach (var i in Items)
                {
                    Model.Items.Add(i);
                }
            }

            public void Undo()
            {
                foreach (var i in Items)
                {
                    Model.Items.Remove(i);
                }
            }

            public void Dispose()
            {
                foreach (var i in Items)
                {
                    i.Dispose();
                }
            }
        }
    }
}
