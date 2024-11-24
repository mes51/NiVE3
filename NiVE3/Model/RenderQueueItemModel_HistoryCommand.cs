using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class RenderQueueItemModel
    {
        private class ChangeSettingHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueItemSetting);

            RenderQueueItemModel Model { get; }

            string OldFilePath { get; }

            RenderRangeType OldRenderRangeType { get; }

            double OldBeginTime { get; }

            double OldEndTime { get; }

            bool OldIsOutputVideo { get; }

            bool OldIsOutputAudio { get; }

            object? OldSetting { get; }

            string NewFilePath { get; }

            RenderRangeType NewRenderRangeType { get; }

            double NewBeginTime { get; } 

            double NewEndTime { get; }

            bool NewIsOutputVideo { get; }

            bool NewIsOutputAudio { get; }

            object? NewSetting { get; }

            public ChangeSettingHistoryCommand(
                RenderQueueItemModel model,
                string oldFilePath,
                RenderRangeType oldRenderRangeType,
                double oldBeginTime,
                double oldEndTime,
                bool oldIsOutputVideo,
                bool oldIsOutputAudio,
                object? oldSetting,
                string newFilePath,
                RenderRangeType newRenderRangeType,
                double newBeginTime,
                double newEndTime,
                bool newIsOutputVideo,
                bool newIsOutputAudio,
                object? newSetting
            )
            {
                Model = model;
                OldFilePath = oldFilePath;
                OldRenderRangeType = oldRenderRangeType;
                OldBeginTime = oldBeginTime;
                OldEndTime = oldEndTime;
                OldIsOutputVideo = oldIsOutputVideo;
                OldIsOutputAudio = oldIsOutputAudio;
                OldSetting = oldSetting;
                NewFilePath = newFilePath;
                NewRenderRangeType = newRenderRangeType;
                NewBeginTime = newBeginTime;
                NewEndTime = newEndTime;
                NewIsOutputVideo = newIsOutputVideo;
                NewIsOutputAudio = newIsOutputAudio;
                NewSetting = newSetting;
            }

            public void Redo()
            {
                Model.FilePath = NewFilePath;
                Model.RenderRangeType = NewRenderRangeType;
                Model.BeginTime = NewBeginTime;
                Model.EndTime = NewEndTime;
                Model.IsOutputVideo = NewIsOutputVideo;
                Model.IsOutputAudio = NewIsOutputAudio;
                Model.Output?.Value?.LoadSetting(NewSetting);
            }

            public void Undo()
            {
                Model.FilePath = OldFilePath;
                Model.RenderRangeType = OldRenderRangeType;
                Model.BeginTime = OldBeginTime;
                Model.EndTime = OldEndTime;
                Model.IsOutputVideo = OldIsOutputVideo;
                Model.IsOutputAudio = OldIsOutputAudio;
                Model.Output?.Value?.LoadSetting(OldSetting);
            }

            public void Dispose() { }
        }

        private class ChangeSettingWithNewOutputHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueItemSetting);

            RenderQueueItemModel Model { get; }

            string OldFilePath { get; }

            RenderRangeType OldRenderRangeType { get; }

            double OldBeginTime { get; }

            double OldEndTime { get; }

            bool OldIsOutputVideo { get; }

            bool OldIsOutputAudio { get; }

            IOutputMetadata? OldOutputMetadata { get; }

            ExportLifetimeContext<IOutput>? OldOutput { get; }

            string NewFilePath { get; }

            RenderRangeType NewRenderRangeType { get; }

            double NewBeginTime { get; }

            double NewEndTime { get; }

            bool NewIsOutputVideo { get; }

            bool NewIsOutputAudio { get; }

            IOutputMetadata? NewOutputMetadata { get; }

            ExportLifetimeContext<IOutput>? NewOutput { get; }

            public ChangeSettingWithNewOutputHistoryCommand(
                RenderQueueItemModel model,
                string oldFilePath,
                RenderRangeType oldRenderRangeType,
                double oldBeginTime,
                double oldEndTime,
                bool oldIsOutputVideo,
                bool oldIsOutputAudio,
                IOutputMetadata? oldOutputMetadata,
                ExportLifetimeContext<IOutput>? oldOutput,
                string newFilePath,
                RenderRangeType newRenderRangeType,
                double newBeginTime,
                double newEndTime,
                bool newIsOutputVideo,
                bool newIsOutputAudio,
                IOutputMetadata? newOutputMetadata,
                ExportLifetimeContext<IOutput>? newOutput
            )
            {
                Model = model;
                OldFilePath = oldFilePath;
                OldRenderRangeType = oldRenderRangeType;
                OldBeginTime = oldBeginTime;
                OldEndTime = oldEndTime;
                OldIsOutputVideo = oldIsOutputVideo;
                OldIsOutputAudio = oldIsOutputAudio;
                OldOutputMetadata = oldOutputMetadata;
                OldOutput = oldOutput;
                NewFilePath = newFilePath;
                NewRenderRangeType = newRenderRangeType;
                NewBeginTime = newBeginTime;
                NewEndTime = newEndTime;
                NewIsOutputVideo = newIsOutputVideo;
                NewIsOutputAudio = newIsOutputAudio;
                NewOutputMetadata = newOutputMetadata;
                NewOutput = newOutput;
            }

            public void Redo()
            {
                Model.FilePath = NewFilePath;
                Model.RenderRangeType = NewRenderRangeType;
                Model.BeginTime = NewBeginTime;
                Model.EndTime = NewEndTime;
                Model.IsOutputVideo = NewIsOutputVideo;
                Model.IsOutputAudio = NewIsOutputAudio;
                Model.OutputPluginId = NewOutputMetadata != null ? Guid.Parse(NewOutputMetadata.OutputUuid) : Guid.Empty;
                Model.OutputPluginName = NewOutputMetadata?.Name ?? "";
                Model.Output = NewOutput;
            }

            public void Undo()
            {
                Model.FilePath = OldFilePath;
                Model.RenderRangeType = OldRenderRangeType;
                Model.BeginTime = OldBeginTime;
                Model.EndTime = OldEndTime;
                Model.IsOutputVideo = OldIsOutputVideo;
                Model.IsOutputAudio = OldIsOutputAudio;
                Model.OutputPluginId = OldOutputMetadata != null ? Guid.Parse(OldOutputMetadata.OutputUuid) : Guid.Empty;
                Model.OutputPluginName = OldOutputMetadata?.Name ?? "";
                Model.Output = OldOutput;
            }

            public void Dispose()
            {
                NewOutput?.Dispose();
            }
        }

        private class ChangeFilePathHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueItemSetting);

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

        private class UpdateStateFromReadyHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeRenderQueueItemSetting);

            RenderQueueItemModel Model { get; }

            RenderQueueItemState NewState { get; }

            public UpdateStateFromReadyHistoryCommand(RenderQueueItemModel model, RenderQueueItemState newState)
            {
                Model = model;
                NewState = newState;
            }

            public void Redo()
            {
                Model.State = NewState;
            }

            public void Undo()
            {
                Model.State = RenderQueueItemState.Ready;
            }

            public void Dispose() { }
        }
    }
}
