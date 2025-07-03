using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;
using NiVE3.ValueObject;
using NiVE3.View.Resource;

namespace NiVE3.Model
{
    partial class CompositionModel
    {
        private class ChangeCompositionSettingHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeCompositionSetting);

            CompositionModel Model { get; }

            string OldName { get; }

            int OldWidth { get; }

            int OldHeight { get; }

            double OldFrameRate { get; }

            Time OldDuration { get; }

            bool OldIsRetentionFrameRate { get; }

            bool OldApplyToneMappingWhenNested { get; }

            int OldShutterAngle { get; }

            int OldShutterPhase { get; }

            int OldMotionBlurSampleCount { get; }

            Guid OldRendererPluginId { get; }

            Guid OldToneMapperPluginId { get; }

            object? OldRendererSetting { get; }

            Int128 OldRendererSettingHash { get; }

            object? OldToneMapperSetting { get; }

            Int128 OldToneMapperSettingHash { get; }

            Time OldWorkareaBegin { get; }

            Time OldWorkareaEnd { get; }

            string NewName { get; }

            int NewWidth { get; }

            int NewHeight { get; }

            double NewFrameRate { get; }

            Time NewDuration { get; }

            bool NewIsRetentionFrameRate { get; }

            bool NewApplyToneMappingWhenNested { get; }

            int NewShutterAngle { get; }

            int NewShutterPhase { get; }

            int NewMotionBlurSampleCount { get; }

            Guid NewRendererPluginId { get; }

            Guid NewToneMapperPluginId { get; }

            object? NewRendererSetting { get; }

            Int128 NewRendererSettingHash { get; }

            object? NewToneMapperSetting { get; }

            Int128 NewToneMapperSettingHash { get; }

            Time NewWorkareaBegin { get; }

            Time NewWorkareaEnd { get; }

            public ChangeCompositionSettingHistoryCommand(
                CompositionModel model,
                string oldName,
                int oldWidth,
                int oldHeight,
                double oldFrameRate,
                Time oldDuration,
                bool oldIsRetentionFrameRate,
                bool oldApplyToneMappingWhenNested,
                int oldShutterAngle,
                int oldShutterPhase,
                int oldMotionBlurSampleCount,
                Guid oldRendererPluginId,
                Guid oldToneMapperPluginId,
                object? oldRendererSetting,
                Int128 oldRendererSettingHash,
                object? oldToneMapperSetting,
                Int128 oldToneMapperSettingHash,
                Time oldWorkareaBegin,
                Time oldWorkareaEnd,
                string newName,
                int newWidth,
                int newHeight,
                double newFrameRate,
                Time newDuration,
                bool newIsRetentionFrameRate,
                bool newApplyToneMappingWhenNested,
                int newShutterAngle,
                int newShutterPhase,
                int newMotionBlurSampleCount,
                Guid newRendererPluginId,
                Guid newToneMapperPluginId,
                object? newRendererSetting,
                Int128 newRendererSettingHash,
                object? newToneMapperSetting,
                Int128 newToneMapperSettingHash,
                Time newWorkareaBegin,
                Time newWorkareaEnd
            )
            {
                Model = model;
                OldName = oldName;
                OldWidth = oldWidth;
                OldHeight = oldHeight;
                OldFrameRate = oldFrameRate;
                OldDuration = oldDuration;
                OldIsRetentionFrameRate = oldIsRetentionFrameRate;
                OldApplyToneMappingWhenNested = oldApplyToneMappingWhenNested;
                OldShutterAngle = oldShutterAngle;
                OldShutterPhase = oldShutterPhase;
                OldMotionBlurSampleCount = oldMotionBlurSampleCount;
                OldRendererPluginId = oldRendererPluginId;
                OldToneMapperPluginId = oldToneMapperPluginId;
                OldRendererSetting = oldRendererSetting;
                OldRendererSettingHash = oldRendererSettingHash;
                OldToneMapperSetting = oldToneMapperSetting;
                OldToneMapperSettingHash = oldToneMapperSettingHash;
                OldWorkareaBegin = oldWorkareaBegin;
                OldWorkareaEnd = oldWorkareaEnd;
                NewName = newName;
                NewWidth = newWidth;
                NewHeight = newHeight;
                NewFrameRate = newFrameRate;
                NewDuration = newDuration;
                NewIsRetentionFrameRate = newIsRetentionFrameRate;
                NewApplyToneMappingWhenNested = newApplyToneMappingWhenNested;
                NewShutterAngle = newShutterAngle;
                NewShutterPhase = newShutterPhase;
                NewMotionBlurSampleCount = newMotionBlurSampleCount;
                NewRendererPluginId = newRendererPluginId;
                NewToneMapperPluginId = newToneMapperPluginId;
                NewRendererSetting = newRendererSetting;
                NewRendererSettingHash = newRendererSettingHash;
                NewToneMapperSetting = newToneMapperSetting;
                NewToneMapperSettingHash = newToneMapperSettingHash;
                NewWorkareaBegin = newWorkareaBegin;
                NewWorkareaEnd = newWorkareaEnd;
            }

