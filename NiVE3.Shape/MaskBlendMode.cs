using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Shape
{
    public enum MaskBlendMode
    {
        Add,
        Subtract,
        Multiply,
        Darken,
        Lighten,
        Difference
    }

    public static class MaskBlendModeExtensions
    {
        /// <summary>
        /// 初期状態に1.0Fである必要があるもの
        /// </summary>
        /// <param name="blendMode"></param>
        /// <returns></returns>
        public static bool IsInverted(this MaskBlendMode blendMode)
        {
            return blendMode == MaskBlendMode.Subtract || blendMode == MaskBlendMode.Multiply;
        }
    }
}
