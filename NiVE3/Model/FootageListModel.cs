using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using NiVE3.Config;
using NiVE3.Extension;
using NiVE3.Input;
using NiVE3.Input.Special;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Shared.Extension;
using NiVE3.Util;
using NiVE3.View.Resource;
using Prism.Mvvm;

namespace NiVE3.Model
{
    partial class FootageListModel : BindableBase
    {
        public IReadOnlyDictionary<Type, IInputMetadata> InputMetadatas { get; private set; } = new Dictionary<Type, IInputMetadata>();

        [ImportMany]
        List<ExportFactory<IInput, IInputMetadata>>? Inputs { get; set; }

        private ObservableCollection<IFootageModel> footages = new ObservableCollection<IFootageModel>();
        public ObservableCollection<IFootageModel> Footages
        {
            get { return footages; }
            set { SetProperty(ref footages, value); }
        }

        private FootageSortKey sortKey = FootageSortKey.Name;
        public FootageSortKey SortKey
        {
            get { return sortKey; }
            set { SetProperty(ref sortKey, value); }
        }

        private bool sortIsAscending = true;
        public bool SortIsAscending
        {
            get { return sortIsAscending; }
            set { SetProperty(ref sortIsAscending, value); }
        }

        public FootageModel CameraFootage { get; }

        public FootageModel LightFootage { get; }

        public FootageModel NullObjectFootage { get; }

        Dictionary<Type, string[]> SupportedFileTypes { get; set; } = new Dictionary<Type, string[]>();

        List<InputModel> LoadedInputs { get; } = new List<InputModel>();

        HistoryModel HistoryModel { get; }

        AcceleratorModel AcceleratorModel { get; }

        public event EventHandler<ShowLoadSettingEventArgs>? ShowLoadSetting;

        public event EventHandler<FootageModelEventArgs>? ShowFootagePreview;

        public event EventHandler<CompositionEventArgs>? ShowCompositionPreview;

        public event EventHandler<FootageEventArgs>? FootageDeleted;

        public event EventHandler<FootageEventArgs>? DeleteFootageByUndo;

        public FootageListModel(AcceleratorModel acceleratorModel, HistoryModel historyModel)
        {
            var pluginCatalog = new DirectoryCatalog(Paths.PluginDirectory);
            var selfCatalog = new AssemblyCatalog(typeof(FootageListModel).Assembly);
            var catalog = new AggregateCatalog(pluginCatalog, selfCatalog);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            InitializePlugin();

            AcceleratorModel = acceleratorModel;
            HistoryModel = historyModel;

            //TODO: イベントの追加方法をfieldに対し行うか、nullableにした上でコンストラクタでインスタンスをセットするのが良いか
            Footages = new ObservableCollection<IFootageModel>();

            PropertyChanged += FootageListModel_PropertyChanged;

            CameraFootage = new FootageModel(new InputModel(CameraInput.Instance, false), EmptyFootageSource.Instance, HistoryModel);
            LightFootage = new FootageModel(new InputModel(LightInput.Instance, false), EmptyFootageSource.Instance, HistoryModel);
            NullObjectFootage = new FootageModel(new InputModel(NullObjectInput.Instance, false), EmptyFootageSource.Instance, HistoryModel);
        }

        public Guid? AddSolid()
        {
            var solidInput = new SolidInput();
            var solidFolder = Footages.OfType<FootageFolderModel>().FirstOrDefault(f => f.Name == ApplicationSetting.Setting.SolidFolderName);
            var createFolder = false;
            if (solidFolder == null)
            {
                HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_LoadFootageFile));
                solidFolder = AddFolderInternal(ApplicationSetting.Setting.SolidFolderName);
                createFolder = true;
            }
            var loaded = LoadFile(solidInput, "", solidFolder.FootageId, null, true);
            if (createFolder)
            {
                if (loaded != null)
                {
                    HistoryModel.EndGroup();
                }
                else
                {
                    HistoryModel.AbortGroup();
                }
            }

