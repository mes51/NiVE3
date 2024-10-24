using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Native.ShadowRealm;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using NiVE3.Config;
using NiVE3.Expression.Converter;
using NiVE3.Expression.Utility;
using NiVE3.Model;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Shared.Extension;

namespace NiVE3.Expression
{
    class ExpressionContext : IDisposable
    {
        Engine Engine { get; }

        bool Disposed { get; set; }

        ProjectModel ProjectModel { get; }

        LayerModel LayerModel { get; }

        EffectModel? EffectModel { get; }

        PropertyModel PropertyModel { get; }

        public ExpressionContext(double globalTime, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, EffectModel? effectModel, PropertyModel propertyModel)
        {
            ProjectModel = projectModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            PropertyModel = propertyModel;

            Engine = new Engine(options =>
            {
                options.Strict = true;
                options.AddObjectConverter<ObjectDictionaryConverter>();
                options.AddObjectConverter<ObjectArrayConverter>();
                options.Interop.CreateClrObject = static io => new Dictionary<string, object?>();
                options.TimeoutInterval(TimeSpan.FromSeconds(ApplicationSetting.Setting.ExpressionTimeout));
                options.SetTypeResolver(new TypeResolver
                {
                    MemberFilter = static member => Attribute.IsDefined(member, typeof(ExpressionPublicMemberAttribute))
                });
            });
            Engine.SetValue("time", globalTime);
            Engine.SetValue("comp", (Func<string, JsValue>)(key => FindComposition(this, key, globalTime)));
            Engine.SetValue("thisComp", WrapComposition(this, compositionModel, globalTime));
            Engine.SetValue("thisLayer", WrapLayer(this, layerModel, globalTime));
            if (effectModel != null)
            {
                Engine.SetValue("thisEffect", WrapEffect(this, effectModel, globalTime));
            }
            Engine.SetValue("thisProperty", WrapProperty(this, propertyModel, globalTime));
            Engine.SetValue("Random", new ExpressionRandom(globalTime, propertyModel.ObjectId));
        }

        public object? Evaluate(ExpressionScript script, object? value)
        {
            Engine.SetValue("thisPropertyValue", value ?? JsValue.Null);
            return ToExpressionValue(Engine.Evaluate(script.Script).ToObject());
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                Engine.Dispose();
                Disposed = true;
            }
        }

        /// <summary>
        /// 配列をobject[]に変換、認識できない値をnullにする
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static object? ToExpressionValue(object? value)
        {
            switch (value)
            {
                case byte:
                case sbyte:
                case short:
                case ushort:
                case int:
                case uint:
                case long:
                case ulong:
                case float:
                case double:
                case decimal:
                case string:
                case DateTime:
                    return value;
                case object?[] array:
                    for (var i = 0; i < array.Length; i++)
                    {
                        array[i] = ToExpressionValue(array[i]);
                    }
                    return array;
                case Array typedArray:
                    {
                        var objectArray = new object[typedArray.Length];
                        Array.Copy(typedArray, objectArray, objectArray.Length);
                        return objectArray;
                    }
                case IDictionary<string, object?> dictionary:
                    foreach (var (k, v) in dictionary)
                    {
                        dictionary[k] = ToExpressionValue(dictionary[k]);
                    }
                    return dictionary;
                default:
                    return null;
            }
        }

        static JsValue FindComposition(ExpressionContext context, string name, double time)
        {
            foreach (var comp in context.ProjectModel.CompositionModels)
            {
                if (comp.Name == name)
                {
                    return WrapComposition(context, comp, time);
                }
            }

            return JsValue.Undefined;
        }

        static JsValue FindLayer(ExpressionContext context, CompositionModel compositionModel, object key, double time)
        {
            if (key is string name)
            {
                foreach (var layer in compositionModel.Layers)
                {
                    if (layer.Name == name)
                    {
                        return WrapLayer(context, layer, time);
                    }
                }
            }
            else if (key is int index && index > 0 && compositionModel.Layers.Count <= index)
            {
                return WrapLayer(context, compositionModel.Layers[index - 1], time);
            }

            return JsValue.Undefined;
        }

        static JsValue FindEffect(ExpressionContext context, LayerModel layer, object key, double time)
        {
            if (key is string name)
            {
                foreach (var effect in layer.Effects)
                {
                    if (effect.Name == name)
                    {
                        return WrapEffect(context, effect, time);
                    }
                }
            }
            else if (key is int index && index > 0 && index <= layer.Effects.Count)
            {
                return WrapEffect(context, layer.Effects[index - 1], time);
            }

            return JsValue.Undefined;
        }

