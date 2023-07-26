using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Attributes
{
    public interface IRendererMetadata
    {
        /// <summary>
        /// レンダラの表示名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// レンダラの作成者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// レンダラの概要
        /// </summary>
        string Description { get; }

        /// <summary>
        /// レンダラの識別のためのGuid
        /// この値はすべてのレンダラの中で一意である必要があります
        /// </summary>
        string RendererUuid { get; }
    }

    /// <summary>
    /// NiVEに対し公開するレンダラの概要を表します
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RendererMetadataAttribute : Attribute, IRendererMetadata
    {
        public string Name { get; }

        public string Author { get; }

        public string Description { get; }

        public string RendererUuid { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">レンダラの表示名</param>
        /// <param name="author">レンダラの作成者</param>
        /// <param name="description">レンダラの概要</param>
        /// <param name="rendererUuid">レンダラの識別のためのGuid</param>
        public RendererMetadataAttribute(string name, string author, string description, string rendererUuid)
        {
            Name = name;
            Author = author;
            Description = description;
            RendererUuid = rendererUuid;
        }
    }
}
