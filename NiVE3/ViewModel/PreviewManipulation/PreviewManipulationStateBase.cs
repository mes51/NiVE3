using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
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
        protected const double ChangeRate = 0.2;

        protected abstract PropertyViewModel[] Properties { get; }

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

        public virtual void Commit(Vector2d screenPos)
        {
            Update(screenPos);

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ChangePropertyValue));
            foreach (var property in Properties)
            {
                property.EndEditCommand.Execute(null);
            }
            HistoryModel.EndGroup();
        }

        public virtual void Abort()
        {
            foreach (var property in Properties)
            {
                property.AbortEditCommand.Execute(null);
            }
        }
    }

    class PositionPreviewManipulationState : PreviewManipulationStateBase
    {
        (bool isEnable3d, PropertyViewModel property)[] PositionProperties { get; }

        Vector3d[] PrevPositions { get; }

        Vector3d StartPosition { get; }

        LayerSkeleton GrabbingLayerSkeleton { get; }

        protected override PropertyViewModel[] Properties => PositionProperties.Select(t => t.property).ToArray();

        public PositionPreviewManipulationState(LayerViewModel[] layers, LayerSkeleton grabbingLayerSkeleton, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel)
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

            GrabbingLayerSkeleton = grabbingLayerSkeleton;
            PositionProperties = [..properties];
            PrevPositions = [..prevPositions];
            StartPosition = compositionModel.Unprojection(cameraSetting, GrabbingLayerSkeleton, startScreenPosition);
        }

        public override void Update(Vector2d screenPos)
        {
            var newPosition = CompositionModel.Unprojection(CameraSetting, GrabbingLayerSkeleton, screenPos);
            var diff3D = newPosition - StartPosition;
            var diff2D = screenPos - StartScreenPosition;
            if (double.IsNaN(diff3D.X) || double.IsNaN(diff3D.Y) || double.IsNaN(diff3D.Z))
            {
                return;
            }
            foreach (var ((isEnable3d, property), prev) in PositionProperties.Zip(PrevPositions))
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
    }

    class RotateAllPreviewManipulationState : PreviewManipulationStateBase
    {
        protected override PropertyViewModel[] Properties { get; }

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
            var diff = (Vector3d)(screenPos - StartScreenPosition) * ChangeRate;
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
    }

    class RotateXPreviewManipulationState : PreviewManipulationStateBase
    {
        protected override PropertyViewModel[] Properties { get; }

        double[] PrevX { get; }

        public RotateXPreviewManipulationState(LayerViewModel[] layers, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel) : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var properties = new List<PropertyViewModel>();
            var prevX = new List<double>();
            foreach (var layer in layers)
            {
                var z = layer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformXAngleId);
                if (z is PropertyViewModel vm)
                {
                    prevX.Add((double)(vm.CurrentTimeValue ?? 0.0));
                    vm.BeginEditCommand.Execute(null);
                    properties.Add(vm);
                }
            }

            Properties = [..properties];
            PrevX = [..prevX];
        }

        public override void Update(Vector2d screenPos)
        {
            var diff = (screenPos.Y - StartScreenPosition.Y) * ChangeRate;
            foreach (var (property, prev) in Properties.Zip(PrevX))
            {
                property.CurrentTimeValue = prev + diff;
            }
        }
    }

    class RotateYPreviewManipulationState : PreviewManipulationStateBase
    {
        protected override PropertyViewModel[] Properties { get; }

        double[] PrevY { get; }

        public RotateYPreviewManipulationState(LayerViewModel[] layers, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel) : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var properties = new List<PropertyViewModel>();
            var prevX = new List<double>();
            foreach (var layer in layers)
            {
                var z = layer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformYAngleId);
                if (z is PropertyViewModel vm)
                {
                    prevX.Add((double)(vm.CurrentTimeValue ?? 0.0));
                    vm.BeginEditCommand.Execute(null);
                    properties.Add(vm);
                }
            }

            Properties = [.. properties];
            PrevY = [.. prevX];
        }

        public override void Update(Vector2d screenPos)
        {
            var diff = (screenPos.X - StartScreenPosition.X) * ChangeRate;
            foreach (var (property, prev) in Properties.Zip(PrevY))
            {
                property.CurrentTimeValue = prev - diff;
            }
        }
    }

    class RotateZPreviewManipulationState : PreviewManipulationStateBase
    {
        protected override PropertyViewModel[] Properties { get; }

        double[] PrevZ { get; }

        Vector2d AnchorPoint { get; }

        double PrevPointRadian { get; set; }

        public RotateZPreviewManipulationState(LayerViewModel[] layers, LayerSkeleton grabbingLayerSkeleton, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel) : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var properties = new List<PropertyViewModel>();
            var prevZ = new List<double>();
            foreach (var layer in layers)
            {
                var z = layer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformZAngleId);
                if (z is PropertyViewModel vm)
                {
                    prevZ.Add((double)(vm.CurrentTimeValue ?? 0.0));
                    vm.BeginEditCommand.Execute(null);
                    properties.Add(vm);
                }
            }

            Properties = [..properties];
            PrevZ = [..prevZ];

            var anchorPoint = (Vector3d)(grabbingLayerSkeleton.Transform[ILayerObject.TransformAnchorPointId] ?? Vector3d.Zero);
            AnchorPoint = compositionModel.Projection(cameraSetting, grabbingLayerSkeleton, anchorPoint);
            var prevPoint = startScreenPosition - AnchorPoint;
            PrevPointRadian = Math.Atan2(prevPoint.Y, prevPoint.X);
        }

        public override void Update(Vector2d screenPos)
        {
            var pos = screenPos - AnchorPoint;
            var prevSin = Math.Sin(-PrevPointRadian);
            var prevCos = Math.Cos(-PrevPointRadian);
            var rotated = new Vector2d(
                pos.X * prevCos - pos.Y * prevSin,
                pos.X * prevSin + pos.Y * prevCos
            );

            var rad = Math.Atan2(rotated.Y, rotated.X);
            var diffAngle = rad / Math.PI * 180.0;
            for (var i = 0; i < Properties.Length; i++)
            {
                var newAngle = PrevZ[i] + diffAngle;
                Properties[i].CurrentTimeValue = newAngle;
                PrevZ[i] = newAngle;
            }

            PrevPointRadian = Math.Atan2(pos.Y, pos.X);
        }
    }

    class ScalePreviewManipulationState : PreviewManipulationStateBase
    {
        const double DefaultInvalidScale = 1000000.0;

        protected override PropertyViewModel[] Properties { get; }

        Vector3d[] PrevScale { get; }

        Vector3d AnchorPoint { get; }

        Vector2d LayerGrabPosition { get; }

        LayerSkeleton GrabbingLayerSkeleton { get; }

        public ScalePreviewManipulationState(LayerViewModel[] layers, LayerSkeleton grabbingLayerSkeleton, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel) : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var properties = new List<PropertyViewModel>();
            var prevScale = new List<Vector3d>();
            foreach (var layer in layers)
            {
                var scale = layer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformScaleId);
                if (scale is PropertyViewModel vm)
                {
                    prevScale.Add((Vector3d)(vm.CurrentTimeValue ?? Vector3d.Zero));
                    vm.BeginEditCommand.Execute(null);
                    properties.Add(vm);
                }
            }

            Properties = [..properties];
            PrevScale = [..prevScale];
            AnchorPoint = (Vector3d)(grabbingLayerSkeleton.Transform[ILayerObject.TransformAnchorPointId] ?? Vector3d.Zero);
            GrabbingLayerSkeleton = grabbingLayerSkeleton;
            LayerGrabPosition = (Vector2d)(compositionModel.Unprojection(cameraSetting, grabbingLayerSkeleton, startScreenPosition) - AnchorPoint);
        }

        public override void Update(Vector2d screenPos)
        {
            var newPosition = (Vector2d)(CompositionModel.Unprojection(CameraSetting, GrabbingLayerSkeleton, screenPos) - AnchorPoint);
            var rate = newPosition / LayerGrabPosition;
            if (double.IsNaN(rate.X))
            {
                rate = new Vector2d(DefaultInvalidScale, rate.X);
            }
            else if (double.IsInfinity(rate.X))
            {
                rate = new Vector2d(DefaultInvalidScale * Math.Sign(rate.X), rate.Y);
            }
            if (double.IsNaN(rate.Y))
            {
                rate = new Vector2d(rate.X, DefaultInvalidScale);
            }
            else if (double.IsInfinity(rate.Y))
            {
                rate = new Vector2d(rate.X, DefaultInvalidScale * Math.Sign(rate.Y));
            }

            foreach (var (property, prev) in Properties.Zip(PrevScale))
            {
                property.CurrentTimeValue = prev * new Vector3d(rate, 1.0);
            }
        }
    }

    class CameraOrbitPreviewManipulationState : PreviewManipulationStateBase
    {
        protected override PropertyViewModel[] Properties { get; }

        Vector3d StartCameraPosition { get; }

        Vector3d CameraPointOfInterest { get; }

        public CameraOrbitPreviewManipulationState(LayerViewModel targetCameraLayer, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel) : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var cameraPositionProperty = targetCameraLayer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPositionId);
            var cameraPoiProperty = targetCameraLayer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPointOfInterestId);
            if (cameraPositionProperty is PropertyViewModel posVm && cameraPoiProperty is PropertyViewModel poiVm)
            {
                StartCameraPosition = (Vector3d)(posVm.CurrentTimeValue ?? Vector3d.Zero);
                CameraPointOfInterest = (Vector3d)(poiVm.CurrentTimeValue ?? Vector3d.Zero);

                posVm.BeginEditCommand.Execute(null);

                Properties = [posVm];
            }
            else
            {
                Properties = [];
            }
        }

        public override void Update(Vector2d screenPos)
        {
            var diff = (screenPos - StartScreenPosition) * ChangeRate;
            var rotate = Matrix4x4d.CreateTranslate(-CameraPointOfInterest.X, -CameraPointOfInterest.Y, -CameraPointOfInterest.Z)
                .RotateY(diff.X)
                .RotateX(-diff.Y)
                .Translate(CameraPointOfInterest.X, CameraPointOfInterest.Y, CameraPointOfInterest.Z);

            var move = rotate.Transform(StartCameraPosition.ToHomogeneousCoord());
            move /= move.GetElement(3);

            foreach (var property in Properties)
            {
                property.CurrentTimeValue = (Vector3d)move;
            }
        }
    }

    class CameraPanPreviewManipulationState : PreviewManipulationStateBase
    {
        protected override PropertyViewModel[] Properties { get; }

        Vector3d StartCameraPosition { get; }

        Vector3d StartCameraPointOfInterest { get; }

        Vector3d StartPosition { get; }

        PropertyViewModel? PositionProperty { get; }

        PropertyViewModel? PointOfInterestProperty { get; }

        public CameraPanPreviewManipulationState(LayerViewModel targetCameraLayer, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel) : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var cameraPositionProperty = targetCameraLayer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPositionId);
            var cameraPoiProperty = targetCameraLayer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPointOfInterestId);
            if (cameraPositionProperty is PropertyViewModel posVm && cameraPoiProperty is PropertyViewModel poiVm)
            {
                StartCameraPosition = (Vector3d)(posVm.CurrentTimeValue ?? Vector3d.Zero);
                StartCameraPointOfInterest = (Vector3d)(poiVm.CurrentTimeValue ?? Vector3d.Zero);

                posVm.BeginEditCommand.Execute(null);
                poiVm.BeginEditCommand.Execute(null);

                PointOfInterestProperty = poiVm;
                PositionProperty = posVm;
                Properties = [posVm, poiVm];
                StartPosition = compositionModel.Unprojection(cameraSetting, null, startScreenPosition);
            }
            else
            {
                Properties = [];
            }
        }

        public override void Update(Vector2d screenPos)
        {
            if (PositionProperty == null || PointOfInterestProperty == null)
            {
                return;
            }

            var newPosition = CompositionModel.Unprojection(CameraSetting, null, screenPos);
            var diff = StartPosition - newPosition;
            PositionProperty.CurrentTimeValue = StartCameraPosition + diff;
            PointOfInterestProperty.CurrentTimeValue = StartCameraPointOfInterest + diff;
        }
    }

    class CameraDollyPreviewManipulationState : PreviewManipulationStateBase
    {
        protected override PropertyViewModel[] Properties { get; }

        Vector3d StartCameraPosition { get; }

        Vector3d StartCameraPointOfInterest { get; }

        Vector3d CameraDirection { get; }

        PropertyViewModel? PositionProperty { get; }

        PropertyViewModel? PointOfInterestProperty { get; }

        public CameraDollyPreviewManipulationState(LayerViewModel targetCameraLayer, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel) : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var cameraPositionProperty = targetCameraLayer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPositionId);
            var cameraPoiProperty = targetCameraLayer.TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPointOfInterestId);
            if (cameraPositionProperty is PropertyViewModel posVm && cameraPoiProperty is PropertyViewModel poiVm)
            {
                StartCameraPosition = (Vector3d)(posVm.CurrentTimeValue ?? Vector3d.Zero);
                StartCameraPointOfInterest = (Vector3d)(poiVm.CurrentTimeValue ?? Vector3d.Zero);
                CameraDirection = (StartCameraPosition - StartCameraPointOfInterest).Normalize();

                posVm.BeginEditCommand.Execute(null);
                poiVm.BeginEditCommand.Execute(null);

                PointOfInterestProperty = poiVm;
                PositionProperty = posVm;
                Properties = [posVm, poiVm];
            }
            else
            {
                Properties = [];
            }
        }

        public override void Update(Vector2d screenPos)
        {
            if (PositionProperty == null || PointOfInterestProperty == null)
            {
                return;
            }

            var move = CameraDirection * (screenPos.Y - StartScreenPosition.Y);
            PositionProperty.CurrentTimeValue = StartCameraPosition + move;
            PointOfInterestProperty.CurrentTimeValue = StartCameraPointOfInterest + move;
        }
    }
}