        static JsValue FindProperty(ExpressionContext context, PropertyGroupModel propertyGroupModel, string name, double time)
        {
            foreach (var child in propertyGroupModel.Children)
            {
                if (child.Name == name)
                {
                    return WrapProperty(context, child, time);
                }
            }

            return JsValue.Undefined;
        }

        static JsValue FindProperty(ExpressionContext context, AppendablePropertyModel appendablePropertyModel, object key, double time)
        {
            if (key is string name)
            {
                foreach (var child in appendablePropertyModel.Children)
                {
                    if (child.Name == name)
                    {
                        return WrapProperty(context, child, time);
                    }
                }
            }
            else if (key is int index && index > 0 && index <= appendablePropertyModel.Children.Count)
            {
                return WrapProperty(context, appendablePropertyModel.Children[index - 1], time);
            }

            return JsValue.Undefined;
        }

        static JsValue WrapComposition(ExpressionContext context, CompositionModel compositionModel, double time)
        {
            var values = new Dictionary<string, object>
            {
                { "name", compositionModel.Name },
                { "width", compositionModel.Width },
                { "height", compositionModel.Height },
                { "frameRate", compositionModel.FrameRate },
                { "frameDuration", compositionModel.FrameDuration },
                { "duration", compositionModel.Duration },
                { "isRetentionFrameRate", compositionModel.IsRetentionFrameRate },
                { "applyToneMappingWhenNested", compositionModel.ApplyToneMappingWhenNested },
                { "shutterAngle", compositionModel.ShutterAngle },
                { "shutterPhase", compositionModel.ShutterPhase },
                { "motionBlurSampleCount", compositionModel.MotionBlurSampleCount },
                { "workareaBegin", compositionModel.WorkareaBegin },
                { "workareaEnd", compositionModel.WorkareaEnd },
                //{ "isEnableFrameBlend", compositionModel.IsEnableFrameBlend },
                { "isEnableMotionBlur", compositionModel.IsEnableMotionBlur },
                { "isEnableShy", compositionModel.IsEnableShy },
                { "layer", (Func<object, JsValue>)(key => FindLayer(context, compositionModel, key, time)) }
            };

            return JsValue.FromObject(context.Engine, values);
        }

