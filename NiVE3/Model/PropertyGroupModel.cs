using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data.Clipboard;
using NiVE3.Data.Json.Project;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property.Control;
using NiVE3.Plugin.Property.Types;
using NiVE3.Plugin.Property;
using Prism.Mvvm;
using NiVE3.View.Resource;
using NiVE3.Shared.Extension;
using System.IO.Hashing;
using NiVE3.Extension;
using System.ComponentModel;

namespace NiVE3.Model
{
    partial class PropertyGroupModel : BindableBase, IPropertyModel, IOverwriteablePropertyModel
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public double SourceStartPoint { get; set; }

        private bool isEnabled = true;
        public bool IsEnable
        {
            get { return isEnabled; }
            set { SetProperty(ref isEnabled, value); }
        }

        public Guid InstanceId { get; }

        public ObservableCollection<KeyFrame>? KeyFrames => null;

        private ObservableCollection<IPropertyModel> children = [];
        public ObservableCollection<IPropertyModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public PropertyBase Property { get; }

        public Int128 ObjectId { get; }

        public string Id => Property.Id;

        public bool UseEnableSwitch { get; }

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

        CompositionModel CompositionModel { get; }

        LayerModel? LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        public PropertyGroupModel(PropertyBase property, Int128 parentObjectId, CompositionModel compositionModel, LayerModel layerModel, HistoryModel historyModel, Guid? instanceId = null) : this(property, parentObjectId, compositionModel, layerModel, null, historyModel, false, instanceId) { }

        public PropertyGroupModel(PropertyBase property, Int128 parentObjectId, CompositionModel compositionModel, LayerModel layerModel, EffectModel? effectModel, HistoryModel historyModel, bool useEnableSwitch, Guid? instanceId = null)
        {
            Property = property;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Name = property.DisplayName;
            UseEnableSwitch = useEnableSwitch;
            InstanceId = instanceId ?? Guid.NewGuid();
            SourceStartPoint = layerModel.SourceStartPoint;

            var objectIdHash = new XxHash3();
            objectIdHash.Append(parentObjectId);
            objectIdHash.Append(instanceId ?? Guid.Empty);
            objectIdHash.Append(property.Id);
            ObjectId = objectIdHash.ToInt128();

            // NOTE: 本来はモデル側から設定してもらうものだが、引き回しの経路が複雑になりすぎる(レイヤーからだったり、エフェクトやマスクだったり)ため、自分から取りに行く
            layerModel.PropertyChanged += LayerModel_PropertyChanged;

            foreach (var c in ((PropertyGroup)property).Children)
            {
                if (c is PropertyGroup)
                {
                    Children.Add(new PropertyGroupModel(c, ObjectId, compositionModel, layerModel, effectModel, historyModel, false));
                }
                else if (c is AppendableProperty)
                {
                    Children.Add(new AppendablePropertyModel(c, ObjectId, compositionModel, layerModel, effectModel, historyModel));
                }
                else
                {
                    Children.Add(new PropertyModel(c, ObjectId, compositionModel, layerModel, effectModel, historyModel));
                }
            }

            foreach (var c in Children)
            {
                c.ValueUpdated += Child_ValueUpdated;
                c.ValueCommited += Child_ValueCommited;
            }
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(new CompositionViewModelProxy(CompositionModel), LayerModel != null ? new LayerViewModelProxy(LayerModel) : null, EffectModel != null ? new EffectViewModelProxy(EffectModel) : null, viewModel);
        }

        public IReadOnlyCollection<IPropertyObject>? GetChildren()
        {
            return Children;
        }

        public object? GetValue(double layerTime)
        {
            return null;
        }

        public object? GetValue(double time, double globalTime)
        {
            return null;
        }

        public PropertyValueGroup GetValues(double layerTime, bool withoutDisableProperty = false)
        {
            return GetValues(layerTime, layerTime + SourceStartPoint, withoutDisableProperty);
        }