            public void Redo()
            {
                Model.IsSettingChanging = true;

                Model.Name = NewName;
                Model.Width = NewWidth;
                Model.Height = NewHeight;
                Model.FrameRate = NewFrameRate;
                Model.Duration = NewDuration;
                Model.IsRetentionFrameRate = NewIsRetentionFrameRate;
                Model.ApplyToneMappingWhenNested = NewApplyToneMappingWhenNested;
                Model.ShutterAngle = NewShutterAngle;
                Model.ShutterPhase = NewShutterPhase;
                Model.MotionBlurSampleCount = NewMotionBlurSampleCount;
                Model.Transformer?.SetSize(NewWidth, NewHeight);

                if (Model.RendererPluginId != NewRendererPluginId)
                {
                    Model.RendererContext.Dispose();
                    Model.RendererContext = Model.RendererListModel.CreateRenderer(NewRendererPluginId);
                    Model.RendererPluginId = NewRendererPluginId;
                    Model.RendererContext.Value.SetSize(NewWidth, NewHeight);
                    Model.RendererContext.Value.LoadSetting(NewRendererSetting);
                    Model.RendererSettingHash = NewRendererSettingHash;
                    Model.Transformer = null;
                }
                else if (Model.RendererSettingHash != NewRendererSettingHash)
                {
                    Model.RendererContext.Value.LoadSetting(NewRendererSetting);
                    Model.RendererSettingHash = NewRendererSettingHash;
                }
                Model.RendererSettingHash = CalcPluginSettingHash(NewRendererSetting);
                if (Model.ToneMapperPluginId != NewRendererPluginId)
                {
                    Model.ToneMapperContext.Dispose();
                    Model.ToneMapperContext = Model.ToneMapperListModel.CreateToneMapper(NewToneMapperPluginId);
                    Model.ToneMapperPluginId = NewToneMapperPluginId;
                    Model.ToneMapperContext.Value.LoadSetting(NewToneMapperSetting);
                    Model.ToneMapperSettingHash = NewToneMapperSettingHash;
                }
                else if (Model.ToneMapperSettingHash != NewToneMapperSettingHash)
                {
                    Model.ToneMapperContext.Value.LoadSetting(NewToneMapperSetting);
                    Model.ToneMapperSettingHash = NewToneMapperSettingHash;
                }

                Model.WorkareaBegin = NewWorkareaBegin;
                Model.WorkareaEnd = NewWorkareaEnd;
                Model.FrameDuration = new Time(1, NewFrameRate);

                if (Model.TimeBarRange > NewDuration)
                {
                    Model.TimeBarRange = NewDuration;
                }
                if (Model.TimeBarRangeStart + Model.TimeBarRange > NewDuration)
                {
                    Model.TimeBarRangeStart = Time.Max(NewDuration - Model.TimeBarRangeStart, Time.Zero);
                }

                if (OldWidth != NewWidth || OldHeight != NewHeight)
                {
                    Model.Renderer.SetSize(Model.Width, Model.Height);
                }

                Model.IsSettingChanging = false;
                Model.OnCompositionUpdated(false);
            }

