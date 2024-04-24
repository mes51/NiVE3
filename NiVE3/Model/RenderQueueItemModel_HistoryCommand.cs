using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Interfaces;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class RenderQueueItemModel
    {
        private class ChangeOutputPluginHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueSetting);

            RenderQueueItemModel Model { get; }

            Guid OldSelectedOutputPluginId { get; }

            ExportLifetimeContext<IOutput> OldPlugin { get; }

            bool OldHasOutputSetting { get; }

            Guid NewSelectedOutputPluginId { get; }

            ExportLifetimeContext<IOutput> NewPlugin { get; }

            bool NewHasOutputSetting { get; }

            public ChangeOutputPluginHistoryCommand(RenderQueueItemModel model, Guid oldSelectedOutputPluginId, ExportLifetimeContext<IOutput> oldPlugin, bool oldHasOutputSetting, Guid newSelectedOutputPluginId, ExportLifetimeContext<IOutput> newPlugin, bool newHasOutputSetting)
            {
                Model = model;
                OldSelectedOutputPluginId = oldSelectedOutputPluginId;
                OldPlugin = oldPlugin;
                OldHasOutputSetting = oldHasOutputSetting;
                NewSelectedOutputPluginId = newSelectedOutputPluginId;
                NewPlugin = newPlugin;
                NewHasOutputSetting = newHasOutputSetting;
            }

            public void Redo()
            {
                Model.SelectedOutputPluginId = NewSelectedOutputPluginId;
                Model.Output = NewPlugin;
                Model.HasOutputSetting = NewHasOutputSetting;
            }

            public void Undo()
            {
                Model.SelectedOutputPluginId = OldSelectedOutputPluginId;
                Model.Output = OldPlugin;
                Model.HasOutputSetting = OldHasOutputSetting;
            }

            public void Dispose()
            {
                NewPlugin?.Dispose();
            }
        }

        private class ChangeFilePathHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueSetting);

            RenderQueueItemModel Model { get; }

            string OldFilePath { get; }

            string NewFilePath { get; }

            public ChangeFilePathHistoryCommand(RenderQueueItemModel model, string oldFilePath, string newFilePath)
            {
                Model = model;
                OldFilePath = oldFilePath;
                NewFilePath = newFilePath;
            }

            public void Redo()
            {
                Model.FilePath = NewFilePath;
            }

            public void Undo()
            {
                Model.FilePath = OldFilePath;
            }

            public void Dispose() { }
        }

        private class ChangeOutputSettingHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueSetting);

            RenderQueueItemModel Model { get; }

            object? OldSetting { get; }

            object? NewSetting { get; }

            public ChangeOutputSettingHistoryCommand(RenderQueueItemModel model, object? oldSetting, object? newSetting)
            {
                Model = model;
                OldSetting = oldSetting;
                NewSetting = newSetting;
            }

            public void Redo()
            {
                Model.Output.Value.LoadData(NewSetting);
            }

            public void Undo()
            {
                Model.Output.Value.LoadData(OldSetting);
            }

            public void Dispose() { }
        }

        private class ChangeOutputTargetSourceHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueSetting);

            RenderQueueItemModel Model { get; }

            bool OldIsOutputVideo { get; }

            bool OldIsOutputAudio { get; }

            bool NewIsOutputVideo { get; }

            bool NewIsOutputAudio { get; }

            public ChangeOutputTargetSourceHistoryCommand(RenderQueueItemModel model, bool oldIsOutputVideo, bool oldIsOutputAudio, bool newIsOutputVideo, bool newIsOutputAudio)
            {
                Model = model;
                OldIsOutputVideo = oldIsOutputVideo;
                OldIsOutputAudio = oldIsOutputAudio;
                NewIsOutputVideo = newIsOutputVideo;
                NewIsOutputAudio = newIsOutputAudio;
            }

            public void Redo()
            {
                Model.IsOutputVideo = NewIsOutputVideo;
                Model.IsOutputAudio = NewIsOutputAudio;
            }

            public void Undo()
            {
                Model.IsOutputVideo = OldIsOutputVideo;
                Model.IsOutputAudio = OldIsOutputAudio;
            }

            public void Dispose() { }
        }

        private class ChangeUseRenderQueueItemTimeRangeHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueSetting);

            RenderQueueItemModel Model { get; }

            bool OldUseRenderQueueItemTimeRange { get; }

            bool NewUseRenderQueueItemTimeRange { get; }

            public ChangeUseRenderQueueItemTimeRangeHistoryCommand(RenderQueueItemModel model, bool oldUseRenderQueueItemTimeRange, bool newUseRenderQueueItemTimeRange)
            {
                Model = model;
                OldUseRenderQueueItemTimeRange = oldUseRenderQueueItemTimeRange;
                NewUseRenderQueueItemTimeRange = newUseRenderQueueItemTimeRange;
            }

            public void Redo()
            {
                Model.UseRenderQueueItemTimeRange = NewUseRenderQueueItemTimeRange;
            }

            public void Undo()
            {
                Model.UseRenderQueueItemTimeRange = OldUseRenderQueueItemTimeRange;
            }

            public void Dispose() { }
        }

        private class ChangeRenderTimeRangeHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueSetting);

            RenderQueueItemModel Model { get; }

            double OldBeginTime { get; }

            double OldEndTime { get; }

            double NewBeginTime { get; }

            double NewEndTime { get; }

            public ChangeRenderTimeRangeHistoryCommand(RenderQueueItemModel model, double oldBeginTime, double oldEndTime, double newBeginTime, double newEndTime)
            {
                Model = model;
                OldBeginTime = oldBeginTime;
                OldEndTime = oldEndTime;
                NewBeginTime = newBeginTime;
                NewEndTime = newEndTime;
            }

            public void Redo()
            {
                Model.BeginTime = NewBeginTime;
                Model.EndTime = NewEndTime;
            }

            public void Undo()
            {
                Model.BeginTime = OldBeginTime;
                Model.EndTime = OldEndTime;
            }

            public void Dispose() { }
        }
    }
}
