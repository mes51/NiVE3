using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Numerics;
using NiVE3.Plugin.Property;

namespace NiVE3.Plugin.Interfaces.RendererParams
{
    /// <summary>
    /// カメラの設定を表します
    /// </summary>
    public class CameraSetting
    {
        /// <summary>
        /// カメラの目標点
        /// </summary>
        public Vector3d PointOfInterest { get; }

        /// <summary>
        /// カメラの位置
        /// </summary>
        public Vector3d Position { get; }

        /// <summary>
        /// カメラの方向
        /// </summary>
        public Vector3d Orientation { get; }

        /// <summary>
        /// カメラのX回転
        /// </summary>
        public double AngleX { get; }

        /// <summary>
        /// カメラのY回転
        /// </summary>
        public double AngleY { get; }

        /// <summary>
        /// カメラのZ回転
        /// </summary>
        public double AngleZ { get; }

        /// <summary>
        /// カメラのズーム(視野角)
        /// </summary>
        public double Zoom { get; }

        public Tuple<ParentType, PropertyValueGroup>[] ParentTransforms { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pointOfInterest">カメラの目標点</param>
        /// <param name="position">カメラの位置</param>
        /// <param name="orientation">カメラの方向</param>
        /// <param name="angleX">カメラのX回転</param>
        /// <param name="angleY">カメラのY回転    </param>
        /// <param name="angleZ">カメラのZ回転</param>
        /// <param name="zoom">カメラのズーム(視野角)</param>
        /// <param name="parentTransforms">カメラの親のトランスフォームの値</param>
        public CameraSetting(Vector3d pointOfInterest, Vector3d position, Vector3d orientation, double angleX, double angleY, double angleZ, double zoom, Tuple<ParentType, PropertyValueGroup>[] parentTransforms)
        {
            PointOfInterest = pointOfInterest;
            Position = position;
            Orientation = orientation;
            AngleX = angleX;
            AngleY = angleY;
            AngleZ = angleZ;
            Zoom = zoom;
            ParentTransforms = parentTransforms;
        }
    }
}
