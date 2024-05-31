using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        Guid? GrabbingLayerId { get; }

        public PositionPreviewManipulationState(LayerViewModel[] layers, Guid? grabbingLayerId, double time, CompositionModel compositionModel, CameraSetting cameraSetting, Vector2d startScreenPosition, HistoryModel historyModel)
            : base(time, compositionModel, cameraSetting, startScreenPosition, historyModel)
        {
            var imageLayers = new List<(bool, PropertyViewModel)>();
            var prevPositions = new List<Vector3d>();
            for (var i = 0; i <  layers.Length; i++)
            {
                var property = layers[i].TransformProperties?.Children?.FirstOrDefault(p => p.Property.Id == ILayerObject.TransformPositionId) as PropertyViewModel;
                if (property != null)
                {
                    var position = (Vector3d)(property.CurrentTimeValue ?? Vector3d.Zero);
                    property.BeginEditCommand.Execute(null);

                    imageLayers.Add((layers[i].IsEnable3D, property));
                    prevPositions.Add(position);
                }
            }

            GrabbingLayerId = compositionModel.Layers.Any(l => l.LayerId == grabbingLayerId && l.IsEnable3D) ? grabbingLayerId : null;
            Properties = [..imageLayers];
            PrevPositions = [..prevPositions];
            StartPosition = compositionModel.Unproject(cameraSetting, Time, startScreenPosition.X, startScreenPosition.Y, GrabbingLayerId);
        }

        public override void Update(Vector2d screenPos)
        {
            var newPosition = CompositionModel.Unproject(CameraSetting, Time, screenPos.X, screenPos.Y, GrabbingLayerId);
            var diff3D = newPosition - StartPosition;
            var diff2D = screenPos - StartScreenPosition;
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
}
