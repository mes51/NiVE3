using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces.RendererParams;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Interfaces
{
    /// <summary>
    /// 座標系の変換処理、プレビューパネルに表示するバウンディングボックスの生成などを行います
    /// </summary>
    public interface ITransformer
    {
        /// <summary>
        /// コンポジションのサイズを設定します
        /// </summary>
        /// <param name="width">コンポジションの幅</param>
        /// <param name="height">コンポジションの高さ</param>
        void SetSize(int width, int height);

        /// <summary>
        /// 2Dレイヤーのバウンディングボックスを取得します
        /// </summary>
        /// <param name="origin">フッテージ画像の位置</param>
        /// <param name="width">レイヤーの幅</param>
        /// <param name="height">レイヤーの高さ</param>
        /// <param name="transform">レイヤーのトランスフォームの値</param>
        /// <param name="parentTransforms">親のレイヤーのトランスフォームの値</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetBoundingBox2D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms);

        /// <summary>
        /// 3Dレイヤーのバウンディングボックスを取得します
        /// </summary>
        /// <param name="origin">フッテージ画像の位置</param>
        /// <param name="width">レイヤーの幅</param>
        /// <param name="height">レイヤーの高さ</param>
        /// <param name="transform">レイヤーのトランスフォームの値</param>
        /// <param name="parentTransforms">親のレイヤーのトランスフォームの値</param>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetBoundingBox3D(Vector2d origin, int width, int height, PropertyValueGroup transform, ParentTransform[] parentTransforms, CameraSetting cameraSetting);

        /// <summary>
        /// カメラのバウンディングボックスを取得します
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetCameraBoundingBox(CameraSetting targetCameraSetting, CameraSetting cameraSetting);

        /// <summary>
        /// ライトのバウンディングボックスを取得します
        /// </summary>
        /// <param name="lightSetting">バウンディングボックスを算出するライトの設定</param>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <returns>プレビューで表示するバウンディングボックス</returns>
        PreviewBoundingBox GetLightBoundingBox(LightSetting lightSetting, CameraSetting cameraSetting);

        /// <summary>
        /// スクリーン座標からレイヤーを選択します
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <param name="layers">現在コンポジションに存在する選択可能なレイヤーの配列</param>
        /// <param name="pos">スクリーンの座標</param>
        /// <returns>スクリーンの座標最前面に存在するレイヤーのID、存在しない場合はnull</returns>
        Guid? SelectLayer(CameraSetting cameraSetting, LayerSkeleton[] layers, Vector2d pos);

        /// <summary>
        /// スクリーン座標からコンポジションのレイヤーの座標に変換します。
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <param name="baseLayer">基準となるレイヤー</param>
        /// <param name="pos">スクリーンの座標</param>
        /// <returns>レイヤーの座標</returns>
        Vector3d ScreenCoordToLocalCoord(CameraSetting cameraSetting, LayerSkeleton baseLayer, Vector2d pos);

        /// <summary>
        /// スクリーン座標からコンポジションのワールド座標に変換します。
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <param name="pos">スクリーンの座標</param>
        /// <returns>コンポジションのワールド座標</returns>
        Vector3d ScreenCoordToWorldCoord(CameraSetting cameraSetting, Vector2d pos);

        /// <summary>
        /// レイヤーの座標からスクリーン座標に変換します
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <param name="baseLayer">基準となるレイヤー</param>
        /// <param name="pos">レイヤーの座標</param>
        /// <returns>スクリーン座標</returns>
        Vector2d LocalCoordToScreenCoord(CameraSetting cameraSetting, LayerSkeleton baseLayer, Vector3d pos);

        /// <summary>
        /// コンポジションのワールド座標からスクリーン座標に変換します
        /// </summary>
        /// <param name="cameraSetting">カメラの設定</param>
        /// <param name="pos">コンポジションのワールドの座標</param>
        /// <returns>スクリーン座標</returns>
        Vector2d WorldCoordToScreenCoord(CameraSetting cameraSetting, Vector3d pos);
    }
}
