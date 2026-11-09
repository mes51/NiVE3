using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Config;
using NiVE3.Image;
using NiVE3.Image.Internal;
using NiVE3.Plugin.ValueObject;
using NiVE3.Util;

namespace NiVE3.Cache
{
    class ImageCache
    {
        static readonly int ImageElementSize = Marshal.SizeOf<Vector4>();

        const int MinReuseCountForPromoteOnAdd = 2;

        const long DiscreteGpuMinimumDedicatedMemory = 1024L * Const.MiB;

        static ImageCache Instance { get; } = new ImageCache();

        static bool IsCompressCache { get; set; }

        public static double GpuTierLimitRate
        {
            get => Instance.GpuTierLimitRateValue;
            set
            {
                Instance.ReleaseGpuTier();
                Instance.GpuTierDevice = null;
                Instance.GpuTierLimitRateValue = value;
            }
        }

        public static long GpuTierBytes => Instance.GpuCachedSize;

        public static long GpuTierLimitBytes => Instance.GpuCacheLimit;

        long CacheLimit { get; set; } = Math.Min(16L * 1024 * Const.MiB, SystemInfo.MaxCacheLimit);

        DualKeyDictionary<Guid, (Time, Int128), (Guid, Int128), CacheEntry> CachedImages { get; } = [];

        long CachedSize { get; set; }

        ImageCacheKeyLru KeyLru { get; } = new ImageCacheKeyLru();

        double GpuTierLimitRateValue { get; set; } = 0.5;

        bool UseGpuTier { get; set; }

        LinkedList<CacheEntry> GpuLru { get; } = [];

        long GpuCachedSize { get; set; }

        long GpuCacheLimit { get; set; }

        GraphicsDevice? GpuTierDevice { get; set; }

        long AccessClock { get; set; }

        Dictionary<Guid, ObjectHistory> Histories { get; } = [];

        private ImageCache()
        {
            CacheLimit = Math.Min(ApplicationSetting.Setting.ImageCacheLimit * Const.MiB, SystemInfo.MaxCacheLimit);
            // TODO: GPUのバッファのキャッシュ周りの設定を他に持って行くかどうか検討する
            GPUBufferCache.CacheLimitRate = ApplicationSetting.Setting.GpuCacheLimitRate * 0.01;
            GPUBufferCache.SetUseCache(ApplicationSetting.Setting.UseGpuCache);
            UseGpuTier = ApplicationSetting.Setting.UseGpuCache;

            ApplicationSetting.Setting.UpdateSetting += Setting_UpdateSetting;
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, Time time, GraphicsDevice? device, out (NImage, ROI) image)
        {
            var result = CachedImages.TryGetValue(objectId, (time, key), out var entry);
            if (result && entry != null)
            {
                image = GetImage(objectId, entry, device, false);
                KeyLru.Add(objectId, key, time);
            }
            else
            {
                image = (null!, ROI.Empty);
            }
            return result;
        }

        private bool TryGetInternal(in Guid objectId, in Int128 key, GraphicsDevice? device, out (NImage, ROI) image)
        {
            if (CachedImages.TryGetValues((objectId, key), out var values))
            {
                var entry = values[0];
                image = GetImage(objectId, entry, device, true);
                KeyLru.UpdateLastAccessBySecondaryKey(objectId, key);
                return true;
            }
            else
            {
                image = (null!, ROI.Empty);
                return false;
            }
        }

