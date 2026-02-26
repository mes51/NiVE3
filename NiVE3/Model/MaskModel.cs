using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using System.Windows.Xps.Packaging;
using NiVE3.Data.Clipboard;
using NiVE3.Data.Json.Project;
using NiVE3.Extension;
using NiVE3.Image;
using NiVE3.Numerics;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.Shape;
using NiVE3.Util;
using NiVE3.View.Resource;
using Prism.Mvvm;
using SixLabors.ImageSharp.Drawing;
using NiVE3.Property;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Polygon = NiVE3.Shape.Polygon;

namespace NiVE3.Model
{
    [UseReactiveProperty]
    partial class MaskModel : BindableBase, IMaskObject
    {
        const string PropertyMaskSettingId = nameof(PropertyMaskSettingId);

        const string PropertyMaskSettingBezierPathId = nameof(PropertyMaskSettingBezierPathId);

        const string PropertyMaskSettingShapeTypeId = nameof(PropertyMaskSettingShapeTypeId);

        const string PropertyMaskSettingSizeId = nameof(PropertyMaskSettingSizeId);

        const string PropertyMaskSettingPositionId = nameof(PropertyMaskSettingPositionId);

        const string PropertyMaskSettingBlurId = nameof(PropertyMaskSettingBlurId);

        const string PropertyMaskSettingOpacityId = nameof(PropertyMaskSettingOpacityId);

        const string PropertyMaskSettingBlendModeId = nameof(PropertyMaskSettingBlendModeId);

        const string PropertyMaskSettingIsInvertId = nameof(PropertyMaskSettingIsInvertId);

        public Guid MaskId { get; }

        [ReactiveProperty]
        public partial string Name { get; set; } = "";

        [ReactiveProperty]
        public partial bool IsEnable { get; set; } = true;

        public bool IsBezierPath { get; }

        public PropertyGroupModel Properties { get; }

        MaskShapeType DefaultShapeType { get; }

        LayerModel LayerModel { get; }

        HistoryModel HistoryModel { get; }

        AcceleratorModel AcceleratorModel { get; }

        public event EventHandler<EventArgs>? MaskUpdated;

