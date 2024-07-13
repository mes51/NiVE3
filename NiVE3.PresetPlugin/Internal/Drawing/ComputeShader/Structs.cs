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

        public Float4 Ambient;

        public Float4 E;
    }

    struct GPUShadowPixel
    {
        public Float4 Color;

        public float Depth;

        public int TriangleId;

        public int NextIndex;
    }
}