            public void Undo()
            {
                Model.IsSettingChanging = true;

                Model.Name = OldName;
                Model.Width = OldWidth;
                Model.Height = OldHeight;
                Model.FrameRate = OldFrameRate;
                Model.Duration = OldDuration;
                Model.IsRetentionFrameRate = OldIsRetentionFrameRate;
                Model.ApplyToneMappingWhenNested = OldApplyToneMappingWhenNested;
                Model.ShutterAngle = OldShutterAngle;
                Model.ShutterPhase = OldShutterPhase;
                Model.MotionBlurSampleCount = OldMotionBlurSampleCount;
                Model.Transformer?.SetSize(OldWidth, OldHeight);

                if (Model.RendererPluginId != OldRendererPluginId)
                {
                    Model.RendererContext.Dispose();
                    Model.RendererContext = Model.RendererListModel.CreateRenderer(OldRendererPluginId);
                    Model.RendererPluginId = OldRendererPluginId;
                    Model.RendererContext.Value.SetSize(OldWidth, OldHeight);
                    Model.RendererContext.Value.LoadSetting(OldRendererSetting);
                    Model.RendererSettingHash = OldRendererSettingHash;
                    Model.Transformer = null;
                }
                else if (Model.RendererSettingHash != OldRendererSettingHash)
                {
                    Model.RendererContext.Value.LoadSetting(OldRendererSetting);
                    Model.RendererSettingHash = OldRendererSettingHash;
                }
                if (Model.ToneMapperPluginId != OldRendererPluginId)
                {
                    Model.ToneMapperContext.Dispose();
                    Model.ToneMapperContext = Model.ToneMapperListModel.CreateToneMapper(OldToneMapperPluginId);
                    Model.ToneMapperPluginId = OldToneMapperPluginId;
                    Model.ToneMapperContext.Value.LoadSetting(OldToneMapperSetting);
                    Model.ToneMapperSettingHash = OldToneMapperSettingHash;
                }
                else if (Model.ToneMapperSettingHash != OldToneMapperSettingHash)
                {
                    Model.ToneMapperContext.Value.LoadSetting(OldToneMapperSetting);
                    Model.ToneMapperSettingHash = OldToneMapperSettingHash;
                }

                Model.WorkareaBegin = OldWorkareaBegin;
                Model.WorkareaEnd = OldWorkareaEnd;
                Model.FrameDuration = new Time(1, OldFrameRate);

                if (Model.TimeBarRange > OldDuration)
                {
                    Model.TimeBarRange = OldDuration;
                }
                if (Model.TimeBarRangeStart + Model.TimeBarRange > OldDuration)
                {
                    Model.TimeBarRangeStart = Time.Max(OldDuration - Model.TimeBarRangeStart, Time.Zero);
                }

                if (OldWidth != NewWidth || OldHeight != NewHeight)
                {
                    Model.Renderer.SetSize(Model.Width, Model.Height);
                }

                Model.IsSettingChanging = false;
                Model.OnCompositionUpdated(false);
            }

