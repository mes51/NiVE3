using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Json.Project;
using NiVE3.Extension;
using NiVE3.Numerics;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.ValueObject;
using NiVE3.View.Resource;
using Prism.Mvvm;

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

        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool isEnable;
        public bool IsEnable
        {
            get { return isEnable; }
            set { SetProperty(ref isEnable, value); }
        }

        public PropertyGroupModel Properties { get; }

        MaskShapeType DefaultShapeType { get; }

        Guid MaskId { get; }

        HistoryModel HistoryModel { get; }

        public event EventHandler<EventArgs>? MaskUpdated;

        public MaskModel(ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel, MaskShapeType shapeType = MaskShapeType.Rectangle, Guid? maskId = null)
        {
            MaskId = maskId ?? Guid.NewGuid();
            HistoryModel = historyModel;
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
                        new DoubleProperty(PropertyMaskSettingOpacityId, LanguageResourceDictionary.ResourceKeys.MaskProperty_Setting_Opacity, 100.0, 0.0, 100.0, digit: 2),
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

    enum MaskBlendMode
    {
        Add,
        Subtract,
        Multiply,
        Darken,
        Lighten,
        Difference
    }
}