        private void AddInternal(Guid objectId, in Int128 key, Time time, NImage image, ROI roi)
        {
            CacheEntry entry;
            var managedImage = image.ToManaged();
            // NOTE: 実際に確保したメモリの容量で判定する
            var managedImageSize = managedImage.Data.Length * ImageElementSize;
            if (IsCompressCache)
            {
                var qoiImage = Qoi.Encode(managedImage);
                var size = qoiImage.GetAllocatedSize();
                if (size < managedImageSize)
                {
                    entry = new CacheEntry(qoiImage, size, roi);
                    if (image != managedImage)
                    {
                        managedImage.Dispose();
                    }
                }
                else
                {
                    qoiImage.Dispose();
                    if (managedImage == image)
                    {
                        managedImage = (NManagedImage)managedImage.Copy();
                    }
                    entry = new CacheEntry(managedImage, managedImageSize, roi);
                }
            }
            else
            {
                if (managedImage == image)
                {
                    managedImage = (NManagedImage)managedImage.Copy();
                }
                entry = new CacheEntry(managedImage, managedImageSize, roi);
            }
            if (CachedImages.ContainsUpdateKey(objectId))
            {
                var oldImageKeys = CachedImages.GetUpdateTargetKeys(objectId).Where(k => k.Item1 == time).ToArray();
                if (oldImageKeys.Length > 0)
                {
                    foreach (var k in oldImageKeys)
                    {
                        var oldEntry = CachedImages[objectId, k];
                        CachedImages.Remove(objectId, k);
                        CachedSize -= oldEntry.Size;
                        DisposeEntry(oldEntry);
                        KeyLru.Remove(objectId, k.Item2, k.Item1);
                    }
                }

                CachedImages.Add(objectId, (time, key), (objectId, key), entry);
            }
            else
            {
                CachedImages.Add(objectId, (time, key), (objectId, key), entry);
            }
            KeyLru.Add(objectId, key, time);
            CachedSize += entry.Size;

            if (UseGpuTier && image is NGPUImage gpuImage)
            {
                PromoteOnAdd(objectId, entry, gpuImage);
            }

            while (CachedSize > CacheLimit)
            {
                var primaryKey = KeyLru.RemoveLast();
                if (primaryKey.Item1 == Guid.Empty)
                {
                    break;
                }

                var oldEntry = CachedImages[primaryKey.Item1, (primaryKey.Item3, primaryKey.Item2)];
                CachedSize -= oldEntry.Size;
                DisposeEntry(oldEntry);
                CachedImages.Remove(primaryKey.Item1, (primaryKey.Item3, primaryKey.Item2));
            }
        }

        private Time[] GetCachedTimeInternal(in Guid objectId)
        {
            if (CachedImages.ContainsUpdateKey(objectId))
            {
                return [..CachedImages.GetUpdateTargetKeys(objectId).Select(t => t.Item1)];
            }
            else
            {
                return [];
            }
        }

        private void ClearInternal(in Guid objectId)
        {
            if (!CachedImages.ContainsUpdateKey(objectId))
            {
                return;
            }

            foreach (var k in CachedImages.GetUpdateTargetKeys(objectId))
            {
                var entry = CachedImages[objectId, k];
                CachedSize -= entry.Size;
                DisposeEntry(entry);
                KeyLru.Remove(objectId, k.Item2, k.Item1);
            }
            CachedImages.Remove(objectId);
            Histories.Remove(objectId);
        }

        private void ClearAllInternal()
        {
            foreach (var entry in CachedImages.Values)
            {
                DisposeEntry(entry);
            }
            CachedImages.Clear();
            KeyLru.Clear();
            Histories.Clear();
            CachedSize = 0;
        }

        private (NImage, ROI) GetImage(in Guid objectId, CacheEntry entry, GraphicsDevice? device, bool isTimeIndependentKey)
        {
            if (device == null || !UseGpuTier)
            {
                return Decompress(entry, device);
            }

            BindGpuTierDevice(device);

            var previousAccess = entry.LastAccess;
            AccessClock++;
            entry.LastAccess = AccessClock;
            entry.HitCount++;
            var history = GetHistory(objectId);
            history.HitsSinceLastAdd++;
            history.LastHit = AccessClock;

            if (entry.GpuImage != null)
            {
                GpuLru.Remove(entry.GpuNode!);
                GpuLru.AddFirst(entry.GpuNode!);
                return (entry.GpuImage.CopyToGpu(), entry.Roi);
            }

            var (image, roi) = Decompress(entry, device);
            var gpuImage = image.ToGpu(device);
            if (gpuImage != image)
            {
                image.Dispose();
            }

            var wantPromote = isTimeIndependentKey || entry.HitCount >= 2;
            if (!wantPromote || !TryPromote(entry, gpuImage, previousAccess))
            {
                return (gpuImage, roi);
            }

            return (gpuImage.CopyToGpu(), roi);
        }

