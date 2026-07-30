using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.OpenFX.Interop;

namespace NiVE3.OpenFX.Host
{
    /// <summary>
    /// Describe / DescribeInContext でプラグインが構築するエフェクトの定義情報
    /// </summary>
    public sealed class EffectDescriptor : IDisposable
    {
        /// <summary>
        /// このデスクリプタの OFX ハンドル (OfxImageEffectHandle)
        /// </summary>
        public nint Handle { get; }

        /// <summary>
        /// エフェクトのプロパティセット
        /// </summary>
        public PropertySet Properties { get; }

        /// <summary>
        /// パラメータの定義の集合
        /// </summary>
        public ParamSetDescriptor Params { get; }

        /// <summary>
        /// クリップ名 → クリップ定義
        /// </summary>
        public Dictionary<string, ClipDescriptor> Clips { get; } = new Dictionary<string, ClipDescriptor>();

        public EffectDescriptor(string name)
        {
            Handle = HandleTable.Alloc(this);
            Properties = new PropertySet($"{name}.Effect");
            Properties.SetAll(OfxNames.PropType, OfxNames.TypeImageEffect);
            PropertyDefaults.ApplyEffectDefaults(Properties);
            Params = new ParamSetDescriptor(name);
        }

        /// <summary>
        /// クリップ定義を追加します
        /// </summary>
        /// <param name="clipName">クリップ名</param>
        /// <returns>追加されたクリップ定義</returns>
        public ClipDescriptor DefineClip(string clipName)
        {
            var clip = new ClipDescriptor(clipName, Clips.Count);
            Clips[clipName] = clip;
            return clip;
        }

        public void Dispose()
        {
            Properties.Dispose();
            Params.Dispose();
            foreach (var clip in Clips.Values)
            {
                clip.Dispose();
            }
            HandleTable.Free(Handle);
        }
    }

    /// <summary>
    /// クリップの定義情報
    /// </summary>
    public sealed class ClipDescriptor : IDisposable
    {
        public string Name { get; }

        /// <summary>
        /// プラグインが定義した順序 (メイン入力クリップの決定に使用)
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// このクリップの OFX ハンドル (OfxImageClipHandle)
        /// </summary>
        public nint Handle { get; }

        public PropertySet Properties { get; }

        public ClipDescriptor(string name, int order = 0)
        {
            Name = name;
            Order = order;
            Handle = HandleTable.Alloc(this);
            Properties = new PropertySet($"Clip:{name}");
            Properties.SetAll(OfxNames.PropType, OfxNames.TypeClip);
            Properties.SetAll(OfxNames.PropName, name);
            PropertyDefaults.ApplyClipDefaults(Properties, name);
        }

        public void Dispose()
        {
            Properties.Dispose();
            HandleTable.Free(Handle);
        }
    }

    /// <summary>
    /// パラメータ定義の集合 (OfxParamSetHandle に対応)
    /// </summary>
    public sealed class ParamSetDescriptor : IDisposable
    {
        /// <summary>
        /// この集合の OFX ハンドル (OfxParamSetHandle)
        /// </summary>
        public nint Handle { get; }

        /// <summary>
        /// パラメータセット自体のプロパティセット
        /// </summary>
        public PropertySet Properties { get; }

        /// <summary>
        /// 定義順のパラメータの一覧
        /// </summary>
        public List<ParamDescriptor> Params { get; } = new List<ParamDescriptor>();

        public ParamSetDescriptor(string name)
        {
            Handle = HandleTable.Alloc(this);
            Properties = new PropertySet($"{name}.ParamSet");
        }

        /// <summary>
        /// パラメータ定義を追加します
        /// </summary>
        /// <param name="paramType">OFX のパラメータ型名</param>
        /// <param name="name">パラメータ名</param>
        /// <returns>追加されたパラメータ定義</returns>
        public ParamDescriptor Define(string paramType, string name)
        {
            var param = new ParamDescriptor(paramType, name);
            Params.Add(param);
            return param;
        }

        /// <summary>
        /// 名前からパラメータ定義を取得します
        /// </summary>
        /// <param name="name">パラメータ名</param>
        /// <returns>パラメータ定義。存在しない場合は null</returns>
        public ParamDescriptor? Find(string name)
        {
            return Params.FirstOrDefault(p => p.Name == name);
        }

        public void Dispose()
        {
            Properties.Dispose();
            foreach (var param in Params)
            {
                param.Dispose();
            }
            HandleTable.Free(Handle);
        }
    }

    /// <summary>
    /// 1 つのパラメータの定義情報
    /// </summary>
    public sealed class ParamDescriptor : IDisposable
    {
        public string ParamType { get; }

        public string Name { get; }

        /// <summary>
        /// このパラメータの OFX ハンドル (OfxParamHandle)
        /// </summary>
        public nint Handle { get; }

        public PropertySet Properties { get; }

        public ParamDescriptor(string paramType, string name)
        {
            ParamType = paramType;
            Name = name;
            Handle = HandleTable.Alloc(this);
            Properties = new PropertySet($"Param:{name}");
            Properties.SetAll(OfxNames.PropType, OfxNames.TypeParameter);
            Properties.SetAll(OfxNames.PropName, name);
            Properties.SetAll(OfxNames.ParamPropType, paramType);
            PropertyDefaults.ApplyParamDefaults(Properties, paramType, name);
        }

        public void Dispose()
        {
            Properties.Dispose();
            HandleTable.Free(Handle);
        }
    }
}
