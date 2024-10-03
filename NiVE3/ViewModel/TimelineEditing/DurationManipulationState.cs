using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
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

        public abstract void Update(double inPointDiff, double outPointDiff, double sourceStartPointDiff);

        public virtual void Commit(double inPointDiff, double outPointDiff, double sourceStartPointDiff)
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
        double DiffTime { get; set; }

        double[] PrevInPoints { get; set; }

        public InPointDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel)
        {
            PrevInPoints = [..layers.Select(l => l.InPoint)];
        }

        public override void Update(double inPointDiff, double outPointDiff, double sourceStartPointDiff)
        {
            DiffTime += inPointDiff;

            foreach (var (layer, prevInPoint) in Layers.Zip(PrevInPoints))
            {
                var max = TimeCalc.AlignFloor(layer.OutPoint - CompositionModel.FrameDuration, CompositionModel.FrameRate);
                if (layer.HasDuration && !layer.IsEnableTimeRemap)
                {
                    max = Math.Max(max, 0.0);
                    layer.InPoint = Math.Min(Math.Max(prevInPoint + DiffTime, 0.0), max);
                }
                else
                {
                    layer.InPoint = Math.Min(prevInPoint + DiffTime, max);
                }
            }
        }
    }

    class OutPointDurationManipulationState : DurationManipulationStateBase
    {
        double DiffTime { get; set; }

        double[] PrevOutPoints { get; set; }

        public OutPointDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel)
        {
            PrevOutPoints = [..layers.Select(l => l.OutPoint)];
        }

        public override void Update(double inPointDiff, double outPointDiff, double sourceStartPointDiff)
        {
            DiffTime += outPointDiff;

            foreach (var (layer, prevOutPoint) in Layers.Zip(PrevOutPoints))
            {
                var min = TimeCalc.AlignFloor(layer.InPoint + CompositionModel.FrameDuration, CompositionModel.FrameRate);

                if (layer.HasDuration && !layer.IsEnableTimeRemap)
                {
                    min = Math.Min(min, layer.Duration);
                    layer.OutPoint = Math.Min(Math.Max(prevOutPoint + DiffTime, min), layer.Duration);
                }
                else
                {
                    layer.OutPoint = Math.Max(prevOutPoint + DiffTime, min);
                }
            }
        }
    }

    class SourceStartPointDurationManipulationState : DurationManipulationStateBase
    {
        public SourceStartPointDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel) { }

        public override void Update(double inPointDiff, double outPointDiff, double sourceStartPointDiff)
        {
            foreach (var layer in Layers)
            {
                layer.SourceStartPoint += sourceStartPointDiff;
            }
        }
    }

    class SlipDurationManipulationState : DurationManipulationStateBase
    {
        double DiffTime { get; set; }

        double[] PrevInPoints { get; }

        double[] PrevOutPoints { get; }

        double[] PrevSourceStartPoints { get; }

        public SlipDurationManipulationState(CompositionModel compositionModel, LayerViewModel[] layers, HistoryModel historyModel) : base(compositionModel, layers, historyModel)
        {
            PrevInPoints = [..layers.Select(l => l.InPoint)];
            PrevOutPoints = [..layers.Select(l => l.OutPoint)];
            PrevSourceStartPoints = [..layers.Select(l => l.SourceStartPoint)];
        }

        public override void Update(double inPointDiff, double outPointDiff, double sourceStartPointDiff)
        {
            DiffTime += sourceStartPointDiff;

            if (DiffTime > 0.0)
            {
                foreach (var (layer, prevInPoint, prevOutPoint, prevSourceStartPoint) in Layers.Zip(PrevInPoints, PrevOutPoints, PrevSourceStartPoints).Where(t => t.First.HasDuration && !t.First.IsEnableTimeRemap))
                {
                    var newInPoint = Math.Max(prevInPoint - DiffTime, 0.0);
                    var newDiffTime = prevInPoint - newInPoint;

                    layer.SourceStartPoint = prevSourceStartPoint + DiffTime;
                    layer.InPoint = newInPoint;
                    layer.OutPoint = Math.Max(prevOutPoint - newDiffTime, newInPoint + CompositionModel.FrameDuration);
                }
            }
            else
            {
                foreach (var (layer, prevInPoint, prevOutPoint, prevSourceStartPoint) in Layers.Zip(PrevInPoints, PrevOutPoints, PrevSourceStartPoints).Where(t => t.First.HasDuration && !t.First.IsEnableTimeRemap))
                {
                    var newOutPoint = Math.Min(prevOutPoint - DiffTime, layer.Duration);
                    var newDiffTime = prevOutPoint - newOutPoint;

                    layer.SourceStartPoint = prevSourceStartPoint + DiffTime;
                    layer.InPoint = Math.Max(prevInPoint - newDiffTime, 0.0);
                    layer.OutPoint = newOutPoint;
                }
            }

            foreach (var (layer, prevInPoint, prevOutPoint, prevSourceStartPoint) in Layers.Zip(PrevInPoints, PrevOutPoints, PrevSourceStartPoints).Where(t => !t.First.HasDuration || t.First.IsEnableTimeRemap))
            {
                layer.SourceStartPoint = prevSourceStartPoint + DiffTime;
                layer.InPoint = prevInPoint - DiffTime;
                layer.OutPoint = prevOutPoint - DiffTime;
            }
        }
    }
}