        private void PromoteOnAdd(in Guid objectId, CacheEntry entry, NGPUImage gpuImage)
        {
            var history = GetHistory(objectId);
            var previousEntryReuseCount = history.HitsSinceLastAdd;
            var lastHit = history.LastHit;
            var previousAdd = history.LastAdd;
            AccessClock++;
            history.HitsSinceLastAdd = 0;
            history.LastAdd = AccessClock;

            for (var i = history.PromotedOnAdd.Count - 1; i >= 0; i--)
            {
                var oldEntry = history.PromotedOnAdd[i];
                if (oldEntry.GpuImage != null && oldEntry.LastAccess >= previousAdd)
                {
                    continue;
                }

                ReleaseGpuImage(oldEntry);
                history.PromotedOnAdd.RemoveAt(i);
            }

            if (previousEntryReuseCount < MinReuseCountForPromoteOnAdd)
            {
                return;
            }

            BindGpuTierDevice(gpuImage.Data.GraphicsDevice);
            entry.LastAccess = AccessClock;
            var gpuCopy = gpuImage.CopyToGpu();
            if (!TryPromote(entry, gpuCopy, lastHit))
            {
                gpuCopy.Dispose();
                return;
            }

            history.PromotedOnAdd.Add(entry);
        }

        private bool TryPromote(CacheEntry entry, NGPUImage gpuImage, long candidateLastAccess)
        {
            var size = (long)gpuImage.DataLength * ImageElementSize;
            if (size > GpuCacheLimit)
            {
                return false;
            }

            var shortage = GpuCachedSize + size - GpuCacheLimit;
            for (var node = GpuLru.Last; shortage > 0 && node != null; node = node.Previous)
            {
                if (node.Value.LastAccess >= candidateLastAccess)
                {
                    return false;
                }
                shortage -= node.Value.GpuSize;
            }

            ReleaseGpuImage(entry);
            entry.GpuImage = gpuImage;
            entry.GpuNode = GpuLru.AddFirst(entry);
            GpuCachedSize += size;

            while (GpuCachedSize > GpuCacheLimit && GpuLru.Last != null && GpuLru.Last.Value != entry)
            {
                ReleaseGpuImage(GpuLru.Last.Value);
            }

            return true;
        }

        private ObjectHistory GetHistory(in Guid objectId)
        {
            if (!Histories.TryGetValue(objectId, out var history))
            {
                history = new ObjectHistory();
                Histories.Add(objectId, history);
            }

            return history;
        }

        private void BindGpuTierDevice(GraphicsDevice device)
        {
            if (GpuTierDevice == device)
            {
                return;
            }

            ReleaseGpuTier();
            GpuTierDevice = device;
            GpuCacheLimit = (long)(CalcGpuTierBudgetBasis(device) * GpuTierLimitRateValue);
        }

        // TODO: GPUBufferCache と使用量を共有化(ImageCache 優先)する
        private static long CalcGpuTierBudgetBasis(GraphicsDevice device)
        {
            var poolLimit = GPUBufferCache.GetInstance(device).CacheLimit;
            // NOTE: dGPU かどうかの判定が GraphicsDevice から出来ないので、DedicatedMemorySize のサイズで判定する
            if (device.DedicatedMemorySize < DiscreteGpuMinimumDedicatedMemory)
            {
                return poolLimit;
            }
            else
            {
                return Math.Min(poolLimit, (long)(device.DedicatedMemorySize * GPUBufferCache.CacheLimitRate));
            }
        }

