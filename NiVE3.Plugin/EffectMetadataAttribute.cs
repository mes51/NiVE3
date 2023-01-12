using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin
{
    /// <summary>
    /// エフェクトの概要
    /// </summary>
    public interface IEffectMetadata
    {
        /// <summary>
        /// エフェクトの表示名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// エフェクトの作成者
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// エフェクトのカテゴリ
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// エフェクトの概要
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// エフェクトの識別のためのGuid
        /// この値は全てのエフェクトの中で一意で或必要があります
        /// </summary>
        public string EffectUuid { get; }

        /// <summary>
        /// 何もしないエフェクトであることを表します
        /// </summary>
        public bool IsDummyEffect { get; }
    }

    /// <summary>
    /// NiVEに対し公開するエフェクトの概要を表します
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EffectMetadataAttribute : Attribute, IEffectMetadata
    {
        public string Name { get; }

        public string Author { get; }

        public string Description { get; }

        public string Category { get; }

        public string EffectUuid { get; }

        public bool IsDummyEffect { get; set; } = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">エフェクトの表示名</param>
        /// <param name="author">エフェクトの制作者</param>
        /// <param name="description">エフェクトの概要</param>
        /// <param name="effectUuid">エフェクトの識別のためのGuid</param>
        public EffectMetadataAttribute(string name, string author, string category, string description, string effectUuid)
        {
            Name = name;
            Author = author;
            Category = category;
            Description = description;
            EffectUuid = effectUuid;
        }
    }
}
