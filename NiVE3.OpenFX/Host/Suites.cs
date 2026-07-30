using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiVE3.OpenFX.Host.GL;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// fetchSuite の実装と各 Suite のネイティブ構造体の管理
    /// </summary>
    public static unsafe class SuiteRegistry
    {
        static readonly Dictionary<(string Name, int Version), nint> Suites = new Dictionary<(string, int), nint>();

        static readonly object Lock = new object();

        /// <summary>
        /// fetchSuite への関数ポインタを取得します
        /// </summary>
        public static delegate* unmanaged[Cdecl]<nint, byte*, int, void*> FetchSuitePointer => &FetchSuite;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void* FetchSuite(nint host, byte* suiteName, int version)
        {
            var name = Marshal.PtrToStringUTF8((nint)suiteName) ?? "";
            var suite = GetSuite(name, version);
            OfxLog.Info($"fetchSuite: {name} v{version} -> {(suite == null ? "未対応" : "提供")}");
            return suite;
        }

        static void* GetSuite(string name, int version)
        {
            lock (Lock)
            {
                if (Suites.TryGetValue((name, version), out var cached))
                {
                    return (void*)cached;
                }

                var suite = (name, version) switch
                {
                    (OfxNames.PropertySuite, 1) => PropertySuite.Build(),
                    (OfxNames.ImageEffectSuite, 1) => ImageEffectSuite.Build(),
                    (OfxNames.ParameterSuite, 1) => ParameterSuite.Build(),
                    (OfxNames.MemorySuite, 1) => MemorySuite.Build(),
                    (OfxNames.MultiThreadSuite, 1) => MultiThreadSuite.Build(),
                    (OfxNames.MessageSuite, 1) => MessageSuite.Build(),
                    (OfxNames.MessageSuite, 2) => MessageSuite.BuildV2(),
                    (OfxNames.ProgressSuite, 1) => ProgressSuite.BuildV1(),
                    (OfxNames.ProgressSuite, 2) => ProgressSuite.BuildV2(),
                    (OfxNames.TimeLineSuite, 1) => TimeLineSuite.Build(),
                    (OfxNames.OpenGLRenderSuite, 1) when GlContextManager.Shared != null => OpenGLRenderSuite.Build(),
                    _ => null
                };
                if (suite != null)
                {
                    Suites[(name, version)] = (nint)suite;
                }
                return suite;
            }
        }

        /// <summary>
        /// Suite のポインタを取得します (ホスト内部・テスト用)
        /// </summary>
        /// <param name="name">Suite 名</param>
        /// <param name="version">Suite のバージョン</param>
        /// <returns>Suite のポインタ。未対応の場合は null</returns>
        public static void* GetSuitePointer(string name, int version)
        {
            return GetSuite(name, version);
        }

        internal static void* AllocSuite(int functionCount)
        {
            return NativeMemory.AllocZeroed((nuint)(sizeof(void*) * functionCount));
        }

        internal static string Str(byte* value)
        {
            return Marshal.PtrToStringUTF8((nint)value) ?? "";
        }
    }

    /// <summary>
    /// OfxPropertySuiteV1 の実装
    /// </summary>
    static unsafe class PropertySuite
    {
        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(18);
            s[0] = (delegate* unmanaged[Cdecl]<nint, byte*, int, void*, int>)&PropSetPointer;
            s[1] = (delegate* unmanaged[Cdecl]<nint, byte*, int, byte*, int>)&PropSetString;
            s[2] = (delegate* unmanaged[Cdecl]<nint, byte*, int, double, int>)&PropSetDouble;
            s[3] = (delegate* unmanaged[Cdecl]<nint, byte*, int, int, int>)&PropSetInt;
            s[4] = (delegate* unmanaged[Cdecl]<nint, byte*, int, void**, int>)&PropSetPointerN;
            s[5] = (delegate* unmanaged[Cdecl]<nint, byte*, int, byte**, int>)&PropSetStringN;
            s[6] = (delegate* unmanaged[Cdecl]<nint, byte*, int, double*, int>)&PropSetDoubleN;
            s[7] = (delegate* unmanaged[Cdecl]<nint, byte*, int, int*, int>)&PropSetIntN;
            s[8] = (delegate* unmanaged[Cdecl]<nint, byte*, int, void**, int>)&PropGetPointer;
            s[9] = (delegate* unmanaged[Cdecl]<nint, byte*, int, byte**, int>)&PropGetString;
            s[10] = (delegate* unmanaged[Cdecl]<nint, byte*, int, double*, int>)&PropGetDouble;
            s[11] = (delegate* unmanaged[Cdecl]<nint, byte*, int, int*, int>)&PropGetInt;
            s[12] = (delegate* unmanaged[Cdecl]<nint, byte*, int, void**, int>)&PropGetPointerN;
            s[13] = (delegate* unmanaged[Cdecl]<nint, byte*, int, byte**, int>)&PropGetStringN;
            s[14] = (delegate* unmanaged[Cdecl]<nint, byte*, int, double*, int>)&PropGetDoubleN;
            s[15] = (delegate* unmanaged[Cdecl]<nint, byte*, int, int*, int>)&PropGetIntN;
            s[16] = (delegate* unmanaged[Cdecl]<nint, byte*, int>)&PropReset;
            s[17] = (delegate* unmanaged[Cdecl]<nint, byte*, int*, int>)&PropGetDimension;
            return s;
        }

        static PropertySet? Resolve(nint handle)
        {
            var set = HandleTable.Get<PropertySet>(handle);
            if (set == null)
            {
                OfxLog.Warn($"不明なプロパティセットハンドル: 0x{handle:X}");
            }
            return set;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetPointer(nint handle, byte* name, int index, void* value)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            set.Set(SuiteRegistry.Str(name), index, (nint)value);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetString(nint handle, byte* name, int index, byte* value)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            set.Set(SuiteRegistry.Str(name), index, SuiteRegistry.Str(value));
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetDouble(nint handle, byte* name, int index, double value)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            set.Set(SuiteRegistry.Str(name), index, value);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetInt(nint handle, byte* name, int index, int value)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            set.Set(SuiteRegistry.Str(name), index, value);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetPointerN(nint handle, byte* name, int count, void** values)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var key = SuiteRegistry.Str(name);
            for (var i = 0; i < count; i++)
            {
                set.Set(key, i, (nint)values[i]);
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetStringN(nint handle, byte* name, int count, byte** values)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var key = SuiteRegistry.Str(name);
            for (var i = 0; i < count; i++)
            {
                set.Set(key, i, SuiteRegistry.Str(values[i]));
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetDoubleN(nint handle, byte* name, int count, double* values)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var key = SuiteRegistry.Str(name);
            for (var i = 0; i < count; i++)
            {
                set.Set(key, i, values[i]);
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropSetIntN(nint handle, byte* name, int count, int* values)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var key = SuiteRegistry.Str(name);
            for (var i = 0; i < count; i++)
            {
                set.Set(key, i, values[i]);
            }
            return (int)OfxStatus.OK;
        }

        static int GetCore(nint handle, byte* name, int index, out object? value)
        {
            value = null;
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            if (!set.TryGet(SuiteRegistry.Str(name), index, out value))
            {
                return (int)OfxStatus.ErrUnknown;
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetPointer(nint handle, byte* name, int index, void** value)
        {
            var status = GetCore(handle, name, index, out var obj);
            if (status == (int)OfxStatus.OK)
            {
                *value = (void*)(obj is nint p ? p : 0);
            }
            return status;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetString(nint handle, byte* name, int index, byte** value)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var key = SuiteRegistry.Str(name);
            if (!set.TryGet(key, index, out var obj))
            {
                return (int)OfxStatus.ErrUnknown;
            }
            *value = (byte*)set.GetNativeString(key, index, obj as string ?? "");
            return (int)OfxStatus.OK;
        }

        static double ToDouble(object? obj)
        {
            return obj switch
            {
                double d => d,
                int i => i,
                _ => 0.0
            };
        }

        static int ToInt(object? obj)
        {
            return obj switch
            {
                int i => i,
                double d => (int)d,
                _ => 0
            };
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetDouble(nint handle, byte* name, int index, double* value)
        {
            var status = GetCore(handle, name, index, out var obj);
            if (status == (int)OfxStatus.OK)
            {
                *value = ToDouble(obj);
            }
            return status;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetInt(nint handle, byte* name, int index, int* value)
        {
            var status = GetCore(handle, name, index, out var obj);
            if (status == (int)OfxStatus.OK)
            {
                *value = ToInt(obj);
            }
            return status;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetPointerN(nint handle, byte* name, int count, void** values)
        {
            for (var i = 0; i < count; i++)
            {
                var status = GetCore(handle, name, i, out var obj);
                if (status != (int)OfxStatus.OK)
                {
                    return status;
                }
                values[i] = (void*)(obj is nint p ? p : 0);
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetStringN(nint handle, byte* name, int count, byte** values)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var key = SuiteRegistry.Str(name);
            for (var i = 0; i < count; i++)
            {
                if (!set.TryGet(key, i, out var obj))
                {
                    return (int)OfxStatus.ErrUnknown;
                }
                values[i] = (byte*)set.GetNativeString(key, i, obj as string ?? "");
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetDoubleN(nint handle, byte* name, int count, double* values)
        {
            for (var i = 0; i < count; i++)
            {
                var status = GetCore(handle, name, i, out var obj);
                if (status != (int)OfxStatus.OK)
                {
                    return status;
                }
                values[i] = ToDouble(obj);
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetIntN(nint handle, byte* name, int count, int* values)
        {
            for (var i = 0; i < count; i++)
            {
                var status = GetCore(handle, name, i, out var obj);
                if (status != (int)OfxStatus.OK)
                {
                    return status;
                }
                values[i] = ToInt(obj);
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropReset(nint handle, byte* name)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            set.Reset(SuiteRegistry.Str(name));
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int PropGetDimension(nint handle, byte* name, int* count)
        {
            var set = Resolve(handle);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *count = set.GetDimension(SuiteRegistry.Str(name));
            return (int)OfxStatus.OK;
        }
    }

    /// <summary>
    /// OfxImageEffectSuiteV1 の実装 (P0 では Describe に必要な範囲のみ)
    /// </summary>
    static unsafe class ImageEffectSuite
    {
        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(13);
            s[0] = (delegate* unmanaged[Cdecl]<nint, nint*, int>)&GetPropertySet;
            s[1] = (delegate* unmanaged[Cdecl]<nint, nint*, int>)&GetParamSet;
            s[2] = (delegate* unmanaged[Cdecl]<nint, byte*, nint*, int>)&ClipDefine;
            s[3] = (delegate* unmanaged[Cdecl]<nint, byte*, nint*, nint*, int>)&ClipGetHandle;
            s[4] = (delegate* unmanaged[Cdecl]<nint, nint*, int>)&ClipGetPropertySet;
            s[5] = (delegate* unmanaged[Cdecl]<nint, double, OfxRectD*, nint*, int>)&ClipGetImage;
            s[6] = (delegate* unmanaged[Cdecl]<nint, int>)&ClipReleaseImage;
            s[7] = (delegate* unmanaged[Cdecl]<nint, double, OfxRectD*, int>)&ClipGetRegionOfDefinition;
            s[8] = (delegate* unmanaged[Cdecl]<nint, int>)&Abort;
            s[9] = (delegate* unmanaged[Cdecl]<nint, nuint, nint*, int>)&ImageMemoryAlloc;
            s[10] = (delegate* unmanaged[Cdecl]<nint, int>)&ImageMemoryFree;
            s[11] = (delegate* unmanaged[Cdecl]<nint, void**, int>)&ImageMemoryLock;
            s[12] = (delegate* unmanaged[Cdecl]<nint, int>)&ImageMemoryUnlock;
            return s;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int GetPropertySet(nint effect, nint* propHandle)
        {
            var properties = HandleTable.Get<object>(effect) switch
            {
                EffectDescriptor descriptor => descriptor.Properties,
                EffectInstance instance => instance.Properties,
                _ => null
            };
            if (properties == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *propHandle = properties.Handle;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int GetParamSet(nint effect, nint* paramSetHandle)
        {
            var handle = HandleTable.Get<object>(effect) switch
            {
                EffectDescriptor descriptor => descriptor.Params.Handle,
                EffectInstance instance => instance.Params.Handle,
                _ => (nint)0
            };
            if (handle == 0)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *paramSetHandle = handle;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipDefine(nint effect, byte* name, nint* propertySet)
        {
            var descriptor = HandleTable.Get<EffectDescriptor>(effect);
            if (descriptor == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var clip = descriptor.DefineClip(SuiteRegistry.Str(name));
            if (propertySet != null)
            {
                *propertySet = clip.Properties.Handle;
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipGetHandle(nint effect, byte* name, nint* clip, nint* propertySet)
        {
            var clipName = SuiteRegistry.Str(name);
            (nint ClipHandle, nint PropsHandle) result = HandleTable.Get<object>(effect) switch
            {
                EffectDescriptor descriptor when descriptor.Clips.TryGetValue(clipName, out var clipDescriptor)
                    => (clipDescriptor.Handle, clipDescriptor.Properties.Handle),
                EffectInstance instance when instance.Clips.TryGetValue(clipName, out var clipInstance)
                    => (clipInstance.Handle, clipInstance.Properties.Handle),
                _ => (0, 0)
            };
            if (result.ClipHandle == 0)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *clip = result.ClipHandle;
            if (propertySet != null)
            {
                *propertySet = result.PropsHandle;
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipGetPropertySet(nint clip, nint* propertySet)
        {
            var properties = HandleTable.Get<object>(clip) switch
            {
                ClipDescriptor descriptor => descriptor.Properties,
                ClipInstance instance => instance.Properties,
                _ => null
            };
            if (properties == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *propertySet = properties.Handle;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipGetImage(nint clip, double time, OfxRectD* region, nint* image)
        {
            var clipInstance = HandleTable.Get<ClipInstance>(clip);
            var owner = clipInstance?.Owner;
            if (clipInstance == null || owner == null)
            {
                OfxLog.Warn("clipGetImage: クリップインスタンス以外のハンドルが渡されました");
                return (int)OfxStatus.ErrBadHandle;
            }

            // Output クリップはホストが用意したレンダリング先を返す
            if (owner.OutputImage != null && clipInstance.Name == "Output")
            {
                *image = owner.OutputImage.Handle;
                return (int)OfxStatus.OK;
            }

            var frame = owner.FrameProvider?.GetSourceFrame(clipInstance.Name, time);
            if (frame == null)
            {
                OfxLog.Warn($"clipGetImage: {clipInstance.Name} @{time} の画像を取得できませんでした");
                return (int)OfxStatus.Failed;
            }

            // region はヒントであり、ホストはより広い範囲 (全体) を返してよい
            var ofxImage = OfxImage.FromBgraTopDown(frame.Value.Pixels, frame.Value.Width, frame.Value.Height, false, $"{clipInstance.Name}@{time}");
            ofxImage.Properties.SetAll(OfxNames.ImageEffectPropRenderScale, owner.CurrentRenderScale.X, owner.CurrentRenderScale.Y);
            lock (owner.FetchedImages)
            {
                owner.FetchedImages.Add(ofxImage);
            }
            *image = ofxImage.Handle;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipReleaseImage(nint image)
        {
            var ofxImage = OfxImage.Resolve(image);
            if (ofxImage == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            if (!ofxImage.HostOwned)
            {
                ofxImage.Dispose();
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipGetRegionOfDefinition(nint clip, double time, OfxRectD* bounds)
        {
            var clipInstance = HandleTable.Get<ClipInstance>(clip);
            var owner = clipInstance?.Owner;
            if (clipInstance == null || owner == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }

            // RoD はキャノニカル座標 (レンダリングスケール適用前) で返す
            var (scaleX, scaleY) = owner.CurrentRenderScale;
            if (clipInstance.Name == "Output")
            {
                var output = owner.OutputImage;
                bounds->X1 = 0.0;
                bounds->Y1 = 0.0;
                bounds->X2 = (output?.Width ?? owner.Settings.Width) / scaleX;
                bounds->Y2 = (output?.Height ?? owner.Settings.Height) / scaleY;
                return (int)OfxStatus.OK;
            }

            var size = owner.FrameProvider?.GetSourceBounds(clipInstance.Name, time);
            if (size == null)
            {
                return (int)OfxStatus.Failed;
            }
            bounds->X1 = 0.0;
            bounds->Y1 = 0.0;
            bounds->X2 = size.Value.Width / scaleX;
            bounds->Y2 = size.Value.Height / scaleY;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int Abort(nint effect)
        {
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ImageMemoryAlloc(nint effect, nuint size, nint* memoryHandle)
        {
            *memoryHandle = (nint)NativeMemory.Alloc(size);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ImageMemoryFree(nint memoryHandle)
        {
            NativeMemory.Free((void*)memoryHandle);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ImageMemoryLock(nint memoryHandle, void** data)
        {
            *data = (void*)memoryHandle;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ImageMemoryUnlock(nint memoryHandle)
        {
            return (int)OfxStatus.OK;
        }
    }

    /// <summary>
    /// OfxParameterSuiteV1 の実装 (P0 では定義系のみ、値の取得・設定はスタブ)
    /// </summary>
    static unsafe class ParameterSuite
    {
        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(18);
            s[0] = (delegate* unmanaged[Cdecl]<nint, byte*, byte*, nint*, int>)&ParamDefine;
            s[1] = (delegate* unmanaged[Cdecl]<nint, byte*, nint*, nint*, int>)&ParamGetHandle;
            s[2] = (delegate* unmanaged[Cdecl]<nint, nint*, int>)&ParamSetGetPropertySet;
            s[3] = (delegate* unmanaged[Cdecl]<nint, nint*, int>)&ParamGetPropertySet;
            s[4] = (delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, int>)&ParamGetValue;
            s[5] = (delegate* unmanaged[Cdecl]<nint, double, nint, nint, nint, nint, int>)&ParamGetValueAtTime;
            s[6] = (delegate* unmanaged[Cdecl]<nint, double, nint, nint, nint, nint, int>)&ParamGetDerivative;
            s[7] = (delegate* unmanaged[Cdecl]<nint, double, double, nint, nint, nint, nint, int>)&ParamGetIntegral;
            s[8] = (delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, int>)&ParamSetValue;
            s[9] = (delegate* unmanaged[Cdecl]<nint, double, nint, nint, nint, nint, int>)&ParamSetValueAtTime;
            s[10] = (delegate* unmanaged[Cdecl]<nint, uint*, int>)&ParamGetNumKeys;
            s[11] = (delegate* unmanaged[Cdecl]<nint, uint, double*, int>)&ParamGetKeyTime;
            s[12] = (delegate* unmanaged[Cdecl]<nint, double, int, int*, int>)&ParamGetKeyIndex;
            s[13] = (delegate* unmanaged[Cdecl]<nint, double, int>)&ParamDeleteKey;
            s[14] = (delegate* unmanaged[Cdecl]<nint, int>)&ParamDeleteAllKeys;
            s[15] = (delegate* unmanaged[Cdecl]<nint, nint, double, OfxRangeD*, int>)&ParamCopy;
            s[16] = (delegate* unmanaged[Cdecl]<nint, byte*, int>)&ParamEditBegin;
            s[17] = (delegate* unmanaged[Cdecl]<nint, int>)&ParamEditEnd;
            return s;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamDefine(nint paramSet, byte* paramType, byte* name, nint* propertySet)
        {
            var set = HandleTable.Get<ParamSetDescriptor>(paramSet);
            if (set == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var param = set.Define(SuiteRegistry.Str(paramType), SuiteRegistry.Str(name));
            if (propertySet != null)
            {
                *propertySet = param.Properties.Handle;
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetHandle(nint paramSet, byte* name, nint* param, nint* propertySet)
        {
            var paramName = SuiteRegistry.Str(name);
            (nint ParamHandle, nint PropsHandle) result = HandleTable.Get<object>(paramSet) switch
            {
                ParamSetDescriptor descriptorSet when descriptorSet.Find(paramName) is ParamDescriptor descriptor
                    => (descriptor.Handle, descriptor.Properties.Handle),
                ParamSetInstance instanceSet when instanceSet.Find(paramName) is ParamInstance instance
                    => (instance.Handle, instance.Properties.Handle),
                _ => (0, 0)
            };
            if (result.ParamHandle == 0)
            {
                return (int)OfxStatus.ErrUnknown;
            }
            *param = result.ParamHandle;
            if (propertySet != null)
            {
                *propertySet = result.PropsHandle;
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamSetGetPropertySet(nint paramSet, nint* propertySet)
        {
            var properties = HandleTable.Get<object>(paramSet) switch
            {
                ParamSetDescriptor descriptorSet => descriptorSet.Properties,
                ParamSetInstance instanceSet => instanceSet.Properties,
                _ => null
            };
            if (properties == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *propertySet = properties.Handle;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetPropertySet(nint param, nint* propertySet)
        {
            var properties = HandleTable.Get<object>(param) switch
            {
                ParamDescriptor descriptor => descriptor.Properties,
                ParamInstance instance => instance.Properties,
                _ => null
            };
            if (properties == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *propertySet = properties.Handle;
            return (int)OfxStatus.OK;
        }

        // 値の取得・設定は C の可変長引数関数だが、Win x64 ABI では可変長引数の浮動小数点値が
        // 汎用レジスタにも複製されるため、最大次元数 (4) 分の nint 引数を持つ固定シグネチャで受けて
        // パラメータ型に応じて再解釈する
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetValue(nint param, nint v1, nint v2, nint v3, nint v4)
        {
            var instance = HandleTable.Get<ParamInstance>(param);
            if (instance == null)
            {
                OfxLog.Warn("paramGetValue: パラメータインスタンス以外のハンドルが渡されました");
                return (int)OfxStatus.ErrBadHandle;
            }
            return (int)instance.WriteValuesTo([v1, v2, v3, v4]);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetValueAtTime(nint param, double time, nint v1, nint v2, nint v3, nint v4)
        {
            // キーフレームは NiVE3 側が管理するため、インスタンスは常に現在時刻の値を返す
            var instance = HandleTable.Get<ParamInstance>(param);
            if (instance == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            return (int)instance.WriteValuesTo([v1, v2, v3, v4]);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetDerivative(nint param, double time, nint v1, nint v2, nint v3, nint v4)
        {
            // アニメーションはホスト側管理のため微分は常に 0
            var instance = HandleTable.Get<ParamInstance>(param);
            if (instance == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var slots = (Span<nint>)[v1, v2, v3, v4];
            unsafe
            {
                for (var i = 0; i < instance.Dimension; i++)
                {
                    if (slots[i] == 0)
                    {
                        return (int)OfxStatus.ErrValue;
                    }
                    switch (instance.ValueKind)
                    {
                        case OfxParamValueKind.Int:
                            *(int*)slots[i] = 0;
                            break;
                        case OfxParamValueKind.Double:
                            *(double*)slots[i] = 0.0;
                            break;
                        default:
                            return (int)OfxStatus.ErrUnsupported;
                    }
                }
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetIntegral(nint param, double time1, double time2, nint v1, nint v2, nint v3, nint v4)
        {
            // 定数値の積分 = 値 × 期間
            var instance = HandleTable.Get<ParamInstance>(param);
            if (instance == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var slots = (Span<nint>)[v1, v2, v3, v4];
            unsafe
            {
                for (var i = 0; i < instance.Dimension; i++)
                {
                    if (slots[i] == 0)
                    {
                        return (int)OfxStatus.ErrValue;
                    }
                    if (instance.ValueKind != OfxParamValueKind.Double)
                    {
                        return (int)OfxStatus.ErrUnsupported;
                    }
                    *(double*)slots[i] = Convert.ToDouble(instance.Values[i]) * (time2 - time1);
                }
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamSetValue(nint param, nint v1, nint v2, nint v3, nint v4)
        {
            var instance = HandleTable.Get<ParamInstance>(param);
            if (instance == null)
            {
                OfxLog.Warn("paramSetValue: パラメータインスタンス以外のハンドルが渡されました");
                return (int)OfxStatus.ErrBadHandle;
            }
            var status = instance.ReadValuesFrom([v1, v2, v3, v4]);
            OfxLog.Info($"paramSetValue: {instance.Name} = [{string.Join(", ", instance.Values)}]");
            return (int)status;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamSetValueAtTime(nint param, double time, nint v1, nint v2, nint v3, nint v4)
        {
            var instance = HandleTable.Get<ParamInstance>(param);
            if (instance == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            var status = instance.ReadValuesFrom([v1, v2, v3, v4]);
            OfxLog.Info($"paramSetValueAtTime: {instance.Name} @{time} = [{string.Join(", ", instance.Values)}]");
            return (int)status;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetNumKeys(nint param, uint* numKeys)
        {
            *numKeys = 0;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetKeyTime(nint param, uint nth, double* time)
        {
            return (int)OfxStatus.ErrBadIndex;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamGetKeyIndex(nint param, double time, int direction, int* index)
        {
            return (int)OfxStatus.Failed;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamDeleteKey(nint param, double time)
        {
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamDeleteAllKeys(nint param)
        {
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamCopy(nint paramTo, nint paramFrom, double dstOffset, OfxRangeD* frameRange)
        {
            return (int)OfxStatus.ErrMissingHostFeature;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamEditBegin(nint paramSet, byte* name)
        {
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ParamEditEnd(nint paramSet)
        {
            return (int)OfxStatus.OK;
        }
    }

    /// <summary>
    /// OfxMemorySuiteV1 の実装
    /// </summary>
    static unsafe class MemorySuite
    {
        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(2);
            s[0] = (delegate* unmanaged[Cdecl]<nint, nuint, void**, int>)&MemoryAlloc;
            s[1] = (delegate* unmanaged[Cdecl]<void*, int>)&MemoryFree;
            return s;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MemoryAlloc(nint handle, nuint size, void** data)
        {
            *data = NativeMemory.Alloc(size);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MemoryFree(void* data)
        {
            NativeMemory.Free(data);
            return (int)OfxStatus.OK;
        }
    }

    /// <summary>
    /// OfxMultiThreadSuiteV1 の実装
    /// </summary>
    static unsafe class MultiThreadSuite
    {
        [ThreadStatic]
        static uint CurrentThreadIndex;

        [ThreadStatic]
        static bool IsSpawned;

        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(9);
            s[0] = (delegate* unmanaged[Cdecl]<delegate* unmanaged[Cdecl]<uint, uint, void*, void>, uint, void*, int>)&MultiThread;
            s[1] = (delegate* unmanaged[Cdecl]<uint*, int>)&MultiThreadNumCPUs;
            s[2] = (delegate* unmanaged[Cdecl]<uint*, int>)&MultiThreadIndex;
            s[3] = (delegate* unmanaged[Cdecl]<int>)&MultiThreadIsSpawnedThread;
            s[4] = (delegate* unmanaged[Cdecl]<nint*, int, int>)&MutexCreate;
            s[5] = (delegate* unmanaged[Cdecl]<nint, int>)&MutexDestroy;
            s[6] = (delegate* unmanaged[Cdecl]<nint, int>)&MutexLock;
            s[7] = (delegate* unmanaged[Cdecl]<nint, int>)&MutexUnLock;
            s[8] = (delegate* unmanaged[Cdecl]<nint, int>)&MutexTryLock;
            return s;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MultiThread(delegate* unmanaged[Cdecl]<uint, uint, void*, void> func, uint threadCount, void* customArg)
        {
            if (func == null)
            {
                return (int)OfxStatus.Failed;
            }

            var count = (int)Math.Max(threadCount, 1);
            var arg = (nint)customArg;
            var funcPtr = (nint)func;
            try
            {
                Parallel.For(0, count, i =>
                {
                    CurrentThreadIndex = (uint)i;
                    IsSpawned = true;
                    try
                    {
                        ((delegate* unmanaged[Cdecl]<uint, uint, void*, void>)funcPtr)((uint)i, (uint)count, (void*)arg);
                    }
                    finally
                    {
                        IsSpawned = false;
                    }
                });
            }
            catch (Exception ex)
            {
                OfxLog.Warn($"multiThread の実行中にエラーが発生しました: {ex.Message}");
                return (int)OfxStatus.Failed;
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MultiThreadNumCPUs(uint* count)
        {
            *count = (uint)Environment.ProcessorCount;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MultiThreadIndex(uint* index)
        {
            *index = CurrentThreadIndex;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MultiThreadIsSpawnedThread()
        {
            return IsSpawned ? 1 : 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MutexCreate(nint* mutex, int lockCount)
        {
            var semaphore = new SemaphoreSlim(1, 1);
            for (var i = 0; i < lockCount; i++)
            {
                semaphore.Wait(0);
            }
            *mutex = HandleTable.Alloc(semaphore);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MutexDestroy(nint mutex)
        {
            HandleTable.Get<SemaphoreSlim>(mutex)?.Dispose();
            HandleTable.Free(mutex);
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MutexLock(nint mutex)
        {
            var semaphore = HandleTable.Get<SemaphoreSlim>(mutex);
            if (semaphore == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            semaphore.Wait();
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MutexUnLock(nint mutex)
        {
            var semaphore = HandleTable.Get<SemaphoreSlim>(mutex);
            if (semaphore == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            semaphore.Release();
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int MutexTryLock(nint mutex)
        {
            var semaphore = HandleTable.Get<SemaphoreSlim>(mutex);
            if (semaphore == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            return semaphore.Wait(0) ? (int)OfxStatus.OK : (int)OfxStatus.Failed;
        }
    }

    /// <summary>
    /// OfxMessageSuiteV1 の実装 (P0 ではログ出力のみ)
    /// </summary>
    static unsafe class MessageSuite
    {
        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(1);
            s[0] = (delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, nint, nint, nint, nint, int>)&Message;
            return s;
        }

        public static void* BuildV2()
        {
            var s = (void**)SuiteRegistry.AllocSuite(3);
            s[0] = (delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, nint, nint, nint, nint, int>)&Message;
            s[1] = (delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, nint, nint, nint, nint, int>)&SetPersistentMessage;
            s[2] = (delegate* unmanaged[Cdecl]<nint, int>)&ClearPersistentMessage;
            return s;
        }

        // format 以降は printf 形式の可変長引数 (レジスタ渡し分の最大 4 個まで展開する)
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int Message(nint handle, byte* messageType, byte* messageId, byte* format, nint a1, nint a2, nint a3, nint a4)
        {
            var type = SuiteRegistry.Str(messageType);
            var text = FormatMessage(SuiteRegistry.Str(format), [a1, a2, a3, a4]);
            OfxLog.Info($"message [{type}] {text}");

            var handler = OfxHostCallbacks.MessageHandler;
            if (handler != null)
            {
                try
                {
                    return (int)handler(type, text);
                }
                catch (Exception ex)
                {
                    OfxLog.Warn($"メッセージハンドラでエラーが発生しました: {ex.Message}");
                }
            }
            return type.Contains("Question") ? (int)OfxStatus.ReplyYes : (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int SetPersistentMessage(nint handle, byte* messageType, byte* messageId, byte* format, nint a1, nint a2, nint a3, nint a4)
        {
            OfxLog.Info($"setPersistentMessage [{SuiteRegistry.Str(messageType)}] {FormatMessage(SuiteRegistry.Str(format), [a1, a2, a3, a4])}");
            return (int)OfxStatus.OK;
        }

        /// <summary>
        /// printf 形式の書式文字列を展開します
        /// Win x64 の可変長引数はレジスタ渡し分 (4 個) までしか受け取れないため、それを超える指定子はそのまま出力します
        /// </summary>
        static string FormatMessage(string format, ReadOnlySpan<nint> args)
        {
            var result = new StringBuilder();
            var argIndex = 0;
            for (var i = 0; i < format.Length; i++)
            {
                var c = format[i];
                if (c != '%')
                {
                    result.Append(c);
                    continue;
                }
                if (i + 1 >= format.Length)
                {
                    result.Append(c);
                    break;
                }
                i++;
                if (format[i] == '%')
                {
                    result.Append('%');
                    continue;
                }

                // %[flags][width][.precision][length]spec
                var specStart = i;
                while (i < format.Length && "+-# 0".Contains(format[i]))
                {
                    i++;
                }
                var widthStart = i;
                while (i < format.Length && char.IsAsciiDigit(format[i]))
                {
                    i++;
                }
                var width = i > widthStart && int.TryParse(format[widthStart..i], out var w) ? w : 0;
                var precision = -1;
                if (i < format.Length && format[i] == '.')
                {
                    i++;
                    var precisionStart = i;
                    while (i < format.Length && char.IsAsciiDigit(format[i]))
                    {
                        i++;
                    }
                    precision = i > precisionStart && int.TryParse(format[precisionStart..i], out var p) ? p : 0;
                }
                while (i < format.Length && "hlLqjzt".Contains(format[i]))
                {
                    i++;
                }
                if (i >= format.Length)
                {
                    result.Append('%').Append(format[specStart..]);
                    break;
                }

                var spec = format[i];
                if (argIndex >= args.Length)
                {
                    result.Append('%').Append(format[specStart..(i + 1)]);
                    continue;
                }

                var arg = args[argIndex++];
                var text = spec switch
                {
                    // 引数の数が合わないプラグイン対策として、明らかに不正なポインタは参照しない
                    's' => arg == 0 || (nuint)arg < 0x10000 ? "(null)" : Marshal.PtrToStringUTF8(arg) ?? "",
                    'd' or 'i' => ((int)arg).ToString(),
                    'u' => ((uint)(int)arg).ToString(),
                    'x' => ((int)arg).ToString("x"),
                    'X' => ((int)arg).ToString("X"),
                    'c' => ((char)(int)arg).ToString(),
                    'p' => $"0x{arg:X}",
                    // 可変長引数の浮動小数点値は汎用レジスタにも複製されるため、ビットを再解釈する
                    'f' or 'F' => BitConverter.Int64BitsToDouble(arg).ToString("F" + (precision >= 0 ? precision : 6)),
                    'e' or 'E' or 'g' or 'G' => BitConverter.Int64BitsToDouble(arg).ToString(),
                    _ => $"%{spec}"
                };
                result.Append(width > 0 ? text.PadLeft(width) : text);
            }
            return result.ToString();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClearPersistentMessage(nint handle)
        {
            OfxLog.Info("clearPersistentMessage");
            return (int)OfxStatus.OK;
        }
    }

    /// <summary>
    /// OfxImageEffectOpenGLRenderSuiteV1 の実装
    /// </summary>
    static unsafe class OpenGLRenderSuite
    {
        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(3);
            s[0] = (delegate* unmanaged[Cdecl]<nint, double, byte*, OfxRectD*, nint*, int>)&ClipLoadTexture;
            s[1] = (delegate* unmanaged[Cdecl]<nint, int>)&ClipFreeTexture;
            s[2] = (delegate* unmanaged[Cdecl]<int>)&FlushResources;
            return s;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipLoadTexture(nint clip, double time, byte* format, OfxRectD* region, nint* textureHandle)
        {
            var gl = GlContextManager.Shared;
            var clipInstance = HandleTable.Get<ClipInstance>(clip);
            var owner = clipInstance?.Owner;
            if (gl == null || clipInstance == null || owner == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }

            if (clipInstance.Name == "Output")
            {
                // openfx-misc 系プラグインは出力テクスチャを取得し、自前の FBO で描画する
                if (owner.OutputGlTexture == null)
                {
                    OfxLog.Warn("clipLoadTexture: GL レンダリング外で Output クリップが要求されました");
                    return (int)OfxStatus.Failed;
                }
                *textureHandle = owner.OutputGlTexture.Handle;
                return (int)OfxStatus.OK;
            }

            var frame = owner.FrameProvider?.GetSourceFrame(clipInstance.Name, time);
            if (frame == null)
            {
                OfxLog.Warn($"clipLoadTexture: {clipInstance.Name} @{time} の画像を取得できませんでした");
                return (int)OfxStatus.Failed;
            }

            var texture = gl.Invoke(() => OfxGlTexture.CreateFromBgraTopDown(frame.Value.Pixels, frame.Value.Width, frame.Value.Height, $"{clipInstance.Name}@{time}"));
            texture.Properties.SetAll(OfxNames.ImageEffectPropRenderScale, owner.CurrentRenderScale.X, owner.CurrentRenderScale.Y);
            lock (owner.LoadedGlTextures)
            {
                owner.LoadedGlTextures.Add(texture);
            }
            *textureHandle = texture.Handle;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ClipFreeTexture(nint textureHandle)
        {
            var gl = GlContextManager.Shared;
            var texture = OfxGlTexture.Resolve(textureHandle);
            if (gl == null || texture == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            if (!texture.HostOwned)
            {
                gl.Invoke(texture.Dispose);
            }
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int FlushResources()
        {
            var gl = GlContextManager.Shared;
            if (gl == null)
            {
                return (int)OfxStatus.ErrMissingHostFeature;
            }
            gl.Invoke(GlNative.glFinish);
            return (int)OfxStatus.OK;
        }
    }

    /// <summary>
    /// OfxProgressSuiteV1/V2 の実装 (現状はログ出力のみ。アプリ統合時に進捗 UI へ接続する)
    /// </summary>
    static unsafe class ProgressSuite
    {
        public static void* BuildV1()
        {
            var s = (void**)SuiteRegistry.AllocSuite(3);
            s[0] = (delegate* unmanaged[Cdecl]<nint, byte*, int>)&ProgressStartV1;
            s[1] = (delegate* unmanaged[Cdecl]<nint, double, int>)&ProgressUpdate;
            s[2] = (delegate* unmanaged[Cdecl]<nint, int>)&ProgressEnd;
            return s;
        }

        public static void* BuildV2()
        {
            var s = (void**)SuiteRegistry.AllocSuite(3);
            s[0] = (delegate* unmanaged[Cdecl]<nint, byte*, byte*, int>)&ProgressStartV2;
            s[1] = (delegate* unmanaged[Cdecl]<nint, double, int>)&ProgressUpdate;
            s[2] = (delegate* unmanaged[Cdecl]<nint, int>)&ProgressEnd;
            return s;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ProgressStartV1(nint handle, byte* label)
        {
            OfxLog.Info($"progressStart: {SuiteRegistry.Str(label)}");
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ProgressStartV2(nint handle, byte* message, byte* messageId)
        {
            OfxLog.Info($"progressStart: {SuiteRegistry.Str(message)}");
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ProgressUpdate(nint handle, double progress)
        {
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int ProgressEnd(nint handle)
        {
            return (int)OfxStatus.OK;
        }
    }

    /// <summary>
    /// OfxTimeLineSuiteV1 の実装
    /// </summary>
    static unsafe class TimeLineSuite
    {
        public static void* Build()
        {
            var s = (void**)SuiteRegistry.AllocSuite(3);
            s[0] = (delegate* unmanaged[Cdecl]<nint, double*, int>)&GetTime;
            s[1] = (delegate* unmanaged[Cdecl]<nint, double, int>)&GotoTime;
            s[2] = (delegate* unmanaged[Cdecl]<nint, double*, double*, int>)&GetTimeBounds;
            return s;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int GetTime(nint instance, double* time)
        {
            var effectInstance = HandleTable.Get<EffectInstance>(instance);
            if (effectInstance == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *time = effectInstance.CurrentTime;
            return (int)OfxStatus.OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int GotoTime(nint instance, double time)
        {
            // ホスト側のタイムライン移動は許可しない
            return (int)OfxStatus.ErrMissingHostFeature;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int GetTimeBounds(nint instance, double* firstTime, double* lastTime)
        {
            var effectInstance = HandleTable.Get<EffectInstance>(instance);
            if (effectInstance == null)
            {
                return (int)OfxStatus.ErrBadHandle;
            }
            *firstTime = 0.0;
            *lastTime = effectInstance.Settings.DurationFrames;
            return (int)OfxStatus.OK;
        }
    }
}
