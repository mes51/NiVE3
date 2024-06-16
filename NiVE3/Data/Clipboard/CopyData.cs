using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Data.Clipboard
{
    [Serializable]
    class CopyData<T>
    {
        public CopyDataType Type { get; set; }

        public T[] Data { get; set; }

        public CopyData(CopyDataType type, T[] data)
        {
            Type = type;
            Data = data;
        }
    }

    enum CopyDataType
    {
        None,
        Layer,
        Effect,
        Property,
        PropertyGroup,
        AppendablePropertyChildren,
        KeyFrame
    }
}
