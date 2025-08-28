using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal.Util;

namespace NiVE3.PresetPlugin.Effect.Util
{
    static class CameraProperties
    {
        public const string PropertyCameraUseCompositionId = nameof(PropertyCameraUseCompositionId);

        public const string PropertyCameraPointOfInterestId = nameof(PropertyCameraPointOfInterestId);

        public const string PropertyCameraPositionId = nameof(PropertyCameraPositionId);

        public const string PropertyCameraOrientationId = nameof(PropertyCameraOrientationId);

        public const string PropertyCameraZoomId = nameof(PropertyCameraZoomId);

        public const string PropertyCameraYAngleId = nameof(PropertyCameraYAngleId);

        public const string PropertyCameraXAngleId = nameof(PropertyCameraXAngleId);

        public const string PropertyCameraZAngleId = nameof(PropertyCameraZAngleId);

        public static (Matrix4x4d viewMatrix, double fov) GetViewMatrixAndFov(IReadOnlyCollection<IPropertyObject> cameraProperties, ICompositionObject composition, ILayerObject layer, Time layerTime, ROI roi, int imageWidth, int imageHeight, double downSamplingRateX, double downSamplingRateY)
        {
            var realWidth = (int)Math.Round(imageWidth * downSamplingRateX);
            var realHeight = (int)Math.Round(imageHeight * downSamplingRateY);
            var useCompositionCamera = cameraProperties.GetValue(PropertyCameraUseCompositionId, layerTime, false);
            var originalWidth = roi.OriginalImageSize.Width * downSamplingRateX;
            var originalHeight = roi.OriginalImageSize.Height * downSamplingRateY;
            var fov = 0.0;
            // TODO: ROI変更分のズレを合わせる
            if (useCompositionCamera)
            {
                var size = Math.Max(realWidth, realHeight);
                var cameraSetting = composition.GetActiveCameraSetting(layerTime + layer.SourceStartPoint);
                fov = Math.Atan(realWidth / cameraSetting.Zoom * 0.5) * 2.0;
                var offsetX = (composition.Width - originalWidth) * 0.5 / size;
                var offsetY = (composition.Height - originalHeight) * 0.5 / size;
                return (Transform3D.Calc3DViewMatrix(cameraSetting, realWidth, realHeight) * Matrix4x4d.CreateTranslate(offsetX, offsetY, 0.0), fov);
            }
            else
            {
                var zoom = cameraProperties.GetValue(PropertyCameraZoomId, layerTime, 0.0);
                fov = Math.Atan(realWidth / zoom * 0.5) * 2.0;
                return (Transform3D.Calc3DViewMatrix(
                    new CameraSetting(
                        cameraProperties.GetValue(PropertyCameraPointOfInterestId, layerTime, Vector3d.Zero),
                        cameraProperties.GetValue(PropertyCameraPositionId, layerTime, Vector3d.Zero),
                        cameraProperties.GetValue(PropertyCameraOrientationId, layerTime, Vector3d.Zero),
                        cameraProperties.GetValue(PropertyCameraXAngleId, layerTime, 0.0),
                        cameraProperties.GetValue(PropertyCameraYAngleId, layerTime, 0.0),
                        cameraProperties.GetValue(PropertyCameraZAngleId, layerTime, 0.0),
                        zoom,
                        []
                    ),
                    realWidth,
                    realHeight
                ), fov);
            }
        }
    }
}