        static JsValue WrapLayer(ExpressionContext context, LayerModel layerModel, double time)
        {
            var values = new Dictionary<string, object>
            {
                { "name", layerModel.Name },
                { "index", layerModel.Index },
                { "width", layerModel.SourceWidth },
                { "height", layerModel.SourceHeight },
                { "comment", layerModel.Comment },
                { "duration", layerModel.Duration },
                { "sourceStartPoint", layerModel.SourceStartPoint },
                { "inPoint", layerModel.InPoint },
                { "outPoint", layerModel.OutPoint },
                //{ "isEnableTimeRemap", layerModel.IsEnableTimeRemap },
                { "hasImage", layerModel.HasImage },
                { "hasAudio", layerModel.HasAudio },
                { "tagColor", ToNumberArray(layerModel.TagColor.ToVector4()) },
                { "isEnableVideo", layerModel.IsEnableVideo },
                { "isEnableAudio", layerModel.IsEnableAudio },
                { "isEnableSolo", layerModel.IsEnableSolo },
                { "isLock", layerModel.IsLock },
                { "isEnableShy", layerModel.IsEnableShy },
                //{ "isEnableCollapse", layerModel.IsEnableCollapse },
                { "isEnableEffect", layerModel.IsEnableEffect },
                //{ "isEnableFrameBlend", layerModel.IsEnableFrameBlend },
                { "isEnableMotionBlur", layerModel.IsEnableMotionBlur },
                { "isEnableAdjustmentLayer", layerModel.IsEnableAdjustmentLayer },
                { "isEnable3D", layerModel.IsEnable3D },
                { "interpolationQuality", layerModel.InterpolationQuality.ToString() },
                { "blendMode", layerModel.BlendMode.ToString() },
                { "effect", (Func<object, JsValue>)(key => FindEffect(context, layerModel, key, time)) },
                { "getSourceRect", (Func<double, JsValue>)(time => GetSourceRect(context, layerModel, time)) }
            };
            if (layerModel.TransformProperties != null)
            {
                var transform = new Dictionary<string, JsValue>
                {
                    { "anchorPoint", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformAnchorPointId), time) },
                    { "position", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformPositionId), time) },
                    { "direction", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformDirectionId), time) },
                    { "xAngle", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformXAngleId), time) },
                    { "yAngle", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformYAngleId), time) },
                    { "angle", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformZAngleId), time) },
                    { "scale", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformScaleId), time) },

                    // normal layer
                    { "opacity", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformPropertyOpacityId), time) },

                    // camera, light
                    { "pointOfInterest", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformPointOfInterestId), time) },
                    { "orientation", WrapProperty(context, layerModel.TransformProperties.FindProperty(ILayerObject.TransformOrientationId), time) },
                };
                foreach (var key in transform.Keys.ToArray())
                {
                    if (transform[key] == JsValue.Undefined)
                    {
                        transform.Remove(key);
                    }
                }
                values.Add("transform", transform);
            }
            if (layerModel.LayerOptionProperties != null)
            {
                var options = new Dictionary<string, JsValue>
                {
                    // camera
                    { "zoom", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.CameraLayerOptionZoomId), time) },

                    // light
                    { "lightType", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionLightTypeId), time) },
                    { "color", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionColorId), time) },
                    { "intensity", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionIntensityId), time) },
                    { "coneAngle", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionConeAngleId), time) },
                    { "coneAttenuation", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionConeAttenuationId), time) },
                    { "falloffType", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionFalloffTypeId), time) },
                    { "falloffStart", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionFalloffStartId), time) },
                    { "falloffLength", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionFalloffLengthId), time) },
                    { "enableShadow", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionEnableShadowId), time) },
                    { "shadowStrength", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionShadowStrengthId), time) },
                    { "shadowScatterSize", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.LightLayerOptionShadowScatterSizeId), time) },

                    // normal layer
                    { "isCastShadow", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionIsCastShadowId), time) },
                    { "lightTransmission", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionLightTransmissionId), time) },
                    { "acceptShadow", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionIsAcceptShadowId), time) },
                    { "acceptLight", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionIsAcceptLightId), time) },
                    { "ambient", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionAmbientId), time) },
                    { "diffuse", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionDiffuseId), time) },
                    { "specularIntensity", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionSpecularIntensityId), time) },
                    { "specularShininess", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionSpecularShininessId), time) },
                    { "metal", WrapProperty(context, layerModel.LayerOptionProperties.FindProperty(ILayerObject.ImageLayerOptionMetalId), time) },
                };
                foreach (var key in options.Keys.ToArray())
                {
                    if (options[key] == JsValue.Undefined)
                    {
                        options.Remove(key);
                    }
                }
                values.Add("layerOptions", options);
            }
            if (layerModel.AudioOptionProperties != null)
            {
                var audio = new Dictionary<string, JsValue>
                {
                    { "level", WrapProperty(context, layerModel.AudioOptionProperties.FindProperty(ILayerObject.AudioLevelId), time) },
                };
                values.Add("audio", audio);
            }
            if (layerModel.TextProperties != null)
            {
                values.Add("text", WrapProperty(context, layerModel.TextProperties, time));
            }
            if (layerModel.ShapeProperties != null)
            {
                values.Add("shape", WrapProperty(context, layerModel.ShapeProperties, time));
            }
            if (layerModel.SourceOptionProperties != null)
            {
                values.Add("sourceOption", WrapProperty(context, layerModel.SourceOptionProperties, time));
            }

            return JsValue.FromObject(context.Engine, values);
        }

        static JsValue WrapEffect(ExpressionContext context, EffectModel effectModel, double time)
        {
            var values = new Dictionary<string, object>
            {
                { "name", effectModel.Name },
                { "comment", effectModel.Comment },
                { "isEnable", effectModel.IsEnable },
                { "property", (Func<string, JsValue>)(key => FindProperty(context, effectModel.Properties, key, time)) }
            };

            return JsValue.FromObject(context.Engine, values);
        }

        static JsValue WrapProperty(ExpressionContext context, IPropertyModel? propertyModel, double time)
        {
            if (propertyModel == null)
            {
                return JsValue.Undefined;
            }

            switch (propertyModel)
            {
                case PropertyModel p:
                    {
                        var result = new JsObject(context.Engine);
                        result.Set("name", p.Name);
                        result.FastSetProperty("valueAtTime", new PropertyDescriptor(
                            new ClrFunction(context.Engine, "valueAtTime", (_, args) =>
                            {
                                if (args.Length < 1)
                                {
#pragma warning disable CA1507 // NOTE: JSから引数を配列として受け取る都合上、引数名は存在しないので定数で引数名を渡す
                                    throw new ArgumentOutOfRangeException("time");
#pragma warning restore CA1507 // nameof を使用してシンボル名を表現します
                                }
                                var time = args[0].AsNumber();
                                return JsValue.FromObject(context.Engine, p.ToExpressionValue(p.GetValue(time - p.SourceStartPoint, time)) ?? JsValue.Null);
                            }, 1),
                            false,
                            false,
                            false
                        ));
                        result.FastSetProperty("value", new GetSetPropertyDescriptor(
                            new ClrFunction(context.Engine, "value", (_, _) => JsValue.FromObject(context.Engine, p.ToExpressionValue(p.GetValue(time - p.SourceStartPoint, time)))),
                            null,
                            false,
                            false
                        ));

                        result.Set("keyFrameCount", propertyModel.KeyFrames?.Count ?? 0);
                        result.FastSetProperty("keyFrame", new PropertyDescriptor(
                            new ClrFunction(context.Engine, "keyFrame", (_, args) =>
                            {
                                if (args.Length < 1)
                                {
#pragma warning disable CA1507 // NOTE: JSから引数を配列として受け取る都合上、引数名は存在しないので定数で引数名を渡す
                                    throw new ArgumentOutOfRangeException("index");
#pragma warning restore CA1507 // nameof を使用してシンボル名を表現します
                                }

                                var index = (int)args[0].AsNumber();
                                if (index < 1 || index > p.KeyFrames.Count)
                                {
                                    return JsValue.Undefined;
                                }

                                return WrapKeyFrame(context, p, p.KeyFrames[index - 1]);
                            }, 1),
                            false,
                            false,
                            false
                        ));
                        result.FastSetProperty("getKeyFrameNextTime", new PropertyDescriptor(
                            new ClrFunction(context.Engine, "getKeyFrameNextTime", (_, args) =>
                            {
                                if (args.Length < 1)
                                {
#pragma warning disable CA1507 // NOTE: JSから引数を配列として受け取る都合上、引数名は存在しないので定数で引数名を渡す
                                    throw new ArgumentOutOfRangeException("time");
#pragma warning restore CA1507 // nameof を使用してシンボル名を表現します
                                }

                                var time = args[0].AsNumber();
                                foreach (var keyFrame in p.KeyFrames)
                                {
                                    if (keyFrame.Time >= time)
                                    {
                                        return WrapKeyFrame(context, p, keyFrame);
                                    }
                                }

                                return JsValue.Undefined;
                            }, 1),
                            false,
                            false,
                            false
                        ));
                        result.FastSetProperty("getKeyFramePrevTime", new PropertyDescriptor(
                            new ClrFunction(context.Engine, "getKeyFramePrevTime", (_, args) =>
                            {
                                if (args.Length < 1)
                                {
#pragma warning disable CA1507 // NOTE: JSから引数を配列として受け取る都合上、引数名は存在しないので定数で引数名を渡す
                                    throw new ArgumentOutOfRangeException("time");
#pragma warning restore CA1507 // nameof を使用してシンボル名を表現します
                                }

                                var time = args[0].AsNumber();
                                foreach (var keyFrame in p.KeyFrames.Reverse())
                                {
                                    if (keyFrame.Time <= time)
                                    {
                                        return WrapKeyFrame(context, p, keyFrame);
                                    }
                                }

                                return JsValue.Undefined;
                            }, 1),
                            false,
                            false,
                            false
                        ));

                        return result;
                    }
                case PropertyGroupModel g:
                    return WrapPropertyGroup(context, g, time);
                case AppendablePropertyModel a:
                    return WrapAppendableProperty(context, a, time);
            }

            return JsValue.Undefined;
        }

        static JsValue WrapPropertyGroup(ExpressionContext context, PropertyGroupModel propertyGroupModel, double time)
        {
            var values = new Dictionary<string, object>
            {
                { "name", propertyGroupModel.Name },
                { "property", (Func<string, JsValue>)(key => FindProperty(context, propertyGroupModel, key, time)) }
            };

            return JsValue.FromObject(context.Engine, values);
        }

        static JsValue WrapAppendableProperty(ExpressionContext context, AppendablePropertyModel appendablePropertyModel, double time)
        {
            var values = new Dictionary<string, object>
            {
                { "name", appendablePropertyModel.Name },
                { "property", (Func<object, JsValue>)(key => FindProperty(context, appendablePropertyModel, key, time)) }
            };

            return JsValue.FromObject(context.Engine, values);
        }

        static JsValue WrapKeyFrame(ExpressionContext context, PropertyModel propertyModel, KeyFrame keyFrame)
        {
            var values = new Dictionary<string, object>
            {
                { "time", keyFrame.Time },
                { "value", propertyModel.ToExpressionValue(keyFrame.Value) ?? JsValue.Null }
            };

            return JsValue.FromObject(context.Engine, values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static JsValue GetSourceRect(ExpressionContext context, LayerModel layer, double time)
        {
            var rect = layer.GetSourceFootageRect(time);

            return JsValue.FromObject(context.Engine, new object[] { rect.Origin.X, rect.Origin.Y, rect.Width, rect.Height });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static object[] ToNumberArray(in Vector4 v)
        {
            return new object[] { v.X, v.Y, v.Z, v.W };
        }
    }
}