            switch (loaded)
            {
                case FootageModel solid:
                    return solid.FootageId;
                case FootageFolderModel folder:
                    return Flatten(folder.Children).First().FootageId;
                default:
                    return null;
            }
        }

        public void AddComposition(CompositionModel composition)
        {
            var compositionInput = new CompositionInput(composition);
            LoadFile(compositionInput, "", null, null, true);
        }

        public void AddFolder()
        {
            AddFolderInternal(null);
        }

        public void MoveFootage(Guid sourceFootageId, Guid targetFolderId)
        {
            var model = FindModel(sourceFootageId, Footages);
            var targetFolder = FindModel(targetFolderId, Footages) as FootageFolderModel;

            if (model == null || targetFolder == null)
            {
                return;
            }

            var oldParent = FindParent(sourceFootageId);
            if (oldParent == null)
            {
                // root
                DeleteFootageFromRootInternal(model);
            }
            else
            {
                oldParent.RemoveFootage(model);
            }
            targetFolder.AddFootage(model);

            HistoryModel.Add(new MoveHistoryCommand(this, sourceFootageId, oldParent?.FootageId, targetFolderId));
        }

        public void MoveFootages(Guid[] sourceFootageIds, Guid targetFolderId)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveFootage));

            foreach (var id in  sourceFootageIds)
            {
                MoveFootage(id, targetFolderId);
            }

            HistoryModel.EndGroup();
        }

        public void MoveFootageToRoot(Guid sourceFootageId)
        {
            var model = FindModel(sourceFootageId, Footages);
            if (model == null)
            {
                return;
            }

            var oldParent = FindParent(sourceFootageId);
            if (oldParent != null)
            {
                oldParent.RemoveFootage(model);
                AddFootageToRoot(model);

                HistoryModel.Add(new MoveHistoryCommand(this, sourceFootageId, oldParent.FootageId, null));
            }
        }

        public void MoveFootagesToRoot(Guid[] sourceFootageIds)
        {
            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_MoveFootage));

            foreach (var id in sourceFootageIds)
            {
                MoveFootageToRoot(id);
            }

            HistoryModel.EndGroup();
        }

        public void DeleteFootages(Guid[] footageIds)
        {
            var targetIds = new List<Guid>(footageIds);
            foreach (var id in footageIds)
            {
                var parent = FindParent(id);
                while(parent != null)
                {
                    if (targetIds.Contains(parent.FootageId))
                    {
                        targetIds.Remove(id);
                        break;
                    }
                    parent = FindParent(parent.FootageId);
                }
            }

            var footages = targetIds.Select(id => FindModel(id, Footages)).NonNull().ToArray();
            var parents = footages.Select(f => FindParent(f.FootageId)).ToArray();
            var allTargetFootages = Flatten(footages).ToArray();
            var allFootages = Flatten(Footages);
            var inputs = allTargetFootages.Select(m => m.InputModel)
                .Distinct()
                .Where(i =>
                {
                    var fs = allFootages.Where(m => m.InputModel == i).ToArray();
                    return allTargetFootages.Intersect(fs).Count() == fs.Length;
                })
                .ToArray();

            HistoryModel.BeginGroup(LanguageResourceDictionary.Dictionary.GetText(LanguageResourceDictionary.History_DeleteFootages));

            foreach (var f in footages)
            {
                DeleteFootageInternal(f);
            }
            HistoryModel.Add(new DeleteFootageHistoryCommand(this, footages, parents, inputs));

            OnFootageDeleted(allTargetFootages);

            HistoryModel.EndGroup();
        }

        public bool CheckSupportFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).Trim('.');
            return SupportedFileTypes.Values.Any(e => e.Contains(ext));
        }

        public void LoadFile(string filePath, Guid? targetFolderId)
        {
            if (Inputs == null || !File.Exists(filePath))
            {
                return;
            }

            var ext = Path.GetExtension(filePath).Trim('.');
            foreach (var (t, supported) in SupportedFileTypes)
            {
                if (!supported.Contains(ext))
                {
                    continue;
                }

                var factory = Inputs.First(i => i.Metadata.PluginType == t);
                var context = factory.CreateExport();
                if (LoadFile(context.Value, filePath, targetFolderId, context, factory.Metadata.IsSupportLoadToGpu) != null)
                {
                    break;
                }
                else
                {
                    context.Dispose();
                }
            }
        }

        public void ShowPreview(Guid footageId)
        {
            var footage = FindModel(footageId, Footages) as FootageModel;
            if (footage == null)
            {
                return;
            }

            if (footage.InputModel.Input is CompositionInput compositionInput)
            {
                OnShowCompositionPreview(compositionInput.Composition);
            }
            else
            {
                OnShowFootagePreview(footage);
            }
        }

        public FootageModel[] GetFootages(Guid footageId)
        {
            var footage = FindModel(footageId, Footages);
            if (footage == null)
            {
                return Array.Empty<FootageModel>();
            }

            if (footage is FootageModel footageModel)
            {
                return new FootageModel[] { footageModel };
            }
            else if (footage is FootageFolderModel footageFolderModel)
            {
                return Flatten(footageFolderModel.Children).ToArray();
            }

            return Array.Empty<FootageModel>();
        }

        FootageFolderModel AddFolderInternal(string? name)
        {
            var folder = new FootageFolderModel(HistoryModel);
            if (!string.IsNullOrEmpty(name))
            {
                folder.Name = name;
            }
            AddFootage(folder, null);

            HistoryModel.Add(new AddFolderHistoryCommand(this, name, folder.FootageId, null));

            return folder;
        }

        void AddInput(InputModel inputModel)
        {
            LoadedInputs.Add(inputModel);
        }

        void DeleteInput(InputModel inputModel)
        {
            LoadedInputs.Remove(inputModel);
        }

        FootageFolderModel? FindParent(Guid targetId)
        {
            // rootの場合はnull
            if (Footages.Any(f => f.FootageId == targetId))
            {
                return null;
            }

            return Footages.OfType<FootageFolderModel>()
                .Select(f => FindParent(targetId, f))
                .SkipWhile(p => p == null)
                .FirstOrDefault();
        }

        IFootageModel? LoadFile(IInput plugin, string fileName, Guid? targetFolderId, ExportLifetimeContext<IInput>? pluginContext, bool isSupportLoadToGpu)
        {
            if (isSupportLoadToGpu)
            {
                plugin.SetupAccelerator(AcceleratorModel);
            }
            if (!plugin.Load(fileName))
            {
                plugin.Dispose();
                return null;
            }

            if (InputMetadatas[plugin.GetType()].HasSettingView)
            {
                // TODO: コンポジションサイズをとってくる
                var view = plugin.GetLoadSetting(null);
                if (view != null)
                {
                    var e = new ShowLoadSettingEventArgs(view);
                    ShowLoadSetting?.Invoke(this, e);
                    if (!e.IsOK)
                    {
                        plugin.Dispose();
                        return null;
                    }

                    if (!plugin.ApplyLoadSetting(view.DataContext))
                    {
                        plugin.Dispose();
                        return null;
                    }
                }
            }

            var inputModel = pluginContext != null ? new InputModel(pluginContext, isSupportLoadToGpu) : new InputModel(plugin, isSupportLoadToGpu);

            var group = plugin.GetGroup();
            if (!group.Flatten().Any())
            {
                // ソースが何もなかった
                plugin.Dispose();
                return null;
            }

            IFootageModel loadedFootage;
            if (group.ChildrenGroup.Length < 1 && group.Sources.Length < 2)
            {
                loadedFootage = new FootageModel(inputModel, group.Sources[0], HistoryModel);
                AddFootage(loadedFootage, targetFolderId);
            }
            else
            {
                loadedFootage = AddFootageSourceGroup(inputModel, group, targetFolderId);
            }

            AddInput(inputModel);

            HistoryModel.Add(new LoadFileHistoryCommand(this, inputModel, loadedFootage, targetFolderId));

            return loadedFootage;
        }

        IFootageModel AddFootageSourceGroup(InputModel inputModel, FootageSourceGroup group, Guid? targetFolderId)
        {
            var folder = new FootageFolderModel(HistoryModel) { Name = group.Name };
            AddFootage(folder, targetFolderId);
            foreach (var s in group.Sources)
            {
                AddFootage(new FootageModel(inputModel, s, HistoryModel), folder.FootageId);
            }

            foreach (var c in group.ChildrenGroup)
            {
                AddFootageSourceGroup(inputModel, c, folder.FootageId);
            }

            return folder;
        }

        void AddFootage(IFootageModel footage, Guid? targetFolderId)
        {
            footage.SortKey = SortKey;
            footage.SortIsAscending = SortIsAscending;
            if (targetFolderId.HasValue && FindModel(targetFolderId.Value, Footages) is FootageFolderModel folder)
            {
                folder.AddFootage(footage);
            }
            else
            {
                AddFootageToRoot(footage);
            }
        }

        void AddFootageToRoot(IFootageModel footage)
        {
            footage.PropertyChanged += Footage_PropertyChanged;
            Footages.Add(footage);
            Footages.Sort(new FootageComparer(SortKey, SortIsAscending));
        }

        void DeleteFootageInternal(IFootageModel footage)
        {
            var folder = FindParent(footage.FootageId);
            if (folder != null)
            {
                folder.RemoveFootage(footage);
            }
            else
            {
                DeleteFootageFromRootInternal(footage);
            }
        }

        void DeleteFootageFromRootInternal(IFootageModel footage)
        {
            if (Footages.Contains(footage))
            {
                footage.PropertyChanged -= Footage_PropertyChanged;
                Footages.Remove(footage);
            }
        }

        // for test
        // NOTE: 本来は不要(直接コンストラクタに書きたい)が、MEFの都合上、テスト用のモッククラスを差し込めるようにするため、メソッドに切り出す
        // TODO: オブジェクト作成時にCatalogにモッククラスを差し込める方法があれば差し替える
        void InitializePlugin()
        {
            if (Inputs != null)
            {
                InputMetadatas = Inputs.Select(e => e.Metadata).ToDictionary(m => m.PluginType, m => m);
            }
            else
            {
                InputMetadatas = new ReadOnlyDictionary<Type, IInputMetadata>(new Dictionary<Type, IInputMetadata>());
            }

            SupportedFileTypes = InputMetadatas.ToDictionary(m => m.Key, m => m.Value.SupportedFileType.Split(",").Select(e => e.Trim('*', '.')).ToArray());
        }

        void OnShowFootagePreview(FootageModel footage)
        {
            ShowFootagePreview?.Invoke(this, new FootageModelEventArgs(footage));
        }

        void OnShowCompositionPreview(CompositionModel composition)
        {
            ShowCompositionPreview?.Invoke(this, new CompositionEventArgs(composition));
        }

        void OnDeleteFootageByUndo(IFootageModel[] footages)
        {
            DeleteFootageByUndo?.Invoke(this, new FootageEventArgs(footages));
        }

        void OnFootageDeleted(IFootageModel[] footages)
        {
            FootageDeleted?.Invoke(this, new FootageEventArgs(footages));
        }

        static IFootageModel? FindModel(Guid targetId, IEnumerable<IFootageModel> list)
        {
            var model = list.FirstOrDefault(f => f.FootageId == targetId);
            if (model != null)
            {
                return model;
            }

            return list.OfType<FootageFolderModel>()
                .Select(l => FindModel(targetId, l.Children))
                .FirstOrDefault(r => r != null);
        }

        static FootageFolderModel? FindParent(Guid targetId, FootageFolderModel parent)
        {
            if (parent.Children?.Any(f => f.FootageId == targetId) ?? false)
            {
                return parent;
            }

            return parent.Children?.OfType<FootageFolderModel>()
                ?.Select(f => FindParent(targetId, f))
                ?.SkipWhile(p => p == null)
                ?.FirstOrDefault();
        }

        static IEnumerable<FootageModel> Flatten(IEnumerable<IFootageModel> footages)
        {
            var result = new List<FootageModel>();

            foreach (var f in footages)
            {
                if (f is FootageModel footageModel)
                {
                    result.Add(footageModel);
                }
                else if (f is FootageFolderModel footageFolderModel)
                {
                    result.AddRange(Flatten(footageFolderModel.Children));
                }
            }

            return result;
        }

        private void FootageListModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SortKey) || e.PropertyName == nameof(SortIsAscending))
            {
                foreach (var footage in Footages)
                {
                    footage.SortKey = SortKey;
                    footage.SortIsAscending = SortIsAscending;
                }
                Footages.Sort(new FootageComparer(SortKey, SortIsAscending));
            }
        }

        private void Footage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Enum.TryParse(typeof(FootageSortKey), e.PropertyName, out var changed) && SortKey == (FootageSortKey)changed)
            {
                Footages.Sort(new FootageComparer(SortKey, SortIsAscending));
            }
        }
    }

    enum FootageSortKey
    {
        Name,
        Width,
        FrameRate,
        Duration,
        Comment,
        FilePath
    }

    class FootageComparer : IComparer<IFootageModel>
    {
        FootageSortKey Key { get; }

        bool Ascending { get; }

        public FootageComparer(FootageSortKey sortKey, bool ascending)
        {
            Key = sortKey;
            Ascending = ascending;
        }

        public int Compare(IFootageModel? x, IFootageModel? y)
        {
            if (x == null)
            {
                if (y != null)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
            else if (y == null)
            {
                return 1;
            }

            switch (Key)
            {
                case FootageSortKey.Width:
                    return x.Width.CompareTo(y.Width);
                case FootageSortKey.FrameRate:
                    return x.FrameRate.CompareTo(y.FrameRate);
                case FootageSortKey.Duration:
                    return x.Duration.CompareTo(y.Duration);
                case FootageSortKey.Comment:
                    return x.Comment.CompareTo(y.Comment);
                case FootageSortKey.FilePath:
                    return x.FilePath.CompareTo(y.FilePath);
                default:
                    return x.Name.CompareTo(y.Name);
            }
        }
    }

    file static class FootageSourceGroupExtension
    {
        public static IEnumerable<IFootageSource> Flatten(this FootageSourceGroup group)
        {
            return group.Sources.Concat(group.ChildrenGroup.SelectMany(c => c.Flatten()));
        }
    }
}
