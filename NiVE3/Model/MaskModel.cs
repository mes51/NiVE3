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

namespace NiVE3.Model
{
    partial class MaskModel : BindableBase
    {
        const string PropertyMaskSettingId = nameof(PropertyMaskSettingId);

        const string PropertyMaskSettingShapeTypeId = nameof(PropertyMaskSettingShapeTypeId);

        const string PropertyMaskSettingSizeId = nameof(PropertyMaskSettingSizeId);

        const string PropertyMaskSettingPositionId = nameof(PropertyMaskSettingPositionId);

        const string PropertyMaskSettingOpacityId = nameof(PropertyMaskSettingOpacityId);

        const string PropertyMaskSettingBlendModeId = nameof(PropertyMaskSettingBlendModeId);

        public Guid MaskId { get; }

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isEnable = true;
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        public PropertyGroupModel Properties { get; }

        MaskShapeType DefaultShapeType { get; }

        HistoryModel HistoryModel { get; }

        AcceleratorModel AcceleratorModel { get; }

        public event EventHandler<EventArgs>? MaskUpdated;

        public MaskModel(ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, AcceleratorModel acceleratorModel, HistoryModel historyModel, MaskShapeType shapeType = MaskShapeType.Rectangle, Guid? maskId = null)
        {
            MaskId = maskId ?? Guid.NewGuid();
            HistoryModel = historyModel;
            AcceleratorModel = acceleratorModel;
            DefaultShapeType = shapeType;

            var maskWidth = layerModel.SourceWidth;
            var maskHeight = layerModel.SourceHeight;
            Properties = new PropertyGroupModel(
                new PropertyGroup(
                    PropertyMaskSettingId,
                    LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting,
                    [
                        new EnumProperty(PropertyMaskSettingShapeTypeId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_ShapeType, typeof(MaskShapeType), typeof(LanguageResourceDictionary), shapeType, false, 90.0),
                        new Vector3dProperty(PropertyMaskSettingSizeId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Size, new Vector3d(maskWidth, maskHeight, 0.0), digit: 2, useLinkRatio: true),
                        new Vector3dProperty(PropertyMaskSettingPositionId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Position, new Vector3d(maskWidth * 0.5, maskHeight * 0.5, 0.0), digit: 2),
                        new DoubleProperty(PropertyMaskSettingOpacityId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Opacity, 100.0, 0.0, 100.0, digit: 2, unitKey: LanguageResourceDictionary.ResourceKeys.Unit_Percent),
                        new EnumProperty(PropertyMaskSettingBlendModeId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_BlendMode, typeof(MaskBlendMode), typeof(LanguageResourceDictionary), MaskBlendMode.Add, false, 90.0),
                    ]
                ),
                MaskId.ToInt128(),
                projectModel,
                compositionModel,
                layerModel,
                null,
                historyModel,
                true
            );

            Properties.ValueUpdated += Properties_ValueUpdated;

            PropertyChanged += MaskModel_PropertyChanged;
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
            var oldData = SaveData();
            LoadData(data);

            HistoryModel.Add(new OverwriteMaskHistoryCommand(this, oldData, data));
        }

        public RasterizedMaskImage RenderMask(Time layerTime, Time globalTime, RasterizedMaskImage image, ImageInterpolationQuality imageInterpolationQuality, double downSamplingRateX, double downSamplingRateY, bool useGpu)
        {
            using var entry = CycleChecker.TryEnter(MaskId);
            if (entry == null)
            {
                return image;
            }

            var propertyValues = Properties.GetValues(layerTime, globalTime);
            if (!propertyValues.TryGetValue(PropertyMaskSettingId, out var group) || group is not PropertyValueGroup setting)
            {
                return image;
            }

            var shapeType = (MaskShapeType)(setting[PropertyMaskSettingShapeTypeId] ?? MaskShapeType.Rectangle);
            var size = (Vector2)((Vector3d)(setting[PropertyMaskSettingSizeId] ?? Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var position = (Vector2)((Vector3d)(setting[PropertyMaskSettingPositionId] ?? Vector3d.Zero) / new Vector3d(downSamplingRateX, downSamplingRateY, 1.0));
            var opacity = (float)(double)(setting[PropertyMaskSettingOpacityId] ?? 0.0);
            var blendMode = (MaskBlendMode)(setting[PropertyMaskSettingBlendModeId] ?? MaskBlendMode.Add);

            var noOp = (blendMode == MaskBlendMode.Add || blendMode == MaskBlendMode.Subtract) && opacity <= 0.0F;
            if (noOp || size.X <= 0.0 || size.Y <= 0.0)
            {
                return image;
            }

            position += (Vector2)(image.Origin + new Vector2d(image.Width, image.Height) * 0.5);
            var polygons = (shapeType switch
            {
                MaskShapeType.Ellipse => (IPath)new EllipsePolygon(position.X, position.Y, size.X, size.Y),
                _ => new RectangularPolygon(position.X, position.Y, size.X, size.Y)
            }).Flatten().Select(p => new NiVE3.Shape.Polygon(p.Points.Span)).ToArray();

            if (useGpu)
            {
                var device = AcceleratorModel.CurrentDevice;
                var gpuImage = image.ToGpu(device);
                switch (imageInterpolationQuality)
                {
                    case ImageInterpolationQuality.Level1:
                        ShapeMaskRenderGPU.FillAliased(device, polygons, gpuImage, opacity, blendMode: blendMode);
                        break;
                    default:
                        ShapeMaskRenderGPU.Fill(device, polygons, gpuImage, opacity, blendMode: blendMode);
                        break;
                }
                return gpuImage;
            }
            else
            {
                var managedImage = image.ToManaged();
                switch (imageInterpolationQuality)
                {
                    case ImageInterpolationQuality.Level1:
                        ShapeMaskRendererCPU.FillAiliased(polygons, managedImage, opacity, blendMode: blendMode);
                        break;
                    default:
                        ShapeMaskRendererCPU.Fill(polygons, managedImage, opacity, blendMode: blendMode);
                        break;
                }
                return managedImage;
            }
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
                 DefaultShapeType = DefaultShapeType,
                 Name = Name,
                 IsEnabled = IsEnable,
                 Properties = Properties.SaveData()
            };
        }

        private void Properties_ValueUpdated(object? sender, EventArgs e)
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
        Ellipse,
        //Path
    }
}
