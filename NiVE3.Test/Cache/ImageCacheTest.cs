using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using NiVE3.Cache;
using NiVE3.Config;
using NiVE3.Image;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Test.Cache
{
    /// <summary>
    /// ImageCache のテスト。GPU を使うテストは、ハードウェア GPU が無い環境ではスキップされます
    /// </summary>
    [NonParallelizable]
    public class ImageCacheTest
    {
        const int Width = 32;

        const int Height = 16;

        const long ImageSize = Width * Height * 16L;

        static readonly Int128 Key = 0x1234;

        static readonly Int128 OtherKey = 0x5678;

        Guid ObjectId { get; set; }

        ROI Roi { get; } = new ROI(new Int32Point(), new Int32Size(Width, Height), 0, 0, Width, Height);

        double OriginalGpuTierLimitRate { get; set; }

        bool OriginalUseGpuCache { get; set; }

        List<IDisposable> Disposables { get; } = [];

        [SetUp]
        public void SetUp()
        {
            ObjectId = Guid.NewGuid();
            OriginalGpuTierLimitRate = ImageCache.GpuTierLimitRate;
            OriginalUseGpuCache = ApplicationSetting.Setting.UseGpuCache;
            ImageCache.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in Disposables)
            {
                disposable.Dispose();
            }
            Disposables.Clear();

            ImageCache.ClearAll();
            ImageCache.GpuTierLimitRate = OriginalGpuTierLimitRate;
            SetUseGpuCache(OriginalUseGpuCache);
        }

        [Test]
        public void TestTryGetReturnsCopyOfAddedImage()
        {
            var source = CreateImage(1);
            var time = new Time(3, 30.0);

            ImageCache.Add(ObjectId, Key, time, source, Roi);

            Assert.That(ImageCache.TryGet(ObjectId, Key, time, null, out var cached), Is.True);
            Disposables.Add(cached.Item1);
            Assert.That(cached.Item1, Is.Not.SameAs(source));
            Assert.That(cached.Item2, Is.EqualTo(Roi));
            AssertSamePixels(source, cached.Item1);

            // 返された画像を破棄・変更してもキャッシュには影響しない
            ((NManagedImage)cached.Item1).GetDataSpan().Clear();
            Assert.That(ImageCache.TryGet(ObjectId, Key, time, null, out var cachedAgain), Is.True);
            Disposables.Add(cachedAgain.Item1);
            AssertSamePixels(source, cachedAgain.Item1);
        }

        [Test]
        public void TestTryGetMissesOtherKeyAndTime()
        {
            var source = CreateImage(1);
            var time = new Time(3, 30.0);

            ImageCache.Add(ObjectId, Key, time, source, Roi);

            Assert.That(ImageCache.TryGet(ObjectId, OtherKey, time, null, out _), Is.False);
            Assert.That(ImageCache.TryGet(ObjectId, Key, new Time(4, 30.0), null, out _), Is.False);
            Assert.That(ImageCache.TryGet(Guid.NewGuid(), Key, time, null, out _), Is.False);
        }

        [Test]
        public void TestTryGetByTimeIndependentKey()
        {
            var source = CreateImage(1);

            ImageCache.Add(ObjectId, Key, new Time(3, 30.0), source, Roi);

            Assert.That(ImageCache.TryGet(ObjectId, Key, null, out var cached), Is.True);
            Disposables.Add(cached.Item1);
            AssertSamePixels(source, cached.Item1);
            Assert.That(ImageCache.TryGet(ObjectId, OtherKey, null, out _), Is.False);
        }

        [Test]
        public void TestAddReplacesEntryOfSameTime()
        {
            var first = CreateImage(1);
            var second = CreateImage(2);
            var time = new Time(3, 30.0);

            ImageCache.Add(ObjectId, Key, time, first, Roi);
            ImageCache.Add(ObjectId, OtherKey, time, second, Roi);

            Assert.That(ImageCache.TryGet(ObjectId, Key, time, null, out _), Is.False);
            Assert.That(ImageCache.TryGet(ObjectId, OtherKey, time, null, out var cached), Is.True);
            Disposables.Add(cached.Item1);
            AssertSamePixels(second, cached.Item1);
            Assert.That(ImageCache.GetCachedTime(ObjectId), Is.EqualTo(new[] { time }));
        }

        [Test]
        public void TestClearRemovesOnlyTargetObject()
        {
            var otherObjectId = Guid.NewGuid();
            var time = new Time(3, 30.0);

            ImageCache.Add(ObjectId, Key, time, CreateImage(1), Roi);
            ImageCache.Add(otherObjectId, Key, time, CreateImage(2), Roi);
            ImageCache.Clear(ObjectId);

            Assert.That(ImageCache.TryGet(ObjectId, Key, time, null, out _), Is.False);
            Assert.That(ImageCache.TryGet(otherObjectId, Key, time, null, out var cached), Is.True);
            Disposables.Add(cached.Item1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestGpuHitReturnsSamePixelsWithAndWithoutGpuCache(bool useGpuCache)
        {
            var device = GetDeviceOrIgnore();
            SetUseGpuCache(useGpuCache);
            var source = CreateImage(1);
            var time = new Time(3, 30.0);
            ImageCache.Add(ObjectId, Key, time, source, Roi);

            // 1 回目はアップロード、2 回目以降は設定によって第 2 層からの GPU 内コピーになる。どの経路でも同じ画像が返る
            for (var i = 0; i < 4; i++)
            {
                Assert.That(ImageCache.TryGet(ObjectId, Key, time, device, out var byTime), Is.True);
                Disposables.Add(byTime.Item1);
                Assert.That(byTime.Item1, Is.InstanceOf<NGPUImage>());
                Assert.That(byTime.Item2, Is.EqualTo(Roi));
                AssertSamePixels(source, byTime.Item1);

                Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var byKey), Is.True);
                Disposables.Add(byKey.Item1);
                Assert.That(byKey.Item1, Is.Not.SameAs(byTime.Item1));
                AssertSamePixels(source, byKey.Item1);
            }
        }

        [Test]
        public void TestGpuTierIsNotUsedWhenGpuCacheIsDisabled()
        {
            var device = GetDeviceOrIgnore();
            SetUseGpuCache(false);
            ImageCache.Add(ObjectId, Key, new Time(3, 30.0), CreateImage(1), Roi);

            for (var i = 0; i < 3; i++)
            {
                Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cached), Is.True);
                Disposables.Add(cached.Item1);
            }

            Assert.That(ImageCache.GpuTierBytes, Is.Zero);
        }

        [Test]
        public void TestGpuTierPromotesTimeIndependentHitImmediately()
        {
            var device = GetDeviceOrIgnore();
            ImageCache.Add(ObjectId, Key, new Time(3, 30.0), CreateImage(1), Roi);

            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cached), Is.True);
            Disposables.Add(cached.Item1);

            Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize));
        }

        [Test]
        public void TestGpuTierPromotesTimeKeyedEntryOnSecondHit()
        {
            var device = GetDeviceOrIgnore();
            var time = new Time(3, 30.0);
            ImageCache.Add(ObjectId, Key, time, CreateImage(1), Roi);

            Assert.That(ImageCache.TryGet(ObjectId, Key, time, device, out var first), Is.True);
            Disposables.Add(first.Item1);
            Assert.That(ImageCache.GpuTierBytes, Is.Zero);

            Assert.That(ImageCache.TryGet(ObjectId, Key, time, device, out var second), Is.True);
            Disposables.Add(second.Item1);
            Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize));
        }

        [Test]
        public void TestGpuTierResultIsIndependentCopy()
        {
            var device = GetDeviceOrIgnore();
            var source = CreateImage(1);
            var other = CreateImage(2);
            var gpuOther = other.CopyToGpu(device);
            Disposables.Add(gpuOther);
            ImageCache.Add(ObjectId, Key, new Time(3, 30.0), source, Roi);

            // 第 2 層に載せる
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var promoted), Is.True);
            Disposables.Add(promoted.Item1);

            // 第 2 層から返された画像を書き換えても、キャッシュ側の画像は変わらない
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cached), Is.True);
            Disposables.Add(cached.Item1);
            gpuOther.CopyTo((NGPUImage)cached.Item1);
            AssertSamePixels(other, cached.Item1);

            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cachedAgain), Is.True);
            Disposables.Add(cachedAgain.Item1);
            AssertSamePixels(source, cachedAgain.Item1);
        }

        [Test]
        public void TestGpuTierDoesNotPromoteOnAddWithoutReuse()
        {
            var device = GetDeviceOrIgnore();
            var gpuImage = CreateImage(1).CopyToGpu(device);
            Disposables.Add(gpuImage);

            // 毎フレーム新しい結果を登録するだけで再利用しないオブジェクト (動画レイヤーなど) は第 2 層に載らない
            for (var frame = 0; frame < 4; frame++)
            {
                ImageCache.Add(ObjectId, Key, new Time(frame, 30.0), gpuImage, Roi);
            }

            Assert.That(ImageCache.GpuTierBytes, Is.Zero);
        }

        [Test]
        public void TestGpuTierPromotesOnAddWhenPreviousEntryWasReused()
        {
            var device = GetDeviceOrIgnore();
            var sources = Enumerable.Range(0, 4).Select(i => CreateImage(i + 1)).ToArray();

            // 同じ結果が 1 フレームの中で 3 回再利用されるオブジェクト (同じプリコンポーズを複数のレイヤーで使う場合など)
            for (var frame = 0; frame < sources.Length; frame++)
            {
                var time = new Time(frame, 30.0);
                var gpuImage = sources[frame].CopyToGpu(device);
                Disposables.Add(gpuImage);
                ImageCache.Add(ObjectId, Key, time, gpuImage, Roi);
                if (frame > 0)
                {
                    // 2 フレーム目以降は登録した時点で第 2 層に載る
                    Assert.That(ImageCache.GpuTierBytes, Is.GreaterThanOrEqualTo(ImageSize), $"frame {frame}");
                }

                for (var i = 0; i < 3; i++)
                {
                    Assert.That(ImageCache.TryGet(ObjectId, Key, time, device, out var cached), Is.True);
                    Disposables.Add(cached.Item1);
                    AssertSamePixels(sources[frame], cached.Item1);
                }
            }

            // 登録時に載せた複製は、使われなくなった古いフレームの分が解放される (frame 0 はヒットで載せた分、frame 2 / 3 が残る)
            Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize * 3));

            // 複製が解放されても、エントリ自体は第 1 層に残っている
            Assert.That(ImageCache.TryGet(ObjectId, Key, new Time(1, 30.0), device, out var old), Is.True);
            Disposables.Add(old.Item1);
            AssertSamePixels(sources[1], old.Item1);
        }

        [Test]
        public void TestGpuTierEvictionKeepsEntry()
        {
            var device = GetDeviceOrIgnore();
            var otherObjectId = Guid.NewGuid();
            var sourceA = CreateImage(1);
            var sourceB = CreateImage(2);
            var time = new Time(3, 30.0);
            ImageCache.Add(ObjectId, Key, time, sourceA, Roi);
            ImageCache.Add(otherObjectId, Key, time, sourceB, Roi);
            LimitGpuTierTo(device, ImageSize * 3 / 2);

            // 容量は 1 枚分。1 枚目が載る
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cachedA), Is.True);
            Disposables.Add(cachedA.Item1);
            Assert.That(ImageCache.HasGpuCopy(ObjectId, Key), Is.True);
            Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize));

            // 2 枚目の 1 回目のヒットでは、1 枚目のほうが最近使われているので載せない
            Assert.That(ImageCache.TryGet(otherObjectId, Key, device, out var cachedB), Is.True);
            Disposables.Add(cachedB.Item1);
            Assert.That(ImageCache.HasGpuCopy(ObjectId, Key), Is.True);
            Assert.That(ImageCache.HasGpuCopy(otherObjectId, Key), Is.False);

            // 続けてもう一度ヒットすると 2 枚目のほうが最近使われたことになり、1 枚目の複製を追い出して載せる
            Assert.That(ImageCache.TryGet(otherObjectId, Key, device, out var cachedBAgain), Is.True);
            Disposables.Add(cachedBAgain.Item1);
            Assert.That(ImageCache.HasGpuCopy(ObjectId, Key), Is.False);
            Assert.That(ImageCache.HasGpuCopy(otherObjectId, Key), Is.True);
            Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize));

            // 追い出されたのは GPU 上の複製だけで、エントリは残っている
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cachedAAgain), Is.True);
            Disposables.Add(cachedAAgain.Item1);
            AssertSamePixels(sourceA, cachedAAgain.Item1);
            AssertSamePixels(sourceB, cachedBAgain.Item1);
        }

        [Test]
        public void TestGpuTierDoesNotEvictMoreRecentlyUsedEntry()
        {
            var device = GetDeviceOrIgnore();
            var otherObjectId = Guid.NewGuid();
            var time = new Time(3, 30.0);
            ImageCache.Add(ObjectId, Key, time, CreateImage(1), Roi);
            ImageCache.Add(otherObjectId, Key, time, CreateImage(2), Roi);
            LimitGpuTierTo(device, ImageSize * 3 / 2);

            // 容量は 1 枚分。毎フレーム 2 枚が交互に使われる場合、載せては追い出すことを繰り返さず、先に載った 1 枚目だけを第 2 層に残す
            // (繰り返していれば、各フレームの最後に使った 2 枚目が載っている)
            for (var frame = 0; frame < 4; frame++)
            {
                Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cachedA), Is.True);
                Disposables.Add(cachedA.Item1);
                Assert.That(ImageCache.TryGet(otherObjectId, Key, device, out var cachedB), Is.True);
                Disposables.Add(cachedB.Item1);

                Assert.That(ImageCache.HasGpuCopy(ObjectId, Key), Is.True);
                Assert.That(ImageCache.HasGpuCopy(otherObjectId, Key), Is.False);
                Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize));
            }
        }

        [Test]
        public void TestReleaseGpuResourcesKeepsEntries()
        {
            var device = GetDeviceOrIgnore();
            var source = CreateImage(1);
            ImageCache.Add(ObjectId, Key, new Time(3, 30.0), source, Roi);
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var promoted), Is.True);
            Disposables.Add(promoted.Item1);
            Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize));

            ImageCache.ReleaseGpuResources();

            Assert.That(ImageCache.GpuTierBytes, Is.Zero);
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var cached), Is.True);
            Disposables.Add(cached.Item1);
            AssertSamePixels(source, cached.Item1);
        }

        [Test]
        public void TestClearReleasesGpuCopy()
        {
            var device = GetDeviceOrIgnore();
            ImageCache.Add(ObjectId, Key, new Time(3, 30.0), CreateImage(1), Roi);
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out var promoted), Is.True);
            Disposables.Add(promoted.Item1);
            Assert.That(ImageCache.GpuTierBytes, Is.EqualTo(ImageSize));

            ImageCache.Clear(ObjectId);

            Assert.That(ImageCache.GpuTierBytes, Is.Zero);
            Assert.That(ImageCache.TryGet(ObjectId, Key, device, out _), Is.False);
        }

        /// <summary>
        /// GPU を使うテスト用のデバイスを取得します。ハードウェア GPU が無い環境ではテストをスキップします
        /// </summary>
        GraphicsDevice GetDeviceOrIgnore()
        {
            GraphicsDevice? device = null;
            try
            {
                device = GraphicsDevice.GetDefault();
            }
            catch (Exception)
            {
                // NOTE: DirectX 12 が使えない環境
            }
            if (device == null || !device.IsHardwareAccelerated)
            {
                Assert.Ignore("ハードウェア GPU が無い環境ではテストしない");
            }

            // 第 2 層は設定の GPU キャッシュが有効なときだけ動作する
            SetUseGpuCache(true);

            return device!;
        }

        /// <summary>
        /// 設定の GPU キャッシュ (第 2 層と GPU バッファのプール) の有効・無効を切り替えます
        /// </summary>
        static void SetUseGpuCache(bool useGpuCache)
        {
            if (ApplicationSetting.Setting.UseGpuCache == useGpuCache)
            {
                return;
            }

            ApplicationSetting.Setting.UseGpuCache = useGpuCache;
            ApplicationSetting.Setting.RaiseUpdateSetting();
        }

        /// <summary>
        /// 第 2 層の上限を指定したバイト数にします
        /// </summary>
        void LimitGpuTierTo(GraphicsDevice device, long limitBytes)
        {
            // 上限は GPUBufferCache の上限に対する割合で指定するため、一度使用して現在の上限 (バイト) を得てから割合を逆算する
            var probeObjectId = Guid.NewGuid();
            ImageCache.Add(probeObjectId, Key, new Time(0, 30.0), CreateImage(99), Roi);
            Assert.That(ImageCache.TryGet(probeObjectId, Key, device, out var probe), Is.True);
            probe.Item1.Dispose();
            var poolLimitBytes = ImageCache.GpuTierLimitBytes / ImageCache.GpuTierLimitRate;
            ImageCache.Clear(probeObjectId);

            ImageCache.GpuTierLimitRate = limitBytes / poolLimitBytes;
        }

        NManagedImage CreateImage(int seed)
        {
            var image = new NManagedImage(Width, Height);
            Disposables.Add(image);
            var data = image.GetDataSpan();
            for (var i = 0; i < Width * Height; i++)
            {
                data[i] = new Vector4(seed + i * 0.25F, seed * 2.0F - i, (i % 7) / 7.0F, 1.0F / seed);
            }

            return image;
        }

        static void AssertSamePixels(NManagedImage expected, NImage actual)
        {
            var managedImage = actual.ToManaged();
            try
            {
                Assert.That(actual.Width, Is.EqualTo(expected.Width));
                Assert.That(actual.Height, Is.EqualTo(expected.Height));
                Assert.That(managedImage.GetDataSpan()[..expected.DataLength].SequenceEqual(expected.GetDataSpan()[..expected.DataLength]), Is.True);
            }
            finally
            {
                if (managedImage != actual)
                {
                    managedImage.Dispose();
                }
            }
        }
    }
}