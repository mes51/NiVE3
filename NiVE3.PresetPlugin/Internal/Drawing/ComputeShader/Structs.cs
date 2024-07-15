using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.PresetPlugin.Internal.Drawing.ComputeShader
{
    struct GPURasterizedPixel
    {
        public Float4 Color;

        public Float4 Specular;

        public Float4 Diffuse;
    }

    struct GPUShadowPixel
    {
        public Float3 Color;

        public float Depth;

        public int TriangleId;

        public int NextIndex;
    }
}