        public PropertyValueGroup GetValues(double time, double globalTime, bool withoutDisableProperty = false)
        {
            var result = new Dictionary<string, object?>();
            var propertyTypes = new Dictionary<string, IPropertyType>();

            foreach (var p in Children)
            {
                if (p is PropertyGroupModel pg)
                {
                    result.Add(pg.Property.Id, pg.GetValues(time, globalTime, withoutDisableProperty));
                }
                else if (p is AppendablePropertyModel ap)
                {
                    result.Add(ap.Property.Id, ap.GetChildPropertyValues(time, globalTime, withoutDisableProperty));
                }
                else if (p is PropertyModel pp)
                {
                    result.Add(pp.Property.Id, pp.GetValue(time, globalTime));
                }

                propertyTypes.Add(p.Property.Id, p.Property.PropertyType);
            }

            return new PropertyValueGroup(Property.Id, result, propertyTypes, IsEnable);
        }

        public void UpdateValueByCompositionStateChanged()
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_UpdateValueByCompositionStateChanged));

            foreach (var child in Children)
            {
                child.UpdateValueByCompositionStateChanged();
            }

            HistoryModel.EndGroup();
        }

        public bool HasCompositionDependProperty()
        {
            return Children.Any(c => c.HasCompositionDependProperty());
        }

        public bool HasKeyFrames()
        {
            return Children.Any(p => p.HasKeyFrames());
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

        public void CreateKeyFrames(string[] ids)
        {
            var children = Children.Where(p => ids.Contains(p.Property.Id)).OrderBy(Children.IndexOf);
            if (!children.Any())
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddKeyFrame));

            foreach (var c in children)
            {
                switch (c)
                {
                    case PropertyModel p:
                        p.CreateKeyFrame(p.GetCurrentTimeValue());
                        break;
                    case PropertyGroupModel pg:
                        pg.CreateKeyFramesAllChildren();
                        break;
                    case AppendablePropertyModel ap:
                        ap.CreateKeyFramesAllChildren();
                        break;
                }
            }

            HistoryModel.EndGroup();
        }

        public void CreateKeyFramesAllChildren()
        {
            CreateKeyFrames([.. Children.Select(c => c.Property.Id)]);
        }

        public void ResetProperties(string[] ids)
        {
            var children = Children.Where(p => ids.Contains(p.Property.Id)).OrderBy(Children.IndexOf);
            if (!children.Any())
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ResetPropertyValue));

            foreach (var c in children)
            {
                switch (c)
                {
                    case PropertyModel p:
                        p.ResetProperty();
                        break;
                    case PropertyGroupModel pg:
                        pg.ResetAllChildren();
                        break;
                    case AppendablePropertyModel ap:
                        ap.ResetAllChildren();
                        break;
                }
            }

            HistoryModel.EndGroup();
        }

        public void ResetAllChildren()
        {
            ResetProperties([.. Children.Select(c => c.Property.Id)]);
        }

        public IPropertyModel? FindProperty(string propertyId)
        {
            var child = Children.FirstOrDefault(c => c.Property.Id == propertyId);
            if (child != null)
            {
                return child;
            }

            foreach (var childGroup in Children.OfType<PropertyGroupModel>())
            {
                var grandChild = childGroup.FindProperty(propertyId);
                if (grandChild != null)
                {
                    return grandChild;
                }
            }

            return null;
        }

        public PropertyData SaveData()
        {
            return new PropertyData
            {
                PropertyId = Property.Id,
                InstanceId = InstanceId,
                IsEnabled = IsEnable,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = Children.Select(p => p.SaveData()).ToArray()
            };
        }

        public void LoadData(PropertyData data)
        {
            Name = data.Name;
            IsEnable = !UseEnableSwitch || data.IsEnabled;
            if (data.Children == null)
            {
                return;
            }

            foreach (var childData in data.Children)
            {
                Children.FirstOrDefault(c => c.Property.Id == childData.PropertyId)?.LoadData(childData);
            }
        }

        public void CoerceValues()
        {
            foreach (var child in Children)
            {
                child.CoerceValues();
            }
        }

        public void PasteProperty(PropertyData data)
        {
            PasteChildrenPropertyInternal(data, []);
        }

        public void OverwriteProperty(PropertyData data)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName || data.Children == null)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_PasteProperty));

            var targetChildren = Children.Where(c => data.Children.Any(d => d.PropertyId == c.Property.Id)).OrderBy(c => data.Children.IndexOf(d => d.PropertyId == c.Property.Id)).OfType<IOverwriteablePropertyModel>();
            foreach (var (childData, child) in data.Children.Zip(targetChildren))
            {
                child.OverwriteProperty(childData);
            }

            HistoryModel.EndGroup();
        }

        public CopyData<PropertyData> CopyChildrenProperty(string[] ids)
        {
            var children = Children.Where(p => ids.Contains(p.Property.Id)).OrderBy(Children.IndexOf);
            var data = new PropertyData
            {
                PropertyId = Property.Id,
                InstanceId = InstanceId,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = [.. children.Select(c => c.SaveData())]
            };

            return new CopyData<PropertyData>(CopyDataType.PropertyGroup, [data]);
        }

        public void PasteChildrenProperty(CopyData<PropertyData> data, string[] ids)
        {
            if (data.Data.Length < 1)
            {
                return;
            }

            if (data.Type == CopyDataType.PropertyGroup)
            {
                PasteChildrenPropertyInternal(data.Data[0], ids);
            }
            else if (ids.Length == 1 && Children.FirstOrDefault(c => c.Property.Id == ids[0]) is IPropertyModel targetChild)
            {
                targetChild.PasteProperty(data.Data[0]);
            }
        }

        public CopyData<PropertyData> CutChildrenKeyFrames(string[] ids)
        {
            var data = CopyChildrenProperty(ids);
            DeleteChildrenKeyFramesInternal(ids, true);

            return data;
        }

        public void DeleteChildrenKeyFrames(string[] ids)
        {
            DeleteChildrenKeyFramesInternal(ids, false);
        }

        void DeleteChildrenKeyFramesInternal(string[] ids, bool isCut)
        {
            var children = Children.OfType<PropertyModel>().Where(p => ids.Contains(p.Property.Id));
            if (children.All(c => c.KeyFrames.Count < 1))
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_RemoveKeyFrame));
            foreach (var child in children)
            {
                child.ClearKeyFrame();
            }
            HistoryModel.EndGroup();
        }

        void PasteChildrenPropertyInternal(PropertyData data, string[] ids)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName || data.Children == null || data.Children.Length < 1)
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_PasteProperty));
            if (data.Children.Length > 1)
            {
                // NOTE: 複数同時貼り付けの場合は同一のグループにしか貼り付けられない
                if (data.PropertyId == Property.Id)
                {
                    var targetChildren = Children.Where(c => data.Children.Any(d => d.PropertyId == c.Property.Id)).OrderBy(c => data.Children.IndexOf(d => d.PropertyId == c.Property.Id)).OfType<IOverwriteablePropertyModel>();
                    if (ids.Length > 0)
                    {
                        targetChildren = targetChildren.Where(c => ids.Contains(c.Property.Id));
                    }
                    foreach (var (childData, child) in data.Children.Zip(targetChildren))
                    {
                        switch (child)
                        {
                            case PropertyModel:
                            case AppendableProperty:
                                child.PasteProperty(childData);
                                break;
                            default:
                                // NOTE: グループの貼り付けは孫以降すべて上書きする
                                child.OverwriteProperty(childData);
                                break;
                        }
                        child.OverwriteProperty(childData);
                    }
                    HistoryModel.EndGroup();
                }
                else
                {
                    HistoryModel.AbortGroup();
                }
            }
            else if (ids.Length > 0)
            {
                var propertyData = data.Children[0];
                var target = Children.Where(c => c.Property.PropertyType.GetType().FullName == propertyData.PropertyTypeName && ids.Contains(c.Property.Id));

                var pasted = false;
                foreach (var t in target)
                {
                    t.PasteProperty(propertyData);
                    pasted = true;
                }

                if (pasted)
                {
                    HistoryModel.EndGroup();
                }
                else
                {
                    HistoryModel.AbortGroup();
                }
            }
            else
            {
                HistoryModel.AbortGroup();
            }
        }

        private void Child_ValueUpdated(object? sender, EventArgs e)
        {
            ValueUpdated?.Invoke(sender, e);
        }

        private void Child_ValueCommited(object? sender, EventArgs e)
        {
            ValueCommited?.Invoke(sender, e);
        }

        private void LayerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerModel.SourceStartPoint))
            {
                SourceStartPoint = LayerModel?.SourceStartPoint ?? 0.0;
            }
        }
    }
}
