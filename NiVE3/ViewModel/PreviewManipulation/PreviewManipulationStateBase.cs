using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Extension;
using NiVE3.Model;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.View.Resource;

namespace NiVE3.ViewModel.PreviewManipulation
{
    abstract class PreviewManipulationStateBase
    {
        protected double Time { get; }

        protected CompositionModel CompositionModel { get; }

        protected CameraSetting CameraSetting { get; }

        protected Vector2d StartScreenPosition { get; }

        protected HistoryModel HistoryModel { get; }

        protected PreviewManipulationStateBase(double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel)
        {
            Time = time;
            CompositionModel = compositionModel;
            CameraSetting = cameraSetting;
            StartScreenPosition = startScreenPosition;
            HistoryModel = historyModel;
        }

        public abstract void Update(Vector2d screenPos);

        public abstract void Commit(Vector2d screenPos);

        public abstract void Abort();
    }

    class PositionPreviewManipulationState : PreviewManipulationStateBase
    {
        (bool isEnable3d, PropertyViewModel property)[] Properties { get; }

        Vector3d[] PrevPositions { get; }

        Vector3d StartPosition { get; }

        LayerSkeleton? GrabbingLayerSkeleton { get; }

        public PositionPreviewManipulationState(LayerViewModel[] layers, LayerSkeleton? grabbingLayerSkeleton, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel)
            : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var properties = new List<(bool, PropertyViewModel)>();
            var prevPositions = new List<Vector3d>();
            foreach (var layer in layers)
            {
                var property = layer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPositionId) as PropertyViewModel;
                if (property != null)
                {
                    var position = (Vector3d)(property.CurrentTimeValue ?? Vector3d.Zero);
                    property.BeginEditCommand.Execute(null);

                    properties.Add((layer.IsEnable3D, property));
                    prevPositions.Add(position);
                }
            }

            GrabbingLayerSkeleton = grabbingLayerSkeleton?.IsEnable3D ?? false ? grabbingLayerSkeleton : null;
            Properties = [..properties];
            PrevPositions = [..prevPositions];
            StartPosition = compositionModel.Unproject(cameraSetting, startScreenPosition.X, startScreenPosition.Y, GrabbingLayerSkeleton);
        }

        public override void Update(Vector2d screenPos)
        {
            var newPosition = CompositionModel.Unproject(CameraSetting, screenPos.X, screenPos.Y, GrabbingLayerSkeleton);
            var diff3D = newPosition - StartPosition;
            var diff2D = screenPos - StartScreenPosition;
            if (double.IsNaN(diff3D.X) || double.IsNaN(diff3D.Y) || double.IsNaN(diff3D.Z))
            {
                return;
            }
            foreach (var ((isEnable3d, property), prev) in Properties.Zip(PrevPositions))
            {
                if (isEnable3d)
                {
                    property.CurrentTimeValue = prev + diff3D;
                }
                else
                {
                    property.CurrentTimeValue = prev + (Vector3d)diff2D;
                }
            }
        }

        public override void Commit(Vector2d screenPos)
        {
            Update(screenPos);

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue));
            foreach (var (_, property) in Properties)
            {
                property.EndEditCommand.Execute(null);
            }
            HistoryModel.EndGroup();
        }

        public override void Abort()
        {
            foreach (var (_, property) in Properties)
            {
                property.AbortEditCommand.Execute(null);
            }
        }
    }

    class RotateAllPreviewManipulationState : PreviewManipulationStateBase
    {
        PropertyViewModel[] Properties { get; }

        Vector3d[] PrevDirections { get; }

        public RotateAllPreviewManipulationState(LayerViewModel[] layers, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel)
            : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var properties = new List<PropertyViewModel>();
            var prevRotations = new List<Vector3d>();
            foreach (var layer in layers)
            {
                var direction = layer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformDirectionId);
                if (direction is PropertyViewModel vm)
                {
                    prevRotations.Add((Vector3d)(vm.CurrentTimeValue ?? Vector3d.Zero));
                    vm.BeginEditCommand.Execute(null);
                    properties.Add(vm);
                }
            }

            Properties = [..properties];
            PrevDirections = [..prevRotations];
        }

        public override void Update(Vector2d screenPos)
        {
            var diff = (Vector3d)(screenPos - StartScreenPosition) * 0.2;
            diff = new Vector3d(diff.Y, -diff.X, diff.Z);
            foreach (var (direction, prev) in Properties.Zip(PrevDirections))
            {
                var dir = prev + diff;
                while (dir.X < 0.0 || dir.Y < 0.0 || dir.Z < 0.0)
                {
                    dir = (dir + new Vector3d(360.0)) % 360.0;
                }
                direction.CurrentTimeValue = dir;
            }
        }

        public override void Commit(Vector2d screenPos)
        {
            Update(screenPos);

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue));
            foreach (var property in Properties)
            {
                property.EndEditCommand.Execute(null);
            }
            HistoryModel.EndGroup();
        }

        public override void Abort()
        {
            foreach (var property in Properties)
            {
                property.AbortEditCommand.Execute(null);
            }
        }
    }
}