        public MaskModel(ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, AcceleratorModel acceleratorModel, HistoryModel historyModel, bool isBezierPath, MaskShapeType shapeType = MaskShapeType.Rectangle, Guid? maskId = null)
        {
            MaskId = maskId ?? Guid.NewGuid();
            IsBezierPath = isBezierPath;
            LayerModel = layerModel;
            HistoryModel = historyModel;
            AcceleratorModel = acceleratorModel;
            DefaultShapeType = shapeType;

            var maskWidth = layerModel.SourceWidth;
            var maskHeight = layerModel.SourceHeight;
            var maskCenter = new Vector3d(layerModel.FootageWidth, layerModel.FootageHeight, 0.0) * 0.5;
            if (isBezierPath)
            {
                Properties = new PropertyGroupModel(
                    new PropertyGroup(
                        PropertyMaskSettingId,
                        LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting,
                        [
                            new BezierPathProperty(PropertyMaskSettingBezierPathId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_BezierPath),
                            new Vector3dProperty(PropertyMaskSettingPositionId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Position, Vector3d.Zero, digit: 2),
                            new Vector3dProperty(PropertyMaskSettingBlurId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Blur, Vector3d.Zero, digit: 2),
                            new DoubleProperty(PropertyMaskSettingOpacityId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new EnumProperty(PropertyMaskSettingBlendModeId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_BlendMode, typeof(MaskBlendMode), typeof(LanguageResourceDictionary), MaskBlendMode.Add, false, 90.0),
                            new CheckBoxProperty(PropertyMaskSettingIsInvertId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_IsInvert, false)
                        ]
                    ),
                    MaskId.ToInt128(),
                    projectModel,
                    compositionModel,
                    layerModel,
                    null,
                    this,
                    historyModel,
                    true
                );
            }
            else
            {
                Properties = new PropertyGroupModel(
                    new PropertyGroup(
                        PropertyMaskSettingId,
                        LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting,
                        [
                            new EnumProperty(PropertyMaskSettingShapeTypeId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_ShapeType, typeof(MaskShapeType), typeof(LanguageResourceDictionary), shapeType, false, 90.0),
                            new Vector3dProperty(PropertyMaskSettingSizeId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Size, new Vector3d(maskWidth, maskHeight, 0.0), digit: 2, useLinkRatio: true),
                            new Vector3dProperty(PropertyMaskSettingPositionId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Position, maskCenter, digit: 2),
                            new Vector3dProperty(PropertyMaskSettingBlurId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Blur, Vector3d.Zero, digit: 2),
                            new DoubleProperty(PropertyMaskSettingOpacityId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                            new EnumProperty(PropertyMaskSettingBlendModeId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_BlendMode, typeof(MaskBlendMode), typeof(LanguageResourceDictionary), MaskBlendMode.Add, false, 90.0),
                            new CheckBoxProperty(PropertyMaskSettingIsInvertId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_IsInvert, false)
                        ]
                    ),
                    MaskId.ToInt128(),
                    projectModel,
                    compositionModel,
                    layerModel,
                    null,
                    this,
                    historyModel,
                    true
                );
            }

            Properties.ValueUpdated += Properties_ValueUpdated;
            Properties.ValueCommited += Properties_ValueCommited;

            PropertyChanged += MaskModel_PropertyChanged;
        }

        public BezierPath GetPath(Time globalTime, double downSamplingRate)
        {
            var layerTime = globalTime - LayerModel.SourceStartPoint;

            using var entry = CycleChecker.TryEnter(MaskId);
            if (entry == null)
            {
                return BezierPath.Empty;
            }

            var setting = Properties.GetValues(layerTime, globalTime);
            var position = (Vector2)((Vector3d)(setting[PropertyMaskSettingPositionId] ?? Vector3d.Zero) / downSamplingRate);

            if (IsBezierPath)
            {
                var path = (BezierPath)(setting[PropertyMaskSettingBezierPathId] ?? BezierPath.Empty);
                return path.Transform(Matrix3x2.CreateTranslation(position.X, position.Y));
            }
            else
            {
                var shapeType = (MaskShapeType)(setting[PropertyMaskSettingShapeTypeId] ?? MaskShapeType.Rectangle);
                var size = (Vector2)((Vector3d)(setting[PropertyMaskSettingSizeId] ?? Vector3d.Zero) / downSamplingRate);

                position -= size * 0.5F;
                return shapeType switch
                {
                    MaskShapeType.Ellipse => new BezierEllipsePolygon(position.X + size.X * 0.5F, position.Y + size.Y * 0.5F, size.X, size.Y).BezierPath,
                    _ => new RectangularPolygon(position.X, position.Y, size.X, size.Y).ToBezierPath()
                };
            }
        }

        public void ChangeName(string name)
        {
            if (name != Name)
            {
                var oldNeme = Name;
                Name = name;

                HistoryModel.Add(new ChangeNameHistoryCommand(this, oldNeme, name));
            }
        }

        public void CoerceProperties()
        {
            Properties.CoerceValues();
        }

        public void CalcPropertyHash(Time layerTime, Time globalTime, XxHash3 hash)
        {
            hash.Append(Name);
            hash.Append(IsEnable);
            Properties.GetValues(layerTime, globalTime).CalcHash(hash);
        }

        public bool ClearExpressionError()
        {
            return Properties.ClearExpressionError();
        }

        public bool PropertyIsChangeableByTime()
        {
            return Properties.IsChangeableByTime();
        }

        public void OverwriteMask(MaskData data)
        {
            if (data.IsBezierPath != IsBezierPath)
            {
                return;
            }

            var oldData = SaveData();
            LoadData(data);

            HistoryModel.Add(new OverwriteMaskHistoryCommand(this, oldData, data));
        }

        public bool IsOperativeMask(Time layerTime, Time globalTime)
        {
            using var entry = CycleChecker.TryEnter(MaskId);
            if (entry == null)
            {
                return false;
            }

            var setting = Properties.GetValues(layerTime, globalTime);
            var opacity = (float)(double)(setting[PropertyMaskSettingOpacityId] ?? 0.0) * 0.01F;
            var blendMode = (MaskBlendMode)(setting[PropertyMaskSettingBlendModeId] ?? MaskBlendMode.Add);

            if (IsBezierPath)
            {
                var path = (BezierPath)(setting[PropertyMaskSettingBezierPathId] ?? BezierPath.Empty);

                return path.IsClosed && !path.IsEmpty();
            }
            else
            {
                return true;
            }
        }

        public LayerMaskImage? RenderMask(Time layerTime, Time globalTime, int width, int height, Vector2d imageOrigin, ImageInterpolationQuality imageInterpolationQuality, double downSamplingRateX, double downSamplingRateY, bool useGpu)
        {
            using var entry = CycleChecker.TryEnter(MaskId);
            if (entry == null)
            {
                return null;
            }

            var setting = Properties.GetValues(layerTime, globalTime);

            var position = (Vector2)((Vector3d)(setting[PropertyMaskSettingPositionId] ?? Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var blur = (Vector2)((Vector3d)(setting[PropertyMaskSettingBlurId] ?? Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var opacity = (float)(double)(setting[PropertyMaskSettingOpacityId] ?? 0.0) * 0.01F;
            var blendMode = (MaskBlendMode)(setting[PropertyMaskSettingBlendModeId] ?? MaskBlendMode.Add);
            var isInvert = (bool)(setting[PropertyMaskSettingIsInvertId] ?? false);

            var noOp = (blendMode == MaskBlendMode.Add || blendMode == MaskBlendMode.Subtract) && opacity <= 0.0F;

            Polygon[] polygons;
            if (IsBezierPath)
            {
                var path = (BezierPath)(setting[PropertyMaskSettingBezierPathId] ?? BezierPath.Empty);
                if (noOp || path.IsEmpty() || !path.IsClosed)
                {
                    if (useGpu)
                    {
                        return new LayerMaskImage(new GPURasterizedMaskImage(width, height, AcceleratorModel.CurrentDevice, 1.0F) { Origin = imageOrigin }, opacity, blendMode, isInvert);
                    }
                    else
                    {
                        return new LayerMaskImage(new ManagedRasterizedMaskImage(width, height) { Origin = imageOrigin }, opacity, blendMode, isInvert);
                    }
                }

                var flattendPath = path.Transform(Matrix3x2.CreateTranslation(position.X, position.Y)).BuildPath()?.Flatten();
                if (flattendPath == null)
                {
                    if (useGpu)
                    {
                        return new LayerMaskImage(new GPURasterizedMaskImage(width, height, AcceleratorModel.CurrentDevice, 1.0F) { Origin = imageOrigin }, opacity, blendMode, isInvert);
                    }
                    else
                    {
                        return new LayerMaskImage(new ManagedRasterizedMaskImage(width, height) { Origin = imageOrigin }, opacity, blendMode, isInvert);
                    }
                }

                polygons = [..flattendPath.Select(p => new Polygon(p.Points.Span))];
            }
            else
            {
                var shapeType = (MaskShapeType)(setting[PropertyMaskSettingShapeTypeId] ?? MaskShapeType.Rectangle);
                var size = (Vector2)((Vector3d)(setting[PropertyMaskSettingSizeId] ?? Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
                if (noOp || size.X <= 0.0 || size.Y <= 0.0)
                {
                    if (useGpu)
                    {
                        return new LayerMaskImage(new GPURasterizedMaskImage(width, height, AcceleratorModel.CurrentDevice, 1.0F) { Origin = imageOrigin }, opacity, blendMode, isInvert);
                    }
                    else
                    {
                        return new LayerMaskImage(new ManagedRasterizedMaskImage(width, height) { Origin = imageOrigin }, opacity, blendMode, isInvert);
                    }
                }

                position += (Vector2)(imageOrigin - (Vector2d)size * 0.5);
                polygons = [..(shapeType switch
                {
                    MaskShapeType.Ellipse => (IPath)new BezierEllipsePolygon(position.X + size.X * 0.5F, position.Y + size.Y * 0.5F, size.X, size.Y),
                    _ => new RectangularPolygon(position.X, position.Y, size.X, size.Y)
                }).Flatten().Select(p => new Polygon(p.Points.Span))];
            }

            if (useGpu)
            {
                var device = AcceleratorModel.CurrentDevice;
                var gpuImage = new GPURasterizedMaskImage(width, height, device, 0.0F) { Origin = imageOrigin };
                switch (imageInterpolationQuality)
                {
                    case ImageInterpolationQuality.Level1:
                        ShapeMaskRenderGPU.FillAliased(device, polygons, gpuImage, 1.0F);
                        break;
                    default:
                        ShapeMaskRenderGPU.Fill(device, polygons, gpuImage, 1.0F);
                        break;
                }

                if (blur.X > 0.0F || blur.Y > 0.0F)
                {
                    MaskBlur.ProcessGpu(device, gpuImage, blur.X, blur.Y);
                }

                return new LayerMaskImage(gpuImage, opacity, blendMode, isInvert);
            }
            else
            {
                var managedImage = new ManagedRasterizedMaskImage(width, height) { Origin = imageOrigin };
                switch (imageInterpolationQuality)
                {
                    case ImageInterpolationQuality.Level1:
                        ShapeMaskRendererCPU.FillAiliased(polygons, managedImage, 1.0F);
                        break;
                    default:
                        ShapeMaskRendererCPU.Fill(polygons, managedImage, 1.0F);
                        break;
                }

                if (blur.X > 0.0F || blur.Y > 0.0F)
                {
                    MaskBlur.ProcessCpu(managedImage, blur.X, blur.Y);
                }

                return new LayerMaskImage(managedImage, opacity, blendMode, isInvert);
            }
        }

        public bool IsInverted(Time layerTime, Time globalTime)
        {
            using var entry = CycleChecker.TryEnter(MaskId);
            if (entry == null)
            {
                return false;
            }

            var setting = Properties.GetValues(layerTime, globalTime);

            return ((MaskBlendMode)(setting[PropertyMaskSettingBlendModeId] ?? MaskBlendMode.Add)).IsInverted();
        }

        public void LoadData(MaskData data)
        {
            Name = data.Name;
            IsEnable = data.IsEnabled;
            if (data.Properties != null)
            {
                Properties.LoadData(data.Properties);
            }
        }

        public MaskData SaveData()
        {
            return new MaskData
            {
                 MaskId = MaskId,
                 IsBezierPath = IsBezierPath,
                 DefaultShapeType = DefaultShapeType,
                 Name = Name,
                 IsEnabled = IsEnable,
                 Properties = Properties.SaveData()
            };
        }

        public bool IsAlive()
        {
            return LayerModel.IsAlive(this);
        }

        public static (Guid oldId, Guid newId) ConvertDataForImport(MaskData maskData)
        {
            var oldId = maskData.MaskId;
            maskData.MaskId = Guid.NewGuid();

            return (oldId, maskData.MaskId);
        }

        private void Properties_ValueUpdated(object? sender, EventArgs e)
        {
            MaskUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Properties_ValueCommited(object? sender, EventArgs e)
        {
            MaskUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void MaskModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            MaskUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum MaskShapeType
    {
        Rectangle,
        Ellipse
    }

    record LayerMaskImage(RasterizedMaskImage MaskImage, float Opacity, MaskBlendMode BlendMode, bool IsInvert) : IDisposable
    {
        public void Dispose()
        {
            MaskImage.Dispose();
        }
    }

    file static class PathPolygonExtensions
    {
        public static BezierPath ToBezierPath(this RectangularPolygon polygon)
        {
            var points = polygon.Points.Span;
            var bezierPoints = new BezierPoint[points.Length - 1];
            for (var i = 0; i < bezierPoints.Length; i++)
            {
                bezierPoints[i] = new BezierPoint(Vector2d.Zero, Vector2d.Zero, (Vector2d)(Vector2)points[i + 1], true, false);
            }
            return new BezierPath((Vector2d)(Vector2)points[0], bezierPoints, true);
        }
    }
}
