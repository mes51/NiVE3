using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Property.Interaction
{
    public abstract class PropertyInteractionBase
    {
        public bool IsInteracting { get; set; }

        protected IPropertyInteractionViewModel ViewModel { get; }

        protected PropertyInteractionBase(IPropertyInteractionViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        /// <summary>
        /// マウスの左ボタンを押下したとき、のヒットテストを実行します
        /// </summary>
        /// <param name="mousePositionInPreview">プレビューパネル上のマウスの位置。これはDPIの補正、拡大率を適用した後の値です</param>
        /// <param name="previewImageScale">プレビューエリアの拡大率。これはDPIによる補正も含みます</param>
        /// <param name="coordTransformer">現在のコンポジションの座標系を表すICoordTransformerObject</param>
        /// <returns>インタラクションを開始できる場合はtrue、そうでない場合はfalse</returns>
        public abstract bool HitTestInteraction(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);

        /// <summary>
        /// マウスの左ボタンを押下したときの処理を実行します
        /// </summary>
        /// <param name="mousePositionInPreview">プレビューパネル上のマウスの位置。これはDPIの補正、拡大率を適用した後の値です</param>
        /// <param name="previewImageScale">プレビューエリアの拡大率。これはDPIによる補正も含みます</param>
        /// <param name="coordTransformer">現在のコンポジションの座標系を表すICoordTransformerObject</param>
        /// <returns>インタラクションを開始した場合はtrue、そうでない場合はfalse</returns>
        public abstract bool MouseLeftButtonDown(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);

        /// <summary>
        /// マウスの左ボタンを押下した状態でマウスを動かしたときの処理を実行します
        /// </summary>
        /// <param name="mousePositionInPreview">プレビューパネル上のマウスの位置。これはDPIの補正、拡大率を適用した後の値です</param>
        /// <param name="previewImageScale">プレビューエリアの拡大率。これはDPIによる補正も含みます</param>
        /// <param name="coordTransformer">現在のコンポジションの座標系を表すICoordTransformerObject</param>
        public abstract void MouseLeftButtonDrag(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);

        /// <summary>
        /// マウスの左ボタンを離した時の処理を実行します
        /// </summary>
        /// <param name="mousePositionInPreview">プレビューパネル上のマウスの位置。これはDPIの補正、拡大率を適用した後の値です</param>
        /// <param name="previewImageScale">プレビューエリアの拡大率。これはDPIによる補正も含みます</param>
        /// <param name="coordTransformer">現在のコンポジションの座標系を表すICoordTransformerObject</param>
        public abstract void MouseLeftButtonUp(Vector2d mousePositionInPreview, Vector2d previewImageScale, ICoordTransformerObject coordTransformer);

        /// <summary>
        /// インタラクションを中断します
        /// </summary>
        public abstract void AbortInteraction();

        /// <summary>
        /// プレビューパネルに操作・および現在値のプロパティを描画します
        /// </summary>
        /// <param name="drawingContext">描画対象のDrawingContext</param>
        /// <param name="previewImagePosition">プレビューエリアの位置</param>
        /// <param name="previewImageScale">プレビューエリアの拡大率。これはDPIによる補正も含みます</param>
        /// <param name="globalTime">プレビューに表示している現在時間</param>
        /// <param name="frameRate">コンポジションのフレームレート</param>
        /// <param name="previewFrameRange">プレビューに表示するプロパティの前後フレーム数の幅</param>
        /// <param name="tagColor">レイヤーのタグの色</param>
        /// <param name="coordTransformer">現在のコンポジションの座標系を表すICoordTransformerObject</param>
        public abstract void Render(DrawingContext drawingContext, Vector2d previewImagePosition, Vector2d previewImageScale, Time globalTime, double frameRate, int previewFrameRange, Color tagColor, ICoordTransformerObject coordTransformer);
    }
}
