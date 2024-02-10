using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Image;
using NiVE3.Image.Drawing;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Image
{
    /// <summary>
    /// レンダリング可能な画像を表します
    /// </summary>
    /// <param name="Image">レンダリングを行う画像</param>
    /// <param name="ROI">画像の最終的なROI</param>
    /// <param name="DownSampleRate">ダウンサンプリングの倍率</param>
    /// <param name="IsEnableMotionBlur">モーションブラーが有効かどうか</param>
    /// <param name="IsEnable3D">3Dが有効かどうか</param>
    /// <param name="InterpolationQuality">画像のレンダリング時の補間方法</param>
    /// <param name="BlendMode">画像のブレンドモード</param>
    /// <param name="Transform">画像のトランスフォームの値</param>
    /// <param name="ParentTransforms">親レイヤーりトランスフォームの値</param>
    /// <param name="LayerOptions">レイヤーのオプション</param>
    public record RenderableImage(
        NImage Image,
        ROI ROI,
        double DownSampleRate,
        bool IsEnableMotionBlur,
        bool IsEnable3D,
        ImageInterpolationQuality InterpolationQuality,
        BlendMode BlendMode,
        PropertyValueGroup Transform,
        ParentTransform[] ParentTransforms,
        PropertyValueGroup? LayerOptions,
        RenderableImage? TrackMatteImage,
        TrackMatteMode? TrackMatteMode
    ) : IDisposable
    {
        public void Dispose()
        {
            Image.Dispose();
            TrackMatteImage?.Dispose();
        }
    }
}
