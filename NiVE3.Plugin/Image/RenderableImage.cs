using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Image
{
    /// <summary>
    /// レンダリング可能な画像を表します
    /// </summary>
    public class RenderableImage : IDisposable
    {
        /// <summary>
        /// レンダリングを行う画像
        /// </summary>
        public NImage Image { get; }

        /// <summary>
        /// 画像の最終的なROI
        /// </summary>
        public Int32Rect ROI { get; }

        /// <summary>
        /// レンダリングする画像の中のオリジナルの画像の位置
        /// </summary>
        public Int32Point OrigimalImagePosition { get; }

        /// <summary>
        /// ダウンサンプリングの倍率
        /// </summary>
        public double DownSampleRate { get; }

        /// <summary>
        /// モーションブラーが有効かどうか
        /// </summary>
        public bool IsEnableMotionBlur { get; }

        /// <summary>
        /// 3Dが有効かどうか
        /// </summary>
        public bool IsEnable3D { get; }

        /// <summary>
        /// 画像のレンダリング時の補間方法
        /// </summary>
        public ImageInterpolationQuality InterpolationQuality { get; }

        /// <summary>
        /// 画像のブレンドモード
        /// </summary>
        public BlendMode BlendMode { get; }

        /// <summary>
        /// 画像のトランスフォームの値
        /// </summary>
        public PropertyValueGroup Transform { get; }

        /// <summary>
        /// 親レイヤーりトランスフォームの値。
        /// </summary>
        public ParentTransform[] ParentTransforms { get; }

        /// <summary>
        /// レイヤーのオプション
        /// </summary>
        public PropertyValueGroup? LayerOptions { get; }

        internal RenderableImage(NImage image, Int32Rect roi, Int32Point origimalImagePosition, double downSampleRate, bool isEnableMotionBlur, bool isEnable3D, ImageInterpolationQuality interpolationQuality, BlendMode blendMode, PropertyValueGroup transform, ParentTransform[] parentTransforms, PropertyValueGroup? layerOptions)
        {
            Image = image;
            ROI = roi;
            OrigimalImagePosition = origimalImagePosition;
            DownSampleRate = downSampleRate;
            IsEnableMotionBlur = isEnableMotionBlur;
            IsEnable3D = isEnable3D;
            InterpolationQuality = interpolationQuality;
            BlendMode = blendMode;
            Transform = transform;
            ParentTransforms = parentTransforms;
            LayerOptions = layerOptions;
        }

        public void Dispose()
        {
            Image.Dispose();
        }
    }
}
