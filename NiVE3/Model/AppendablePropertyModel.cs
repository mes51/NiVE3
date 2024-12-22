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
using NiVE3.Plugin.Property;
using Prism.Mvvm;
using NiVE3.Shared.Extension;
using NiVE3.View.Resource;
using System.IO.Hashing;
using NiVE3.Extension;
using System.ComponentModel;
using System.Collections.Specialized;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Model
{
    partial class AppendablePropertyModel : BindableBase, IPropertyModel, IOverwriteablePropertyModel
    {
        public string Name { get; }

        public double SourceStartPoint { get; set; }

        public bool IsEnable => true;

        public ObservableCollection<KeyFrame>? KeyFrames => null;

        private ObservableCollection<IPropertyModel> children = [];
        public ObservableCollection<IPropertyModel> Children
        {
            get { return children; }
            set
            {
                children.CollectionChanged -= Children_CollectionChanged;

                SetProperty(ref children, value);

                value.CollectionChanged += Children_CollectionChanged;
            }
        }

        public PropertyBase Property { get; }

        public IPropertyModel ParentPropertyModel { get; }

        public Int128 ObjectId { get; }

        public string Id => Property.Id;

        public AppendablePropertyItem[] Items { get; }

        public event EventHandler<EventArgs>? ValueUpdated;

        public event EventHandler<EventArgs>? ValueCommited;

        bool UseEnableSwitch { get; }

        ProjectModel ProjectModel { get; }

        CompositionModel CompositionModel { get; }

        LayerModel LayerModel { get; }

        EffectModel? EffectModel { get; }

        HistoryModel HistoryModel { get; }

        public AppendablePropertyModel(PropertyBase property, IPropertyModel parentPropertyModel, ProjectModel projectModel, CompositionModel compositionModel, LayerModel layerModel, EffectModel? effectModel, HistoryModel historyModel)
        {
            Property = property;
            ParentPropertyModel = parentPropertyModel;
            ProjectModel = projectModel;
            CompositionModel = compositionModel;
            LayerModel = layerModel;
            EffectModel = effectModel;
            HistoryModel = historyModel;
            Name = property.DisplayName;
            SourceStartPoint = layerModel.SourceStartPoint;
            Children = [];

            var objectIdHash = new XxHash3();
            objectIdHash.Append(parentPropertyModel.ObjectId);
            objectIdHash.Append(property.Id);
            ObjectId = objectIdHash.ToInt128();

            var ap = (AppendableProperty)property;
            Items = ap.Items;
            UseEnableSwitch = ap.UseEnableSwitch;
            if (ap.DefaultAppendedItem != null)
            {
                AddChildInternal(ap.DefaultAppendedItem, null);
            }

            // NOTE: 本来はモデル側から設定してもらうものだが、引き回しの経路が複雑になりすぎる(レイヤーからだったり、エフェクトやマスクだったり)ため、自分から取りに行く
            layerModel.PropertyChanged += LayerModel_PropertyChanged;
        }

        public PropertyControlBase CreateControl(IPropertyViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public PropertyViewState CreateState(IPropertyViewModel viewModel)
        {
            return Property.CreateState(new CompositionViewModelProxy(CompositionModel), LayerModel != null ? new LayerViewModelProxy(LayerModel) : null, EffectModel != null ? new EffectViewModelProxy(EffectModel) : null, viewModel);
        }

        public bool ClearExpressionError()
        {
            var result = false;
            foreach (var c in Children)
            {
                result |= c.ClearExpressionError();
            }

            return result;
        }

        public IReadOnlyCollection<IPropertyObject>? GetChildren()
        {
            return Children;
        }

        object? IPropertyObject.GetValue(Time layerTime)
        {
            return GetValue(layerTime, layerTime + SourceStartPoint);
        }

        public object? GetValue(double time, double globalTime)
        {
            return GetChildPropertyValues(time, globalTime, false);
        }

        PropertyValueGroup? IPropertyObject.GetValues(double layerTime, bool withoutDisableProperty)
        {
            return null;
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

        public bool IsChangeableByTime()
        {
            return Children.Any(p => p.IsChangeableByTime()) || HasKeyFrames();
        }

        public void CreateKeyFrames(Guid[] propertyInstanceIds)
        {
            var children = Children.OfType<PropertyGroupModel>().Where(c => propertyInstanceIds.Contains(c.InstanceId));
            if (!children.Any())
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_AddKeyFrame));

            foreach (var c in children)
            {
                c.CreateKeyFramesAllChildren();
            }

            HistoryModel.EndGroup();
        }

        public void CreateKeyFramesAllChildren()
        {
            CreateKeyFrames([.. Children.OfType<PropertyGroupModel>().Select(c => c.InstanceId)]);
        }

        public void ResetProperties(Guid[] propertyInstanceIds)
        {
            var children = Children.OfType<PropertyGroupModel>().Where(c => propertyInstanceIds.Contains(c.InstanceId));
            if (!children.Any())
            {
                return;
            }

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_ResetPropertyValue));

            foreach (var c in children)
            {
                c.ResetAllChildren();
            }

            HistoryModel.EndGroup();
        }

        public void ResetAllChildren()
        {
            ResetProperties([.. Children.OfType<PropertyGroupModel>().Select(c => c.InstanceId)]);
        }

        public void AddChild(AppendablePropertyItem item)
        {
            var child = AddChildInternal(item, null);
            ValueCommited?.Invoke(this, EventArgs.Empty);

            HistoryModel.Add(new AddAppendablePropertyChildHistoryCommand(this, child, Children.IndexOf(child)));
        }

        public void DeleteChildren(Guid[] propertyInstanceIds)
        {
            DeleteChildrenInternal(propertyInstanceIds, false);
        }

        public void MoveChild(Guid propertyInstanceId, int newIndex)
        {
            MoveChildren([propertyInstanceId], propertyInstanceId, newIndex);
        }

        public void MoveChildren(Guid[] propertyInstanceIds, Guid referencePropertyInstanceId, int newIndex)
        {
            if (Children.Count == propertyInstanceIds.Length)
            {
                return;
            }

            var prevOrderedChildren = Children.OfType<PropertyGroupModel>().ToArray();
            var targetGroups = prevOrderedChildren.Where(g => propertyInstanceIds.Contains(g.InstanceId)).ToArray();
            var prevIndices = targetGroups.Select(Children.IndexOf).ToArray();
            var startIndex = newIndex - targetGroups.FindIndex(c => c.InstanceId == referencePropertyInstanceId);
            var newOrderedChildren = new List<IPropertyModel>(prevOrderedChildren.Length);
            newOrderedChildren.AddRange(prevOrderedChildren.Except(targetGroups).Take(startIndex));
            newOrderedChildren.AddRange(targetGroups);
            newOrderedChildren.AddRange(prevOrderedChildren.Except(newOrderedChildren.ToArray()));

            Children.SortBy(newOrderedChildren.IndexOf);
            ValueCommited?.Invoke(this, EventArgs.Empty);

            if (!prevOrderedChildren.SequenceEqual(Children))
            {
                HistoryModel.Add(new MoveAppendablePropertyChildrenHistoryCommand(this, prevOrderedChildren, [.. Children]));
            }
        }

        public PropertyValueGroup[] GetChildPropertyValues(double time, double globalTime, bool withoutDisableProperty)
        {
            if (withoutDisableProperty)
            {
                return Children.OfType<PropertyGroupModel>().Where(m => m.IsEnable).Select(m => m.GetValues(time, globalTime, withoutDisableProperty)).ToArray();
            }
            else
            {
                return Children.OfType<PropertyGroupModel>().Select(m => m.GetValues(time, globalTime, withoutDisableProperty)).ToArray();
            }
        }

        public PropertyData SaveData()
        {
            return new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = Children.Select(p => p.SaveData()).ToArray()
            };
        }

        public void LoadData(PropertyData data)
        {
            if (data.Children == null)
            {
                return;
            }

            Children.Clear();

            foreach (var childData in data.Children)
            {
                var item = Items.FirstOrDefault(i => i.Id == childData.PropertyId);
                if (item == null)
                {
                    continue;
                }

                AddChildInternal(item, childData.InstanceId).LoadData(childData);
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
            PasteChildrenInternal(data, false);
        }

        public void OverwriteProperty(PropertyData data)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName)
            {
                return;
            }

            var oldChildren = Children.ToArray();

            LoadData(data);

            HistoryModel.Add(new OverwritePropertyHistoryCommand(this, oldChildren, [.. Children]));
        }

        public CopyData<PropertyData> CutChildren(Guid[] propertyInstanceIds)
        {
            var result = CopyChildrenProperty(propertyInstanceIds);
            DeleteChildrenInternal(propertyInstanceIds, true);

            return result;
        }

        public CopyData<PropertyData> CopyChildrenProperty(Guid[] ids)
        {
            var children = Children.OfType<PropertyGroupModel>().Where(p => ids.Contains(p.InstanceId)).OrderBy(Children.IndexOf);
            var data = new PropertyData
            {
                PropertyId = Property.Id,
                PropertyTypeName = Property.PropertyType.GetType().FullName ?? "",
                Name = Name,
                Children = [.. children.Select(c => c.SaveData())]
            };

            return new CopyData<PropertyData>(CopyDataType.AppendablePropertyChildren, [data]);
        }

        public void PasteChildrenProperty(CopyData<PropertyData> data, Guid? targetId)
        {
            if (data.Data.Length < 1)
            {
                return;
            }

            if (data.Type == CopyDataType.AppendablePropertyChildren && data.Data[0].PropertyId == Property.Id)
            {
                PasteChildrenInternal(data.Data[0], false);
            }
            else if (data.Type == CopyDataType.PropertyGroup &&
                Children.OfType<PropertyGroupModel>().FirstOrDefault(c => c.InstanceId == targetId) is IOverwriteablePropertyModel targetChild &&
                targetChild.Property.Id == data.Data[0].PropertyId)
            {
                targetChild.OverwriteProperty(data.Data[0]);
            }
        }

        public void DuplicateChildrenProperty(Guid[] ids)
        {
            var data = CopyChildrenProperty(ids);
            PasteChildrenInternal(data.Data[0], true);
        }

        public void ChangeIsEnable(Guid[] ids, bool isEnable)
        {
            var children = Children.OfType<PropertyGroupModel>().Where(c => ids.Contains(c.InstanceId)).ToArray();
            var oldState = children.Select(c => c.IsEnable).ToArray();

            foreach (var c in children)
            {
                c.IsEnable = isEnable;
            }

            HistoryModel.Add(new ChangeIsEnableHistoryCommand(this, children, oldState, isEnable));

            ValueCommited?.Invoke(this, EventArgs.Empty);
        }

        PropertyGroupModel AddChildInternal(AppendablePropertyItem item, Guid? instanceId)
        {
            var group = item.CreateFunc();
            var groupModel = new PropertyGroupModel(group, this, ProjectModel, CompositionModel, LayerModel, EffectModel, HistoryModel, UseEnableSwitch, instanceId);
            groupModel.ValueUpdated += Child_ValueUpdated;
            groupModel.ValueCommited += Child_ValueCommited;

            var newChildNumber = 1;
            var newName = "";
            while (true)
            {
                newName = $"{groupModel.Name} {newChildNumber}";
                if (Children.All(c => c.Name != newName))
                {
                    break;
                }
                newChildNumber++;
            }
            groupModel.Name = newName;

            Children.Add(groupModel);

            return groupModel;
        }

        void InsertInternal(int index, PropertyGroupModel child)
        {
            if (Items.All(i => i.Id != child.Property.Id))
            {
                return;
            }

            child.ValueUpdated += Child_ValueUpdated;
            child.ValueCommited += Child_ValueCommited;
            Children.Insert(index, child);
        }

        void RemoveInternal(PropertyGroupModel child)
        {
            if (Children.Remove(child))
            {
                child.ValueUpdated -= Child_ValueUpdated;
                child.ValueCommited -= Child_ValueCommited;
            }
        }

        void DeleteChildrenInternal(Guid[] propertyInstanceIds, bool isCut)
        {
            var children = Children.OfType<PropertyGroupModel>().Where(c => propertyInstanceIds.Contains(c.InstanceId)).ToArray();
            var indices = children.Select(Children.IndexOf).ToArray();

            foreach (var c in children)
            {
                RemoveInternal(c);
            }
            ValueCommited?.Invoke(this, EventArgs.Empty);

            HistoryModel.Add(new DeleteAppendablePropertyChildHistoryCommand(this, children, indices, isCut));
        }

        void PasteChildrenInternal(PropertyData data, bool isDuplicate)
        {
            if (Property.PropertyType.GetType().FullName != data.PropertyTypeName)
            {
                return;
            }

            var newChildren = new List<PropertyGroupModel>();
            foreach (var childData in data.Children ?? [])
            {
                var item = Items.FirstOrDefault(i => i.Id == childData.PropertyId);
                if (item == null)
                {
                    continue;
                }

                var newChild = AddChildInternal(item, null);
                newChild.LoadData(childData);
                newChildren.Add(newChild);
            }

            if (newChildren.Count > 0)
            {
                HistoryModel.Add(new PastePropertyHistoryCommand(this, [.. newChildren], isDuplicate));
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

        private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ValueCommited?.Invoke(this, EventArgs.Empty);
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
