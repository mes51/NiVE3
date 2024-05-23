using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;

namespace NiVE3.Plugin.ValueObject
{
    /// <summary>
    /// フッテージ画像のサイズ、位置を含む四角形を表します
    /// </summary>
    /// <param name="Origin">フッテージ画像の位置</param>
    /// <param name="Width">画像の幅</param>
    /// <param name="Height">画像の高さ</param>
    public readonly record struct SourceFootageRect(Vector2d Origin, int Width, int Height)
    {
        public static readonly SourceFootageRect Empty = new SourceFootageRect(Vector2d.Zero, 0, 0);
    }
}
