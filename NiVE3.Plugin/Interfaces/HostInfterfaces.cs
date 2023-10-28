using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NiVE3.Plugin.Image;
using NiVE3.Plugin.Property;

namespace NiVE3.Plugin.Interfaces
{
    public interface IAcceleratorObject { }

    public interface ICompositionObject { }

    public interface ILayerObject
    {
        public const string TransformAnchorPointId = nameof(TransformAnchorPointId);

        public const string TransformPositionId = nameof(TransformPositionId);

        public const string TransformDirectionId = nameof(TransformDirectionId);

        public const string TransformXAngleId = nameof(TransformXAngleId);

        public const string TransformYAngleId = nameof(TransformYAngleId);

        public const string TransformZAngleId = nameof(TransformZAngleId);

        public const string TransformScaleId = nameof(TransformScaleId);

        public const string TransformPropertyOpacityId = nameof(TransformPropertyOpacityId);

        public const string CameraTransformPointOfInterestId = nameof(CameraTransformPointOfInterestId);

        public const string CameraTransformOrientationId = nameof(CameraTransformOrientationId);

        public const string CameraLayerOptionZoomId = nameof(CameraLayerOptionZoomId);

        bool IsEnable3D { get; }
    }

    public interface IEffectObject { }

    public interface IPropertyViewModel : INotifyPropertyChanged
    {
        PropertyBase Property { get; }

        object? CurrentTimeValue { get; set; }

        ICommand BeginEditCommand { get; }

        ICommand EndEditCommand { get; }

        ICommand AbortEditCommand { get; }
    }
}