        private void ReleaseGpuImage(CacheEntry entry)
        {
            if (entry.GpuImage == null)
            {
                return;
            }

            GpuCachedSize -= entry.GpuSize;
            if (entry.GpuNode != null)
            {
                GpuLru.Remove(entry.GpuNode);
            }
            entry.GpuImage.Dispose();
            entry.GpuImage = null;
            entry.GpuNode = null;
        }

        private void ReleaseGpuTier()
        {
            while (GpuLru.First != null)
            {
                ReleaseGpuImage(GpuLru.First.Value);
            }
            GpuCachedSize = 0;
        }

        private void DisposeEntry(CacheEntry entry)
        {
            ReleaseGpuImage(entry);
            entry.Image.Dispose();
        }

        private void Setting_UpdateSetting(object? sender, EventArgs e)
        {
            CacheLimit = ApplicationSetting.Setting.ImageCacheLimit * Const.MiB;
            if (ApplicationSetting.Setting.IsCompressCache != IsCompressCache)
            {
                ClearAllInternal();
                IsCompressCache = ApplicationSetting.Setting.IsCompressCache;
            }

            ReleaseGpuTier();
            GpuTierDevice = null;
            UseGpuTier = ApplicationSetting.Setting.UseGpuCache;

            // TODO: GPUのバッファのキャッシュ周りの設定を他に持って行くかどうか検討する
            GPUBufferCache.CacheLimitRate = ApplicationSetting.Setting.GpuCacheLimitRate * 0.01;
            GPUBufferCache.SetUseCache(ApplicationSetting.Setting.UseGpuCache);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, Time time, GraphicsDevice? device, out (NImage, ROI) image)
        {
            return Instance.TryGetInternal(objectId, key, time, device, out image);
        }

        public static bool TryGet(in Guid objectId, in Int128 key, GraphicsDevice? device, out (NImage, ROI) image)
        {
            return Instance.TryGetInternal(objectId, key, device, out image);
        }

        public static void Add(in Guid objectId, in Int128 key, Time time, NImage image, ROI roi)
        {
            Instance.AddInternal(objectId, key, time, image, roi);
        }

        public static Time[] GetCachedTime(in Guid objectId)
        {
            return Instance.GetCachedTimeInternal(objectId);
        }

        public static void Clear(in Guid objectId)
        {
            Instance.ClearInternal(objectId);
        }

        public static void ClearAll()
        {
            Instance.ClearAllInternal();
        }

        public static void ReleaseGpuResources()
        {
            Instance.ReleaseGpuTier();
            Instance.GpuTierDevice = null;
        }

        public static bool HasGpuCopy(in Guid objectId, in Int128 key)
        {
            return Instance.CachedImages.TryGetValues((objectId, key), out var values) && values[0].GpuImage != null;
        }

        static (NImage, ROI) Decompress(CacheEntry entry, GraphicsDevice? device)
        {
            if (entry.Image is NManagedImage notCompressed)
            {
                if (device != null)
                {
                    return (notCompressed.ToGpu(device), entry.Roi);
                }
                else
                {
                    return (notCompressed.Copy(), entry.Roi);
                }
            }
            else
            {
                return (Qoi.Decode((SlicedQoiImage)entry.Image), entry.Roi);
            }
        }

        sealed class CacheEntry
        {
            public IDisposable Image { get; }

            public long Size { get; }

            public ROI Roi { get; }

            public NGPUImage? GpuImage { get; set; }

            public LinkedListNode<CacheEntry>? GpuNode { get; set; }

            public long GpuSize => GpuImage != null ? (long)GpuImage.DataLength * ImageElementSize : 0;

            public int HitCount { get; set; }

            public long LastAccess { get; set; }

            public CacheEntry(IDisposable image, long size, ROI roi)
            {
                Image = image;
                Size = size;
                Roi = roi;
            }
        }

        sealed class ObjectHistory
        {
            public int HitsSinceLastAdd { get; set; }

            public long LastHit { get; set; }

            public long LastAdd { get; set; }

            public List<CacheEntry> PromotedOnAdd { get; } = [];
        }
    }
}