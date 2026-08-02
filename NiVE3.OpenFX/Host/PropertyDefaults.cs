using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// OFX 仕様で定められた、ホストが事前設定すべきデスクリプタのデフォルトプロパティ値
    /// (OFX Support Library 製プラグインは paramDefine 直後にこれらを読みに来る)
    /// </summary>
    public static class PropertyDefaults
    {
        /// <summary>
        /// エフェクトデスクリプタのデフォルト値を設定します
        /// </summary>
        /// <param name="props">対象のプロパティセット</param>
        public static void ApplyEffectDefaults(PropertySet props)
        {
            // デスクリプタにもインスタンスデータの格納先が必要 (Sapphire 等が Describe 時に読み書きする)
            props.SetAll(OfxNames.PropInstanceData, (nint)0);
            props.SetAll(OfxNames.PropLabel, "");
            props.SetAll(OfxNames.PropShortLabel, "");
            props.SetAll(OfxNames.PropLongLabel, "");
            props.SetAll(OfxNames.PropPluginDescription, "");
            props.SetAll(OfxNames.ImageEffectPluginPropGrouping, "");
            props.SetAll(OfxNames.ImageEffectPluginPropSingleInstance, 0);
            props.SetAll(OfxNames.ImageEffectPluginPropHostFrameThreading, 1);
            props.SetAll(OfxNames.ImageEffectPluginRenderThreadSafety, OfxNames.ImageEffectRenderFullySafe);
            props.SetAll(OfxNames.ImageEffectPropSupportsMultiResolution, 1);
            props.SetAll(OfxNames.ImageEffectPropSupportsTiles, 1);
            props.SetAll(OfxNames.ImageEffectPropTemporalClipAccess, 0);
            props.SetAll(OfxNames.ImageEffectPropSupportsMultipleClipDepths, 0);
            props.SetAll(OfxNames.ImageEffectPropSupportsMultipleClipPARs, 0);
            props.SetAll(OfxNames.ImageEffectPluginPropOverlayInteractV1, (nint)0);
            props.SetAll(OfxNames.ImageEffectPropOpenGLRenderSupported, "false");
            // 1.5.1 追加分の既定値 (プラグインが Describe で上書きする)
            props.SetAll(OfxNames.ImageEffectPropCPURenderSupported, "true");
            props.SetAll(OfxNames.ImageEffectPropNoSpatialAwareness, "false");
        }

        /// <summary>
        /// クリップデスクリプタのデフォルト値を設定します
        /// </summary>
        /// <param name="props">対象のプロパティセット</param>
        /// <param name="name">クリップ名</param>
        public static void ApplyClipDefaults(PropertySet props, string name)
        {
            props.SetAll(OfxNames.PropLabel, name);
            props.SetAll(OfxNames.PropShortLabel, name);
            props.SetAll(OfxNames.PropLongLabel, name);
            props.SetAll(OfxNames.ImageClipPropOptional, 0);
            props.SetAll(OfxNames.ImageClipPropIsMask, 0);
            props.SetAll(OfxNames.ImageClipPropFieldExtraction, OfxNames.ImageFieldExtractionDoubled);
            props.SetAll(OfxNames.ImageEffectPropTemporalClipAccess, 0);
        }

        /// <summary>
        /// パラメータデスクリプタのデフォルト値を設定します
        /// </summary>
        /// <param name="props">対象のプロパティセット</param>
        /// <param name="paramType">OFX のパラメータ型名</param>
        /// <param name="name">パラメータ名</param>
        public static void ApplyParamDefaults(PropertySet props, string paramType, string name)
        {
            props.SetAll(OfxNames.PropLabel, name);
            props.SetAll(OfxNames.PropShortLabel, name);
            props.SetAll(OfxNames.PropLongLabel, name);
            props.SetAll(OfxNames.ParamPropScriptName, name);
            props.SetAll(OfxNames.ParamPropHint, "");
            props.SetAll(OfxNames.ParamPropParent, "");
            props.SetAll(OfxNames.ParamPropSecret, 0);
            props.SetAll(OfxNames.ParamPropEnabled, 1);
            props.SetAll(OfxNames.ParamPropCanUndo, 1);
            props.SetAll(OfxNames.ParamPropPersistant, 1);
            props.SetAll(OfxNames.ParamPropEvaluateOnChange, 1);
            props.SetAll(OfxNames.ParamPropPluginMayWrite, 0);
            props.SetAll(OfxNames.ParamPropInteractV1, (nint)0);

            var isValueType = paramType is not (OfxNames.ParamTypeGroup or OfxNames.ParamTypePage or OfxNames.ParamTypePushButton);
            if (isValueType)
            {
                // ホストは String / Custom / StrChoice のアニメーションに対応しない
                props.SetAll(OfxNames.ParamPropAnimates, paramType is OfxNames.ParamTypeString or OfxNames.ParamTypeCustom or OfxNames.ParamTypeStrChoice ? 0 : 1);
                props.SetAll(OfxNames.ParamPropIsAnimating, 0);
                props.SetAll(OfxNames.ParamPropIsAutoKeying, 0);
            }

            switch (paramType)
            {
                case OfxNames.ParamTypeInteger:
                    ApplyNumericDefaults(props, 1, isInteger: true);
                    break;
                case OfxNames.ParamTypeInteger2D:
                    ApplyNumericDefaults(props, 2, isInteger: true);
                    break;
                case OfxNames.ParamTypeInteger3D:
                    ApplyNumericDefaults(props, 3, isInteger: true);
                    break;
                case OfxNames.ParamTypeDouble:
                    ApplyNumericDefaults(props, 1, isInteger: false);
                    props.SetAll(OfxNames.ParamPropIncrement, 1.0);
                    props.SetAll(OfxNames.ParamPropDigits, 2);
                    props.SetAll(OfxNames.ParamPropDoubleType, OfxNames.ParamDoubleTypePlain);
                    break;
                case OfxNames.ParamTypeDouble2D:
                    ApplyNumericDefaults(props, 2, isInteger: false);
                    props.SetAll(OfxNames.ParamPropIncrement, 1.0);
                    props.SetAll(OfxNames.ParamPropDigits, 2);
                    props.SetAll(OfxNames.ParamPropDoubleType, OfxNames.ParamDoubleTypePlain);
                    props.SetAll(OfxNames.ParamPropDimensionLabel, "x", "y");
                    props.SetAll(OfxNames.ParamPropDefaultCoordinateSystem, OfxNames.ParamCoordinatesCanonical);
                    break;
                case OfxNames.ParamTypeDouble3D:
                    ApplyNumericDefaults(props, 3, isInteger: false);
                    props.SetAll(OfxNames.ParamPropIncrement, 1.0);
                    props.SetAll(OfxNames.ParamPropDigits, 2);
                    props.SetAll(OfxNames.ParamPropDoubleType, OfxNames.ParamDoubleTypePlain);
                    props.SetAll(OfxNames.ParamPropDimensionLabel, "x", "y", "z");
                    props.SetAll(OfxNames.ParamPropDefaultCoordinateSystem, OfxNames.ParamCoordinatesCanonical);
                    break;
                case OfxNames.ParamTypeBoolean:
                    props.SetAll(OfxNames.ParamPropDefault, 0);
                    break;
                case OfxNames.ParamTypeChoice:
                    props.SetAll(OfxNames.ParamPropDefault, 0);
                    break;
                case OfxNames.ParamTypeStrChoice:
                    // 値は文字列 (ChoiceEnum のいずれか)。未設定の場合はインスタンス生成時に先頭の列挙値へ解決される
                    props.SetAll(OfxNames.ParamPropDefault, "");
                    break;
                case OfxNames.ParamTypeRGB:
                    props.SetAll(OfxNames.ParamPropDefault, 0.0, 0.0, 0.0);
                    props.SetAll(OfxNames.ParamPropDimensionLabel, "r", "g", "b");
                    break;
                case OfxNames.ParamTypeRGBA:
                    props.SetAll(OfxNames.ParamPropDefault, 0.0, 0.0, 0.0, 1.0);
                    props.SetAll(OfxNames.ParamPropDimensionLabel, "r", "g", "b", "a");
                    break;
                case OfxNames.ParamTypeString:
                    props.SetAll(OfxNames.ParamPropDefault, "");
                    props.SetAll(OfxNames.ParamPropStringMode, OfxNames.ParamStringIsSingleLine);
                    props.SetAll(OfxNames.ParamPropStringFilePathExists, 1);
                    break;
                case OfxNames.ParamTypeCustom:
                    props.SetAll(OfxNames.ParamPropDefault, "");
                    break;
                case OfxNames.ParamTypeGroup:
                    props.SetAll(OfxNames.ParamPropGroupOpen, 1);
                    break;
            }
        }

        static void ApplyNumericDefaults(PropertySet props, int dimension, bool isInteger)
        {
            var zero = isInteger ? (object)0 : 0.0;
            var min = isInteger ? (object)int.MinValue : -double.MaxValue;
            var max = isInteger ? (object)int.MaxValue : double.MaxValue;

            props.SetAll(OfxNames.ParamPropDefault, Enumerable.Repeat(zero, dimension).ToArray());
            props.SetAll(OfxNames.ParamPropMin, Enumerable.Repeat(min, dimension).ToArray());
            props.SetAll(OfxNames.ParamPropMax, Enumerable.Repeat(max, dimension).ToArray());
            props.SetAll(OfxNames.ParamPropDisplayMin, Enumerable.Repeat(min, dimension).ToArray());
            props.SetAll(OfxNames.ParamPropDisplayMax, Enumerable.Repeat(max, dimension).ToArray());
        }
    }
}
