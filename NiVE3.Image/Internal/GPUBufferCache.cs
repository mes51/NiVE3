using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.Image.Internal
{
    class GPUBufferCache : CriticalFinalizerObject, IDisposable
    {
        public static double CacheLimitRate { get; set; } = 0.3;

        static readonly int Float4Size = Marshal.SizeOf<Float4>();

        static readonly Dictionary<GraphicsDevice, GPUBufferCache> Instances = [];

        static bool UseCache { get; set; } = true;

        Dictionary<int, List<ReadWriteBuffer<Float4>>> ImageCache { get; } = [];

        Dictionary<int, List<ReadWriteBuffer<float>>> MaskCache { get; } = [];

        GraphicsDevice Device { get; }

        long TotalUsedMemorySize { get; set; }

        long CacheLimit { get; }

        readonly object SyncObj = new object();

        private GPUBufferCache(GraphicsDevice device)
        {
            Device = device;
            CacheLimit = (long)((device.SharedMemorySize + device.DedicatedMemorySize) * CacheLimitRate);
        }

        public static GPUBufferCache GetInstance(GraphicsDevice device)
        {
            if (!Instances.ContainsKey(device))
            {
                Instances.Add(device, new GPUBufferCache(device));
            }

            return Instances[device];
        }

        public static void SetUseCache(bool useCache)
        {
            UseCache = useCache;

            if (!useCache)
            {
                foreach (var i in Instances.Values)
                {
                    i.Dispose();
                }
                Instances.Clear();
            }
        }

        public ReadWriteBuffer<Float4> RentImageBuffer(int size)
        {
            if (!UseCache)
            {
                return Device.AllocateReadWriteBuffer<Float4>(size);
            }

            lock (SyncObj)
            {
                if (!ImageCache.TryGetValue(size, out var cacheList))
                {
                    cacheList = [];
                    ImageCache.Add(size, cacheList);
                }

                if (cacheList.Count == 0)
                {
                    var allocSize = size * Float4Size;
                    FreeImageBuffer(allocSize);
                    FreeMaskBuffer(allocSize);

                    var result = Device.AllocateReadWriteBuffer<Float4>(size);
                    TotalUsedMemorySize += allocSize;
                    return result;
                }
                else
                {
                    var result = cacheList[^1];
                    cacheList.RemoveAt(cacheList.Count - 1);

                    return result;
                }
            }
        }

        public ReadWriteBuffer<float> RentMaskBuffer(int size)
        {
            if (!UseCache)
            {
                return Device.AllocateReadWriteBuffer<float>(size);
            }

            lock (SyncObj)
            {
                if (!MaskCache.TryGetValue(size, out var cacheList))
                {
                    cacheList = [];
                    MaskCache.Add(size, cacheList);
                }

                if (cacheList.Count == 0)
                {
                    var allocSize = size * sizeof(float);
                    FreeMaskBuffer(allocSize);
                    FreeImageBuffer(allocSize);

                    var result = Device.AllocateReadWriteBuffer<float>(size);
                    TotalUsedMemorySize += allocSize;
                    return result;
                }
                else
                {
                    var result = cacheList[^1];
                    cacheList.RemoveAt(cacheList.Count - 1);

                    return result;
                }
            }
        }

        public void ReturnImageBuffer(ReadWriteBuffer<Float4> buffer)
        {
            if (!UseCache)
            {
                buffer.Dispose();
            }

            lock (SyncObj)
            {
                if (ImageCache.TryGetValue(buffer.Length, out var cacheList))
                {
                    cacheList.Add(buffer);
                }
                else
                {
                    buffer.Dispose();
                }
            }
        }

        public void ReturnMaskBuffer(ReadWriteBuffer<float> buffer)
        {
            if (!UseCache)
            {
                buffer.Dispose();
            }

            lock (SyncObj)
            {
                if (MaskCache.TryGetValue(buffer.Length, out var cacheList))
                {
                    cacheList.Add(buffer);
                }
                else
                {
                    buffer.Dispose();
                }
            }
        }

        void FreeImageBuffer(long need)
        {
            if (CacheLimit - TotalUsedMemorySize > need)
            {
                return;
            }
            foreach (var buffers in ImageCache.Values)
            {
                foreach (var buffer in buffers.ToArray())
                {
                    var free = buffer.Length * Float4Size;
                    buffer.Dispose();
                    buffers.Remove(buffer);
                    TotalUsedMemorySize -= free;

                    if (CacheLimit - TotalUsedMemorySize > need)
                    {
                        return;
                    }
                }
            }
        }

        void FreeMaskBuffer(long need)
        {
            if (CacheLimit - TotalUsedMemorySize > need)
            {
                return;
            }
            foreach (var buffers in MaskCache.Values)
            {
                foreach (var buffer in buffers.ToArray())
                {
                    var free = buffer.Length * sizeof(float);
                    buffer.Dispose();
                    buffers.Remove(buffer);
                    TotalUsedMemorySize -= free;

                    if (CacheLimit - TotalUsedMemorySize > need)
                    {
                        return;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            foreach (var buffer in ImageCache.Values.SelectMany(_ => _))
            {
                buffer.Dispose();
            }
        }

        ~GPUBufferCache()
        {
            Dispose(false);
        }
    }
}
