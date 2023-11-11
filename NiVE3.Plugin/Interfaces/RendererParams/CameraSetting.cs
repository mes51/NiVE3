using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Numerics;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces.RendererParams
{
    /// <summary>
    /// カメラの設定を表します
    /// </summary>
    /// <param name="pointOfInterest">カメラの目標点</param>
    /// <param name="position">カメラの位置</param>
    /// <param name="orientation">カメラの方向</param>
    /// <param name="angleX">カメラのX回転</param>
    /// <param name="angleY">カメラのY回転    </param>
    /// <param name="angleZ">カメラのZ回転</param>
    /// <param name="zoom">カメラのズーム(視野角)</param>
    /// <param name="parentTransforms">カメラの親のトランスフォームの値</param>
    public record CameraSetting(Vector3d PointOfInterest, Vector3d Position, Vector3d Orientation, double AngleX, double AngleY, double AngleZ, double Zoom, ParentTransform[] ParentTransforms)
    {
    }
}