            public void Dispose() { }
        }

        private class ChangeWorkareaHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeWorkarea);

            CompositionModel Model { get; }

            Time OldWorkareaBegin { get; }

            Time OldWorkareaEnd { get; }

            Time NewWorkareaBegin { get; }

            Time NewWorkareaEnd { get; }

            public ChangeWorkareaHistoryCommand(CompositionModel model, Time oldWorkareaBegin, Time oldWorkareaEnd, Time newWorkareaBegin, Time newWorkareaEnd)
            {
                Model = model;
                OldWorkareaBegin = oldWorkareaBegin;
                OldWorkareaEnd = oldWorkareaEnd;
                NewWorkareaBegin = newWorkareaBegin;
                NewWorkareaEnd = newWorkareaEnd;
            }

            public void Redo()
            {
                Model.WorkareaBegin = NewWorkareaBegin;
                Model.WorkareaEnd = NewWorkareaEnd;
            }

            public void Undo()
            {
                Model.WorkareaBegin = OldWorkareaBegin;
                Model.WorkareaEnd = OldWorkareaEnd;
            }

            public void Dispose() { }
        }

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

            LayerModel? TargetLayer { get; }

            Guid?[] OldValues { get; }

            Guid? NewValue { get; }

            bool OldEnableVideo { get; }

            public ChangeTrackMatteLayerHistoryCommand(LayerModel[] layers, LayerModel? targetLayer, Guid?[] oldValues, Guid? newValue, bool oldEnableVideo)
            {
                Layers = layers;
                TargetLayer = targetLayer;
                OldValues = oldValues;
                NewValue = newValue;
                OldEnableVideo = oldEnableVideo;
            }

            public void Redo()
            {
                foreach (var l in Layers)
                {
                    l.TrackMatteLayerId = NewValue;
                }
                if (TargetLayer != null)
                {
                    TargetLayer.IsEnableVideo = false;
                }
            }

            public void Undo()
            {
                foreach (var (l, t) in Layers.Zip(OldValues))
                {
                    l.TrackMatteLayerId = t;
                }
                if (TargetLayer != null)
                {
                    TargetLayer.IsEnableVideo = OldEnableVideo;
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
            public string Name => LanguageResourceDictionary.Dictionary.GetText(IsCut ? LanguageResourceDictionary.History_CutLayers : LanguageResourceDictionary.History_RemoveLayers);

            CompositionModel CompositionModel { get; }

            LayerModel[] Layers { get; }

            int[] Indices { get; }

            bool IsCut { get; }

            public DeleteLayersHistoryCommand(CompositionModel compositionModel, LayerModel[] layers, int[] indices, bool isCut)
            {
                CompositionModel = compositionModel;
                Layers = layers;
                Indices = indices;
                IsCut = isCut;
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

        private class PasteLayersHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(IsDuplicate ? LanguageResourceDictionary.History_DuplicateLayers : LanguageResourceDictionary.History_PasteLayers);

            CompositionModel Model { get; }

            LayerModel[] NewLayers { get; }

            int InsertStartIndex { get; }

            bool IsDuplicate { get; }

            public PasteLayersHistoryCommand(CompositionModel model, LayerModel[] newLayers, int insertStartIndex, bool isDuplicate)
            {
                Model = model;
                NewLayers = newLayers;
                InsertStartIndex = insertStartIndex;
                IsDuplicate = isDuplicate;
            }

            public void Redo()
            {
                var index = InsertStartIndex;
                foreach (var l in NewLayers)
                {
                    Model.Layers.Insert(index, l);
                    index++;
                }
            }

            public void Undo()
            {
                foreach (var l in NewLayers)
                {
                    Model.Layers.Remove(l);
                }
            }

            public void Dispose()
            {
                foreach (var l in NewLayers)
                {
                    l.Dispose();
                }
            }
        }

        private class SplitLayersHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_SplitLayers);

            CompositionModel Model { get; }

            LayerModel[] TargetLayers { get; }

            Dictionary<Guid, LayerModel> NewLayers { get; }

            Time[] OldOutPoints { get; }

            Time[] NewOutPoints { get; }

            public SplitLayersHistoryCommand(CompositionModel model, LayerModel[] targetLayers, Dictionary<Guid, LayerModel> newLayers, Time[] oldOutPoints, Time[] newOutPoints)
            {
                Model = model;
                TargetLayers = targetLayers;
                NewLayers = newLayers;
                OldOutPoints = oldOutPoints;
                NewOutPoints = newOutPoints;
            }

            public void Redo()
            {
                foreach (var (id, l) in NewLayers)
                {
                    var index = Model.Layers.FindIndex(l => id == l.LayerId);
                    Model.Layers.Insert(index, l);
                }
                foreach (var (l, o ) in TargetLayers.Zip(NewOutPoints))
                {
                    l.OutPoint = o;
                }
            }

            public void Undo()
            {
                foreach (var l in NewLayers.Values)
                {
                    Model.Layers.Remove(l);
                }
                foreach (var (l, o) in TargetLayers.Zip(OldOutPoints))
                {
                    l.OutPoint = o;
                }
            }

            public void Dispose()
            {
                foreach (var l in NewLayers.Values)
                {
                    l.Dispose();
                }
            }
        }

        private class ChangeEnableShyHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeEnableShy);

            CompositionModel Model { get; }

            bool NewState { get; }

            public ChangeEnableShyHistoryCommand(CompositionModel model, bool newState)
            {
                Model = model;
                NewState = newState;
            }

            public void Redo()
            {
                Model.IsEnableShy = NewState;
            }

            public void Undo()
            {
                Model.IsEnableShy = !NewState;
            }

            public void Dispose() { }
        }

        private class ChangeEnableFrameBlendHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeEnableFrameBlend);

            CompositionModel Model { get; }

            bool NewState { get; }

            public ChangeEnableFrameBlendHistoryCommand(CompositionModel model, bool newState)
            {
                Model = model;
                NewState = newState;
            }

            public void Redo()
            {
                Model.IsEnableFrameBlend = NewState;
            }

            public void Undo()
            {
                Model.IsEnableFrameBlend = !NewState;
            }

            public void Dispose() { }
        }

        private class ChangeEnableMotionBlurHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeEnableMotionBlur);

            CompositionModel Model { get; }

            bool NewState { get; }

            public ChangeEnableMotionBlurHistoryCommand(CompositionModel model, bool newState)
            {
                Model = model;
                NewState = newState;
            }

            public void Redo()
            {
                Model.IsEnableMotionBlur = NewState;
            }

            public void Undo()
            {
                Model.IsEnableMotionBlur = !NewState;
            }

            public void Dispose() { }
        }

        private class MoveMarkerHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveCompositionMarker);

            CompositionModel Model { get; }

            Marker OldMarker { get; }

            Marker NewMarker { get; }

            public MoveMarkerHistoryCommand(CompositionModel model, Marker oldMarker, Marker newMarker)
            {
                Model = model;
                OldMarker = oldMarker;
                NewMarker = newMarker;
            }

            public void Redo()
            {
                var index = Model.CompositionMarkers.FindIndex(m => m.Id == OldMarker.Id);

                Model.CompositionMarkers[index] = NewMarker;
                Model.CompositionMarkers.Sort((a, b) => a.Time.CompareTo(b.Time));
            }

            public void Undo()
            {
                var index = Model.CompositionMarkers.FindIndex(m => m.Id == NewMarker.Id);

                Model.CompositionMarkers[index] = OldMarker;
                Model.CompositionMarkers.Sort((a, b) => a.Time.CompareTo(b.Time));
            }

            public void Dispose() { }
        }

        private class MoveAndReplaceMarkerHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveCompositionMarker);

            CompositionModel Model { get; }

            Marker OldMarker { get; }

            Marker NewMarker { get; }

            Marker ReplaceTargetMarker { get; }

            int OldIndex { get; }

            int ReplaceTargetIndex { get; }

            public MoveAndReplaceMarkerHistoryCommand(CompositionModel model, Marker oldMarker, Marker newMarker, Marker replaceTargetMarker, int oldIndex, int replaceTargetIndex)
            {
                Model = model;
                OldMarker = oldMarker;
                NewMarker = newMarker;
                ReplaceTargetMarker = replaceTargetMarker;
                OldIndex = oldIndex;
                ReplaceTargetIndex = replaceTargetIndex;
            }

            public void Redo()
            {
                Model.CompositionMarkers[ReplaceTargetIndex] = NewMarker;
                Model.CompositionMarkers.Remove(OldMarker);
            }

            public void Undo()
            {
                Model.CompositionMarkers.Insert(OldIndex, OldMarker);
                Model.CompositionMarkers[ReplaceTargetIndex] = ReplaceTargetMarker;
            }

            public void Dispose() { }
        }

        private class AddMarkerHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddCompositionMarker);

            CompositionModel Model { get; }

            Marker Marker { get; }

            int InsertIndex { get; }

            public AddMarkerHistoryCommand(CompositionModel model, Marker marker, int insertIndex)
            {
                Model = model;
                Marker = marker;
                InsertIndex = insertIndex;
            }

            public void Redo()
            {
                Model.CompositionMarkers.Insert(InsertIndex, Marker);
            }

            public void Undo()
            {
                Model.CompositionMarkers.Remove(Marker);
            }

            public void Dispose() { }
        }

        private class DeleteMarkerHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DeleteCompositionMarker);

            CompositionModel Model { get; }

            Marker Marker { get; }

            int InsertIndex { get; }

            public DeleteMarkerHistoryCommand(CompositionModel model, Marker marker, int insertIndex)
            {
                Model = model;
                Marker = marker;
                InsertIndex = insertIndex;
            }

            public void Redo()
            {
                Model.CompositionMarkers.Remove(Marker);
            }

            public void Undo()
            {
                Model.CompositionMarkers.Insert(InsertIndex, Marker);
            }

            public void Dispose() { }
        }

        private class ChangeMarkerNameHistoryCommand : IHistoryCommand
        {
            public string Name => LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangeMaskName);

            CompositionModel Model { get; }

            Marker OldMarker { get; }

            Marker NewMarker { get; }

            int Index { get; }

            public ChangeMarkerNameHistoryCommand(CompositionModel model, Marker oldMarker, Marker newMarker, int index)
            {
                Model = model;
                OldMarker = oldMarker;
                NewMarker = newMarker;
                Index = index;
            }

            public void Redo()
            {
                Model.CompositionMarkers[Index] = NewMarker;
            }

            public void Undo()
            {
                Model.CompositionMarkers[Index] = OldMarker;
            }

            public void Dispose() { }
        }
    }
}
