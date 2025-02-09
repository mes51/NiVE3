using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace NiVE3.InternalShader.Shape
{
    readonly struct GPULineHit : IComparable<GPULineHit>
    {
        public readonly float Value;

        public readonly Bool IsDown;

        public GPULineHit(float value, Bool isDown)
        {
            Value = value;
            IsDown = isDown;
        }

        public int CompareTo(GPULineHit other)
        {
            if (Value > other.Value)
            {
                return 1;
            }
            else if (Value < other.Value)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
