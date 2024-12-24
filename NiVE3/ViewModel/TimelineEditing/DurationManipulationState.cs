using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shared.Extension;
using NiVE3.Util;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel.TimelineEditing
{
    abstract class DurationManipulationStateBase
    {
        protected CompositionModel CompositionModel { get; }

        protected LayerViewModel[] Layers { get; }

        protected HistoryModel HistoryModel { get; }

        protected DurationManipulationStateBase(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel)
        {
            CompositionModel = compositionModel;
            Layers = layers;
            HistoryModel = historyModel;

            foreach (var layer in layers)
            {
                layer.BeginEditDurationCommand.Execute(null);
            }
        }

        public abstract void Update(Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff);

        public virtual void Commit(Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff)
        {
            Update(inPointDiff, outPointDiff, sourceStartPointDiff);

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_EditLayerDuration));

            foreach (var layer in Layers)
            {
                layer.CommitEditDurationCommand.Execute(null);
            }

            HistoryModel.EndGroup();
        }

        public virtual void Abort()
        {
            foreach (var layer in Layers)
            {
                layer.AbortEditDurationCommand.Execute(null);
            }
        }
    }

    class InPointDurationManipulationState : DurationManipulationStateBase
    {
        Time DiffTime { get; set; }

        Time[] PrevInPoints { get; set; }

        public InPointDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel)
        {
            PrevInPoints = [..layers.Select(l => l.InPoint)];
        }

        public override void Update(Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff)
        {
            DiffTime += inPointDiff;

            foreach (var (layer, prevInPoint) in Layers.Zip(PrevInPoints))
            {
                var max = (layer.OutPoint - CompositionModel.FrameDuration).FloorToFrameRate(CompositionModel.FrameRate);
                if (layer.HasDuration && !layer.IsDisableDuration)
                {
                    max = Time.Max(max, Time.Zero);
                    layer.InPoint = Time.MaxAndMin(prevInPoint + DiffTime, Time.Zero, max);
                }
                else
                {
                    layer.InPoint = Time.Min(prevInPoint + DiffTime, max);
                }
            }
        }
    }

    class OutPointDurationManipulationState : DurationManipulationStateBase
    {
        Time DiffTime { get; set; }

        Time[] PrevOutPoints { get; set; }

        public OutPointDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel)
        {
            PrevOutPoints = [..layers.Select(l => l.OutPoint)];
        }

        public override void Update(Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff)
        {
            DiffTime += outPointDiff;

            foreach (var (layer, prevOutPoint) in Layers.Zip(PrevOutPoints))
            {
                var min = (layer.InPoint + CompositionModel.FrameDuration).FloorToFrameRate(CompositionModel.FrameRate);

                if (layer.HasDuration && !layer.IsDisableDuration)
                {
                    min = Time.Min(min, layer.Duration);
                    layer.OutPoint = Time.MaxAndMin(prevOutPoint + DiffTime, min, layer.Duration);
                }
                else
                {
                    layer.OutPoint = Time.Max(prevOutPoint + DiffTime, min);
                }
            }
        }
    }

    class SourceStartPointDurationManipulationState : DurationManipulationStateBase
    {
        public SourceStartPointDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel) { }

        public override void Update(Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff)
        {
            foreach (var layer in Layers)
            {
                layer.SourceStartPoint += sourceStartPointDiff;
            }
        }
    }

    class SlipDurationManipulationState : DurationManipulationStateBase
    {
        Time DiffTime { get; set; }

        Time[] PrevInPoints { get; }

        Time[] PrevOutPoints { get; }

        Time[] PrevSourceStartPoints { get; }

        public SlipDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel)
        {
            PrevInPoints = [..layers.Select(l => l.InPoint)];
            PrevOutPoints = [..layers.Select(l => l.OutPoint)];
            PrevSourceStartPoints = [..layers.Select(l => l.SourceStartPoint)];
        }

        public override void Update(Time inPointDiff, Time outPointDiff, Time sourceStartPointDiff)
        {
            DiffTime += sourceStartPointDiff;

            if (DiffTime > 0.0)
            {
                foreach (var (layer, prevInPoint, prevOutPoint, prevSourceStartPoint) in Layers.Zip(PrevInPoints, PrevOutPoints, PrevSourceStartPoints).Where(t => t.First.HasDuration && !t.First.IsDisableDuration))
                {
                    var newInPoint = Time.Max(prevInPoint - DiffTime, Time.Zero);
                    var newDiffTime = prevInPoint - newInPoint;

                    layer.SourceStartPoint = prevSourceStartPoint + DiffTime;
                    layer.InPoint = newInPoint;
                    layer.OutPoint = Time.Max(prevOutPoint - newDiffTime, newInPoint + CompositionModel.FrameDuration);
                }
            }
            else
            {
                foreach (var (layer, prevInPoint, prevOutPoint, prevSourceStartPoint) in Layers.Zip(PrevInPoints, PrevOutPoints, PrevSourceStartPoints).Where(t => t.First.HasDuration && !t.First.IsDisableDuration))
                {
                    var newOutPoint = Time.Min(prevOutPoint - DiffTime, layer.Duration);
                    var newDiffTime = prevOutPoint - newOutPoint;

                    layer.SourceStartPoint = prevSourceStartPoint + DiffTime;
                    layer.InPoint = Time.Max(prevInPoint - newDiffTime, Time.Zero);
                    layer.OutPoint = newOutPoint;
                }
            }

            foreach (var (layer, prevInPoint, prevOutPoint, prevSourceStartPoint) in Layers.Zip(PrevInPoints, PrevOutPoints, PrevSourceStartPoints).Where(t => !t.First.HasDuration || t.First.IsDisableDuration))
            {
                layer.SourceStartPoint = prevSourceStartPoint + DiffTime;
                layer.InPoint = prevInPoint - DiffTime;
                layer.OutPoint = prevOutPoint - DiffTime;
            }
        }
    }
}
